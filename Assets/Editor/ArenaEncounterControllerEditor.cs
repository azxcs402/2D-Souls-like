using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ArenaEncounterController))]
[CanEditMultipleObjects]
public class ArenaEncounterControllerEditor : Editor
{
    private SerializedProperty waveSetProperty;
    private SerializedProperty spawnPointsProperty;
    private SerializedProperty defaultDelayBeforeSpawnProperty;
    private SerializedProperty defaultDelayAfterClearProperty;
    private SerializedProperty doorsProperty;
    private SerializedProperty lockDoorsWhenEncounterStartsProperty;
    private SerializedProperty startOnlyOnceProperty;
    private SerializedProperty playerTagProperty;
    private SerializedProperty clearConfirmDelayProperty;

    private void OnEnable()
    {
        waveSetProperty = serializedObject.FindProperty("waveSet");
        spawnPointsProperty = serializedObject.FindProperty("spawnPoints");
        defaultDelayBeforeSpawnProperty = serializedObject.FindProperty("defaultDelayBeforeSpawn");
        defaultDelayAfterClearProperty = serializedObject.FindProperty("defaultDelayAfterClear");
        doorsProperty = serializedObject.FindProperty("doors");
        lockDoorsWhenEncounterStartsProperty = serializedObject.FindProperty("lockDoorsWhenEncounterStarts");
        startOnlyOnceProperty = serializedObject.FindProperty("startOnlyOnce");
        playerTagProperty = serializedObject.FindProperty("playerTag");
        clearConfirmDelayProperty = serializedObject.FindProperty("clearConfirmDelay");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();
        DrawRuntimeSummary();
        EditorGUILayout.Space(6f);

        DrawWaveSummary();
        EditorGUILayout.Space(8f);

        DrawWaveDataSection();
        DrawDoorsSection();
        DrawTriggerSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        }
    }

    private void DrawRuntimeSummary()
    {
        ArenaEncounterController controller = (ArenaEncounterController)target;
        ArenaWaveSet waveSet = waveSetProperty != null ? waveSetProperty.objectReferenceValue as ArenaWaveSet : null;

        bool hasWaveSet = waveSet != null;
        int waveCount = hasWaveSet ? waveSet.Waves.Count : 0;
        int spawnPointCount = spawnPointsProperty != null ? spawnPointsProperty.arraySize : 0;
        int doorCount = doorsProperty != null ? doorsProperty.arraySize : 0;

        StringBuilder message = new StringBuilder();
        message.AppendLine(hasWaveSet ? $"Wave Set: {waveSet.name} ({waveCount} wave(s))" : "Wave Set: Missing");
        message.AppendLine($"Spawn Points: {spawnPointCount}");
        message.AppendLine($"Doors: {doorCount}");
        message.AppendLine($"Trigger Status: {(Application.isPlaying ? "Playing" : "Edit Mode")}");

        MessageType messageType = MessageType.Info;
        if (!hasWaveSet || waveCount == 0)
        {
            messageType = MessageType.Error;
        }
        else if (!IsWaveSetValid(waveSet, spawnPointCount, out string validationNote))
        {
            messageType = MessageType.Warning;
            message.AppendLine(validationNote);
        }

        EditorGUILayout.HelpBox(message.ToString(), messageType);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Ping Wave Set"))
            {
                if (waveSet != null)
                {
                    EditorGUIUtility.PingObject(waveSet);
                    Selection.activeObject = waveSet;
                }
            }

            if (GUILayout.Button("Open Wave Set"))
            {
                if (waveSet != null)
                {
                    AssetDatabase.OpenAsset(waveSet);
                }
            }

            if (controller != null && GUILayout.Button("Reset Encounter"))
            {
                Undo.RecordObject(controller, "Reset Arena Encounter");
                controller.ResetEncounter();
                EditorUtility.SetDirty(controller);
            }
        }
    }

    private void DrawWaveSummary()
    {
        ArenaWaveSet waveSet = waveSetProperty != null ? waveSetProperty.objectReferenceValue as ArenaWaveSet : null;
        if (waveSet == null)
        {
            EditorGUILayout.HelpBox("Assign a Wave Set first. Without it the encounter will only lock doors and will not spawn enemies.", MessageType.Warning);
            return;
        }

        SerializedObject waveSetSo = new SerializedObject(waveSet);
        SerializedProperty wavesProperty = waveSetSo.FindProperty("waves");
        if (wavesProperty == null)
        {
            EditorGUILayout.HelpBox("Wave Set data is missing or unreadable.", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("Wave Overview", EditorStyles.boldLabel);
        for (int i = 0; i < wavesProperty.arraySize; i++)
        {
            SerializedProperty waveProperty = wavesProperty.GetArrayElementAtIndex(i);
            SerializedProperty spawnsProperty = waveProperty.FindPropertyRelative("spawns");
            SerializedProperty delayBeforeSpawnProperty = waveProperty.FindPropertyRelative("delayBeforeSpawn");

            int spawnCount = spawnsProperty != null ? spawnsProperty.arraySize : 0;
            float delayBeforeSpawn = delayBeforeSpawnProperty != null ? delayBeforeSpawnProperty.floatValue : 0f;

            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField($"Wave {i + 1}", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"Spawns: {spawnCount}");
            EditorGUILayout.LabelField($"Delay Before Spawn: {delayBeforeSpawn:0.###} s");

            if (!IsWaveValid(waveProperty, spawnPointsProperty != null ? spawnPointsProperty.arraySize : 0, out string waveIssue))
            {
                EditorGUILayout.HelpBox(waveIssue, MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawWaveDataSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Wave Data", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(waveSetProperty);
        EditorGUILayout.PropertyField(spawnPointsProperty, true);
        EditorGUILayout.PropertyField(defaultDelayBeforeSpawnProperty);
        EditorGUILayout.PropertyField(defaultDelayAfterClearProperty);
        EditorGUILayout.EndVertical();
    }

    private void DrawDoorsSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Doors", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(doorsProperty, true);
        EditorGUILayout.PropertyField(lockDoorsWhenEncounterStartsProperty);
        EditorGUILayout.EndVertical();
    }

    private void DrawTriggerSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Trigger", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(startOnlyOnceProperty);
        EditorGUILayout.PropertyField(playerTagProperty);
        EditorGUILayout.PropertyField(clearConfirmDelayProperty);
        EditorGUILayout.EndVertical();
    }

    private bool IsWaveSetValid(ArenaWaveSet waveSet, int spawnPointCount, out string note)
    {
        note = string.Empty;
        if (waveSet == null)
        {
            note = "Wave Set is missing.";
            return false;
        }

        SerializedObject waveSetSo = new SerializedObject(waveSet);
        SerializedProperty wavesProperty = waveSetSo.FindProperty("waves");
        if (wavesProperty == null || wavesProperty.arraySize == 0)
        {
            note = "Wave Set contains no waves.";
            return false;
        }

        for (int i = 0; i < wavesProperty.arraySize; i++)
        {
            SerializedProperty waveProperty = wavesProperty.GetArrayElementAtIndex(i);
            if (!IsWaveValid(waveProperty, spawnPointCount, out string waveIssue))
            {
                note = $"Wave {i + 1}: {waveIssue}";
                return false;
            }
        }

        return true;
    }

    private bool IsWaveValid(SerializedProperty waveProperty, int spawnPointCount, out string note)
    {
        note = string.Empty;
        if (waveProperty == null)
        {
            note = "Wave data is missing.";
            return false;
        }

        SerializedProperty spawnsProperty = waveProperty.FindPropertyRelative("spawns");
        if (spawnsProperty == null || spawnsProperty.arraySize == 0)
        {
            note = "No spawns configured.";
            return false;
        }

        for (int i = 0; i < spawnsProperty.arraySize; i++)
        {
            SerializedProperty spawnProperty = spawnsProperty.GetArrayElementAtIndex(i);
            SerializedProperty enemyPrefabProperty = spawnProperty.FindPropertyRelative("enemyPrefab");
            SerializedProperty randomSpawnProperty = spawnProperty.FindPropertyRelative("useRandomSpawnPoint");
            SerializedProperty spawnPointIndexProperty = spawnProperty.FindPropertyRelative("spawnPointIndex");

            if (enemyPrefabProperty == null || enemyPrefabProperty.objectReferenceValue == null)
            {
                note = $"Spawn {i + 1} has no enemy prefab.";
                return false;
            }

            bool useRandom = randomSpawnProperty != null && randomSpawnProperty.boolValue;
            int spawnPointIndex = spawnPointIndexProperty != null ? spawnPointIndexProperty.intValue : -1;
            if (!useRandom && (spawnPointIndex < 0 || spawnPointIndex >= spawnPointCount))
            {
                note = $"Spawn {i + 1} uses fixed point {spawnPointIndex + 1}, but only {spawnPointCount} spawn point(s) exist.";
                return false;
            }
        }

        return true;
    }
}
