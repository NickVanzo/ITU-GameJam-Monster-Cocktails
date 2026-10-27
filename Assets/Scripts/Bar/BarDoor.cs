using TMPro;
using UnityEngine;

public class BarDoor : MonoBehaviour
{
    public Transform Hinge;
    public TMP_Text SignText;

    [SerializeField] float fOpenAngle = -90.0f;
    [Tooltip("Degrees per second.")]
    [SerializeField] float fSwingSpeed = 240.0f;
    [SerializeField] Color OpenColor = new(0.4f, 1.0f, 0.45f);
    [SerializeField] Color ClosedColor = new(1.0f, 0.3f, 0.25f);

    Quaternion m_ClosedRotation;
    bool bSignOpen;
    bool bSwungOpen;

    void Awake()
    {
        m_ClosedRotation = Hinge.localRotation;
    }

    void Start()
    {
        bSignOpen = TimeManager.IsBarOpen;
        bSwungOpen = WantsOpen();
        Hinge.localRotation = TargetRotation();
        RefreshSign();
    }

    void Update()
    {
        if(bSignOpen != TimeManager.IsBarOpen)
        {
            bSignOpen = !bSignOpen;
            RefreshSign();
        }

        if(bSwungOpen != WantsOpen())
        {
            bSwungOpen = !bSwungOpen;
            Sfx.Play(bSwungOpen ? Sfx.Sounds.DoorOpen : Sfx.Sounds.DoorClose, Hinge.position);
        }

        Hinge.localRotation = Quaternion.RotateTowards(Hinge.localRotation, TargetRotation(), fSwingSpeed * Time.deltaTime);
    }

    public void Toggle()
    {
        TimeManager time = TimeManager.Instance;
        if(time == null)
        {
            Debug.LogWarning("BarDoor: no TimeManager, the door only works when playing from the Game scene.", this);
            return;
        }

        bool bDone = TimeManager.IsBarOpen ? time.CloseBar() : time.OpenBar();
        if(!bDone)
        {
            Sfx.Play(Sfx.Sounds.OutOfStock);
        }
    }

    bool WantsOpen()
    {
        return TimeManager.IsBarOpen || (CustomerManager.Instance != null && CustomerManager.Instance.HasCustomerInside);
    }

    void RefreshSign()
    {
        SignText.text = bSignOpen ? "OPEN" : "CLOSED";
        SignText.color = bSignOpen ? OpenColor : ClosedColor;
    }

    Quaternion TargetRotation()
    {
        return bSwungOpen ? m_ClosedRotation * Quaternion.Euler(0.0f, fOpenAngle, 0.0f) : m_ClosedRotation;
    }
}
