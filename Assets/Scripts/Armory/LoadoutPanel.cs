using TMPro;
using UnityEngine;

public class LoadoutPanel : MonoBehaviour
{
    [Tooltip("What is packed in the bag.")]
    public TMP_Text Contents;
    public TMP_Text Exit;

    public string FindLinkUnderCursor()
    {
        return FindLink(Exit);
    }

    static string FindLink(TMP_Text text)
    {
        Camera camera = text.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : text.canvas.worldCamera;
        int nLink = TMP_TextUtilities.FindIntersectingLink(text, Input.mousePosition, camera);
        return nLink >= 0 ? text.textInfo.linkInfo[nLink].GetLinkID() : null;
    }
}
