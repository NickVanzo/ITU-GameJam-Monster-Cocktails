using System;
using UnityEngine;

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

    public override string DebugPrint(GunStats stats) => $"AUTO {stats.Get(GunStat.FireRate, fRoundsPerMinute):0} RPM";

    public override bool TryGetStat(GunStat eStat, GunStats stats, out float fValue)
    {
        fValue = stats.Get(GunStat.FireRate, fRoundsPerMinute);
        return eStat == GunStat.FireRate;
    }
}