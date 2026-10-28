using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum GunStat { 
    FireRate, 
    Damage, 
    Range, 
    Spread, 
    MagazineSize, 
    ReloadTime, 
    Recoil, 
    Zoom
}

public class GunStats
{
    static int Count = Enum.GetValues(typeof(GunStat)).Length;
    float[] m_aAdd = new float[Count];
    float[] m_aMultiply = Enumerable.Repeat(1.0f, Count).ToArray();

    public void Apply(StatChange change)
    {
        m_aAdd[(int)change.eStat] += change.fAdd;
        m_aMultiply[(int)change.eStat] *= 1.0f + change.fPercent / 100.0f;
    }

    public float Get(GunStat eStat, float fBase)
    {
        return (fBase + m_aAdd[(int)eStat]) * m_aMultiply[(int)eStat];
    }
}

[Serializable]
public class StatChange
{
    public GunStat eStat;
    public float fAdd;
    
    [Tooltip("-30 = 30% less. 0 = unchanged.")]
    public float fPercent;
}

public enum GunSlot { TriggerGroup, Magazine, Barrel, Muzzle, Sight, Stock, Ammo }

[Serializable]
public class GunSlotDefinition
{
    public GunSlot eSlot;
    public GunAttachment Default;
    public bool bRequired;
    [Tooltip("What the workbench can fit in this slot, on top of Default (and an empty slot, unless required).")]
    public List<GunAttachment> aOptions = new();

    public List<GunAttachment> GetChoices()
    {
        List<GunAttachment> aChoices = new();
        if(!bRequired)
        {
            aChoices.Add(null);
        }

        if(Default != null)
        {
            aChoices.Add(Default);
        }

        foreach(GunAttachment option in aOptions)
        {
            if(option != null && !aChoices.Contains(option))
            {
                aChoices.Add(option);
            }
        }

        return aChoices;
    }
}

// Parts can hold per-gun state (rounds left, heat), so each Gun clones its own
// Shallow copy: keep part fields as plain values.
[Serializable]
public abstract class GunPart
{
    public GunPart Clone() => (GunPart) MemberwiseClone();

    public virtual void Init(GunStats stats) {}

    public virtual string DebugPrint(GunStats stats)
    {
        return GetType().Name.ToUpper();
    }

    // That stat with attachments applied, if this part uses it. The workbench's stat bars read these.
    public virtual bool TryGetStat(GunStat eStat, GunStats stats, out float fValue)
    {
        fValue = 0.0f;
        return false;
    }
}


[Serializable] public abstract class GunTrigger : GunPart
{
    
    public struct TriggerInput
    {
        public bool bHeld;      // IsPressed()
        public bool bPressed;   // WasPressedThisFrame()
    }

    public abstract int Pull(in TriggerInput input, GunStats stats, float fDeltaTime);
}

[Serializable] public abstract class GunConstraint : GunPart
{
    public virtual void Tick(GunStats stats, float fDelta) {}
    public virtual void Reload() {}
    public abstract bool CanFire(GunStats stats);
    public abstract void OnFired(GunStats stats);
}

[Serializable] public abstract class GunSpawner : GunPart
{
    public abstract void Spawn(in ShooterState shooter, GunStats stats, Action<RayHit> onHit);
}
