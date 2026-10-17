using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorkbenchPanel : MonoBehaviour
{
    [Serializable]
    public class StatBar
    {
        public GunStat eStat;
        public Slider Bar;
        [Tooltip("Value that fills the bar.")]
        public float fMax = 1.0f;
    }

    [Tooltip("The gun's name, with arrows to switch guns.")]
    public TMP_Text GunName;
    [Tooltip("One row per attachment slot.")]
    public TMP_Text Slots;
    [Tooltip("What the attachment on the selected row does.")]
    public TMP_Text Description;
    public TMP_Text Exit;
    public StatBar[] aStatBars;

    public void ShowStats(Gun gun)
    {
        foreach(StatBar bar in aStatBars)
        {
            float fValue = 0.0f;
            bool bHasStat = gun != null && gun.TryGetStat(bar.eStat, out fValue);
            bar.Bar.value = bHasStat ? Mathf.Clamp01(fValue / bar.fMax) : 0.0f;
        }
    }

    // TODO : Frail...
    public string FindLinkUnderCursor()
    {
        return FindLink(GunName) ?? FindLink(Slots) ?? FindLink(Exit);
    }

    static string FindLink(TMP_Text text)
    {
        Camera camera = text.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : text.canvas.worldCamera;
        int nLink = TMP_TextUtilities.FindIntersectingLink(text, Input.mousePosition, camera);
        return nLink >= 0 ? text.textInfo.linkInfo[nLink].GetLinkID() : null;
    }
}
