using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

[ExecuteAlways]
public class LightingManager : MonoBehaviour
{
    // Camera whose position picks the room. Falls back to Camera.main.
    [SerializeField] private Transform volumeTrigger;
    [SerializeField] private LayerMask volumeMask = ~0;

    private static readonly int ShadowColorId = Shader.PropertyToID("_ShadowColor");

    // Own stack so the Scene view camera (which also writes VolumeManager.instance.stack) can't make values flicker.
    private VolumeStack stack;

    void Awake()
    {
        if (!Application.isPlaying)
            return;

        foreach (Volume volume in FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            MakeBoundsTriggers(volume);            
        }

        UnitySceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        UnitySceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Volume volume in root.GetComponentsInChildren<Volume>(true))
            {
                MakeBoundsTriggers(volume);                
            }
        }
    }

    // needless to say this shit must go
    static void MakeBoundsTriggers(Volume volume)
    {
        foreach (Collider bounds in volume.GetComponents<Collider>())
        {
            bounds.isTrigger = true;            
        }
    }

    void LateUpdate()
    {
        if (!VolumeManager.instance.isInitialized)
            return;

        stack ??= VolumeManager.instance.CreateStack();

        Transform trigger = volumeTrigger != null ? volumeTrigger : Camera.main != null ? Camera.main.transform : null;
        VolumeManager.instance.Update(stack, trigger, volumeMask);

        var room = stack.GetComponent<RoomLighting>();
        Shader.SetGlobalColor(ShadowColorId, room.ShadowColor.value);
    }

    void OnDisable()
    {
        if (stack != null && VolumeManager.instance.isInitialized)
        {
            VolumeManager.instance.DestroyStack(stack);            
        }

        stack = null;
    }
}
