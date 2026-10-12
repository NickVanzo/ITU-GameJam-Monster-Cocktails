using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class Elevator : MonoBehaviour
{
    [Tooltip("Level scene names, each scene needs an FPSLevel and has to be in the build settings")]
    public string[] aFloors = { "Arena" };
    [Tooltip("Index in aFloors of the floor Descend() goes to.")]
    public int nFloor = 0;

    public CinemachineBrain Brain;
    [Tooltip("The elevator in the bar / top floor areas - Elevator sounds come from here while the camera sinks or rises")]
    public Transform BarElevator;

    [Header("Bar")]
    public Behaviour[] aDisableDuringLevel;
    public GameObject[] aHideDuringLevel;

    [Header("UI")]
    [Tooltip("Covers the screen while the bar and a level swap.")]
    public CanvasGroup Fader;
    public TMP_Text FaderText;

    [Header("Ride")]
    [Tooltip("Bar camera sinking into the floor on the way down.")]
    [SerializeField] float fDescentDuration = 1.5f;
    [Tooltip("Bar camera coming back up after the level.")]
    [SerializeField] float fAscentDuration = 0.75f;
    [SerializeField] float fDescentDistance = 2.0f;
    [SerializeField] float fFadeDuration = 0.35f;

    bool bBusy = false;
    Camera m_Camera;
    Vector3 m_vBarCameraPosition;
    Quaternion m_qBarCameraRotation;
    float m_fBarFieldOfView;
    float m_fBarNearClip;
    float m_fBarFarClip;
    AudioSource m_RideLoop;
    Transform m_RideSound;
    Transform m_RideSoundSource;

    void Awake()
    {
        MaterialInventory.Clear();

        m_Camera = Brain.GetComponent<Camera>();
        Brain.enabled = false;
        Fader.alpha = 0.0f;
        FaderText.text = "";

        m_RideSound = new GameObject("RideSound").transform;
        m_RideSound.SetParent(transform, false);
    }

    void OnDisable()
    {
        Sfx.StopLoop(m_RideLoop);
        m_RideLoop = null;
    }

    public void Descend()
    {
        if(bBusy || aFloors.Length == 0)
        {
            return;
        }

        StartCoroutine(TripRoutine(aFloors[Mathf.Clamp(nFloor, 0, aFloors.Length - 1)]));
    }

    IEnumerator TripRoutine(string sScene)
    {
        bBusy = true;
        SetBarActive(false);
        StartRideSound(BarElevator);

        AsyncOperation load = null;
        if(!UnitySceneManager.GetSceneByName(sScene).isLoaded)
        {
            load = UnitySceneManager.LoadSceneAsync(sScene, LoadSceneMode.Additive);
            if(load != null)
            {
                load.allowSceneActivation = false;
            }
        }

        yield return SinkCamera();

        if(load != null)
        {
            load.allowSceneActivation = true;
            yield return load;
        }

        Scene scene = UnitySceneManager.GetSceneByName(sScene);
        FPSLevel level = FindLevel(scene);
        if(level != null)
        {
            Brain.enabled = true;
            yield return level.Play(this);
            Brain.enabled = false;
        }
        else
        {
            Debug.LogError($"Elevator: no FPSLevel in scene '{sScene}'. Is the scene in the build settings?", this);
            StopRideSound();
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
        yield return RaiseCamera();

        SetBarActive(true);
        bBusy = false;
    }

    IEnumerator SinkCamera()
    {
        Transform cameraTransform = m_Camera.transform;
        m_vBarCameraPosition = cameraTransform.position;
        m_qBarCameraRotation = cameraTransform.rotation;
        m_fBarFieldOfView = m_Camera.fieldOfView;
        m_fBarNearClip = m_Camera.nearClipPlane;
        m_fBarFarClip = m_Camera.farClipPlane;

        Vector3 vBottom = m_vBarCameraPosition + Vector3.down * fDescentDistance;
        for(float fElapsed = 0.0f; fElapsed < fDescentDuration; fElapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0.0f, 1.0f, fElapsed / fDescentDuration);
            Vector3 vRumble = new Vector3(Mathf.PerlinNoise(fElapsed * 25.0f, 0.0f) - 0.5f, Mathf.PerlinNoise(0.0f, fElapsed * 25.0f) - 0.5f, 0.0f) * 0.04f;
            cameraTransform.position = Vector3.Lerp(m_vBarCameraPosition, vBottom, t) + vRumble;
            Fader.alpha = Mathf.InverseLerp(fDescentDuration - fFadeDuration, fDescentDuration, fElapsed);
            yield return null;
        }
        Fader.alpha = 1.0f;
    }

    IEnumerator RaiseCamera()
    {
        m_Camera.fieldOfView = m_fBarFieldOfView;
        m_Camera.nearClipPlane = m_fBarNearClip;
        m_Camera.farClipPlane = m_fBarFarClip;

        Transform cameraTransform = m_Camera.transform;
        cameraTransform.rotation = m_qBarCameraRotation;
        Vector3 vBottom = m_vBarCameraPosition + Vector3.down * fDescentDistance;
        StartRideSound(BarElevator);
        for(float fElapsed = 0.0f; fElapsed < fAscentDuration; fElapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0.0f, 1.0f, fElapsed / fAscentDuration);
            cameraTransform.position = Vector3.Lerp(vBottom, m_vBarCameraPosition, t);
            Fader.alpha = 1.0f - Mathf.InverseLerp(0.0f, fFadeDuration, fElapsed);
            yield return null;
        }
        cameraTransform.position = m_vBarCameraPosition;
        Fader.alpha = 0.0f;
        StopRideSound();
    }

    public void StartRideSound(Transform elevator)
    {
        m_RideSoundSource = elevator != null ? elevator : m_Camera.transform;
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

    static FPSLevel FindLevel(Scene scene)
    {
        if(!scene.isLoaded)
        {
            return null;
        }

        foreach(GameObject root in scene.GetRootGameObjects())
        {
            FPSLevel level = root.GetComponentInChildren<FPSLevel>(true);
            if(level != null)
            {
                return level;
            }
        }

        return null;
    }

    void SetBarActive(bool bActive)
    {
        foreach(Behaviour behaviour in aDisableDuringLevel)
        {
            if(behaviour != null)
            {
                behaviour.enabled = bActive;
            }
        }

        foreach(GameObject go in aHideDuringLevel)
        {
            if(go != null)
            {
                go.SetActive(bActive);
            }
        }
    }
}
