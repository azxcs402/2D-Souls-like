using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(AbyssFire))]
public class AbyssFireEditor : Editor
{
    private SerializedProperty flameFramesProperty;
    private SerializedProperty framesPerSecondProperty;
    private SerializedProperty loopAnimationProperty;
    private SerializedProperty autoLoadDefaultFramesProperty;
    private SerializedProperty flameTintProperty;
    private SerializedProperty flameLoopSfxKeyProperty;
    private SerializedProperty flameLoopVolumeProperty;
    private SerializedProperty flameLoopMinDistanceProperty;
    private SerializedProperty flameLoopMaxDistanceProperty;
    private SerializedProperty flameRendererProperty;
    private SerializedProperty flameLoopSourceProperty;

    private void OnEnable()
    {
        flameFramesProperty = serializedObject.FindProperty("flameFrames");
        framesPerSecondProperty = serializedObject.FindProperty("framesPerSecond");
        loopAnimationProperty = serializedObject.FindProperty("loopAnimation");
        autoLoadDefaultFramesProperty = serializedObject.FindProperty("autoLoadDefaultFrames");
        flameTintProperty = serializedObject.FindProperty("flameTint");
        flameLoopSfxKeyProperty = serializedObject.FindProperty("flameLoopSfxKey");
        flameLoopVolumeProperty = serializedObject.FindProperty("flameLoopVolume");
        flameLoopMinDistanceProperty = serializedObject.FindProperty("flameLoopMinDistance");
        flameLoopMaxDistanceProperty = serializedObject.FindProperty("flameLoopMaxDistance");
        flameRendererProperty = serializedObject.FindProperty("flameRenderer");
        flameLoopSourceProperty = serializedObject.FindProperty("flameLoopSource");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox("深渊之火的音频设置可以单独同步到所有深渊之火实例，不会影响篝火。", MessageType.Info);

        DrawScriptField();
        DrawAnimationSection();
        DrawAudioSection();
        DrawFlameAudioSection();
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
        EditorGUILayout.PropertyField(flameLoopSfxKeyProperty, new GUIContent("Flame Loop Sfx Key"));
    }

    private void DrawFlameAudioSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Flame Audio", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(flameLoopVolumeProperty, new GUIContent("Flame Loop Volume"));
        EditorGUILayout.PropertyField(flameLoopMinDistanceProperty, new GUIContent("Flame Loop Min Distance"));
        EditorGUILayout.PropertyField(flameLoopMaxDistanceProperty, new GUIContent("Flame Loop Max Distance"));

        EditorGUILayout.Space(4f);
        using (new EditorGUI.DisabledScope(targets == null || targets.Length == 0))
        {
            if (GUILayout.Button("Sync Flame Audio To All Abyss Fires"))
            {
                serializedObject.ApplyModifiedProperties();
                SyncFlameAudioToAllAbyssFires();
                serializedObject.Update();
            }
        }
    }

    private void DrawReferenceSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(flameRendererProperty, new GUIContent("Flame Renderer"));
        EditorGUILayout.PropertyField(flameLoopSourceProperty, new GUIContent("Flame Loop Source"));
    }

    private void SyncFlameAudioToAllAbyssFires()
    {
        AbyssFire source = target as AbyssFire;
        if (source == null)
        {
            return;
        }

        AbyssFire[] abyssFires = Object.FindObjectsByType<AbyssFire>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        System.Collections.Generic.List<AbyssFire> changedAbyssFires = new System.Collections.Generic.List<AbyssFire>();

        for (int i = 0; i < abyssFires.Length; i++)
        {
            AbyssFire abyssFire = abyssFires[i];
            if (abyssFire == null || abyssFire == source)
            {
                continue;
            }

            changedAbyssFires.Add(abyssFire);
        }

        if (changedAbyssFires.Count == 0)
        {
            return;
        }

        Undo.RecordObjects(changedAbyssFires.ToArray(), "Sync Abyss Fire Flame Audio");

        for (int i = 0; i < changedAbyssFires.Count; i++)
        {
            AbyssFire abyssFire = changedAbyssFires[i];
            abyssFire.CopyFlameAudioSettingsFrom(source);
            EditorUtility.SetDirty(abyssFire);
            PrefabUtility.RecordPrefabInstancePropertyModifications(abyssFire);

            if (abyssFire.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(abyssFire.gameObject.scene);
            }
        }
    }
}
