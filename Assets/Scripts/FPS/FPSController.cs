using System.Collections;
using System.Data;
using UnityEngine;

public class FPSController : IDamageable
{
    CharacterController Controller;
    bool bCanMove = false;
    Animation Animation;

    void OnEnable()
    {
        bCanMove = true;    
        Controller.enabled = true;
    }

    void OnDisable()
    {
        bCanMove = false;        
        Controller.enabled = false;
    }

    public void HandleMovement()
    {
        //if()
    }

    public void HandleShooting()
    {
        
    }

    public void Update()
    {
        if(!IsAlive())
            return;

        if(bCanMove)
        {
            
        }        
    }

    public void OnCollision()
    {
        TakeDamage(1);

        if(!IsAlive())
        {
            // ...
            return;
        }
    }
}