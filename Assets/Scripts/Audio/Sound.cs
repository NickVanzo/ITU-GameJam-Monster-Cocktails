using UnityEngine;

// One sound slot in the SoundLibrary. A random clip is picked each time it plays.
[System.Serializable]
public class Sound
{
    public AudioClip[] Clips = new AudioClip[0];
    [Range(0.0f, 1.0f)] public float Volume = 1.0f;
    public Vector2 PitchRange = new(0.95f, 1.05f);

    [Tooltip("0 = 2D, 1 = fully 3D. Only used when the sound is played at a position.")]
    [Range(0.0f, 1.0f)] public float SpatialBlend = 1.0f;
    [Tooltip("Distance at which a 3D sound becomes silent.")]
    public float MaxDistance = 25.0f;
    [Tooltip("How many copies of this sound may play at once. 0 = no limit.")]
    public int MaxInstances = 0;
    [Tooltip("When MaxInstances copies are already playing: on = cut the oldest one off and play the new one, off = skip the new one.")]
    public bool RestartWhenFull = false;

    public AudioClip PickClip()
    {
        if(Clips == null || Clips.Length == 0)
        {
            return null;
        }

        return Clips[Random.Range(0, Clips.Length)];
    }
}
