using UnityEngine;

// Fire-and-forget sound effects from a small pool of AudioSources.
//
//      Sfx.Play(Sfx.Sounds.GunShot);                       2D (UI, the player's own sounds)
//      Sfx.Play(Sfx.Sounds.ZombieGroan, transform.position); 3D at a point
//      Sfx.Play(Sfx.Sounds.ElevatorStart, transform);      3D, follows the transform while it plays
//      AudioSource loop = Sfx.PlayLoop(Sfx.Sounds.ElevatorMotor); ... Sfx.StopLoop(loop);
public static class Sfx
{
    const int nPoolSize = 32;

    static SoundLibrary s_Library;
    static AudioSource[] s_aSources;
    static Sound[] s_aOwners;
    static float[] s_aStartTimes;
    static Transform[] s_aFollow;

    public static SoundLibrary Sounds
    {
        get
        {
            if(s_Library == null)
            {
                s_Library = Resources.Load<SoundLibrary>("SoundLibrary");
                if(s_Library == null)
                {
                    Debug.LogWarning("Sfx: no SoundLibrary asset in a Resources folder, sound effects are muted.");
                    s_Library = ScriptableObject.CreateInstance<SoundLibrary>();
                }
            }

            return s_Library;
        }
    }

    // Statics survive play sessions when domain reload is disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        s_Library = null;
        s_aSources = null;
        s_aOwners = null;
        s_aStartTimes = null;
        s_aFollow = null;
    }

    // No position = 2D. With a position = 3D at that point, using the sound's SpatialBlend.
    public static void Play(Sound sound, Vector3? vPosition = null)
    {
        PlayInternal(sound, vPosition, false);
    }

    // 3D, and the sound moves with the transform until it ends.
    public static void Play(Sound sound, Transform follow)
    {
        PlayInternal(sound, follow.position, false, follow);
    }

    // Returns the source to pass to StopLoop, or null when the sound has no clip.
    public static AudioSource PlayLoop(Sound sound, Vector3? vPosition = null)
    {
        return PlayInternal(sound, vPosition, true);
    }

    public static AudioSource PlayLoop(Sound sound, Transform follow)
    {
        return PlayInternal(sound, follow.position, true, follow);
    }

    public static void StopLoop(AudioSource source)
    {
        if(source != null)
        {
            source.Stop();
            source.loop = false;
        }
    }

    // Cuts off every copy of this sound that is currently playing.
    public static void Stop(Sound sound)
    {
        if(sound == null || s_aSources == null || s_aSources[0] == null)
        {
            return;
        }

        for(int i = 0; i < s_aSources.Length; ++i)
        {
            if(s_aOwners[i] == sound && s_aSources[i].isPlaying)
            {
                StopLoop(s_aSources[i]);
            }
        }
    }

    static AudioSource PlayInternal(Sound sound, Vector3? vPosition, bool bLoop, Transform follow = null)
    {
        AudioClip clip = sound?.PickClip();
        if(clip == null || !Application.isPlaying)
        {
            return null;
        }

        EnsurePool();

        int nIndex;
        if(sound.MaxInstances > 0 && CountPlaying(sound) >= sound.MaxInstances)
        {
            if(!sound.RestartWhenFull)
            {
                return null;
            }

            nIndex = FindOldestPlaying(sound);
        }
        else
        {
            nIndex = FindSource();
        }

        AudioSource source = s_aSources[nIndex];
        source.transform.position = vPosition ?? Vector3.zero;
        source.clip = clip;
        source.volume = sound.Volume;
        source.pitch = Random.Range(sound.PitchRange.x, sound.PitchRange.y);
        source.spatialBlend = vPosition.HasValue ? sound.SpatialBlend : 0.0f;
        source.maxDistance = sound.MaxDistance;
        source.loop = bLoop;
        source.outputAudioMixerGroup = Sounds.Output;
        source.Play();

        s_aOwners[nIndex] = sound;
        s_aStartTimes[nIndex] = Time.unscaledTime;
        s_aFollow[nIndex] = follow;
        return source;
    }

    // Called every frame by SfxFollow, which lives on the pool object.
    public static void UpdateFollow()
    {
        if(s_aFollow == null)
        {
            return;
        }

        for(int i = 0; i < s_aFollow.Length; ++i)
        {
            if(s_aFollow[i] == null)
            {
                continue;
            }

            if(s_aSources[i] == null || !s_aSources[i].isPlaying)
            {
                s_aFollow[i] = null;
                continue;
            }

            s_aSources[i].transform.position = s_aFollow[i].position;
        }
    }

    static int CountPlaying(Sound sound)
    {
        int nCount = 0;
        for(int i = 0; i < s_aSources.Length; ++i)
        {
            if(s_aOwners[i] == sound && s_aSources[i].isPlaying)
            {
                ++nCount;
            }
        }

        return nCount;
    }

    static int FindOldestPlaying(Sound sound)
    {
        int nOldest = -1;
        for(int i = 0; i < s_aSources.Length; ++i)
        {
            if(s_aOwners[i] == sound && s_aSources[i].isPlaying && (nOldest < 0 || s_aStartTimes[i] < s_aStartTimes[nOldest]))
            {
                nOldest = i;
            }
        }

        return nOldest;
    }

    // First idle source, otherwise steal the oldest one-shot. Loops are never stolen.
    static int FindSource()
    {
        int nOldest = -1;
        for(int i = 0; i < s_aSources.Length; ++i)
        {
            AudioSource source = s_aSources[i];
            if(!source.isPlaying)
            {
                return i;
            }

            if(!source.loop && (nOldest < 0 || s_aStartTimes[i] < s_aStartTimes[nOldest]))
            {
                nOldest = i;
            }
        }

        return Mathf.Max(nOldest, 0);
    }

    static void EnsurePool()
    {
        if(s_aSources != null && s_aSources[0] != null)
        {
            return;
        }

        var root = new GameObject("Sfx");
        Object.DontDestroyOnLoad(root);
        root.AddComponent<SfxFollow>();

        s_aSources = new AudioSource[nPoolSize];
        s_aOwners = new Sound[nPoolSize];
        s_aStartTimes = new float[nPoolSize];
        s_aFollow = new Transform[nPoolSize];

        for(int i = 0; i < nPoolSize; ++i)
        {
            var go = new GameObject($"Sfx {i}");
            go.transform.SetParent(root.transform);

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1.0f;
            source.dopplerLevel = 0.0f;
            s_aSources[i] = source;
        }
    }
}
