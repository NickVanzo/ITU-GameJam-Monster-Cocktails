using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBehavior : IDamageable
{
    static readonly List<EnemyBehavior> s_aActive = new();

    public IMonster Monster;

    [SerializeField] float fStopDistance = 1.3f;
    [SerializeField] float fAttackRange = 1.7f;
    [SerializeField] float fAttackCooldown = 1.0f;
    [SerializeField] float fSeparationRadius = 1.2f;
    [SerializeField] float fTurnSpeed = 10.0f;
    [SerializeField] float fSpeedVariance = 0.15f;

    Spawner m_Spawner;
    float m_fAttackTimestamp;
    float m_fHitFlash;
    float m_fSpeedScale = 1.0f;
    Vector3 m_vBaseScale;

    float MoveSpeed => (Monster != null ? Monster.fMoveSpeed : 2.2f) * m_fSpeedScale;
    int Damage => Monster != null ? Monster.nDamage : 1;

    protected override void Awake()
    {
        base.Awake();
        m_vBaseScale = transform.localScale;

        if(!TryGetComponent(out Rigidbody body))
        {
            body = gameObject.AddComponent<Rigidbody>();
        }
        body.isKinematic = true;
    }

    void OnEnable()
    {
        s_aActive.Add(this);
    }

    void OnDisable()
    {
        s_aActive.Remove(this);
    }

    public void SetMonster(IMonster monster, Spawner spawner)
    {
        Monster = monster;
        m_Spawner = spawner;
        m_fAttackTimestamp = Time.time + fAttackCooldown;
        m_fSpeedScale = Random.Range(1.0f - fSpeedVariance, 1.0f + fSpeedVariance);

        if(Monster != null)
        {
            ResetHealth(Monster.nHealth);
        }
    }

    public void Update()
    {
        if(!IsAlive() || m_Spawner == null || m_Spawner.Target == null)
        {
            return;
        }

        FPSController target = m_Spawner.Target;
        Vector3 vToTarget = target.transform.position - transform.position;
        vToTarget.y = 0.0f;
        float fDistance = vToTarget.magnitude;
        Vector3 vDirection = fDistance > 0.001f ? vToTarget / fDistance : transform.forward;

        Vector3 vMove = ComputeSeparation();
        if(fDistance > fStopDistance)
        {
            vMove += vDirection;
        }

        transform.position += Vector3.ClampMagnitude(vMove, 1.0f) * MoveSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(vDirection), fTurnSpeed * Time.deltaTime);

        if(fDistance <= fAttackRange && target.CanMove && Time.time >= m_fAttackTimestamp)
        {
            m_fAttackTimestamp = Time.time + fAttackCooldown;
            target.TakeDamage(Damage);
        }

        m_fHitFlash = Mathf.MoveTowards(m_fHitFlash, 0.0f, Time.deltaTime * 8.0f);
        transform.localScale = m_vBaseScale * (1.0f + 0.2f * m_fHitFlash);
    }

    Vector3 ComputeSeparation()
    {
        Vector3 vSeparation = Vector3.zero;

        foreach(EnemyBehavior other in s_aActive)
        {
            if(other == this)
            {
                continue;
            }

            Vector3 vAway = transform.position - other.transform.position;
            vAway.y = 0.0f;
            float fDistance = vAway.magnitude;

            if(fDistance > 0.001f && fDistance < fSeparationRadius)
            {
                vSeparation += vAway / fDistance * (1.0f - fDistance / fSeparationRadius);
            }
        }

        return vSeparation;
    }

    protected override void OnDamaged()
    {
        m_fHitFlash = 1.0f;
    }

    protected override void OnDeath()
    {
        if(m_Spawner != null)
        {
            m_Spawner.NotifyMonsterKilled(this);
        }

        StartCoroutine(DieRoutine());
    }

    IEnumerator DieRoutine()
    {
        foreach(Collider collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        Vector3 vStart = transform.localScale;
        for(float t = 0.0f; t < 1.0f; t += Time.deltaTime / 0.15f)
        {
            transform.localScale = Vector3.Lerp(vStart, Vector3.zero, t);
            yield return null;
        }

        Destroy(gameObject);
    }
}
