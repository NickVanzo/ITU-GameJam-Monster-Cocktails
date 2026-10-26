using System.Text;
using UnityEngine;

public class LoadoutBench : MonoBehaviour
{
    const string BackLink = "back";

    public Player Player;

    [Tooltip("The LoadoutPanel, only shown at the bench. Its back button is a clickable <link> tag, so keep it on a Screen Space canvas.")]
    public GameObject UIWindow;

    LoadoutPanel m_Panel;

    public bool IsOpen()
    {
        return Player.CurrentRoom == Rooms.Loadout;
    }

    void Awake()
    {
        m_Panel = UIWindow.GetComponent<LoadoutPanel>();
        UIWindow.SetActive(false);
    }

    public void Open()
    {
        if(IsOpen())
        {
            return;
        }

        Player.GoToRoom(Rooms.Loadout);
        Refresh();
    }

    public void Close()
    {
        if(IsOpen())
        {
            Player.GoToRoom(Rooms.Kitchen);
        }
    }

    void Update()
    {
        bool bOpen = IsOpen();

        if(UIWindow.activeSelf != bOpen)
        {
            UIWindow.SetActive(bOpen);
        }

        if(!bOpen)
        {
            return;
        }

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
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
