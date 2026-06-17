using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Enemy_AbyssMageProjectileHoverAreaAnchor))]
public class EnemyAbyssMageProjectileHoverAreaAnchorEditor : Editor
{
    private SerializedProperty hoverAreaSizeProperty;

    private void OnEnable()
    {
        hoverAreaSizeProperty = serializedObject.FindProperty("hoverAreaSize");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SearchableInspectorDrawer.DrawScriptField(serializedObject);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Projectile Hover Area", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(hoverAreaSizeProperty, new GUIContent("Hover Area Size"));

        Enemy_AbyssMageProjectileHoverAreaAnchor anchor = (Enemy_AbyssMageProjectileHoverAreaAnchor)target;
        Enemy_AbyssMage mage = anchor != null ? anchor.Mage : null;

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Owner Mage", mage, typeof(Enemy_AbyssMage), true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI()
    {
        Enemy_AbyssMageProjectileHoverAreaAnchor anchor = (Enemy_AbyssMageProjectileHoverAreaAnchor)target;
        if (anchor == null)
        {
            return;
        }

        serializedObject.Update();

        Vector2 size = hoverAreaSizeProperty != null ? hoverAreaSizeProperty.vector2Value : Vector2.zero;
        Vector3 center = anchor.transform.position;
        DrawHoverAreaGizmo(center, size);

        Vector3 half = new Vector3(size.x * .5f, size.y * .5f, 0f);
        Vector3 oppositeCorner = center - half;
        Vector3 sizeCorner = center + half;

        EditorGUI.BeginChangeCheck();
        Vector3 movedCorner = Handles.FreeMoveHandle(
            sizeCorner,
            0.09f,
            Vector3.zero,
            Handles.CubeHandleCap
        );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(anchor, "Resize Mage Projectile Hover Area");

            Vector2 newCenter = new Vector2(
                (movedCorner.x + oppositeCorner.x) * .5f,
                (movedCorner.y + oppositeCorner.y) * .5f
            );
            Vector2 newSize = new Vector2(
                Mathf.Max(.1f, Mathf.Abs(movedCorner.x - oppositeCorner.x)),
                Mathf.Max(.1f, Mathf.Abs(movedCorner.y - oppositeCorner.y))
            );

            hoverAreaSizeProperty.vector2Value = newSize;
            anchor.transform.position = new Vector3(newCenter.x, newCenter.y, anchor.transform.position.z);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(anchor);
        }
    }

    private static void DrawHoverAreaGizmo(Vector3 center, Vector2 size, bool showLabel = true)
    {
        if (size.x <= 0f || size.y <= 0f)
        {
            return;
        }

        Vector3 half = new Vector3(size.x * .5f, size.y * .5f, 0f);
        Vector3[] corners =
        {
            center + new Vector3(-half.x, -half.y, 0f),
            center + new Vector3(-half.x, half.y, 0f),
            center + new Vector3(half.x, half.y, 0f),
            center + new Vector3(half.x, -half.y, 0f)
        };

        Color fillColor = new Color(.35f, .8f, 1f, .12f);
        Color wireColor = new Color(.35f, .8f, 1f, 1f);

        Handles.DrawSolidRectangleWithOutline(corners, fillColor, wireColor);

        if (!showLabel)
        {
            return;
        }

        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal =
            {
                textColor = wireColor
            }
        };

        Handles.Label(center + Vector3.up * (size.y * .5f + .12f), $"Spell Hover Area / {size.x:0.##} x {size.y:0.##}", labelStyle);
    }
}
