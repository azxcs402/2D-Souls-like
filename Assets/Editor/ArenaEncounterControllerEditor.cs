using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(ArenaEncounterController))]
[CanEditMultipleObjects]
public class ArenaEncounterControllerEditor : Editor
{
    private SerializedProperty waveSetProperty;
    private SerializedProperty spawnPointsProperty;
    private SerializedProperty defaultDelayBeforeSpawnProperty;
    private SerializedProperty defaultDelayAfterClearProperty;
    private SerializedProperty showSpawnPointGizmosProperty;
    private SerializedProperty doorsProperty;
    private SerializedProperty lockDoorsWhenEncounterStartsProperty;
    private SerializedProperty floatingPlatformProperty;
    private SerializedProperty startOnlyOnceProperty;
    private SerializedProperty playerTagProperty;
    private SerializedProperty clearConfirmDelayProperty;

    private void OnEnable()
    {
        waveSetProperty = serializedObject.FindProperty("waveSet");
        spawnPointsProperty = serializedObject.FindProperty("spawnPoints");
        defaultDelayBeforeSpawnProperty = serializedObject.FindProperty("defaultDelayBeforeSpawn");
        defaultDelayAfterClearProperty = serializedObject.FindProperty("defaultDelayAfterClear");
        showSpawnPointGizmosProperty = serializedObject.FindProperty("showSpawnPointGizmos");
        doorsProperty = serializedObject.FindProperty("doors");
        lockDoorsWhenEncounterStartsProperty = serializedObject.FindProperty("lockDoorsWhenEncounterStarts");
        floatingPlatformProperty = serializedObject.FindProperty("floatingPlatform");
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
        ArenaFloatingPlatformController platform = floatingPlatformProperty != null ? floatingPlatformProperty.objectReferenceValue as ArenaFloatingPlatformController : null;

        bool hasWaveSet = waveSet != null;
        int waveCount = hasWaveSet ? waveSet.Waves.Count : 0;
        int spawnPointCount = spawnPointsProperty != null ? spawnPointsProperty.arraySize : 0;
        int doorCount = doorsProperty != null ? doorsProperty.arraySize : 0;

        StringBuilder message = new StringBuilder();
        message.AppendLine(hasWaveSet ? $"Wave Set: {waveSet.name} ({waveCount} wave(s))" : "Wave Set: Missing");
        message.AppendLine($"Spawn Points: {spawnPointCount}");
        message.AppendLine($"Doors: {doorCount}");
        message.AppendLine(platform != null ? $"Floating Platform: {platform.name}" : "Floating Platform: Missing");
        message.AppendLine(controller != null ? $"Current Wave Index: {controller.CurrentWaveIndex}" : "Current Wave Index: N/A");
        message.AppendLine(controller != null ? $"Active Enemies: {controller.ActiveEnemyCount}" : "Active Enemies: N/A");
        if (controller != null)
        {
            message.AppendLine($"Active Enemy List: {controller.ActiveEnemySummary}");
        }
        message.AppendLine("Platform transitions are now driven from ArenaWaveSet per-wave settings.");
        message.AppendLine($"Trigger Status: {(Application.isPlaying ? "Playing" : "Edit Mode")}");

        MessageType messageType = MessageType.Info;
        if (!hasWaveSet || waveCount == 0)
        {
            messageType = MessageType.Error;
        }
        else if (platform == null)
        {
            messageType = MessageType.Warning;
            message.AppendLine("Floating platform is not assigned. The encounter will not animate the platform transitions.");
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
            SerializedProperty platformTransitionProperty = waveProperty.FindPropertyRelative("platformTransition");

            int spawnCount = spawnsProperty != null ? spawnsProperty.arraySize : 0;
            float delayBeforeSpawn = delayBeforeSpawnProperty != null ? delayBeforeSpawnProperty.floatValue : 0f;
            string platformTransition = platformTransitionProperty != null ? platformTransitionProperty.enumDisplayNames[platformTransitionProperty.enumValueIndex] : "None";

            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField($"Wave {i + 1}", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"Spawns: {spawnCount}");
            EditorGUILayout.LabelField($"Delay Before Spawn: {delayBeforeSpawn:0.###} s");
            EditorGUILayout.LabelField($"Platform Transition: {platformTransition}");

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
        EditorGUILayout.PropertyField(showSpawnPointGizmosProperty, new GUIContent("Show Spawn Point Gizmos"));
        EditorGUILayout.EndVertical();
    }

    private void DrawDoorsSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Doors", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(doorsProperty, true);
        EditorGUILayout.PropertyField(lockDoorsWhenEncounterStartsProperty);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Floating Platform", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(floatingPlatformProperty);
        EditorGUILayout.HelpBox(
            "Wave transitions are configured inside ArenaWaveSet. These legacy index fields are kept for fallback only.",
            MessageType.Info);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Repair Platform Layers"))
            {
                ArenaEncounterBootstrapper.RepairFloatingPlatformLayers();
            }

            if (GUILayout.Button("Repair Template"))
            {
                ArenaEncounterBootstrapper.RepairDefaultWaveSet();
            }
        }
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

[CustomEditor(typeof(ArenaFloatingPlatformController))]
public class ArenaFloatingPlatformControllerEditor : Editor
{
    private const string GroundLayerName = "Ground";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Paint each child Tilemap through Tile Palette:\n" +
            "- Background1: first visible platform stage\n" +
            "- Background2: second visible platform stage\n" +
            "- Solid: final solid platform with collision\n" +
            "During play, ArenaEncounterController will switch between the stages automatically.",
            MessageType.Info);

        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Quick Select", EditorStyles.boldLabel);

