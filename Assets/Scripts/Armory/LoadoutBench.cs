using System.Text;
using UnityEngine;

public class LoadoutBench : MonoBehaviour, IClickable
{
    const string BackLink = "back";

    public Player Player;
    public ViewSpot Spot;

    [Tooltip("The LoadoutPanel, only shown at the bench. Its back button is a clickable <link> tag, so keep it on a Screen Space canvas.")]
    public GameObject UIWindow;

    LoadoutPanel m_Panel;
    LoadoutConstraint m_Bag;

    public bool IsOpen()
    {
        return Player.IsEngagedAt(Spot);
    }

    void Awake()
    {
        m_Panel = UIWindow.GetComponent<LoadoutPanel>();
        m_Bag = GetComponentInChildren<LoadoutConstraint>();
        UIWindow.SetActive(false);
    }

    public void Click(Player player)
    {
        player.Engage(Spot);
    }

    public void Pack(EquippableObject item)
    {
        Loadout.Add(item);
        m_Bag.Hold(item);
        Player.Engage(Spot);
    }

    public void Close()
    {
        Player.Move(BarMovementDirections.Down);
    }

    void Update()
    {
        bool bOpen = IsOpen();

        if(UIWindow.activeSelf != bOpen)
        {
            UIWindow.SetActive(bOpen);
            Refresh();
        }

        if(!bOpen)
        {
            return;
        }

        if(Input.GetMouseButtonDown(0) && m_Panel.FindLinkUnderCursor() == BackLink)
        {
            Close();
        }
    }

    // What goes down the elevator, until the bag gets packed by hand.
    void Refresh()
    {
        var contents = new StringBuilder();
        AppendRow(contents, "GUN", Loadout.Frame != null ? Loadout.Frame.sDisplayName.ToUpper() : "NONE");

        foreach(EquippableObject item in Loadout.Offhand)
        {
            AppendRow(contents, "OFFHAND", item.sDisplayName.ToUpper());
        }

        m_Panel.Contents.text = contents.ToString();
    }

    static void AppendRow(StringBuilder builder, string sLabel, string sValue)
    {
        builder.Append("  ").Append(sLabel).Append("<pos=40%>").Append(sValue).Append('\n');
    }
}
