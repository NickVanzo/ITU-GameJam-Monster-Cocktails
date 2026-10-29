using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ArmoryItem)), CanEditMultipleObjects]
public class ArmoryItemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        using(new EditorGUI.DisabledScope(Application.isPlaying))
        {
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("Preview"))
                {
                    foreach(ArmoryItem item in targets)
                    {
                        Clear(item);
                        Preview(item);
                    }
                }

                if(GUILayout.Button("Clear"))
                {
                    foreach(ArmoryItem item in targets)
                    {
                        Clear(item);
                    }
                }
            }

            if(GUILayout.Button("Preview Camera Position"))
            {
                ArmoryItem item = (ArmoryItem)target;
                Clear(item);
                Preview(item);
                ViewSpotEditor.PreviewCamera(item.Spot);
            }
        }
    }

    static void Preview(ArmoryItem item)
    {
        if(item.Item == null || item.Item.PresentationModel == null)
        {
            return;
        }

        foreach(Transform part in item.Item.SpawnPresentation(item.Anchor).GetComponentsInChildren<Transform>(true))
        {
            part.gameObject.hideFlags = HideFlags.DontSave;
        }
    }

    static void Clear(ArmoryItem item)
    {
        for(int i = item.Anchor.childCount - 1; i >= 0; i--)
        {
            GameObject child = item.Anchor.GetChild(i).gameObject;
            if(child.hideFlags == HideFlags.DontSave)
            {
                DestroyImmediate(child);
            }
        }
    }
}
