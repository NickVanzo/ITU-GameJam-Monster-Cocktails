using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class Workbench : MonoBehaviour, IClickable
{
    const string BackLink = "back";

    public Player Player;
    public ViewSpot Spot;
    [Tooltip("The gun lying on the bench.")]
    public WorkbenchDisplay Display;

    [Tooltip("The WorkbenchPanel, only shown at the bench. Its arrows are clickable <link> tags, so keep it on a Screen Space canvas.")]
    public GameObject UIWindow;
    [SerializeField] Color SelectedColor = new(1.0f, 0.82f, 0.3f);

    WorkbenchPanel m_Panel;
    GunObject m_Frame;
    int m_nRow;

    public bool IsOpen()
    {
        return Player.IsEngagedAt(Spot);
    }

    int GetRowCount()
    {
        return m_Frame != null ? m_Frame.aSlots.Count : 0;
    }

    void Awake()
    {
        m_Panel = UIWindow.GetComponent<WorkbenchPanel>();
        UIWindow.SetActive(false);
    }

    void Start()
    {
        m_Frame = Loadout.Frame;
        Display.Show(m_Frame);
    }

    public void Click(Player player)
    {
        Open(m_Frame);
    }

    public void Open(GunObject frame)
    {
        m_Frame = frame;
        Display.Show(frame);
        Player.Engage(Spot);
    }

    void Update()
    {
        bool bOpen = IsOpen();

        if(UIWindow.activeSelf != bOpen)
        {
            UIWindow.SetActive(bOpen);
            m_nRow = 0;
            Refresh();
        }

        if(!bOpen)
        {
            return;
        }

        bool bUp = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        bool bDown = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
        if(bUp != bDown && GetRowCount() > 0)
        {
            m_nRow = (m_nRow + (bDown ? 1 : -1) + GetRowCount()) % GetRowCount();
            Refresh();
        }

        bool bLeft = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
        bool bRight = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
        if(bLeft != bRight)
        {
            Change(m_nRow, bRight ? 1 : -1);
        }

        if(Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    void HandleClick()
    {
        string sLink = m_Panel.FindLinkUnderCursor();
        if(sLink == null)
        {
            return;
        }

        if(sLink == BackLink)
        {
            Player.Move(BarMovementDirections.Down);
            return;
        }

        // Arrows are "<row>:<step>".
        string[] aLink = sLink.Split(':');
        m_nRow = int.Parse(aLink[0]);
        Change(m_nRow, int.Parse(aLink[1]));
    }

    void Change(int nRow, int nStep)
    {
        if(nRow >= GetRowCount())
        {
            return;
        }

        GunSlotDefinition slot = m_Frame.aSlots[nRow];
        List<GunAttachment> aChoices = slot.GetChoices();

        if(aChoices.Count > 1)
        {
            int nIndex = aChoices.IndexOf(Loadout.GetFitted(m_Frame, slot));
            Loadout.Fit(m_Frame, slot.eSlot, aChoices[(nIndex + nStep + aChoices.Count) % aChoices.Count]);
            Display.Pulse();
            Sfx.Play(Sfx.Sounds.GunSelect);
        }

        Refresh();
    }

    void Refresh()
    {
        m_Panel.GunName.text = m_Frame != null ? m_Frame.sDisplayName.ToUpper() : "NO GUN";

        var slots = new StringBuilder();
        var description = new StringBuilder();
        for(int i = 0; i < GetRowCount(); i++)
        {
            GunAttachment fitted = Loadout.GetFitted(m_Frame, m_Frame.aSlots[i]);
            AppendRow(slots, i, Nicify(m_Frame.aSlots[i].eSlot.ToString()), fitted != null ? fitted.DisplayName.ToUpper() : "NONE");
        }

        GunAttachment selected = m_nRow < GetRowCount() ? Loadout.GetFitted(m_Frame, m_Frame.aSlots[m_nRow]) : null;
        if(selected != null)
        {
            foreach(StatChange change in selected.aStatChanges)
            {
                description.Append(Describe(change)).Append('\n');
            }
        }

        m_Panel.Slots.text = slots.ToString();
        m_Panel.Description.text = description.ToString();
        m_Panel.ShowStats(m_Frame != null ? Loadout.Build(m_Frame) : null);
    }

    void AppendRow(StringBuilder builder, int nRow, string sLabel, string sValue)
    {
        bool bSelected = nRow == m_nRow;
        string sRow = (bSelected ? "> " : "  ") + sLabel + "<pos=40%>" + Arrows(nRow, sValue);
        builder.Append(Highlight(sRow, bSelected)).Append('\n');
    }

    // "[<] value [>]", the arrows step that row back and forth when clicked.
    static string Arrows(int nRow, string sValue)
    {
        return $"<link=\"{nRow}:-1\">[<noparse><</noparse>]</link> {sValue} <link=\"{nRow}:1\">[>]</link>";
    }

    string Highlight(string sText, bool bSelected)
    {
        return bSelected ? $"<color=#{ColorUtility.ToHtmlStringRGB(SelectedColor)}>{sText}</color>" : sText;
    }

    // "MAGAZINE SIZE +6", "RELOAD TIME -20%"..
    static string Describe(StatChange change)
    {
        string sChange = change.fAdd != 0.0f ? change.fAdd.ToString("+0.##;-0.##") : "";
        if(change.fPercent != 0.0f)
        {
            sChange += (sChange.Length > 0 ? " " : "") + change.fPercent.ToString("+0.##;-0.##") + "%";
        }

        return $"{Nicify(change.eStat.ToString())} {sChange}";
    }

    public static string Nicify(string sName)
    {
        return Regex.Replace(sName, "(?<=[a-z])(?=[A-Z])", " ").ToUpper();
    }
}
