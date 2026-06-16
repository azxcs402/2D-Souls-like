using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Enemy_AbyssMageTeleportTriggerAnchor))]
public class EnemyAbyssMageTeleportTriggerAnchorEditor : Editor
{
    private SerializedProperty radiusProperty;

    private void OnEnable()
    {
        radiusProperty = serializedObject.FindProperty("radius");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SearchableInspectorDrawer.DrawScriptField(serializedObject);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Teleport Trigger Range", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(radiusProperty, new GUIContent("Radius"));

        Enemy_AbyssMageTeleportTriggerAnchor anchor = (Enemy_AbyssMageTeleportTriggerAnchor)target;
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Owner Mage", anchor != null ? anchor.Mage : null, typeof(Enemy_AbyssMage), true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI()
    {
        Enemy_AbyssMageTeleportTriggerAnchor anchor = (Enemy_AbyssMageTeleportTriggerAnchor)target;
        if (anchor == null || radiusProperty == null)
        {
            return;
        }

        serializedObject.Update();
        float radius = Mathf.Max(.1f, radiusProperty.floatValue);
        Enemy_AbyssMageTeleportTriggerAnchor.DrawRange(anchor.transform.position, radius, anchor.name);

        EditorGUI.BeginChangeCheck();
        float newRadius = Handles.RadiusHandle(Quaternion.identity, anchor.transform.position, radius);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(anchor, "Resize Mage Teleport Trigger Range");
            radiusProperty.floatValue = Mathf.Max(.1f, newRadius);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(anchor);
        }
    }
}
