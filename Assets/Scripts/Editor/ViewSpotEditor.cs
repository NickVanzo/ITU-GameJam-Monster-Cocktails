using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ViewSpot))]
public class ViewSpotEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        using(new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if(GUILayout.Button("Preview Camera Position"))
            {
                PreviewCamera((ViewSpot)target);
            }
        }
    }

    public static void PreviewCamera(ViewSpot spot)
    {
        Camera camera = Camera.main;
        if(spot == null || camera == null)
        {
            return;
        }

        Undo.RecordObject(camera.transform, "Preview Camera Position");
        camera.transform.SetPositionAndRotation(spot.Anchor.position, spot.Anchor.rotation);
    }
}
