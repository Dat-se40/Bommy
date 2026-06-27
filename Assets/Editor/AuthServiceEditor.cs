#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AuthService))]
public sealed class AuthServiceEditor : Editor
{
    SerializedProperty scheme;
    SerializedProperty host;
    SerializedProperty port;
    SerializedProperty serverKey;
    SerializedProperty httpKey;

    void OnEnable()
    {
        scheme = serializedObject.FindProperty("scheme");
        host = serializedObject.FindProperty("host");
        port = serializedObject.FindProperty("port");
        serverKey = serializedObject.FindProperty("serverKey");
        httpKey = serializedObject.FindProperty("httpKey");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "m_Script");

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Connection Presets", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply Local Docker"))
                ApplyLocalDocker();

            if (GUILayout.Button("Apply Railway Production"))
                ApplyRailwayProduction();
        }

        EditorGUILayout.HelpBox(
            "Local Docker fills all connection fields. Railway Production preserves Host and keys, then sets scheme=https and port=443.",
            MessageType.Info
        );

        serializedObject.ApplyModifiedProperties();
    }

    void ApplyLocalDocker()
    {
        scheme.stringValue = "http";
        host.stringValue = "127.0.0.1";
        port.intValue = 7350;
        serverKey.stringValue = "defaultkey";
        httpKey.stringValue = "defaulthttpkey";
    }

    void ApplyRailwayProduction()
    {
        scheme.stringValue = "https";
        port.intValue = 443;
    }
}
#endif
