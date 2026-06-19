using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(ArenaWaveSet))]
public class ArenaWaveSetEditor : Editor
{
    private SerializedProperty wavesProperty;

    private void OnEnable()
    {
        wavesProperty = serializedObject.FindProperty("waves");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Use this asset to configure arena waves.\n" +
            "- Each wave can have multiple enemy entries.\n" +
            "- Disable Random Spawn Point to use a fixed spawnPointIndex.\n" +
            "- Spawn point buttons are discovered from the current scene.\n" +
            "- Platform Transition uses buttons: None / Show / Hide.\n" +
            "- Use Create Wave Copy if you want a per-wave prefab you can edit with the full Enemy inspector.",
            MessageType.Info);

        if (wavesProperty == null)
        {
            EditorGUILayout.HelpBox("Waves property is missing.", MessageType.Error);
            return;
        }

        DrawWaves();

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Wave"))
            {
                AddWave();
            }

            if (GUILayout.Button("Clear Waves"))
            {
                wavesProperty.arraySize = 0;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawWaves()
    {
        for (int waveIndex = 0; waveIndex < wavesProperty.arraySize; waveIndex++)
        {
            SerializedProperty waveProperty = wavesProperty.GetArrayElementAtIndex(waveIndex);
            SerializedProperty spawnsProperty = waveProperty.FindPropertyRelative("spawns");
            SerializedProperty delayBeforeSpawnProperty = waveProperty.FindPropertyRelative("delayBeforeSpawn");
            SerializedProperty delayAfterClearProperty = waveProperty.FindPropertyRelative("delayAfterClear");
            SerializedProperty platformTransitionProperty = waveProperty.FindPropertyRelative("platformTransition");
            bool removeWave = false;

            EditorGUILayout.BeginVertical("box");

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Wave {waveIndex + 1}", EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    removeWave = true;
                }
            }

            EditorGUILayout.PropertyField(delayBeforeSpawnProperty, new GUIContent("Delay Before Spawn"));
            EditorGUILayout.PropertyField(delayAfterClearProperty, new GUIContent("Delay After Clear"));
            DrawPlatformTransitionSelector(platformTransitionProperty);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Spawns", EditorStyles.boldLabel);

            if (spawnsProperty != null)
            {
                for (int spawnIndex = 0; spawnIndex < spawnsProperty.arraySize; spawnIndex++)
                {
                    SerializedProperty spawnProperty = spawnsProperty.GetArrayElementAtIndex(spawnIndex);
                    SerializedProperty enemyPrefabProperty = spawnProperty.FindPropertyRelative("enemyPrefab");
                    SerializedProperty countProperty = spawnProperty.FindPropertyRelative("count");
                    SerializedProperty randomSpawnProperty = spawnProperty.FindPropertyRelative("useRandomSpawnPoint");
                    SerializedProperty spawnPointIndexProperty = spawnProperty.FindPropertyRelative("spawnPointIndex");
                    SerializedProperty overrideScaleProperty = spawnProperty.FindPropertyRelative("overrideScale");
                    SerializedProperty spawnScaleProperty = spawnProperty.FindPropertyRelative("spawnScale");
                    SerializedProperty overrideMaxHealthProperty = spawnProperty.FindPropertyRelative("overrideMaxHealth");
                    SerializedProperty maxHealthProperty = spawnProperty.FindPropertyRelative("maxHealth");
                    SerializedProperty overrideCombatDamageProperty = spawnProperty.FindPropertyRelative("overrideCombatDamage");
                    SerializedProperty combatDamageProperty = spawnProperty.FindPropertyRelative("combatDamage");
                    bool removeSpawn = false;

                    EditorGUILayout.BeginVertical("helpbox");
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"Spawn {spawnIndex + 1}", EditorStyles.miniBoldLabel);
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                        {
                            removeSpawn = true;
                        }
                    }

                    EditorGUILayout.PropertyField(enemyPrefabProperty, new GUIContent("Enemy Prefab"));
                    if (enemyPrefabProperty != null && enemyPrefabProperty.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        bool missingReference = enemyPrefabProperty.objectReferenceValue == null && enemyPrefabProperty.objectReferenceInstanceIDValue != 0;
                        if (missingReference)
                        {
                            EditorGUILayout.HelpBox("This reference is missing or mismatched. Reassign the prefab manually.", MessageType.Warning);
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Open Prefab"))
                        {
                            OpenEnemyPrefab(enemyPrefabProperty);
                        }

                        if (GUILayout.Button("Create Wave Copy"))
                        {
                            CreateWavePrefabCopy(enemyPrefabProperty, waveIndex, spawnIndex);
                        }
                    }

                    EditorGUILayout.PropertyField(countProperty, new GUIContent("Count"));
                    DrawSpawnPointSelector(randomSpawnProperty, spawnPointIndexProperty);

                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField("Spawn Overrides", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(overrideScaleProperty, new GUIContent("Override Scale"));
                    if (overrideScaleProperty != null && overrideScaleProperty.boolValue)
                    {
                        EditorGUILayout.PropertyField(spawnScaleProperty, new GUIContent("Scale"));
                    }

                    EditorGUILayout.PropertyField(overrideMaxHealthProperty, new GUIContent("Override Max Health"));
                    if (overrideMaxHealthProperty != null && overrideMaxHealthProperty.boolValue)
                    {
                        EditorGUILayout.PropertyField(maxHealthProperty, new GUIContent("Max Health"));
                    }

                    EditorGUILayout.PropertyField(overrideCombatDamageProperty, new GUIContent("Override Combat Damage"));
                    if (overrideCombatDamageProperty != null && overrideCombatDamageProperty.boolValue)
                    {
                        EditorGUILayout.PropertyField(combatDamageProperty, new GUIContent("Combat Damage"));
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2f);

                    if (removeSpawn)
                    {
                        spawnsProperty.DeleteArrayElementAtIndex(spawnIndex);
                        break;
                    }
                }
            }

            if (!removeWave && GUILayout.Button("Add Spawn"))
            {
                AddSpawn(spawnsProperty);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);

            if (removeWave)
            {
                wavesProperty.DeleteArrayElementAtIndex(waveIndex);
                break;
            }
        }
    }

    private void AddWave()
    {
        wavesProperty.arraySize++;
        SerializedProperty waveProperty = wavesProperty.GetArrayElementAtIndex(wavesProperty.arraySize - 1);

        SerializedProperty spawnsProperty = waveProperty.FindPropertyRelative("spawns");
        SerializedProperty delayBeforeSpawnProperty = waveProperty.FindPropertyRelative("delayBeforeSpawn");
        SerializedProperty delayAfterClearProperty = waveProperty.FindPropertyRelative("delayAfterClear");
        SerializedProperty platformTransitionProperty = waveProperty.FindPropertyRelative("platformTransition");

        if (spawnsProperty != null)
        {
            spawnsProperty.arraySize = 0;
        }

        if (delayBeforeSpawnProperty != null)
        {
            delayBeforeSpawnProperty.floatValue = 1f;
        }

        if (delayAfterClearProperty != null)
        {
            delayAfterClearProperty.floatValue = 0.75f;
        }

        if (platformTransitionProperty != null)
        {
            platformTransitionProperty.enumValueIndex = 0;
        }
    }

    private void AddSpawn(SerializedProperty spawnsProperty)
    {
        if (spawnsProperty == null)
        {
            return;
        }

        spawnsProperty.arraySize++;
        SerializedProperty spawnProperty = spawnsProperty.GetArrayElementAtIndex(spawnsProperty.arraySize - 1);
        SerializedProperty countProperty = spawnProperty.FindPropertyRelative("count");
        SerializedProperty randomSpawnProperty = spawnProperty.FindPropertyRelative("useRandomSpawnPoint");
        SerializedProperty spawnPointIndexProperty = spawnProperty.FindPropertyRelative("spawnPointIndex");
        SerializedProperty overrideScaleProperty = spawnProperty.FindPropertyRelative("overrideScale");
        SerializedProperty spawnScaleProperty = spawnProperty.FindPropertyRelative("spawnScale");
        SerializedProperty overrideMaxHealthProperty = spawnProperty.FindPropertyRelative("overrideMaxHealth");
        SerializedProperty maxHealthProperty = spawnProperty.FindPropertyRelative("maxHealth");
        SerializedProperty overrideCombatDamageProperty = spawnProperty.FindPropertyRelative("overrideCombatDamage");
        SerializedProperty combatDamageProperty = spawnProperty.FindPropertyRelative("combatDamage");

        if (countProperty != null)
        {
            countProperty.intValue = 1;
        }

        if (randomSpawnProperty != null)
        {
            randomSpawnProperty.boolValue = false;
        }

        if (spawnPointIndexProperty != null)
        {
            spawnPointIndexProperty.intValue = 0;
        }

        if (overrideScaleProperty != null)
        {
            overrideScaleProperty.boolValue = false;
        }

        if (spawnScaleProperty != null)
        {
            spawnScaleProperty.vector3Value = Vector3.one;
        }

        if (overrideMaxHealthProperty != null)
        {
            overrideMaxHealthProperty.boolValue = false;
        }

        if (maxHealthProperty != null)
        {
            maxHealthProperty.intValue = 1;
        }

        if (overrideCombatDamageProperty != null)
        {
            overrideCombatDamageProperty.boolValue = false;
        }

        if (combatDamageProperty != null)
        {
            combatDamageProperty.intValue = 1;
        }
    }

    private static void OpenEnemyPrefab(SerializedProperty enemyPrefabProperty)
    {
        if (enemyPrefabProperty == null || enemyPrefabProperty.objectReferenceValue == null)
        {
            return;
        }

        Object prefabObject = enemyPrefabProperty.objectReferenceValue;
        Selection.activeObject = prefabObject;
        EditorGUIUtility.PingObject(prefabObject);
        AssetDatabase.OpenAsset(prefabObject);
    }

    private void CreateWavePrefabCopy(SerializedProperty enemyPrefabProperty, int waveIndex, int spawnIndex)
    {
        if (enemyPrefabProperty == null || enemyPrefabProperty.objectReferenceValue == null)
        {
            return;
        }

        GameObject sourcePrefab = enemyPrefabProperty.objectReferenceValue as GameObject;
        if (sourcePrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Create Wave Copy",
                "The selected enemy prefab is not a GameObject prefab asset.",
                "OK");
            return;
        }

        string sourcePath = AssetDatabase.GetAssetPath(sourcePrefab);
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            EditorUtility.DisplayDialog(
                "Create Wave Copy",
                "The selected prefab does not have a valid asset path.",
                "OK");
            return;
        }

        string targetFolder = "Assets/Combat/ArenaEncounter/GeneratedEnemyPrefabs";
        Directory.CreateDirectory(targetFolder);

        string baseName = Path.GetFileNameWithoutExtension(sourcePath);
        string targetName = $"{baseName}_Wave{waveIndex + 1:00}_Spawn{spawnIndex + 1:00}.prefab";
        string targetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(targetFolder, targetName).Replace('\\', '/'));

        if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
        {
            EditorUtility.DisplayDialog(
                "Create Wave Copy",
                $"Failed to copy prefab from:\n{sourcePath}\n\nto:\n{targetPath}",
                "OK");
            return;
        }

        GameObject copiedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
        if (copiedPrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Create Wave Copy",
                $"The prefab copy was created, but could not be loaded:\n{targetPath}",
                "OK");
            return;
        }

        enemyPrefabProperty.objectReferenceValue = copiedPrefab;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = copiedPrefab;
        EditorGUIUtility.PingObject(copiedPrefab);
        AssetDatabase.OpenAsset(copiedPrefab);
    }

    private static void DrawSpawnPointSelector(SerializedProperty randomSpawnProperty, SerializedProperty spawnPointIndexProperty)
    {
        if (randomSpawnProperty == null || spawnPointIndexProperty == null)
        {
            return;
        }

        string[] spawnPointLabels = GetSpawnPointLabelsFromScene();

        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("Spawn Point", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(spawnPointLabels.Length > 0
            ? $"Found {spawnPointLabels.Length} spawn point(s) in the current scene."
            : "No scene spawn points found. Falling back to SpawnPoint_1..SpawnPoint_4.", EditorStyles.miniLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawSpawnPointButton(
                "Random",
                randomSpawnProperty.boolValue,
                () =>
                {
                    randomSpawnProperty.boolValue = true;
                });

            for (int i = 0; i < spawnPointLabels.Length; i++)
            {
                int index = i;
                DrawSpawnPointButton(
                    spawnPointLabels[i],
                    !randomSpawnProperty.boolValue && spawnPointIndexProperty.intValue == index,
                    () =>
                    {
                        randomSpawnProperty.boolValue = false;
                        spawnPointIndexProperty.intValue = index;
                    });
            }
        }
    }

    private static string[] GetSpawnPointLabelsFromScene()
    {
        List<string> labels = new List<string>();
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            return GetFallbackSpawnPointLabels();
        }

        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            CollectSpawnPointLabels(roots[i] != null ? roots[i].transform : null, labels);
        }

        if (labels.Count == 0)
        {
            return GetFallbackSpawnPointLabels();
        }

        labels.Sort(CompareSpawnPointLabels);
        return labels.ToArray();
    }

    private static void CollectSpawnPointLabels(Transform parent, List<string> labels)
    {
        if (parent == null || labels == null)
        {
            return;
        }

        foreach (Transform child in parent)
        {
            if (child == null)
            {
                continue;
            }

            if (child.name.StartsWith("SpawnPoint_", System.StringComparison.Ordinal))
            {
                if (!labels.Contains(child.name))
                {
                    labels.Add(child.name);
                }
            }

            CollectSpawnPointLabels(child, labels);
        }
    }

    private static int CompareSpawnPointLabels(string left, string right)
    {
        return GetSpawnPointIndex(left).CompareTo(GetSpawnPointIndex(right));
    }

    private static int GetSpawnPointIndex(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return int.MaxValue;
        }

        int underscoreIndex = label.LastIndexOf('_');
        if (underscoreIndex < 0 || underscoreIndex >= label.Length - 1)
        {
            return int.MaxValue;
        }

        if (int.TryParse(label.Substring(underscoreIndex + 1), out int index))
        {
            return index;
        }

        return int.MaxValue;
    }

    private static string[] GetFallbackSpawnPointLabels()
    {
        return new[]
        {
            "SpawnPoint_1",
            "SpawnPoint_2",
            "SpawnPoint_3",
            "SpawnPoint_4"
        };
    }

    private static void DrawSpawnPointButton(string label, bool selected, System.Action onClick)
    {
        Color oldBackgroundColor = GUI.backgroundColor;
        if (selected)
        {
            GUI.backgroundColor = new Color(0.55f, 0.8f, 1f, 1f);
        }

        if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.MinWidth(72f)))
        {
            onClick?.Invoke();
        }

        GUI.backgroundColor = oldBackgroundColor;
    }

    private static void DrawPlatformTransitionSelector(SerializedProperty platformTransitionProperty)
    {
        if (platformTransitionProperty == null || platformTransitionProperty.propertyType != SerializedPropertyType.Enum)
        {
            return;
        }

        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("Platform Transition", EditorStyles.boldLabel);

        int selectedIndex = Mathf.Clamp(platformTransitionProperty.enumValueIndex, 0, platformTransitionProperty.enumDisplayNames.Length - 1);
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawPlatformTransitionButton(platformTransitionProperty, 0, "None", selectedIndex == 0);
            DrawPlatformTransitionButton(platformTransitionProperty, 1, "Show", selectedIndex == 1);
            DrawPlatformTransitionButton(platformTransitionProperty, 2, "Hide", selectedIndex == 2);
        }

        EditorGUILayout.LabelField($"Current: {platformTransitionProperty.enumDisplayNames[selectedIndex]}", EditorStyles.miniLabel);
    }

    private static void DrawPlatformTransitionButton(SerializedProperty property, int value, string label, bool selected)
    {
        Color oldBackgroundColor = GUI.backgroundColor;
        if (selected)
        {
            GUI.backgroundColor = new Color(0.65f, 0.9f, 0.65f, 1f);
        }

        if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.MinWidth(64f)))
        {
            property.enumValueIndex = value;
        }

        GUI.backgroundColor = oldBackgroundColor;
    }
}
