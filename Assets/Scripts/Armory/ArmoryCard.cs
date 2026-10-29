using System.Text;
using UnityEngine;

public class ArmoryCard : MonoBehaviour
{
    const string AddLink = "add";
    const string RemoveLink = "remove";
    const string ModifyLink = "modify";

    public Player Player;
    public LoadoutBench Bag;
    public Workbench Workbench;

    [Tooltip("A WorkbenchPanel, shown while looking at an item up close. Its actions are clickable <link> tags, so keep it on a Screen Space canvas.")]
    public GameObject UIWindow;

    WorkbenchPanel m_Panel;
    ArmoryItem[] m_aItems;
    ArmoryItem m_Shown;

    void Awake()
    {
        m_Panel = UIWindow.GetComponent<WorkbenchPanel>();
        m_aItems = FindObjectsByType<ArmoryItem>(FindObjectsSortMode.None);
        UIWindow.SetActive(false);
    }

    void Update()
    {
        ArmoryItem shown = System.Array.Find(m_aItems, item => item.Spot == Player.CurrentSpot);
        if(shown != m_Shown)
        {
            m_Shown = shown;
            UIWindow.SetActive(shown != null);
            if(shown != null)
            {
                Refresh();
            }
            return;
        }

        if(m_Shown != null && Input.GetMouseButtonDown(0))
        {
            HandleClick(m_Panel.FindLinkUnderCursor());
        }
    }

    void HandleClick(string sLink)
    {
        if(!m_Shown.bUnlocked)
        {
            return;
        }

        EquippableObject item = m_Shown.Item;
        bool bCarried = Loadout.IsCarried(item);

        if(sLink == AddLink && !bCarried)
        {
            Bag.Pack(item);
        }
        else if(sLink == RemoveLink && bCarried)
        {
            Loadout.Remove(item);
            Refresh();
        }
        else if(sLink == ModifyLink && item is GunObject frame)
        {
            Workbench.Open(frame);
        }
    }

    void Refresh()
    {
        EquippableObject item = m_Shown.Item;
        GunObject frame = item as GunObject;

        var slots = new StringBuilder();
        if(frame != null)
        {
            foreach(GunSlotDefinition slot in frame.aSlots)
            {
                GunAttachment fitted = Loadout.GetFitted(frame, slot);
                slots.Append("  ").Append(Workbench.Nicify(slot.eSlot.ToString())).Append("<pos=40%>").Append(fitted != null ? fitted.DisplayName.ToUpper() : "NONE").Append('\n');
            }
        }

        m_Panel.GunName.text = item.sDisplayName.ToUpper();
        m_Panel.Slots.text = slots.ToString();
        m_Panel.Description.text = !m_Shown.bUnlocked ? "LOCKED" : Loadout.IsCarried(item) ? "IN THE BAG" : "";
        m_Panel.Exit.text = Actions(item, frame);
        m_Panel.ShowStats(frame != null ? Loadout.Build(frame) : null);
    }

    string Actions(EquippableObject item, GunObject frame)
    {
        if(!m_Shown.bUnlocked)
        {
            return "";
        }

        string sActions = Loadout.IsCarried(item) ? Link(RemoveLink, "REMOVE") : Link(AddLink, "ADD TO LOADOUT");
        return frame != null ? sActions + "   " + Link(ModifyLink, "MODIFY") : sActions;
    }

    static string Link(string sId, string sLabel)
    {
        return $"<link=\"{sId}\">[{sLabel}]</link>";
    }
}
