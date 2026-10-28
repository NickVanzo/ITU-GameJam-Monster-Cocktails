using System;
using UnityEngine;

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

    public override string DebugPrint(GunStats stats)
    {
        string sState = m_fCooldown > 0.0f ? $"COOLDOWN {m_fCooldown:0.00}s" : "READY";
        return $"BURST x{nBullets} {stats.Get(GunStat.FireRate, fRoundsPerMinute):0} RPM {sState}";
    }

    public override bool TryGetStat(GunStat eStat, GunStats stats, out float fValue)
    {
        fValue = stats.Get(GunStat.FireRate, fRoundsPerMinute);
        return eStat == GunStat.FireRate;
    }
}