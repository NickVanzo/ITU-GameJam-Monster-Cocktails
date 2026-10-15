using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Vibecoded af
// Draws a [SerializeReference, SubclassPicker] field as a class dropdown, followed by the picked class's fields.
[CustomPropertyDrawer(typeof(SubclassPickerAttribute))]
public class SubclassPickerDrawer : PropertyDrawer
{
    static readonly Dictionary<string, Type[]> s_Choices = new();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if(property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.LabelField(position, label.text, "[SubclassPicker] needs [SerializeReference]");
            return;
        }

        EditorGUI.BeginProperty(position, label, property);

        float fLine = EditorGUIUtility.singleLineHeight;
        float fLabelWidth = EditorGUIUtility.labelWidth;
        Rect labelRect = new(position.x, position.y, fLabelWidth, fLine);
        Rect buttonRect = new(position.x + fLabelWidth + 2.0f, position.y, position.width - fLabelWidth - 2.0f, fLine);

        Type current = ParseType(property.managedReferenceFullTypename);
        string sCurrent = current == null ? "None" : ObjectNames.NicifyVariableName(current.Name);

        // The button goes first so it gets the click before the foldout next to it.
        if(EditorGUI.DropdownButton(buttonRect, new GUIContent(sCurrent), FocusType.Keyboard))
        {
            ShowMenu(property, current, buttonRect);
        }

        bool bHasFields = current != null && property.hasVisibleChildren;
        if(bHasFields)
        {
            property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);
        }
        else
        {
            EditorGUI.LabelField(labelRect, label);
        }

        if(bHasFields && property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float fY = position.y + fLine + EditorGUIUtility.standardVerticalSpacing;
            foreach(SerializedProperty child in Children(property))
            {
                float fHeight = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(new Rect(position.x, fY, position.width, fHeight), child, true);
                fY += fHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float fHeight = EditorGUIUtility.singleLineHeight;
        if(property.propertyType != SerializedPropertyType.ManagedReference
            || string.IsNullOrEmpty(property.managedReferenceFullTypename)
            || !property.isExpanded)
        {
            return fHeight;
        }

        foreach(SerializedProperty child in Children(property))
        {
            fHeight += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(child, true);
        }
        return fHeight;
    }

    static void ShowMenu(SerializedProperty property, Type current, Rect buttonRect)
    {
        // The menu callbacks run after this OnGUI is over, when the property may no longer be valid,
        // so they find it again from its object and path.
        SerializedObject serializedObject = property.serializedObject;
        string sPath = property.propertyPath;

        GenericMenu menu = new();
        menu.AddItem(new GUIContent("None"), current == null, () => Assign(serializedObject, sPath, null));
        menu.AddSeparator("");
        foreach(Type type in Choices(property.managedReferenceFieldTypename))
        {
            menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), type == current, () => Assign(serializedObject, sPath, type));
        }
        menu.DropDown(buttonRect);
    }

    static void Assign(SerializedObject serializedObject, string sPath, Type type)
    {
        serializedObject.Update();
        SerializedProperty property = serializedObject.FindProperty(sPath);
        property.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);
        property.isExpanded = type != null;
        serializedObject.ApplyModifiedProperties();   // also records the undo step
    }

    // Every class that can go in the field: concrete, [Serializable], with a parameterless constructor.
    static Type[] Choices(string sFieldTypename)
    {
        if(s_Choices.TryGetValue(sFieldTypename, out Type[] aTypes))
        {
            return aTypes;
        }

        Type baseType = ParseType(sFieldTypename);
        if(baseType == null)
        {
            aTypes = new Type[0];
        }
        else
        {
            aTypes = TypeCache.GetTypesDerivedFrom(baseType)
                .Append(baseType)
                .Where(type => !type.IsAbstract
                    && !type.IsGenericType
                    && !typeof(UnityEngine.Object).IsAssignableFrom(type)
                    && type.IsDefined(typeof(SerializableAttribute), false)
                    && type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(type => type.Name)
                .ToArray();
        }

        s_Choices[sFieldTypename] = aTypes;
        return aTypes;
    }

    // Unity names managed reference types as "Assembly Namespace.Type".
    static Type ParseType(string sTypename)
    {
        int nSpace = string.IsNullOrEmpty(sTypename) ? -1 : sTypename.IndexOf(' ');
        if(nSpace < 0)
        {
            return null;
        }

        return Type.GetType($"{sTypename.Substring(nSpace + 1)}, {sTypename.Substring(0, nSpace)}");
    }

    static IEnumerable<SerializedProperty> Children(SerializedProperty property)
    {
        SerializedProperty child = property.Copy();
        SerializedProperty end = property.GetEndProperty();
        bool bEnterChildren = true;
        while(child.NextVisible(bEnterChildren) && !SerializedProperty.EqualContents(child, end))
        {
            bEnterChildren = false;
            yield return child;
        }
    }
}
