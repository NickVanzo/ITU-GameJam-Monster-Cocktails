using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

// Lives in the shell scene. Loads the bar, then keeps riding between the bar and the floor below it.
public class LevelLoader : MonoBehaviour
{
    [Tooltip("Scene the game starts in and comes back to after every floor. Needs an ILevel and has to be in the build settings")]
    public string sBar = "Bar";
    [Tooltip("Level scene names, each scene needs an ILevel and has to be in the build settings")]
    public string[] aFloors = { "Arena" };
    [Tooltip("Index in aFloors of the floor the bar's elevator goes to.")]
    public int nFloor = 0;

    [Header("UI")]
    [Tooltip("Covers the screen while levels swap.")]
    public CanvasGroup Fader;
    public TMP_Text FaderText;

    // False only for the bar's very first load, true whenever a level is reached by riding the elevator.
    public bool ArrivedByElevator { get; private set; }

    AudioSource m_RideLoop;
    Transform m_RideSound;
    Transform m_RideSoundSource;
    bool bLevelMissing = false;

    void Awake()
    {
        MaterialInventory.Clear();
        BarGameState.Reset();
        Loadout.Clear();

        // Black until the bar fades itself in.
        Fader.alpha = 1.0f;
        FaderText.text = "";

        m_RideSound = new GameObject("RideSound").transform;
        m_RideSound.SetParent(transform, false);
    }

    void OnDisable()
    {
        Sfx.StopLoop(m_RideLoop);
        m_RideLoop = null;
    }

    // Only one level is loaded at a time: the bar unloads on the way down and loads fresh on the way back up.
    IEnumerator Start()
    {
        if(aFloors.Length == 0)
        {
            Debug.LogError("LevelLoader: aFloors is empty, add at least one level scene name.", this);
            yield break;
        }

        while(true)
        {
            yield return Visit(sBar);
            if(bLevelMissing)
            {
                yield break;
            }

            ArrivedByElevator = true;
            yield return Visit(aFloors[Mathf.Clamp(nFloor, 0, aFloors.Length - 1)]);
            if(bLevelMissing)
            {
                yield break;
            }
        }
    }

    IEnumerator Visit(string sScene)
    {
        yield return UnitySceneManager.LoadSceneAsync(sScene, LoadSceneMode.Additive);

        Scene scene = UnitySceneManager.GetSceneByName(sScene);
        ILevel level = FindLevel(scene);
        if(level != null)
        {
            // Customers and other runtime spawns land in the active scene, so they unload with the level.
            UnitySceneManager.SetActiveScene(scene);
            yield return level.Play(this);
        }
        else
        {
            Debug.LogError($"LevelLoader: no ILevel in scene '{sScene}'. Is the scene in the build settings?", this);
            StopRideSound();
            bLevelMissing = true;
        }

        if(scene.isLoaded)
        {
            yield return UnitySceneManager.UnloadSceneAsync(scene);
            yield return Resources.UnloadUnusedAssets();
        }

        // gotta be careful w/ this
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        FaderText.text = "";
    }

    public void StartRideSound(Transform elevator)
    {
        m_RideSoundSource = elevator != null ? elevator : transform;
        m_RideSound.position = m_RideSoundSource.position;

        if(m_RideLoop != null && m_RideLoop.isPlaying)
        {
            return;
        }

        Sfx.Play(Sfx.Sounds.ElevatorStart, m_RideSound);
        m_RideLoop = Sfx.PlayLoop(Sfx.Sounds.ElevatorMotor, m_RideSound);
    }

    public void StopRideSound()
    {
        Sfx.StopLoop(m_RideLoop);
        m_RideLoop = null;
        Sfx.Play(Sfx.Sounds.ElevatorArrive, m_RideSound);
    }

    void LateUpdate()
    {
        if(m_RideSoundSource != null)
        {
            m_RideSound.position = m_RideSoundSource.position;
        }
    }

    static ILevel FindLevel(Scene scene)
    {
        if(!scene.isLoaded)
        {
            return null;
        }

        foreach(GameObject root in scene.GetRootGameObjects())
        {
            ILevel level = root.GetComponentInChildren<ILevel>(true);
            if(level != null)
            {
                return level;
            }
        }

        return null;
    }
}
