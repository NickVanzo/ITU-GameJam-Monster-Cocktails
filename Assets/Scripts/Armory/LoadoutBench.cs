using System.Text;
using UnityEngine;

public class LoadoutBench : MonoBehaviour
{
    const string BackLink = "back";

    public Player Player;
    public ViewSpot Spot;
    public ArmoryDisplay Rack;

    [Tooltip("The LoadoutPanel, only shown at the bench. Its back button is a clickable <link> tag, so keep it on a Screen Space canvas.")]
    public GameObject UIWindow;

    LoadoutPanel m_Panel;

    public bool IsOpen()
    {
        return Player.IsEngagedAt(Spot);
    }

    void Awake()
    {
        m_Panel = UIWindow.GetComponent<LoadoutPanel>();
        UIWindow.SetActive(false);
    }

    public void Open()
    {
        Player.Engage(Spot);
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

        bool bUp = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        bool bDown = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
        if(bUp != bDown)
        {
            Rack.Step(bDown ? 1 : -1);
            Refresh();
        }

        if(Input.GetMouseButtonDown(0) && m_Panel.FindLinkUnderCursor() == BackLink)
        {
            Player.Move(BarMovementDirections.Down);
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
