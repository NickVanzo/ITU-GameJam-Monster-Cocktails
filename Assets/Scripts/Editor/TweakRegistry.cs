using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Everything marked [Tweak], and the overrides set on it from Window > Tweaks.
// Overrides are kept in EditorPrefs and written into their fields after every domain reload and when play mode starts,
// before any Awake. When play mode ends the fields go back to their start values, like scene objects do.
[InitializeOnLoad]
public static class TweakRegistry
{
    public class Entry
    {
        public string sCategory;
        public GUIContent Label;
        public TweakAttribute Attribute;
        public MemberInfo Member;
        public FieldInfo Field => Member as FieldInfo;      // null for a button
        public MethodInfo Method => Member as MethodInfo;   // null for a value
        public string sProblem;                             // why it can't be shown, when it can't
        public bool bEditable;                              // a writable field, so it can be overridden
        public object Default;                              // the field's value before any override
        public string sKey;
        public bool bOverridden;
    }

    static readonly Type[] s_aStorableTypes = { typeof(bool), typeof(int), typeof(float), typeof(string), typeof(Vector2), typeof(Vector3), typeof(Color) };

    // Sorted by category, then values before buttons, in declaration order.
    public static readonly List<Entry> Entries;

    static TweakRegistry()
    {
        string sPrefix = $"Tweaks.{PlayerSettings.productGUID}.";
        Entries = TypeCache.GetFieldsWithAttribute<TweakAttribute>().Cast<MemberInfo>()
            .Concat(TypeCache.GetMethodsWithAttribute<TweakAttribute>())
            .Select(member => Create(member, sPrefix))
            .OrderBy(entry => entry.sCategory)
            .ThenBy(entry => entry.Method != null)
            .ThenBy(entry => entry.Member.DeclaringType.FullName)
            .ThenBy(entry => entry.Member.MetadataToken)
            .ToList();

        ApplyAll();
        EditorApplication.playModeStateChanged += state =>
        {
            if(state == PlayModeStateChange.EnteredEditMode)
            {
                ApplyAll();
            }
        };
    }

    [InitializeOnEnterPlayMode]
    static void OnEnterPlayMode()
    {
        ApplyAll();

        string[] aOverrides = Entries.Where(entry => entry.bOverridden)
            .Select(entry => $"{entry.Label.text} = {ToText(entry.Field.GetValue(null))}")
            .ToArray();
        if(aOverrides.Length > 0)
        {
            Debug.Log("Tweaks overriding " + string.Join(", ", aOverrides));
        }
    }

    public static void Set(Entry entry, object value)
    {
        entry.Field.SetValue(null, value);
        EditorPrefs.SetString(entry.sKey, ToText(value));
        entry.bOverridden = true;
    }

    public static void Revert(Entry entry)
    {
        entry.Field.SetValue(null, entry.Default);
        EditorPrefs.DeleteKey(entry.sKey);
        entry.bOverridden = false;
    }

    public static void RevertAll()
    {
        foreach(Entry entry in Entries.Where(entry => entry.bOverridden))
        {
            Revert(entry);
        }
    }

    public static void Run(Entry entry)
    {
        try
        {
            entry.Method.Invoke(null, null);
        }
        catch(TargetInvocationException e)
        {
            Debug.LogException(e.InnerException);
        }
    }

    static Entry Create(MemberInfo member, string sPrefix)
    {
        TweakAttribute attribute = member.GetCustomAttribute<TweakAttribute>();
        Entry entry = new()
        {
            sCategory = string.IsNullOrEmpty(attribute.sCategory) ? member.DeclaringType.Name : attribute.sCategory,
            Label = new GUIContent(Nicify(member.Name), member.GetCustomAttribute<TooltipAttribute>()?.tooltip),
            Attribute = attribute,
            Member = member,
            sProblem = Problem(member),
        };

        // Const and readonly fields are still shown, greyed out.
        if(entry.sProblem == null && entry.Field != null && !entry.Field.IsInitOnly && !entry.Field.IsLiteral)
        {
            entry.bEditable = true;
            entry.Default = entry.Field.GetValue(null);
            entry.sKey = sPrefix + member.DeclaringType.FullName + "." + member.Name;
            entry.bOverridden = EditorPrefs.HasKey(entry.sKey);
        }
        return entry;
    }

    static string Problem(MemberInfo member)
    {
        if(member.DeclaringType.ContainsGenericParameters)
        {
            return "can't be in a generic class";
        }

        switch(member)
        {
            case FieldInfo field when !field.IsStatic:
            case MethodInfo method when !method.IsStatic:
                return "needs to be static";
            case FieldInfo field when !field.FieldType.IsEnum && !s_aStorableTypes.Contains(field.FieldType):
                return $"can't edit a {field.FieldType.Name}";
            case MethodInfo method when method.GetParameters().Length > 0 || method.ContainsGenericParameters:
                return "can't take parameters";
            default:
                return null;
        }
    }

    // Every editable field back to its override, or its start value.
    static void ApplyAll()
    {
        foreach(Entry entry in Entries.Where(entry => entry.bEditable))
        {
            entry.Field.SetValue(null, entry.bOverridden ? Load(entry) : entry.Default);
        }
    }

    static object Load(Entry entry)
    {
        try
        {
            return FromText(EditorPrefs.GetString(entry.sKey), entry.Field.FieldType);
        }
        catch(Exception)
        {
            // The field's type changed since the override was stored.
            Debug.LogWarning($"Tweaks dropped the override of {entry.Label.text}, it no longer fits the field");
            EditorPrefs.DeleteKey(entry.sKey);
            entry.bOverridden = false;
            return entry.Default;
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

    // "fCustomerPatience" -> "Customer Patience": drops the type prefix, then lets Unity space the words.
    static string Nicify(string sName)
    {
        return ObjectNames.NicifyVariableName(Regex.Replace(sName, "^(s_|m_)?[a-z](?=[A-Z])", ""));
    }
}
