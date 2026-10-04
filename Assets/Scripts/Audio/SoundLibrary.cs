using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Tooltip("Optional mixer group all sound effects go through.")]
    public AudioMixerGroup Output;

    [Header("FPS - Player")]
    public Sound GunShot = new() { SpatialBlend = 0.0f, MaxInstances = 3, RestartWhenFull = true };
    public Sound BulletImpact = new() { MaxInstances = 4 };
    public Sound HitMarker = new() { SpatialBlend = 0.0f, Volume = 0.6f, MaxInstances = 2 };
    public Sound HeadshotMarker = new() { SpatialBlend = 0.0f };
    public Sound PlayerFootstep = new() { SpatialBlend = 0.0f, Volume = 0.5f, PitchRange = new(0.85f, 1.15f) };
    public Sound PlayerJump = new() { SpatialBlend = 0.0f, Volume = 0.6f };
    public Sound PlayerLand = new() { SpatialBlend = 0.0f, Volume = 0.7f };
    public Sound PlayerHurt = new() { SpatialBlend = 0.0f };
    public Sound PlayerDeath = new() { SpatialBlend = 0.0f };
    public Sound PickupCollect = new() { SpatialBlend = 0.0f, MaxInstances = 2 };

    [Header("FPS - Zombies")]
    public Sound ZombieSpawn = new() { MaxDistance = 40.0f, MaxInstances = 3 };
    public Sound ZombieFootstep = new() { Volume = 0.6f, MaxDistance = 15.0f, MaxInstances = 6, PitchRange = new(0.8f, 1.1f) };
    public Sound ZombieGroan = new() { MaxDistance = 20.0f, MaxInstances = 4, PitchRange = new(0.8f, 1.1f) };
    public Sound ZombieAttack = new() { MaxInstances = 3 };
    public Sound ZombieHurt = new() { MaxInstances = 4 };
    public Sound ZombieDeath = new() { MaxInstances = 4 };

    [Header("Camera")]
    public Sound ZoomIn = new() { SpatialBlend = 0.0f, MaxInstances = 1, RestartWhenFull = true };
    public Sound ZoomOut = new() { SpatialBlend = 0.0f, MaxInstances = 1, RestartWhenFull = true };

    [Header("Elevator")]
    public Sound RopePull = new();
    public Sound RopeTrigger = new();
    public Sound ElevatorStart = new() { SpatialBlend = 0.0f };
    public Sound ElevatorMotor = new() { SpatialBlend = 0.0f, PitchRange = new(1.0f, 1.0f) };
    public Sound ElevatorArrive = new() { SpatialBlend = 0.0f };
    public Sound HarvestBanked = new() { SpatialBlend = 0.0f };

    [Header("Bar")]
    public Sound RoomSwitch = new() { SpatialBlend = 0.0f };
    public Sound IngredientPour = new();
    public Sound IngredientSplash = new() { MaxInstances = 3 };
    public Sound OutOfStock = new() { SpatialBlend = 0.0f };
    public Sound GlassFlush = new();
    public Sound Serve = new() { SpatialBlend = 0.0f };
    public Sound CustomerArrive = new() { SpatialBlend = 0.0f };
    public Sound CustomerFootstep = new() { Volume = 0.5f, MaxDistance = 20.0f, PitchRange = new(0.85f, 1.15f) };
    public Sound CustomerSpeak = new() { MaxInstances = 1 };
    public Sound CustomerHappy = new();
    public Sound CustomerAngry = new();
    public Sound UIClick = new() { SpatialBlend = 0.0f, Volume = 0.7f };
}
