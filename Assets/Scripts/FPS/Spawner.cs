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
    [SerializeField] int m_nWaveSize = 4;
    [SerializeField] int m_nMaxWaveSize = 10;
    [SerializeField] float m_fWaveGrowthInterval = 30.0f;
    [SerializeField] float m_fCooldown = 5.0f;
    [SerializeField] float m_fFirstWaveDelay = 1.5f;
    [Tooltip("Floor area monsters spawn in (X/Z), centered on the spawner.")]
    [SerializeField] Vector2 m_vSpawnArea = new(46.0f, 46.0f);
    [SerializeField] float m_fMinDistanceToTarget = 12.0f;

    readonly List<EnemyBehavior> aMonsters = new();
    float m_fTimestamp;
    float m_fStartTime;
    bool bActive = false;

    public void StartSpawning()
    {
        Clear();
        m_fStartTime = Time.time;
        m_fTimestamp = Time.time + m_fFirstWaveDelay;
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

        SpawnWave();
        m_fTimestamp = Time.time + m_fCooldown;
    }

    // Each monster of the wave gets its own random spot in the room.
    public void SpawnWave()
    {
        int nGrowth = Mathf.FloorToInt((Time.time - m_fStartTime) / m_fWaveGrowthInterval);
        int nCount = Mathf.Min(m_nWaveSize + nGrowth, m_nMaxWaveSize, m_nCap - aMonsters.Count);

        for(int i = 0; i < nCount; ++i)
        {
            Vector3 vCoords = PickSpawnPoint();
            Sfx.Play(Sfx.Sounds.ZombieSpawn, vCoords);
            Spawn(vCoords);
        }
    }

    void Spawn(Vector3 vCoords)
    {
        Vector3 vFacing = Target != null ? Target.transform.position - vCoords : transform.position - vCoords;
        vFacing.y = 0.0f;
        Quaternion qRotation = vFacing.sqrMagnitude > 0.001f ? Quaternion.LookRotation(vFacing) : Quaternion.identity;

        IMonster monster = aAllowedMonster.Count > 0 ? aAllowedMonster[Random.Range(0, aAllowedMonster.Count)] : null;
        GameObject prefab = monster != null && monster.Prefab != null ? monster.Prefab : Prefab;

        var go = Instantiate(prefab, vCoords, qRotation, transform);
        var enemy = go.GetComponent<EnemyBehavior>();
        enemy.SetMonster(monster, this);

        aMonsters.Add(enemy);
    }

    Vector3 PickSpawnPoint()
    {
        Vector3 vCoords = transform.position;

        for(int i = 0; i < 8; ++i)
        {
            Vector3 vOffset = new(Random.Range(-0.5f, 0.5f) * m_vSpawnArea.x, 0.0f, Random.Range(-0.5f, 0.5f) * m_vSpawnArea.y);
            vCoords = transform.position + transform.rotation * vOffset;

            if(Target == null)
            {
                break;
            }

            Vector3 vToTarget = Target.transform.position - vCoords;
            vToTarget.y = 0.0f;
            if(vToTarget.magnitude >= m_fMinDistanceToTarget)
            {
                break;
            }
        }

        return vCoords;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(m_vSpawnArea.x, 0.0f, m_vSpawnArea.y));
    }

    public void NotifyMonsterKilled(EnemyBehavior monster)
    {
        aMonsters.Remove(monster);
        DropLoot(monster);
    }

    void DropLoot(EnemyBehavior monster)
    {
        IMonster data = monster.Monster;
        if(PickupPrefab == null || data == null || !data.HasLoot || Random.value > data.fLootChance)
        {
            return;
        }

        MaterialPickup pickup = Instantiate(PickupPrefab, monster.transform.position + Vector3.up * 0.4f, Quaternion.identity, transform);
        pickup.Init(data.PickLoot(), data.nLootCount, this);
    }

    public void NotifyPickupCollected(MaterialPickup pickup)
    {
        OnMaterialCollected?.Invoke(pickup.Type, pickup.nCount);
        Destroy(pickup.gameObject);
    }
}