        ArenaFloatingPlatformController platform = (ArenaFloatingPlatformController)target;
        if (GUILayout.Button("Generate / Repair Template"))
        {
            GenerateOrRepairPlatformTemplate(platform);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preview Hidden"))
            {
                platform.SetHidden();
                EditorUtility.SetDirty(platform);
                EditorSceneManager.MarkSceneDirty(platform.gameObject.scene);
            }

            if (GUILayout.Button("Preview BG1"))
            {
                platform.SetBackground1();
                EditorUtility.SetDirty(platform);
                EditorSceneManager.MarkSceneDirty(platform.gameObject.scene);
            }

            if (GUILayout.Button("Preview BG2"))
            {
                platform.SetBackground2();
                EditorUtility.SetDirty(platform);
                EditorSceneManager.MarkSceneDirty(platform.gameObject.scene);
            }

            if (GUILayout.Button("Preview Solid"))
            {
                platform.SetSolid();
                EditorUtility.SetDirty(platform);
                EditorSceneManager.MarkSceneDirty(platform.gameObject.scene);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Play Appearance Seq"))
            {
                ArenaFloatingPlatformPreviewDriver.Play(platform, appearance: true);
            }

            if (GUILayout.Button("Play Disappearance Seq"))
            {
                ArenaFloatingPlatformPreviewDriver.Play(platform, appearance: false);
            }
        }

        DrawSelectButton(platform.transform, "Background1");
        DrawSelectButton(platform.transform, "Background2");
        DrawSelectButton(platform.transform, "Solid");

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawSelectButton(Transform root, string childName)
    {
        if (GUILayout.Button($"Select {childName}"))
        {
            Transform child = root != null ? root.Find(childName) : null;
            if (child != null)
            {
                Selection.activeGameObject = child.gameObject;
                EditorGUIUtility.PingObject(child.gameObject);
            }
        }
    }

    private static void GenerateOrRepairPlatformTemplate(ArenaFloatingPlatformController platform)
    {
        if (platform == null)
        {
            return;
        }

        Transform root = platform.transform;
        Undo.RegisterFullObjectHierarchyUndo(platform.gameObject, "Generate Or Repair Arena Floating Platform");

        GameObject background1 = EnsurePlatformLayer(root, "Background1", false, 3);
        GameObject background2 = EnsurePlatformLayer(root, "Background2", false, 4);
        GameObject solid = EnsurePlatformLayer(root, "Solid", true, 5);

        SerializedObject so = new SerializedObject(platform);
        SetObjectReference(so, "background1Tilemap", background1 != null ? background1.GetComponent<Tilemap>() : null);
        SetObjectReference(so, "background2Tilemap", background2 != null ? background2.GetComponent<Tilemap>() : null);
        SetObjectReference(so, "solidTilemap", solid != null ? solid.GetComponent<Tilemap>() : null);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(platform);
        EditorSceneManager.MarkSceneDirty(platform.gameObject.scene);
        Selection.activeGameObject = platform.gameObject;
        EditorGUIUtility.PingObject(platform.gameObject);
    }

    private static GameObject EnsurePlatformLayer(Transform parent, string childName, bool includeCollider, int sortingOrder)
    {
        if (parent == null)
        {
            return null;
        }

        Transform child = parent.Find(childName);
        GameObject childObject = child != null ? child.gameObject : new GameObject(childName);
        if (child == null)
        {
            Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
            Undo.SetTransformParent(childObject.transform, parent, $"Parent {childName}");
        }

        childObject.transform.localPosition = Vector3.zero;
        childObject.transform.localRotation = Quaternion.identity;
        childObject.transform.localScale = Vector3.one;
        childObject.layer = includeCollider ? LayerMask.NameToLayer(GroundLayerName) : LayerMask.NameToLayer("Default");

        if (childObject.GetComponent<Tilemap>() == null)
        {
            Undo.AddComponent<Tilemap>(childObject);
        }

        TilemapRenderer renderer = childObject.GetComponent<TilemapRenderer>();
        if (renderer == null)
        {
            renderer = Undo.AddComponent<TilemapRenderer>(childObject);
        }

        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = sortingOrder;

        if (includeCollider)
        {
            TilemapCollider2D tilemapCollider = childObject.GetComponent<TilemapCollider2D>();
            if (tilemapCollider == null)
            {
                tilemapCollider = Undo.AddComponent<TilemapCollider2D>(childObject);
            }

            tilemapCollider.usedByComposite = true;

            Rigidbody2D body = childObject.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = Undo.AddComponent<Rigidbody2D>(childObject);
            }

            body.bodyType = RigidbodyType2D.Static;
            body.simulated = true;
            body.useAutoMass = false;

            CompositeCollider2D composite = childObject.GetComponent<CompositeCollider2D>();
            if (composite == null)
            {
                composite = Undo.AddComponent<CompositeCollider2D>(childObject);
            }

            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        }

        return childObject;
    }

