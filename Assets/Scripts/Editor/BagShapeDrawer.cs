using UnityEditor;
using UnityEngine;

// Draws a BagShape as a grid of toggles, laid out the way the loadout camera sees the item.
[CustomPropertyDrawer(typeof(BagShape))]
public class BagShapeDrawer : PropertyDrawer
{
    const float CellSize = 18.0f;
    const int MaxSize = 16;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int nRows = Mathf.Max(0, property.FindPropertyRelative("vSize").vector2IntValue.y);
        return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + nRows * CellSize;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty size = property.FindPropertyRelative("vSize");
        SerializedProperty cells = property.FindPropertyRelative("aCells");

        EditorGUI.BeginProperty(position, label, property);
        Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        Rect fields = EditorGUI.PrefixLabel(line, label);

        int nIndent = EditorGUI.indentLevel;
        float fLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUI.indentLevel = 0;
        EditorGUIUtility.labelWidth = 14.0f;

        Vector2Int vSize = size.vector2IntValue;
        float fHalf = fields.width * 0.5f;
        EditorGUI.BeginChangeCheck();
        int nWidth = EditorGUI.IntField(new Rect(fields.x, fields.y, fHalf - 2.0f, fields.height), "W", vSize.x);
        int nHeight = EditorGUI.IntField(new Rect(fields.x + fHalf, fields.y, fHalf, fields.height), "H", vSize.y);
        if(EditorGUI.EndChangeCheck())
        {
            Resize(size, cells, new Vector2Int(Mathf.Clamp(nWidth, 0, MaxSize), Mathf.Clamp(nHeight, 0, MaxSize)));
            vSize = size.vector2IntValue;
        }

        float fTop = line.yMax + EditorGUIUtility.standardVerticalSpacing;
        for(int y = 0; y < vSize.y; y++)
        {
            for(int x = 0; x < vSize.x; x++)
            {
                int i = y * vSize.x + x;
                if(i < cells.arraySize)
                {
                    SerializedProperty cell = cells.GetArrayElementAtIndex(i);
                    cell.boolValue = EditorGUI.Toggle(new Rect(fields.x + x * CellSize, fTop + y * CellSize, CellSize, CellSize), cell.boolValue);
                }
            }
        }

        EditorGUIUtility.labelWidth = fLabelWidth;
        EditorGUI.indentLevel = nIndent;
        EditorGUI.EndProperty();
    }

    // Cells that are still inside the new size keep their tick, new ones start ticked.
    static void Resize(SerializedProperty size, SerializedProperty cells, Vector2Int vNew)
    {
        Vector2Int vOld = size.vector2IntValue;
        var aOld = new bool[cells.arraySize];
        for(int i = 0; i < aOld.Length; i++)
        {
            aOld[i] = cells.GetArrayElementAtIndex(i).boolValue;
        }

        size.vector2IntValue = vNew;
        cells.arraySize = vNew.x * vNew.y;
        for(int y = 0; y < vNew.y; y++)
        {
            for(int x = 0; x < vNew.x; x++)
            {
                int nOld = y * vOld.x + x;
                bool bKept = x < vOld.x && y < vOld.y && nOld < aOld.Length;
                cells.GetArrayElementAtIndex(y * vNew.x + x).boolValue = !bKept || aOld[nOld];
            }
        }
    }
}
