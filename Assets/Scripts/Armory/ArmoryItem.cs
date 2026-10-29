using UnityEngine;

public class ArmoryItem : MonoBehaviour, IClickable
{
    static readonly int TintId = Shader.PropertyToID("_Tint");
    static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");

    public EquippableObject Item;
    public ViewSpot Spot;
    [Tooltip("The model is spawned under this and turned with it.")]
    public Transform ModelAnchor;
    public bool bUnlocked = true;
    [SerializeField] Color CarriedTint = new(0.3f, 0.3f, 0.3f);
    [SerializeField] Color UnfitTint = new(0.9f, 0.2f, 0.15f);

    enum State
    {
        Free,
        Carried,
        Unfit,
        Locked
    }

    Renderer[] m_aRenderers;
    MaterialPropertyBlock m_Block;
    State m_eState;

    public Transform Anchor => ModelAnchor != null ? ModelAnchor : transform;

    void Awake()
    {
        if(Item.PresentationModel != null)
        {
            Item.SpawnPresentation(Anchor);
        }

        m_aRenderers = GetComponentsInChildren<Renderer>(true);
        m_Block = new MaterialPropertyBlock();

        if(GetComponentInChildren<Collider>() == null)
        {
            Bounds bounds = MeshUtil.MeshBounds(transform, transform);
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
        }
    }

    void Update()
    {
        State eState = GetState();
        if(eState != m_eState)
        {
            m_eState = eState;
            Refresh();
        }
    }

    public void Click(Player player)
    {
        player.GoTo(Spot);
    }

    State GetState()
    {
        if(!bUnlocked)
        {
            return State.Locked;
        }

        if(Loadout.Unfit.Contains(Item))
        {
            return State.Unfit;
        }

        return Loadout.IsCarried(Item) ? State.Carried : State.Free;
    }

    void Refresh()
    {
        m_Block.Clear();
        switch(m_eState)
        {
            case State.Locked:
                m_Block.SetColor(TintId, Color.black);
                m_Block.SetColor(EmissiveColorId, Color.black);
                break;
            case State.Unfit:
                m_Block.SetColor(TintId, UnfitTint);
                m_Block.SetColor(EmissiveColorId, UnfitTint);
                break;
            case State.Carried:
                m_Block.SetColor(TintId, CarriedTint);
                break;
        }

        foreach(Renderer renderer in m_aRenderers)
        {
            renderer.SetPropertyBlock(m_Block.isEmpty ? null : m_Block);
        }
    }
}
