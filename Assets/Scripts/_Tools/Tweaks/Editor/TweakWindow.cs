using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class TweakWindow : EditorWindow
{
    Vector2 vScroll;

    [MenuItem("Window/Tweaks")]
    static void Open()
    {
        GetWindow<TweakWindow>("Tweaks");
    }

    [InitializeOnLoadMethod]
    static void LoadSaved()
    {
        foreach(FieldInfo field in Fields().Where(IsSaved))
        {
            try
            {
                field.SetValue(null, FromText(EditorPrefs.GetString(Key(field)), field.FieldType));
            }
            catch(Exception)
            {
                EditorPrefs.DeleteKey(Key(field));
            }
        }
    }

    void OnInspectorUpdate()
    {
        if(EditorApplication.isPlaying)
        {
            Repaint();
        }
    }

    void OnGUI()
    {
        var categories = Fields().Cast<MemberInfo>()
            .Concat(TypeCache.GetMethodsWithAttribute<TweakAttribute>().Where(method => method.IsStatic))
            .GroupBy(member => member.GetCustomAttribute<TweakAttribute>().sCategory ?? member.DeclaringType.Name)
            .OrderBy(category => category.Key);

        int nSaved = Fields().Count(IsSaved);
        using(new EditorGUI.DisabledScope(nSaved == 0))
        {
            if(GUILayout.Button($"Clear saved values ({nSaved})"))
            {
                foreach(FieldInfo field in Fields())
                {
                    EditorPrefs.DeleteKey(Key(field));
                }
            }
        }

        vScroll = EditorGUILayout.BeginScrollView(vScroll);
        foreach(IGrouping<string, MemberInfo> category in categories)
        {
            EditorGUILayout.LabelField(category.Key, EditorStyles.boldLabel);
            foreach(MemberInfo member in category)
            {
                if(member is FieldInfo field)
                {
                    DrawField(field);
                    continue;
                }

                using(new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
                {
                    if(GUILayout.Button(Label(member)))
                    {
                        ((MethodInfo)member).Invoke(null, null);
                    }
                }
            }
            
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();
    }

    static IEnumerable<FieldInfo> Fields()
    {
        return TypeCache.GetFieldsWithAttribute<TweakAttribute>().Where(field => field.IsStatic);
    }

    static string Key(FieldInfo field)
    {
        return $"Tweak.{PlayerSettings.productGUID}.{field.DeclaringType.FullName}.{field.Name}";
    }

    static bool IsSaved(FieldInfo field)
    {
        return EditorPrefs.HasKey(Key(field));
    }

    static void DrawField(FieldInfo field)
    {
        string sLabel = Label(field) + (IsSaved(field) ? " *" : "");
        EditorGUI.BeginChangeCheck();
        object value = DrawValue(sLabel, field.GetValue(null), field.GetCustomAttribute<TweakAttribute>());
        if(EditorGUI.EndChangeCheck())
        {
            field.SetValue(null, value);
            EditorPrefs.SetString(Key(field), ToText(value));
        }
    }

    static object DrawValue(string sLabel, object value, TweakAttribute attribute)
    {
        switch(value)
        {
            case float f:
                return attribute.HasRange ? EditorGUILayout.Slider(sLabel, f, attribute.fMin, attribute.fMax) : EditorGUILayout.FloatField(sLabel, f);
            case int n:
                return attribute.HasRange ? EditorGUILayout.IntSlider(sLabel, n, (int)attribute.fMin, (int)attribute.fMax) : EditorGUILayout.IntField(sLabel, n);
            case bool b:
                return EditorGUILayout.Toggle(sLabel, b);
            case string s:
                return EditorGUILayout.TextField(sLabel, s);
            case Vector2 v:
                return EditorGUILayout.Vector2Field(sLabel, v);
            case Vector3 v:
                return EditorGUILayout.Vector3Field(sLabel, v);
            case Color color:
                return EditorGUILayout.ColorField(sLabel, color);
            case Enum e:
                return EditorGUILayout.EnumPopup(sLabel, e);
            default:
                EditorGUILayout.LabelField(sLabel, value?.ToString() ?? "null");
                return value;
        }
    }

    static string ToText(object value)
    {
        return value is Vector2 or Vector3 or Color ? JsonUtility.ToJson(value) : Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    static object FromText(string sText, Type type)
    {
        if(type.IsEnum)
        {
            return Enum.Parse(type, sText);
        }
        if(type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Color))
        {
            return JsonUtility.FromJson(sText, type);
        }
        return Convert.ChangeType(sText, type, CultureInfo.InvariantCulture);
    }

    static string Label(MemberInfo member)
    {
        return ObjectNames.NicifyVariableName(Regex.Replace(member.Name, "^(s_|m_)?[a-z](?=[A-Z])", ""));
    }
}
