using UnityEngine;

// Added to the Sfx pool object at runtime. Keeps sounds played with Sfx.Play(sound, transform) on their transform.
public class SfxFollow : MonoBehaviour
{
    void LateUpdate()
    {
        Sfx.UpdateFollow();
    }
}
