using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class Spawner
{
    List<IMonster> aMonsters;
    List<IMonster> aAllowedMonster;
    int m_nSpawned = 0;
    int m_nCap = 10;
    float m_fCooldown;
    Time m_Timestamp;
    float m_fRadius = 15.0f;
    GameObject Prefab;
    public void Spawn()
    {
        int n = Random.Range(0, aAllowedMonster.Count - 1);

        Vector2 randomCircle = Random.insideUnitCircle.normalized;
        Vector3 vCoords = new Vector3(randomCircle.x, 0, randomCircle.y) * m_fRadius;
        var go = GameObject.Instantiate(Prefab, vCoords, Quaternion.identity);
        go.GetComponent<EnemyBehavior>().SetMonster(aAllowedMonster[n]);
        ++m_nSpawned;
    }

    public void OnMonsterKilled()
    {
        --m_nSpawned;
    }

    public void Reset()
    {
        
    }

}