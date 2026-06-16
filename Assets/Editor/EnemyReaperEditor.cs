using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Enemy_Reaper))]
public class EnemyReaperEditor : Editor
{
    private void OnEnable()
    {
        Enemy_Reaper reaper = (Enemy_Reaper)target;
        if (reaper != null)
        {
            reaper.EnsureBattleRangeAnchors();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SearchableInspectorDrawer.DrawScriptField(serializedObject);
        EditorGUILayout.HelpBox(
            "Attack Range and Spell Cast Range are driven by the local X position of their child anchors. Drag the child objects in the Scene view to change the range.",
            MessageType.Info
        );

        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI()
    {
        Enemy_Reaper reaper = (Enemy_Reaper)target;
        if (reaper == null)
        {
            return;
        }

        reaper.EnsureBattleRangeAnchors();

        Transform attackAnchor = reaper.AttackRangeAnchor;
        Transform spellAnchor = reaper.SpellCastRangeAnchor;
        if (attackAnchor == null || spellAnchor == null)
        {
            return;
        }

        Vector3 bodyCenter = GetBodyCenter(reaper);
        float boxHeight = Mathf.Max(.1f, reaper.ChaseVerticalDistance * 2f);
        DrawRangeBox(bodyCenter, reaper.AttackDistance, boxHeight, new Color(1f, .45f, .15f, .12f), new Color(1f, .45f, .15f, 1f), $"Attack Range / {reaper.AttackDistance:0.##}");
        DrawRangeBox(bodyCenter, reaper.SpellCastDistance, boxHeight, new Color(.35f, .8f, 1f, .10f), new Color(.35f, .8f, 1f, 1f), $"Spell Cast Range / {reaper.SpellCastDistance:0.##}");

        DrawRangeHandle(reaper, attackAnchor, spellAnchor, "Attack Range", new Color(1f, .45f, .15f, 1f));
        DrawRangeHandle(reaper, spellAnchor, attackAnchor, "Spell Cast Range", new Color(.35f, .8f, 1f, 1f));
    }

    private static Vector3 GetBodyCenter(Enemy_Reaper reaper)
    {
        CapsuleCollider2D collider2D = reaper.GetComponent<CapsuleCollider2D>();
        return collider2D != null ? collider2D.bounds.center : reaper.transform.position;
    }

    private static void DrawRangeBox(Vector3 center, float radius, float height, Color fillColor, Color wireColor, string label)
    {
        float width = Mathf.Max(.1f, radius * 2f);
        float clampedHeight = Mathf.Max(.1f, height);
        Vector3 half = new Vector3(width * .5f, clampedHeight * .5f, 0f);
        Vector3[] corners =
        {
            center + new Vector3(-half.x, -half.y, 0f),
            center + new Vector3(-half.x, half.y, 0f),
            center + new Vector3(half.x, half.y, 0f),
            center + new Vector3(half.x, -half.y, 0f)
        };

        Handles.DrawSolidRectangleWithOutline(corners, fillColor, wireColor);

        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal =
            {
                textColor = wireColor
            }
        };

        Handles.Label(center + Vector3.up * (half.y + .12f), label, labelStyle);
    }

    private static void DrawRangeHandle(Enemy_Reaper reaper, Transform anchor, Transform otherAnchor, string undoName, Color color)
    {
        Vector3 handleWorld = anchor.position;
        Handles.color = color;
        Handles.DrawLine(reaper.transform.position, handleWorld);

        EditorGUI.BeginChangeCheck();
        var fmh_96_61_639171517707598988 = Quaternion.identity; Vector3 moved = Handles.FreeMoveHandle(handleWorld, 0.08f, Vector3.zero, Handles.SphereHandleCap);
        if (!EditorGUI.EndChangeCheck())
        {
            return;
        }

        Undo.RecordObject(anchor, $"Move {undoName}");
        Vector3 local = reaper.transform.InverseTransformPoint(moved);
        local.x = Mathf.Max(.1f, Mathf.Abs(local.x));
        local.y = 0f;
        local.z = 0f;
        anchor.localPosition = local;

        if (otherAnchor != null)
        {
            Vector3 otherLocal = otherAnchor.localPosition;
            float otherDistance = Mathf.Abs(otherLocal.x);
            if (string.Equals(undoName, "Attack Range", System.StringComparison.OrdinalIgnoreCase) && otherDistance < local.x)
            {
                Undo.RecordObject(otherAnchor, "Clamp Spell Cast Range");
                otherLocal.x = local.x;
                otherLocal.y = 0f;
                otherLocal.z = 0f;
                otherAnchor.localPosition = otherLocal;
            }
            else if (string.Equals(undoName, "Spell Cast Range", System.StringComparison.OrdinalIgnoreCase) && otherDistance > local.x)
            {
                Undo.RecordObject(otherAnchor, "Clamp Attack Range");
                otherLocal.x = local.x;
                otherLocal.y = 0f;
                otherLocal.z = 0f;
                otherAnchor.localPosition = otherLocal;
            }
        }

        EditorUtility.SetDirty(anchor);
        EditorUtility.SetDirty(reaper);
    }
}
