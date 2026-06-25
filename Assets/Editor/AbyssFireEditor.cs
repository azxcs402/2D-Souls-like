using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AbyssFire))]
public class AbyssFireEditor : Editor
{
    private SerializedProperty flameFramesProperty;
    private SerializedProperty framesPerSecondProperty;
    private SerializedProperty loopAnimationProperty;
    private SerializedProperty autoLoadDefaultFramesProperty;
    private SerializedProperty flameTintProperty;
    private SerializedProperty appearSfxKeyProperty;
    private SerializedProperty disappearSfxKeyProperty;
    private SerializedProperty flameRendererProperty;

    private void OnEnable()
    {
        flameFramesProperty = serializedObject.FindProperty("flameFrames");
        framesPerSecondProperty = serializedObject.FindProperty("framesPerSecond");
        loopAnimationProperty = serializedObject.FindProperty("loopAnimation");
        autoLoadDefaultFramesProperty = serializedObject.FindProperty("autoLoadDefaultFrames");
        flameTintProperty = serializedObject.FindProperty("flameTint");
        appearSfxKeyProperty = serializedObject.FindProperty("appearSfxKey");
        disappearSfxKeyProperty = serializedObject.FindProperty("disappearSfxKey");
        flameRendererProperty = serializedObject.FindProperty("flameRenderer");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox("Abyss Fire sprite, animation, and transition audio settings.", MessageType.Info);

        DrawScriptField();
        DrawAnimationSection();
        DrawAudioSection();
        DrawReferenceSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        MonoScript script = MonoScript.FromMonoBehaviour((AbyssFire)target);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
        }
    }

    private void DrawAnimationSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(flameFramesProperty, new GUIContent("Flame Frames"), true);
        EditorGUILayout.PropertyField(framesPerSecondProperty, new GUIContent("Frames Per Second"));
        EditorGUILayout.PropertyField(loopAnimationProperty, new GUIContent("Loop Animation"));
        EditorGUILayout.PropertyField(autoLoadDefaultFramesProperty, new GUIContent("Auto Load Default Frames"));
        EditorGUILayout.PropertyField(flameTintProperty, new GUIContent("Flame Tint"));
    }

    private void DrawAudioSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(appearSfxKeyProperty, new GUIContent("Appear Sfx Key"));
        EditorGUILayout.PropertyField(disappearSfxKeyProperty, new GUIContent("Disappear Sfx Key"));
    }

    private void DrawReferenceSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(flameRendererProperty, new GUIContent("Flame Renderer"));
    }
}
