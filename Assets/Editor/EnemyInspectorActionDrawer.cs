using UnityEditor;
using UnityEngine;

public static class EnemyInspectorActionDrawer
{
    public static void DrawKillEnemyButton(Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        if (GUILayout.Button("Kill Enemy"))
        {
            enemy.ForceDieForInspector();
            EditorUtility.SetDirty(enemy);
        }
        EditorGUI.EndDisabledGroup();
    }
}
