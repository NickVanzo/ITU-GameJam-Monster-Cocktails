using UnityEngine;
using UnityEngine.Rendering;

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

        // Volume colliders only mark the room's bounds. Solid ones block clicks (PlayerInteraction) and the FPS player.
        foreach (Volume volume in FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            foreach (Collider bounds in volume.GetComponents<Collider>())
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
            VolumeManager.instance.DestroyStack(stack);
        stack = null;
    }
}
