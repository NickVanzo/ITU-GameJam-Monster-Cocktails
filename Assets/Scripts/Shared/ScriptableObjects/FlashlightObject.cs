using UnityEngine;

public class HeldFlashlight : HeldItem
{
    public HeldFlashlight(FlashlightObject flashlight, IWielder wielder) : base(flashlight, wielder)
    {
        m_Flashlight = flashlight;
    }

    public override void Use()
    {
        if(m_Beam != null)
        {
            m_Beam.enabled = !m_Beam.enabled;
        }
    }

    public override void Tick(in GunTrigger.TriggerInput input, in ShooterState shooter, float fDelta)
    {
        if(input.bPressed)
        {
            Use();
        }
    }

    protected override void OnAttach(Transform hand)
    {
        if(m_Beam == null)
        {
            m_Beam = View != null ? View.GetComponentInChildren<Light>(true) : null;
        }

        if(m_Beam == null)
        {
            m_Beam = CreateBeam();
        }

        if(View == null)
        {
            m_Beam.transform.SetParent(hand, false);
        }

        m_Beam.enabled = true;
    }

    protected override void OnDetach()
    {
        if(m_Beam != null)
        {
            m_Beam.enabled = false;
        }
    }

    Light CreateBeam()
    {
        Light beam = new GameObject("FlashlightBeam").AddComponent<Light>();
        beam.transform.SetParent(View != null ? View.transform : null, false);
        beam.type = LightType.Spot;
        beam.range = m_Flashlight.fRange;
        beam.spotAngle = m_Flashlight.fAngle;
        beam.innerSpotAngle = m_Flashlight.fAngle * 0.5f;
        beam.intensity = m_Flashlight.fIntensity;
        beam.color = m_Flashlight.BeamColor;
        beam.shadows = LightShadows.Soft;
        return beam;
    }

    readonly FlashlightObject m_Flashlight;
    Light m_Beam;
}

[CreateAssetMenu(menuName = "FPS/Flashlight")]
public class FlashlightObject : EquippableObject
{
    [Header("Beam")]
    public float fRange = 30.0f;
    public float fAngle = 55.0f;
    public float fIntensity = 4.0f;
    public Color BeamColor = Color.white;

    protected override HeldItem CreateHeld(IWielder wielder) => new HeldFlashlight(this, wielder);
}
