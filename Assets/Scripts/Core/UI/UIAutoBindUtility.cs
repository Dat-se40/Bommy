using System;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Utility dùng chung cho các ContextMenu auto bind UI.
/// Chỉ tìm theo tên object con và component type.
/// </summary>
public static class UIAutoBindUtility
{
    public static void RecordUndo(UnityEngine.Object target, string actionName)
    {
#if UNITY_EDITOR
        if (target != null)
            Undo.RecordObject(target, actionName);
#endif
    }

    public static void SetDirty(UnityEngine.Object target)
    {
#if UNITY_EDITOR
        if (target == null)
            return;

        EditorUtility.SetDirty(target);
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
#endif
    }

    public static T FindChildComponent<T>(
        Component owner,
        params string[] possibleNames
    ) where T : Component
    {
        if (owner == null)
            return null;

        return FindChildComponent<T>(owner.transform, possibleNames);
    }

    public static T FindChildComponent<T>(
        Transform root,
        params string[] possibleNames
    ) where T : Component
    {
        Transform child = FindChildTransform(root, possibleNames);

        if (child == null)
            return null;

        T component = child.GetComponent<T>();

        if (component == null)
        {
            Debug.LogWarning(
                "[FLOW:UI] Found child '" + child.name + "' but missing component " + typeof(T).Name,
                child
            );
        }

        return component;
    }

    public static GameObject FindChildGameObject(
        Component owner,
        params string[] possibleNames
    )
    {
        if (owner == null)
            return null;

        Transform child = FindChildTransform(owner.transform, possibleNames);

        return child != null ? child.gameObject : null;
    }

    public static GameObject FindChildGameObject(
        Transform root,
        params string[] possibleNames
    )
    {
        Transform child = FindChildTransform(root, possibleNames);

        return child != null ? child.gameObject : null;
    }

    public static Transform FindChildTransform(
        Transform root,
        params string[] possibleNames
    )
    {
        if (root == null || possibleNames == null)
            return null;

        for (int i = 0; i < possibleNames.Length; i++)
        {
            Transform result = FindDeepChildByName(root, possibleNames[i]);

            if (result != null)
                return result;
        }

        return null;
    }

    public static Transform FindDeepChildByName(Transform root, string targetName)
    {
        if (root == null)
            return null;

        if (IsSameName(root.name, targetName))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindDeepChildByName(root.GetChild(i), targetName);

            if (result != null)
                return result;
        }

        return null;
    }

    public static T[] GetComponentsInChildrenSorted<T>(
        Transform root,
        bool includeInactive,
        Comparison<T> comparison
    ) where T : Component
    {
        if (root == null)
            return Array.Empty<T>();

        T[] results = root.GetComponentsInChildren<T>(includeInactive);

        if (comparison != null)
            Array.Sort(results, comparison);

        return results;
    }

    public static bool IsSameName(string a, string b)
    {
        return NormalizeName(a) == NormalizeName(b);
    }

    public static string NormalizeName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "")
            .ToLowerInvariant();
    }

    public static string Bound(UnityEngine.Object obj)
    {
        return obj != null ? "OK" : "MISSING";
    }

    public static void LogBindResult(
        UnityEngine.Object context,
        string title,
        params BindLogItem[] items
    )
    {
        StringBuilder builder = new();

        builder.Append("[FLOW:UI] ");
        builder.Append(title);

        for (int i = 0; i < items.Length; i++)
        {
            builder.Append("\n");
            builder.Append(items[i].Name);
            builder.Append(": ");
            builder.Append(Bound(items[i].Value));
        }

        Debug.Log(builder.ToString(), context);
    }
}

public readonly struct BindLogItem
{
    public readonly string Name;
    public readonly UnityEngine.Object Value;

    public BindLogItem(string name, UnityEngine.Object value)
    {
        Name = name;
        Value = value;
    }
}