    private static void SetObjectReference(SerializedObject so, string propertyName, Object value)
    {
        if (so == null)
        {
            return;
        }

        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
        {
            property.objectReferenceValue = value;
        }
    }
}

internal static class ArenaFloatingPlatformPreviewDriver
{
    private static readonly FieldInfo WaitSecondsField = typeof(WaitForSeconds).GetField("m_Seconds", BindingFlags.Instance | BindingFlags.NonPublic);
    private static IEnumerator routine;
    private static ArenaFloatingPlatformController platform;
    private static double nextStepTime;
    private static bool waiting;

    static ArenaFloatingPlatformPreviewDriver()
    {
        EditorApplication.update += Tick;
    }

    public static void Play(ArenaFloatingPlatformController targetPlatform, bool appearance)
    {
        if (targetPlatform == null)
        {
            return;
        }

        platform = targetPlatform;
        routine = appearance
            ? targetPlatform.PlayAppearanceSequence()
            : targetPlatform.PlayDisappearanceSequence();
        waiting = false;
        nextStepTime = 0d;
        Advance();
    }

    private static void Tick()
    {
        if (routine == null || platform == null)
        {
            return;
        }

        if (waiting && EditorApplication.timeSinceStartup < nextStepTime)
        {
            return;
        }

        waiting = false;
        Advance();
    }

    private static void Advance()
    {
        if (routine == null || platform == null)
        {
            routine = null;
            platform = null;
            return;
        }

        while (routine.MoveNext())
        {
            object yielded = routine.Current;
            if (yielded is WaitForSeconds wait)
            {
                double seconds = GetSeconds(wait);
                nextStepTime = EditorApplication.timeSinceStartup + seconds;
                waiting = true;
                EditorSceneManager.MarkSceneDirty(platform.gameObject.scene);
                return;
            }

            if (yielded == null)
            {
                EditorSceneManager.MarkSceneDirty(platform.gameObject.scene);
                return;
            }
        }

        EditorSceneManager.MarkSceneDirty(platform.gameObject.scene);
        routine = null;
        platform = null;
        waiting = false;
    }

    private static double GetSeconds(WaitForSeconds wait)
    {
        if (wait == null)
        {
            return 0d;
        }

        if (WaitSecondsField != null)
        {
            object value = WaitSecondsField.GetValue(wait);
            if (value is float floatSeconds)
            {
                return floatSeconds;
            }

            if (value is double doubleSeconds)
            {
                return doubleSeconds;
            }
        }

        return 0d;
    }
}
