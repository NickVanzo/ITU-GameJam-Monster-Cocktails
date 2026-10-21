using System.Linq;
using UnityEngine;

public class HeldPhone : HeldItem
{
    readonly PhoneObject m_Phone;
    Transform m_Lid;
    Vector3 m_vLidPosition;
    Quaternion m_qLidRotation;
    Vector3 m_vHinge;
    Vector3 m_vHingeAxis;
    bool m_bOpen;
    float m_fOpen;

    public HeldPhone(PhoneObject phone, IWielder wielder) : base(phone, wielder)
    {
        m_Phone = phone;
    }

    public override bool TakesMainButton => true;

    public override void Use()
    {
        m_bOpen = !m_bOpen;
        Sfx.Play(m_bOpen ? m_Phone.OpenSound : m_Phone.CloseSound);
    }

    public override void Tick(in GunTrigger.TriggerInput input, in ShooterState shooter, float fDelta)
    {
        if(input.bPressed)
        {
            Use();
        }

        if(m_Lid == null)
        {
            return;
        }

        m_fOpen = Mathf.MoveTowards(m_fOpen, m_bOpen ? 1.0f : 0.0f, fDelta / m_Phone.fFlipTime);
        Quaternion qFold = Quaternion.AngleAxis(Mathf.SmoothStep(m_Phone.fClosedAngle, 0.0f, m_fOpen), m_vHingeAxis);
        m_Lid.SetLocalPositionAndRotation(m_vHinge + qFold * (m_vLidPosition - m_vHinge), qFold * m_qLidRotation);
    }

    protected override void OnAttach(Transform hand)
    {
        if(m_Lid != null || View == null)
        {
            return;
        }

        // todo : this is gay af
        Transform[] aParts = View.GetComponentsInChildren<Transform>(true);
        Transform lid = aParts.FirstOrDefault(t => t.name == m_Phone.sLidName);
        Transform hinge = aParts.FirstOrDefault(t => t.name == m_Phone.sHingeName);

        m_Lid = lid;
        m_Lid.GetLocalPositionAndRotation(out m_vLidPosition, out m_qLidRotation);
        m_vHinge = m_Lid.parent.InverseTransformPoint(hinge.TryGetComponent(out MeshFilter mesh) ? hinge.TransformPoint(mesh.sharedMesh.bounds.center) : hinge.position);
        m_vHingeAxis = m_Lid.parent.InverseTransformDirection(hinge.right);
    }
}

[CreateAssetMenu(menuName = "FPS/Phone")]
public class PhoneObject : EquippableObject
{
    [Header("Flip")]
    public string sLidName = "Phone_top";
    public string sHingeName = "Middle_Hinge";
    [Tooltip("Lid rotation around the hinge when closed, from the model's authored (open) pose.. for now")]
    public float fClosedAngle = 150.0f;
    public float fFlipTime = 0.2f;
    public Sound OpenSound = new();
    public Sound CloseSound = new();

    protected override HeldItem CreateHeld(IWielder wielder) => new HeldPhone(this, wielder);
}
