
using System;
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

    public float Get(GunStat eStat, float fBase) => (fBase + m_aAdd[(int)eStat]) * m_aMultiply[(int)eStat];
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
}


// Parts can hold per-gun state (rounds left, heat), so each Gun clones its own
// Shallow copy: keep part fields as plain values.
[Serializable]
public abstract class GunPart
{
    public GunPart Clone() => (GunPart) MemberwiseClone();
    
    public virtual void Init(GunStats stats) {}
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


[Serializable]
public class ContinuousTrigger : GunTrigger   // hold to fire
{
    public float fRoundsPerMinute = 500.0f;   // config
    float m_fTimer;                           // state

    public override void Init(GunStats stats)
    {
        m_fTimer = 0;
    }

    public override int Pull(in TriggerInput input, GunStats stats, float fDeltaTime)
    {
        float fInterval = 60.0f / stats.Get(GunStat.FireRate, fRoundsPerMinute);
        m_fTimer += fDeltaTime;

        if(!input.bHeld)
        {
            m_fTimer = Mathf.Min(m_fTimer, fInterval);
            return 0;
        }
        
        int nShots = Mathf.FloorToInt(m_fTimer / fInterval);
        m_fTimer -= nShots * fInterval;
        
        return nShots;   // 2+ on a slow frame, so the fire rate doesn't drop with the frame rate
    }
}

[Serializable]
public class SingleShotTrigger : GunTrigger   // one shot per click
{
    public float fRoundsPerMinute = 120.0f;   // config: caps how fast clicking can fire
    float m_fCooldown;                        // state

    public override void Init(GunStats stats)
    {
        m_fCooldown = 0;
    }

    public override int Pull(in TriggerInput input, GunStats stats, float fDeltaTime)
    {
        m_fCooldown = Mathf.Max(m_fCooldown - fDeltaTime, 0.0f);
        if(!input.bPressed || m_fCooldown > 0.0f)
        {
            return 0;
        }
        m_fCooldown = 60.0f / stats.Get(GunStat.FireRate, fRoundsPerMinute);
        return 1;
    }
}

[Serializable]
public class BurstShotTrigger : GunTrigger   // one shot per click
{
    public int nBullets = 1;
    public float fRoundsPerMinute = 120.0f;   // config: caps how fast clicking can fire
    float m_fCooldown;                        // state

    public override void Init(GunStats stats)
    {
        m_fCooldown = 0;
    }

    public override int Pull(in TriggerInput input, GunStats stats, float fDeltaTime)
    {
        m_fCooldown = Mathf.Max(m_fCooldown - fDeltaTime, 0.0f);
        if(!input.bPressed || m_fCooldown > 0.0f)
        {
            return 0;
        }
        m_fCooldown = 60.0f / stats.Get(GunStat.FireRate, fRoundsPerMinute);
        return nBullets;
    }
}

[Serializable] public abstract class GunConstraint : GunPart
{
    public virtual void Tick(GunStats stats, float fDelta) {}
    public virtual void Reload() {}
    public abstract bool CanFire(GunStats stats);
    public abstract void OnFired(GunStats stats);
}

[Serializable]
public class AmmoConstraint : GunConstraint
{
    public int nBaseCapacity = 7;
    public float fBaseReloadTime = 1.4f;

    private int m_nCapacity;
    private float m_fReloadTime;
    private int m_nRounds;
    private float m_fReloadTimer;

    public override void Init(GunStats stats)
    {
        m_fReloadTime = stats.Get(GunStat.ReloadTime, fBaseReloadTime);
        m_nCapacity = Mathf.RoundToInt(stats.Get(GunStat.MagazineSize, nBaseCapacity));
        m_nRounds = m_nCapacity;
        m_fReloadTimer = 0;
    }

    public override void Tick(GunStats stats, float fDelta)
    {
        if(m_fReloadTimer <= 0.0f)
        {
            return;
        }
        m_fReloadTimer -= fDelta;
        
        if(m_fReloadTimer <= 0.0f)
        {
            m_nRounds = m_nCapacity;
        }
    }

    public override void Reload()
    {
        if(m_fReloadTimer <= 0.0f && m_nRounds < m_nCapacity)
        {
            m_fReloadTimer = m_fReloadTime;
        }
    }

    public override bool CanFire(GunStats stats) => m_nRounds > 0 && m_fReloadTimer <= 0.0f;
    public override void OnFired(GunStats stats) => m_nRounds--;
}

[Serializable] public abstract class GunSpawner : GunPart
{
    public abstract void Spawn(in ShooterState shooter, GunStats stats, Action<GunHit> onHit);
}


[Serializable]
public class HitscanSpawner : GunSpawner
{
    const string HeadTag = "Head";

    public int nBaseDamage = 1;
    public float fBaseRange = 500;
    public Vector2 vBaseSpread = new Vector2(0.0f, 1.0f);
    public float fBaseHeatRate = 0.15f; // later

    private int m_nDamage;
    private float m_fRange;
    private Vector2 m_vSpread;
    private float m_fHeatRate;
    private float m_fWarmup = 0; // 0-1


    public override void Init(GunStats stats)
    {
        m_nDamage = Mathf.RoundToInt(stats.Get(GunStat.Damage, nBaseDamage));
        m_fRange = stats.Get(GunStat.Range, fBaseRange);
        m_vSpread = new Vector2(vBaseSpread.x, stats.Get(GunStat.Spread, vBaseSpread.y));
        m_fWarmup = 0;
    }

    public override void Spawn(in ShooterState shooter, GunStats stats, Action<GunHit> onHit)
    {
        Vector3 vDirection = Scatter(shooter.vAim, Mathf.Lerp(m_vSpread.x, m_vSpread.y, m_fWarmup));
        if(!Physics.Raycast(shooter.vPos, vDirection, out RaycastHit hit, m_fRange, shooter.HitMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        IDamageable target = hit.collider.GetComponentInParent<IDamageable>(); // potentially expensive check
        if(target == shooter.Owner || (target != null && !target.IsAlive()))
        {
            target = null;
        }

        onHit(new GunHit () {
            vPoint = hit.point,
            vNormal = hit.normal,
            Target = target,
            nDamage = m_nDamage,
            bHeadshot = hit.collider.CompareTag(HeadTag), // ...
        });
    }

    static Vector3 Scatter(Vector3 vAim, float fDegrees)
    {
        if(fDegrees <= 0.0f)
        {
            return vAim;
        }

        Vector2 vOffset = UnityEngine.Random.insideUnitCircle * Mathf.Tan(fDegrees * Mathf.Deg2Rad);
        return Quaternion.LookRotation(vAim) * new Vector3(vOffset.x, vOffset.y, 1.0f).normalized;
    }
}

