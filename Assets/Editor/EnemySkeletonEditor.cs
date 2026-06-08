using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Enemy_Skeleton))]
public class EnemySkeletonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SerializedProperty multiplierProperty = serializedObject.FindProperty("battleMoveSpeedMultiplier");
        if (multiplierProperty == null)
        {
            return;
        }

        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(multiplierProperty, new GUIContent("Battle Move Speed Multiplier"));
        }
        serializedObject.ApplyModifiedProperties();
    }
}
