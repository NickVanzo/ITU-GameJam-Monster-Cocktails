using System.Collections.Generic;
using UnityEngine;

public class HeldGun : HeldItem
{
    public HeldGun(GunObject frame, IWielder wielder) : base(frame, wielder)
    {
        m_Frame = frame;
        m_Gun = Loadout.Build(frame);
        m_Ammo = m_Gun.GetConstraint<AmmoConstraint>();
        m_Gun.OnFired += HandleFired;
        m_Gun.OnHit += Wielder.Hit;
        m_Gun.OnBlocked += m_Gun.Reload;
    }

    bool IsReloading => m_Ammo != null && m_Ammo.IsReloading;

    public override bool TakesMainButton => true;
    public override bool CanAim => !IsReloading;
    public override void Tick(in GunTrigger.TriggerInput input, in ShooterState shooter, float fDelta) => m_Gun.Tick(shooter, input, fDelta);
    public override void Reload() => m_Gun.Reload();
    public override string DebugPrint() => base.DebugPrint() + "\n" + m_Gun.DebugPrint();

    public override bool TryGetCounter(out int nCurrent, out int nMax)
    {
        nCurrent = m_Ammo != null ? m_Ammo.Rounds : 0;
        nMax = m_Ammo != null ? m_Ammo.Capacity : 0;
        return m_Ammo != null;
    }

    public override Quaternion GetPoseSpin(out Vector3 vPivot)
    {
        if(!IsReloading)
        {
            return base.GetPoseSpin(out vPivot);
        }

        vPivot = m_Frame.vReloadSpinPivot;
        float fProgress = m_Ammo.ReloadProgress;
        float fSpin = 360.0f * m_Frame.nReloadSpinTurns * m_Frame.ReloadSpinCurve.Evaluate(fProgress);
        float fYaw = m_Frame.fReloadSpinYaw * Mathf.Sin(fProgress * Mathf.PI);
        return Quaternion.AngleAxis(fYaw, Vector3.up) * Quaternion.AngleAxis(fSpin, m_Frame.vReloadSpinAxis);
    }

    void HandleFired()
    {
        Sfx.Play(m_Frame.Shot != null && m_Frame.Shot.Clips.Length > 0 ? m_Frame.Shot : Sfx.Sounds.GunShot);
        Wielder.Fired(this, m_Gun.Stats.Get(GunStat.Recoil, m_Frame.fBaseRecoil));
    }

    readonly GunObject m_Frame;
    readonly Gun m_Gun;
    readonly AmmoConstraint m_Ammo;
}

[CreateAssetMenu(menuName = "FPS/Gun")]
public class GunObject : EquippableObject
{
    public float fBaseRecoil = 0.8f; // Todo : Change this !!!
    public List<GunSlotDefinition> aSlots = new();
    [SerializeReference, SubclassPicker] public GunTrigger Trigger;
    [SerializeReference, SubclassPicker] public List<GunConstraint> aConstraints = new();
    [SerializeReference, SubclassPicker] public GunSpawner Spawner;
    public Sound Shot, DryFire, Reload;

    [Header("Reload Spin")]
    public AnimationCurve ReloadSpinCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
    [Tooltip("Negative turns spin the other way")]
    public int nReloadSpinTurns = 3;
    public Vector3 vReloadSpinAxis = Vector3.right;
    public Vector3 vReloadSpinPivot;
    public float fReloadSpinYaw = 25.0f;

    protected override HeldItem CreateHeld(IWielder wielder) => new HeldGun(this, wielder);
}
