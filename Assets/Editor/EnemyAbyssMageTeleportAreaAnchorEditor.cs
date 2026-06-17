using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Enemy_AbyssMageTeleportAreaAnchor))]
public class EnemyAbyssMageTeleportAreaAnchorEditor : Editor
{
    private SerializedProperty areaSizeProperty;

    private void OnEnable()
    {
        areaSizeProperty = serializedObject.FindProperty("areaSize");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SearchableInspectorDrawer.DrawScriptField(serializedObject);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Teleport Area", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(areaSizeProperty, new GUIContent("Area Size"));

        Enemy_AbyssMageTeleportAreaAnchor anchor = (Enemy_AbyssMageTeleportAreaAnchor)target;
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Owner Mage", anchor != null ? anchor.Mage : null, typeof(Enemy_AbyssMage), true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI()
    {
        Enemy_AbyssMageTeleportAreaAnchor anchor = (Enemy_AbyssMageTeleportAreaAnchor)target;
        if (anchor == null || areaSizeProperty == null)
        {
            return;
        }

        serializedObject.Update();

        Vector2 size = areaSizeProperty.vector2Value;
        Vector3 center = anchor.transform.position;
        Enemy_AbyssMageTeleportAreaAnchor.DrawOutline(center, size);

        Vector3 half = new Vector3(size.x * .5f, size.y * .5f, 0f);
        Vector3 oppositeCorner = center - half;
        Vector3 sizeCorner = center + half;

        EditorGUI.BeginChangeCheck();
        Vector3 movedCorner = Handles.FreeMoveHandle(sizeCorner, 0.09f, Vector3.zero, Handles.CubeHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(anchor, "Resize Mage Teleport Area");

            Vector2 newCenter = new Vector2(
                (movedCorner.x + oppositeCorner.x) * .5f,
                (movedCorner.y + oppositeCorner.y) * .5f
            );
            Vector2 newSize = new Vector2(
                Mathf.Max(.1f, Mathf.Abs(movedCorner.x - oppositeCorner.x)),
                Mathf.Max(.1f, Mathf.Abs(movedCorner.y - oppositeCorner.y))
            );

            areaSizeProperty.vector2Value = newSize;
            anchor.transform.position = new Vector3(newCenter.x, newCenter.y, anchor.transform.position.z);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(anchor);
        }
    }
}
