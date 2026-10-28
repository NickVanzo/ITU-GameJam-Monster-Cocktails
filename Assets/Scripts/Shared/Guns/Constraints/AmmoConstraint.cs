using System;
using UnityEngine;

[Serializable]
public class AmmoConstraint : GunConstraint
{
    public int nBaseCapacity = 7;
    public float fBaseReloadTime = 1.4f;

    private int m_nCapacity;
    private float m_fReloadTime;
    private int m_nRounds;
    private float m_fReloadTimer;

    public int Rounds => m_nRounds;
    public int Capacity => m_nCapacity;
    public bool IsReloading => m_fReloadTimer > 0.0f;
    public float ReloadProgress => IsReloading ? 1.0f - m_fReloadTimer / m_fReloadTime : 0.0f;

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

    public override string DebugPrint(GunStats stats)
    {
        string sState = IsReloading ? $"RELOADING {m_fReloadTimer:0.00}s / {m_fReloadTime:0.00}s" : m_nRounds == 0 ? "EMPTY" : "";
        return $"AMMO {m_nRounds}/{m_nCapacity}  {sState}";
    }

    public override bool TryGetStat(GunStat eStat, GunStats stats, out float fValue)
    {
        switch(eStat)
        {
            case GunStat.MagazineSize: fValue = m_nCapacity; return true;
            case GunStat.ReloadTime: fValue = m_fReloadTime; return true;
            default: fValue = 0.0f; return false;
        }
    }
}