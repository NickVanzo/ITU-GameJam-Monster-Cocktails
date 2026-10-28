using System;
using UnityEngine;

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

    public override void Spawn(in ShooterState shooter, GunStats stats, Action<RayHit> onHit)
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

        onHit(new RayHit () {
            vPoint = hit.point,
            vNormal = hit.normal,
            Target = target,
            nDamage = m_nDamage,
            bHeadshot = hit.collider.CompareTag(HeadTag), // ...
        });
    }

    public override string DebugPrint(GunStats stats) => $"HITSCAN {m_nDamage} DMG  {m_fRange:0}m";

    public override bool TryGetStat(GunStat eStat, GunStats stats, out float fValue)
    {
        switch(eStat)
        {
            case GunStat.Damage: fValue = m_nDamage; return true;
            case GunStat.Range: fValue = m_fRange; return true;
            case GunStat.Spread: fValue = m_vSpread.y; return true;   // the spread once warmed up, the one attachments change
            default: fValue = 0.0f; return false;
        }
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

