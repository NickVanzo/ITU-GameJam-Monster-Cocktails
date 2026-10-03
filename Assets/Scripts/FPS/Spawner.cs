using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public event System.Action<IngredientType, int> OnMaterialCollected;

    public GameObject Prefab;
    public MaterialPickup PickupPrefab;
    public FPSController Target;
    public List<IMonster> aAllowedMonster = new();

    [SerializeField] int m_nCap = 30;
    [SerializeField] int m_nGroupSize = 4;
    [SerializeField] int m_nMaxGroupSize = 10;
    [SerializeField] float m_fGroupGrowthInterval = 30.0f;
    [SerializeField] float m_fCooldown = 5.0f;
    [SerializeField] float m_fFirstGroupDelay = 1.5f;
    [SerializeField] float m_fRadius = 20.0f;
    [SerializeField] float m_fGroupSpread = 3.0f;
    [SerializeField] float m_fMinDistanceToTarget = 12.0f;

    readonly List<EnemyBehavior> aMonsters = new();
    float m_fTimestamp;
    float m_fStartTime;
    bool bActive = false;

    public void StartSpawning()
    {
        Clear();
        m_fStartTime = Time.time;
        m_fTimestamp = Time.time + m_fFirstGroupDelay;
        bActive = true;
    }

    public void StopSpawning()
    {
        bActive = false;
    }

    public void Clear()
    {
        foreach(Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        aMonsters.Clear();
    }

    public void Update()
    {
        if(!bActive || Time.time < m_fTimestamp)
        {
            return;
        }

        SpawnGroup();
        m_fTimestamp = Time.time + m_fCooldown;
    }

    public void SpawnGroup()
    {
        int nGrowth = Mathf.FloorToInt((Time.time - m_fStartTime) / m_fGroupGrowthInterval);
        int nCount = Mathf.Min(m_nGroupSize + nGrowth, m_nMaxGroupSize, m_nCap - aMonsters.Count);
        Vector3 vCenter = PickGroupCenter();

        for(int i = 0; i < nCount; ++i)
        {
            Vector2 vOffset = Random.insideUnitCircle * m_fGroupSpread;
            Spawn(vCenter + new Vector3(vOffset.x, 0.0f, vOffset.y));
        }
    }

    void Spawn(Vector3 vCoords)
    {
        Vector3 vFacing = Target != null ? Target.transform.position - vCoords : transform.position - vCoords;
        vFacing.y = 0.0f;
        Quaternion qRotation = vFacing.sqrMagnitude > 0.001f ? Quaternion.LookRotation(vFacing) : Quaternion.identity;

        var go = Instantiate(Prefab, vCoords, qRotation, transform);
        var enemy = go.GetComponent<EnemyBehavior>();
        IMonster monster = aAllowedMonster.Count > 0 ? aAllowedMonster[Random.Range(0, aAllowedMonster.Count)] : null;
        enemy.SetMonster(monster, this);

        aMonsters.Add(enemy);
    }

    Vector3 PickGroupCenter()
    {
        Vector3 vCoords = transform.position;

        for(int i = 0; i < 8; ++i)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            vCoords = transform.position + new Vector3(randomCircle.x, 0.0f, randomCircle.y) * m_fRadius;

            if(Target == null || Vector3.Distance(vCoords, Target.transform.position) >= m_fMinDistanceToTarget)
            {
                break;
            }
        }

        return vCoords;
    }

    public void NotifyMonsterKilled(EnemyBehavior monster)
    {
        aMonsters.Remove(monster);
        DropLoot(monster);
    }

    void DropLoot(EnemyBehavior monster)
    {
        IMonster data = monster.Monster;
        if(PickupPrefab == null || data == null || data.nLootCount <= 0 || Random.value > data.fLootChance)
        {
            return;
        }

        MaterialPickup pickup = Instantiate(PickupPrefab, monster.transform.position + Vector3.up * 0.4f, Quaternion.identity, transform);
        pickup.Init(data.Loot, data.nLootCount, this);
    }

    public void NotifyPickupCollected(MaterialPickup pickup)
    {
        OnMaterialCollected?.Invoke(pickup.Type, pickup.nCount);
        Destroy(pickup.gameObject);
    }
}
