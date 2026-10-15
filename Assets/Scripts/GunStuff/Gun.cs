using System;
using System.Collections.Generic;
using UnityEngine;

public struct ShooterState
{
    public Vector3 vPos;
    public Vector3 vAim;
    public LayerMask HitMask;
    public IDamageable Owner;
}

public struct GunHit
{
    public Vector3 vPoint;
    public Vector3 vNormal;
    public IDamageable Target;    // null for walls, the shooter and dead targets
    public int nDamage;
    public bool bHeadshot;
}

public class Gun
{
    GunTrigger Trigger;
    GunSpawner Spawner;
    public event Action OnBlocked;
    public event Action OnFired;
    public event Action<GunHit> OnHit;
    Action<GunHit> m_HandleHit;

    List<GunConstraint> aConstraints = new();
    public GunStats Stats = new();

    public static Gun Build(GunObject frame, IReadOnlyList<GunAttachment> aAttachments)
    {
        Gun gun = new();
        gun.Trigger = (GunTrigger)frame.Trigger.Clone();
        gun.Spawner = (GunSpawner)frame.Spawner.Clone();

        foreach(GunConstraint constraint in frame.aConstraints)
        {
            gun.aConstraints.Add((GunConstraint)constraint.Clone());
        }

        if(aAttachments != null)
        {
            foreach(GunAttachment attachment in aAttachments)
            {
                foreach(StatChange change in attachment.aStatChanges)
                {
                    gun.Stats.Apply(change);
                }

                foreach(GunPart part in attachment.aParts)
                {
                    switch(part.Clone())
                    {
                        case GunTrigger trigger: 
                            gun.Trigger = trigger; 
                            break;

                        case GunSpawner spawner: 
                            gun.Spawner = spawner; 
                            break;

                        case GunConstraint constraint: 
                            gun.aConstraints.Add(constraint); 
                            break;
                    }
                }
            }   
        }

        gun.Trigger.Init(gun.Stats);
        gun.Spawner.Init(gun.Stats);
        foreach(var constraint in gun.aConstraints)
        {
            constraint.Init(gun.Stats);
        }

        return gun;
    }

    public Gun()
    {
        m_HandleHit = HandleHit;
    }

    void HandleHit(GunHit hit)
    {
        if(hit.Target != null)
        {
            hit.Target.TakeDamage(hit.bHeadshot ? hit.Target.Max : hit.nDamage);   // headshots kill for now
        }

        OnHit?.Invoke(hit);
    }

    public void Tick(ShooterState State, GunTrigger.TriggerInput Input, float fDelta)
    {
        int nWanted = Trigger.Pull(Input, Stats, fDelta);
        
        foreach(GunConstraint constraint in aConstraints)
        {
            constraint.Tick(Stats, fDelta);            
        } 

        for(int i = 0; i < nWanted; ++i)
        {
            if(!aConstraints.TrueForAll(c => c.CanFire(Stats))) 
            { 
                OnBlocked?.Invoke(); 
                break; 
            }

            foreach(GunConstraint constraint in aConstraints) 
            {
                constraint.OnFired(Stats);
            }

            Spawner.Spawn(State, Stats, m_HandleHit);
            OnFired?.Invoke();
        }
    }

    public void Reload()
    {
        foreach(GunConstraint constraint in aConstraints)
        {
            constraint.Reload();
        }
    }

}