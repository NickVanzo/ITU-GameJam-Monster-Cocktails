using System.Collections;
using System.Data;
using UnityEngine;

public class EnemyBehavior : IDamageable
{
    public IMonster Monster;

    void OnEnable()
    {
        
    }

    void OnDisable()
    {
        
    }

    public void SetMonster(IMonster monster)
    {
        Monster = monster;
    }

    public void Update()
    {
           
    }

    public void OnCollision()
    {
        if()
        {
            
        }
    }
}