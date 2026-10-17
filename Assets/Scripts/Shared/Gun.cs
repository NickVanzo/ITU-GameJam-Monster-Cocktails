using System;
using System.Collections.Generic;
using System.Text;
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
    GunObject Frame;
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
        gun.Frame = frame;
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
                    if(part == null)   // an entry added in the inspector without picking a type
                    {
                        continue;
                    }

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

    public T GetConstraint<T>() where T : GunConstraint
    {
        foreach(GunConstraint constraint in aConstraints)
        {
            if(constraint is T found)
            {
                return found;
            }
        }

        return null;
    }

    // That stat with attachments applied, false if nothing on this gun uses it (e.g. MagazineSize without an AmmoConstraint).
    public bool TryGetStat(GunStat eStat, out float fValue)
    {
        if(eStat == GunStat.Recoil)   // the frame's, not a part's
        {
            fValue = Stats.Get(GunStat.Recoil, Frame.fBaseRecoil);
            return true;
        }

        if(Trigger.TryGetStat(eStat, Stats, out fValue) || Spawner.TryGetStat(eStat, Stats, out fValue))
        {
            return true;
        }

        foreach(GunConstraint constraint in aConstraints)
        {
            if(constraint.TryGetStat(eStat, Stats, out fValue))
            {
                return true;
            }
        }

        return false;
    }

    public string DebugPrint()
    {
        var builder = new StringBuilder(Trigger.DebugPrint(Stats));
        foreach(GunConstraint constraint in aConstraints)
        {
            builder.Append('\n').Append(constraint.DebugPrint(Stats));
        }
        builder.Append('\n').Append(Spawner.DebugPrint(Stats));

        return builder.ToString();
    }

}