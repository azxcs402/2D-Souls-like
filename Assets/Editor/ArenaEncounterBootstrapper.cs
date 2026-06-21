using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class ArenaEncounterBootstrapper
{
    private const string WaveSetAssetPath = "Assets/Combat/ArenaEncounter/ArenaWaveSet.asset";
    private const string AbyssMagePrefabPath = "Assets/Prefabs/Enemy/Boss/Enemy_AbyssMage.prefab";
    private const string GroundLayerName = "Ground";
    private const string AbyssPowerRootName = "AbyssPower";
    private static readonly Vector3 BossAbyssPowerLocalPosition = new Vector3(0.5f, 0.5f, 0f);

    static ArenaEncounterBootstrapper()
    {
        EditorApplication.delayCall += EnsureSceneObjects;
        EditorSceneManager.sceneOpened += (_, _) => EditorApplication.delayCall += EnsureSceneObjects;
    }

    [MenuItem("Tools/Arena Encounter/Create Room Setup")]
    public static void CreateRoomSetup()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected != null && PrefabUtility.IsPartOfPrefabAsset(selected))
        {
            Debug.LogWarning("Select a scene object, not a prefab asset, before creating the arena setup.");
            return;
        }

        EnsureFolders();

        GameObject encounterRoot = FindExistingArenaEncounter();
        bool createdEncounterRoot = false;
        if (encounterRoot == null)
        {
            encounterRoot = CreateGameObject("ArenaEncounter", selected != null ? selected.transform : null);
            createdEncounterRoot = true;
        }

        BoxCollider2D triggerCollider = encounterRoot.GetComponent<BoxCollider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = Undo.AddComponent<BoxCollider2D>(encounterRoot);
            triggerCollider.isTrigger = true;
        }

        bool createdController = false;
        ArenaEncounterController encounterController = encounterRoot.GetComponent<ArenaEncounterController>();
        if (encounterController == null)
        {
            encounterController = Undo.AddComponent<ArenaEncounterController>(encounterRoot);
            createdController = true;
        }

        GameObject spawnRoot = FindOrCreateChild(encounterRoot.transform, "SpawnPoints");
        Transform[] spawnPoints = EnsureSpawnPoints(spawnRoot.transform);

        Transform gridTransform = selected != null
            ? FindDeepChild(selected.transform.root, "Grid")
            : FindGridInActiveScene();
        Transform doorParent = gridTransform != null ? gridTransform : encounterRoot.transform;
        GameObject doorGroup = FindDeepChild(encounterRoot.transform, "ArenaDoors")?.gameObject
            ?? FindOrCreateChild(doorParent, "ArenaDoors");
        if (doorGroup.transform.parent != doorParent)
        {
            Undo.SetTransformParent(doorGroup.transform, doorParent, "Parent ArenaDoors");
        }

        RemoveChildrenExcept(doorGroup.transform, "Door");
        ArenaDoorController door = FindOrCreateDoor(doorGroup.transform, "Door", new Vector3(-6f, 0f, 0f), 3f);
        ArenaFloatingPlatformController floatingPlatform = FindOrCreateFloatingPlatform(encounterRoot.transform);

        ArenaWaveSet waveSet = EnsureWaveSetAsset();

        SerializedObject controllerSo = new SerializedObject(encounterController);
        if (IsObjectReferenceNull(controllerSo, "waveSet"))
        {
            SetObject(controllerSo, "waveSet", waveSet);
        }

        if (ShouldFillArray(controllerSo, "spawnPoints"))
        {
            SetObjectArray(controllerSo, "spawnPoints", spawnPoints);
        }

        if (ShouldFillArray(controllerSo, "doors"))
        {
            SetObjectArray(controllerSo, "doors", new Object[] { door });
        }

        if (IsObjectReferenceNull(controllerSo, "floatingPlatform"))
        {
            SetObject(controllerSo, "floatingPlatform", floatingPlatform);
        }

        if (createdEncounterRoot || createdController)
        {
            SetBool(controllerSo, "lockDoorsWhenEncounterStarts", true);
            SetBool(controllerSo, "startOnlyOnce", true);
            SetFloat(controllerSo, "defaultDelayBeforeSpawn", 0.25f);
            SetFloat(controllerSo, "defaultDelayAfterClear", 0.75f);
            SetFloat(controllerSo, "clearConfirmDelay", 0.25f);
        }
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = encounterRoot;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log(createdEncounterRoot
            ? "Arena encounter setup created. Fill the wave set, then assign enemy prefabs and door tiles."
            : "Existing ArenaEncounter reused. Missing setup parts were filled without overwriting your current settings.");
    }

    [MenuItem("Tools/Arena Encounter/Create Boss Room Setup")]
    public static void CreateBossRoomSetup()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected != null && PrefabUtility.IsPartOfPrefabAsset(selected))
        {
            Debug.LogWarning("Select a scene object, not a prefab asset, before creating the boss arena setup.");
            return;
        }

        GameObject encounterRoot = CreateGameObject("ArenaBossEncounter", selected != null ? selected.transform : null);

        BoxCollider2D triggerCollider = encounterRoot.GetComponent<BoxCollider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = Undo.AddComponent<BoxCollider2D>(encounterRoot);
        }
        triggerCollider.isTrigger = true;

        ArenaBossEncounterController encounterController = encounterRoot.GetComponent<ArenaBossEncounterController>();
        if (encounterController == null)
        {
            encounterController = Undo.AddComponent<ArenaBossEncounterController>(encounterRoot);
        }

        SerializedObject controllerSo = new SerializedObject(encounterController);
        if (IsObjectReferenceNull(controllerSo, "bossEnemy"))
        {
            Enemy selectedBoss = selected != null ? selected.GetComponentInParent<Enemy>(true) : null;
            if (selectedBoss != null)
            {
                SetObject(controllerSo, "bossEnemy", selectedBoss);
            }
        }

        if (IsObjectReferenceNull(controllerSo, "bossPrefab"))
        {
            GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AbyssMagePrefabPath);
            if (bossPrefab != null)
            {
                SetObject(controllerSo, "bossPrefab", bossPrefab);
            }
        }

        SetBool(controllerSo, "lockDoorsWhenEncounterStarts", true);
        SetBool(controllerSo, "startOnlyOnce", true);
        SetFloat(controllerSo, "clearConfirmDelay", 0.25f);

        GameObject bossDoorGroup = FindOrCreateChild(encounterRoot.transform, "ArenaDoors");
        if (bossDoorGroup.transform.parent != encounterRoot.transform)
        {
            Undo.SetTransformParent(bossDoorGroup.transform, encounterRoot.transform, "Parent ArenaDoors");
        }

        RemoveChildrenExcept(bossDoorGroup.transform, "Door_Left");
        ArenaDoorController leftDoor = FindOrCreateDoor(bossDoorGroup.transform, "Door_Left", new Vector3(0.5f, 0.5f, 0f), 4f);
        SetBossDoorBlockingLayers(leftDoor != null ? leftDoor.transform : null);
        SetObjectArray(controllerSo, "doors", new Object[] { leftDoor });

        GameObject bossBackgroundGroup = FindOrCreateBossBackgrounds(encounterRoot.transform);
        SetObjectArray(controllerSo, "skillPointBackgrounds", FindBossBackgroundChildren(bossBackgroundGroup.transform));

        GameObject abyssPowerGroup = FindOrCreateAbyssPowers(encounterRoot.transform);
        SetObjectArray(controllerSo, "abyssPowers", FindAbyssPowerChildren(abyssPowerGroup.transform));

        ArenaBossFloatingPlatformController bossFloatingPlatform = FindOrCreateBossFloatingPlatform(encounterRoot.transform);
        SetObject(controllerSo, "bossFloatingPlatform", bossFloatingPlatform);

        GameObject bossTarget = FindOrCreateBossTarget(encounterRoot.transform);
        SetObject(controllerSo, "bossTarget", bossTarget != null ? bossTarget.transform : null);

        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = encounterRoot;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Boss arena setup created. The room now uses its own ArenaDoors/Door_Left group.");
    }

    [MenuItem("Tools/Arena Encounter/Repair Boss Room Setup")]
    public static void RepairBossRoomSetup()
    {
        EnsureFolders();
        EnsureSceneObjects();

        Scene activeScene = SceneManager.GetActiveScene();
        GameObject arenaBossEncounter = FindGameObjectInScene(activeScene, "ArenaBossEncounter");
        if (arenaBossEncounter == null)
        {
            Debug.LogWarning("Could not find ArenaBossEncounter in the active scene.");
            return;
        }

        GameObject abyssPowerGroup = FindOrCreateAbyssPowers(arenaBossEncounter.transform);
        if (abyssPowerGroup != null)
        {
            Selection.activeGameObject = abyssPowerGroup;
            EditorGUIUtility.PingObject(abyssPowerGroup);
            EditorApplication.RepaintHierarchyWindow();
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("AbyssPower group repaired or created under ArenaBossEncounter.");
        }

        ArenaBossFloatingPlatformController bossFloatingPlatform = FindOrCreateBossFloatingPlatform(arenaBossEncounter.transform);
        if (bossFloatingPlatform != null)
        {
            Selection.activeGameObject = bossFloatingPlatform.gameObject;
            EditorGUIUtility.PingObject(bossFloatingPlatform.gameObject);
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("Boss floating platform repaired or created under ArenaBossEncounter.");
        }
    }

    [MenuItem("Tools/Arena Encounter/Repair Sample Scene Arena Doors")]
    public static void RepairSampleSceneArenaDoors()
    {
        EnsureFolders();

        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        bool changed = false;

        GameObject arenaEncounter = FindGameObjectInScene(scene, "ArenaEncounter");
        if (arenaEncounter != null)
        {
            changed |= RepairArenaEncounterDoors(arenaEncounter.transform);
        }

        GameObject arenaBossEncounter = FindGameObjectInScene(scene, "ArenaBossEncounter");
        if (arenaBossEncounter != null)
        {
            changed |= RepairArenaBossDoors(arenaBossEncounter.transform);
            changed |= RepairArenaBossAbyssPowers(arenaBossEncounter.transform);
            changed |= RepairArenaBossFloatingPlatform(arenaBossEncounter.transform);
            changed |= RepairArenaBossTarget(arenaBossEncounter.transform);
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log(changed
            ? "ArenaEncounter arena doors repaired and saved."
            : "ArenaEncounter arena doors were already in the expected structure.");
    }

    [MenuItem("Tools/Arena Encounter/Repair Default Wave Set")]
    public static void RepairDefaultWaveSet()
    {
        EnsureFolders();
        ArenaWaveSet waveSet = EnsureWaveSetAsset(forceRepair: true);
        if (waveSet != null)
        {
            Selection.activeObject = waveSet;
            EditorGUIUtility.PingObject(waveSet);
        }
    }

    [MenuItem("Tools/Arena Encounter/Repair Floating Platform Layers")]
    public static void RepairFloatingPlatformLayers()
    {
        int repairedCount = 0;
        ArenaFloatingPlatformController[] platforms = Object.FindObjectsOfType<ArenaFloatingPlatformController>(true);
        for (int i = 0; i < platforms.Length; i++)
        {
            ArenaFloatingPlatformController platform = platforms[i];
            if (platform == null || !platform.gameObject.scene.IsValid() || !platform.gameObject.scene.isLoaded)
            {
                continue;
            }

            if (RepairFloatingPlatformLayerSet(platform))
            {
                repairedCount++;
                EditorUtility.SetDirty(platform);
            }
        }

        if (repairedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        Debug.Log($"ArenaEncounter: repaired floating platform layers on {repairedCount} scene object(s).");
    }

    private static void EnsureSceneObjects()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += EnsureSceneObjects;
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            return;
        }

        GameObject arenaBossEncounter = FindGameObjectInScene(activeScene, "ArenaBossEncounter");
        if (arenaBossEncounter == null)
        {
            return;
        }

        bool changed = RepairArenaBossAbyssPowers(arenaBossEncounter.transform);
        changed |= RepairArenaBossFloatingPlatform(arenaBossEncounter.transform);
        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
        }
    }

    [MenuItem("Tools/Arena Encounter/Create Room Setup", true)]
    private static bool CanCreateRoomSetup()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory("Assets/Combat");
        Directory.CreateDirectory("Assets/Combat/ArenaEncounter");
    }

    private static ArenaWaveSet EnsureWaveSetAsset(bool forceRepair = false)
    {
        ArenaWaveSet waveSet = AssetDatabase.LoadAssetAtPath<ArenaWaveSet>(WaveSetAssetPath);
        if (waveSet != null)
        {
            EnsureDefaultWaveSetSetup(waveSet, forceRepair);
            return waveSet;
        }

        waveSet = ScriptableObject.CreateInstance<ArenaWaveSet>();
        AssetDatabase.CreateAsset(waveSet, WaveSetAssetPath);
        EnsureDefaultWaveSetSetup(waveSet, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.SetDirty(waveSet);
        return waveSet;
    }

    private static void EnsureDefaultWaveSetSetup(ArenaWaveSet waveSet, bool forceRepair)
    {
        if (waveSet == null)
        {
            return;
        }

        SerializedObject waveSetSo = new SerializedObject(waveSet);
        SerializedProperty wavesProperty = waveSetSo.FindProperty("waves");
        if (wavesProperty == null)
        {
            return;
        }

        GameObject skeletonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/Enemy_Skeleton.prefab");
        if (skeletonPrefab == null)
        {
            Debug.LogWarning("Default ArenaWaveSet could not be initialized because Enemy_Skeleton.prefab was not found.");
            return;
        }

        bool needsRepair = forceRepair || wavesProperty.arraySize == 0;
        if (!needsRepair && wavesProperty.arraySize > 0)
        {
            SerializedProperty existingWaveProperty = wavesProperty.GetArrayElementAtIndex(0);
            SerializedProperty existingSpawnsProperty = existingWaveProperty.FindPropertyRelative("spawns");
            if (existingSpawnsProperty == null || existingSpawnsProperty.arraySize < 2)
            {
                needsRepair = true;
            }
            else
            {
                SerializedProperty firstSpawnPrefab = existingSpawnsProperty.GetArrayElementAtIndex(0).FindPropertyRelative("enemyPrefab");
                SerializedProperty secondSpawnPrefab = existingSpawnsProperty.GetArrayElementAtIndex(1).FindPropertyRelative("enemyPrefab");
                needsRepair = firstSpawnPrefab == null || firstSpawnPrefab.objectReferenceValue == null
                    || secondSpawnPrefab == null || secondSpawnPrefab.objectReferenceValue == null;
            }
        }

        if (!needsRepair)
        {
            return;
        }

        wavesProperty.arraySize = 1;
        SerializedProperty waveProperty = wavesProperty.GetArrayElementAtIndex(0);

        SerializedProperty delayBeforeSpawnProperty = waveProperty.FindPropertyRelative("delayBeforeSpawn");
        if (delayBeforeSpawnProperty != null)
        {
            delayBeforeSpawnProperty.floatValue = 1f;
        }

        SerializedProperty delayAfterClearProperty = waveProperty.FindPropertyRelative("delayAfterClear");
        if (delayAfterClearProperty != null)
        {
            delayAfterClearProperty.floatValue = 0.75f;
        }

        SerializedProperty spawnsProperty = waveProperty.FindPropertyRelative("spawns");
        if (spawnsProperty == null)
        {
            waveSetSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(waveSet);
            AssetDatabase.SaveAssets();
            return;
        }

        spawnsProperty.arraySize = 2;
        ConfigureSpawnEntry(spawnsProperty.GetArrayElementAtIndex(0), skeletonPrefab, 0);
        ConfigureSpawnEntry(spawnsProperty.GetArrayElementAtIndex(1), skeletonPrefab, 2);

        waveSetSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(waveSet);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(WaveSetAssetPath, ImportAssetOptions.ForceUpdate);
    }

    private static void ConfigureSpawnEntry(SerializedProperty spawnEntryProperty, GameObject enemyPrefab, int spawnPointIndex)
    {
        if (spawnEntryProperty == null)
        {
            return;
        }

        SerializedProperty enemyPrefabProperty = spawnEntryProperty.FindPropertyRelative("enemyPrefab");
        if (enemyPrefabProperty != null)
        {
            enemyPrefabProperty.objectReferenceValue = enemyPrefab;
        }

        SerializedProperty countProperty = spawnEntryProperty.FindPropertyRelative("count");
        if (countProperty != null)
        {
            countProperty.intValue = 1;
        }

        SerializedProperty useRandomSpawnPointProperty = spawnEntryProperty.FindPropertyRelative("useRandomSpawnPoint");
        if (useRandomSpawnPointProperty != null)
        {
            useRandomSpawnPointProperty.boolValue = false;
        }

        SerializedProperty spawnPointIndexProperty = spawnEntryProperty.FindPropertyRelative("spawnPointIndex");
        if (spawnPointIndexProperty != null)
        {
            spawnPointIndexProperty.intValue = spawnPointIndex;
        }
    }

    private static GameObject FindExistingArenaEncounter()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            return null;
        }

        GameObject[] rootObjects = activeScene.GetRootGameObjects();
        for (int i = 0; i < rootObjects.Length; i++)
        {
            Transform found = FindDeepChild(rootObjects[i].transform, "ArenaEncounter");
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static GameObject FindGameObjectInScene(Scene scene, string objectName)
    {
        if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        GameObject[] rootObjects = scene.GetRootGameObjects();
        for (int i = 0; i < rootObjects.Length; i++)
        {
            GameObject rootObject = rootObjects[i];
            if (rootObject != null && rootObject.name == objectName)
            {
                return rootObject;
            }

            if (rootObject != null)
            {
                Transform found = FindDeepChild(rootObject.transform, objectName);
                if (found != null)
                {
                    return found.gameObject;
                }
            }
        }

        return null;
    }

    private static GameObject FindOrCreateChild(Transform parent, string childName)
    {
        Transform existing = parent != null ? parent.Find(childName) : null;
        if (existing != null)
        {
            return existing.gameObject;
        }

        return CreateGameObject(childName, parent);
    }

    private static Transform[] EnsureSpawnPoints(Transform spawnRoot)
    {
        Vector3[] spawnOffsets =
        {
            new Vector3(-4f, 1f, 0f),
            new Vector3(-2f, 1f, 0f),
            new Vector3(2f, 1f, 0f),
            new Vector3(4f, 1f, 0f)
        };

        Transform[] spawnPoints = new Transform[spawnOffsets.Length];
        for (int i = 0; i < spawnOffsets.Length; i++)
        {
            string spawnPointName = $"SpawnPoint_{i + 1}";
            Transform existing = spawnRoot.Find(spawnPointName);
            if (existing == null)
            {
                GameObject spawnPoint = CreateGameObject(spawnPointName, spawnRoot);
                spawnPoint.transform.localPosition = spawnOffsets[i];
                existing = spawnPoint.transform;
            }

            spawnPoints[i] = existing;
        }

        return spawnPoints;
    }

    private static ArenaDoorController FindOrCreateDoor(Transform parent, string doorName, Vector3 localPosition, float openVerticalOffset)
    {
        Transform existingDoor = parent.Find(doorName);
        if (existingDoor == null)
        {
            return CreateDoor(parent, doorName, localPosition, openVerticalOffset);
        }

        ArenaDoorController doorController = existingDoor.GetComponent<ArenaDoorController>();
        if (doorController == null)
        {
            doorController = Undo.AddComponent<ArenaDoorController>(existingDoor.gameObject);
        }

        EnsureDoorChildren(existingDoor, doorController, openVerticalOffset);
        existingDoor.localPosition = localPosition;
        return doorController;
    }

    private static ArenaDoorController CreateDoor(Transform parent, string doorName, Vector3 localPosition, float openVerticalOffset)
    {
        GameObject doorRoot = CreateGameObject(doorName, parent);
        doorRoot.transform.localPosition = localPosition;

        ArenaDoorController doorController = Undo.AddComponent<ArenaDoorController>(doorRoot);

        GameObject visualRoot = CreateGameObject("Visual", doorRoot.transform);
        GameObject blockingRoot = CreateGameObject("Blocking_1", doorRoot.transform);
        GameObject blockingDropRoot = CreateGameObject("Blocking_2", doorRoot.transform);

        Tilemap visualTilemap = Undo.AddComponent<Tilemap>(visualRoot);
        TilemapRenderer visualRenderer = Undo.AddComponent<TilemapRenderer>(visualRoot);
        visualRenderer.sortingLayerName = "Background";
        visualRenderer.sortingOrder = 100;

        Tilemap blockingTilemap = Undo.AddComponent<Tilemap>(blockingRoot);
        TilemapRenderer blockingRenderer = Undo.AddComponent<TilemapRenderer>(blockingRoot);
        blockingRenderer.sortingLayerName = "Background";
        TilemapCollider2D blockingTilemapCollider = Undo.AddComponent<TilemapCollider2D>(blockingRoot);
        Rigidbody2D blockingBody = Undo.AddComponent<Rigidbody2D>(blockingRoot);
        CompositeCollider2D blockingComposite = Undo.AddComponent<CompositeCollider2D>(blockingRoot);

        blockingBody.bodyType = RigidbodyType2D.Static;
        blockingBody.simulated = true;
        blockingBody.useAutoMass = false;

        blockingTilemapCollider.usedByComposite = true;
        blockingComposite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        blockingRenderer.sortingOrder = 101;

        Tilemap blockingDropTilemap = Undo.AddComponent<Tilemap>(blockingDropRoot);
        TilemapRenderer blockingDropRenderer = Undo.AddComponent<TilemapRenderer>(blockingDropRoot);
        blockingDropRenderer.sortingLayerName = "Background";
        blockingDropRenderer.sortingOrder = 102;
        TilemapCollider2D blockingDropTilemapCollider = Undo.AddComponent<TilemapCollider2D>(blockingDropRoot);
        Rigidbody2D blockingDropBody = Undo.AddComponent<Rigidbody2D>(blockingDropRoot);
        CompositeCollider2D blockingDropComposite = Undo.AddComponent<CompositeCollider2D>(blockingDropRoot);

        blockingDropBody.bodyType = RigidbodyType2D.Static;
        blockingDropBody.simulated = true;
        blockingDropBody.useAutoMass = false;

        blockingDropTilemapCollider.usedByComposite = true;
        blockingDropComposite.geometryType = CompositeCollider2D.GeometryType.Polygons;

        SerializedObject doorSo = new SerializedObject(doorController);
        SetObject(doorSo, "visualRoot", visualRoot);
        SetObject(doorSo, "blockingRoot", blockingRoot);
        SetObject(doorSo, "blockingDropRoot", blockingDropRoot);
        SetFloat(doorSo, "openVerticalOffset", openVerticalOffset);
        SetBool(doorSo, "openOnStart", true);
        SetString(doorSo, "openBoolParameter", "open");
        doorSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(doorController);
        EditorUtility.SetDirty(visualTilemap);
        EditorUtility.SetDirty(visualRenderer);
        EditorUtility.SetDirty(blockingTilemap);
        EditorUtility.SetDirty(blockingRenderer);
        EditorUtility.SetDirty(blockingTilemapCollider);
        EditorUtility.SetDirty(blockingBody);
        EditorUtility.SetDirty(blockingComposite);
        EditorUtility.SetDirty(blockingDropTilemap);
        EditorUtility.SetDirty(blockingDropRenderer);
        EditorUtility.SetDirty(blockingDropTilemapCollider);
        EditorUtility.SetDirty(blockingDropBody);
        EditorUtility.SetDirty(blockingDropComposite);

        return doorController;
    }

    private static ArenaFloatingPlatformController FindOrCreateFloatingPlatform(Transform parent)
    {
        Transform existingPlatform = FindDeepChild(parent, "ArenaFloatingPlatform");
        if (existingPlatform == null)
        {
            return CreateFloatingPlatform(parent);
        }

        ArenaFloatingPlatformController platformController = existingPlatform.GetComponent<ArenaFloatingPlatformController>();
        if (platformController == null)
        {
            platformController = Undo.AddComponent<ArenaFloatingPlatformController>(existingPlatform.gameObject);
        }

        EnsureFloatingPlatformChildren(existingPlatform, platformController);
        return platformController;
    }

    private static bool RepairArenaEncounterDoors(Transform encounterRoot)
    {
        if (encounterRoot == null)
        {
            return false;
        }

        GameObject doorGroup = FindOrCreateChild(encounterRoot, "ArenaDoors");
        if (doorGroup.transform.parent != encounterRoot)
        {
            Undo.SetTransformParent(doorGroup.transform, encounterRoot, "Parent ArenaDoors");
        }

        doorGroup.transform.localPosition = Vector3.zero;
        doorGroup.transform.localRotation = Quaternion.identity;
        doorGroup.transform.localScale = Vector3.one;

        RemoveChildrenExcept(doorGroup.transform, "Door");
        ArenaDoorController door = FindOrCreateDoor(doorGroup.transform, "Door", new Vector3(-6f, 0f, 0f), 3f);

        ArenaEncounterController controller = encounterRoot.GetComponent<ArenaEncounterController>();
        if (controller != null)
        {
            SerializedObject controllerSo = new SerializedObject(controller);
            SetObjectArray(controllerSo, "doors", new Object[] { door });
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        return true;
    }

    private static bool RepairArenaBossDoors(Transform encounterRoot)
    {
        if (encounterRoot == null)
        {
            return false;
        }

        GameObject doorGroup = FindOrCreateChild(encounterRoot, "ArenaDoors");
        if (doorGroup.transform.parent != encounterRoot)
        {
            Undo.SetTransformParent(doorGroup.transform, encounterRoot, "Parent ArenaDoors");
        }

        RemoveChildrenExcept(doorGroup.transform, "Door_Left");
        ArenaDoorController leftDoor = FindOrCreateDoor(doorGroup.transform, "Door_Left", new Vector3(0.5f, 0.5f, 0f), 4f);
        SetBossDoorBlockingLayers(leftDoor != null ? leftDoor.transform : null);

        ArenaBossEncounterController controller = encounterRoot.GetComponent<ArenaBossEncounterController>();
        if (controller != null)
        {
            SerializedObject controllerSo = new SerializedObject(controller);
            SetObjectArray(controllerSo, "doors", new Object[] { leftDoor });
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        return true;
    }

    private static bool RepairArenaBossTarget(Transform encounterRoot)
    {
        if (encounterRoot == null)
        {
            return false;
        }

        GameObject targetObject = FindOrCreateBossTarget(encounterRoot);
        if (targetObject == null)
        {
            return false;
        }

        ArenaBossEncounterController controller = encounterRoot.GetComponent<ArenaBossEncounterController>();
        if (controller != null)
        {
            SerializedObject controllerSo = new SerializedObject(controller);
            SetObject(controllerSo, "bossTarget", targetObject.transform);
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        return true;
    }

    private static bool RepairArenaBossAbyssPowers(Transform encounterRoot)
    {
        if (encounterRoot == null)
        {
            return false;
        }

        GameObject abyssPowerGroup = FindOrCreateAbyssPowers(encounterRoot);
        if (abyssPowerGroup == null)
        {
            return false;
        }

        NormalizeBossAbyssPowerPositions(abyssPowerGroup.transform);

        ArenaBossEncounterController controller = encounterRoot.GetComponent<ArenaBossEncounterController>();
        if (controller != null)
        {
            SerializedObject controllerSo = new SerializedObject(controller);
            SetObjectArray(controllerSo, "abyssPowers", FindAbyssPowerChildren(abyssPowerGroup.transform));
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        return true;
    }

    private static void NormalizeBossAbyssPowerPositions(Transform powersRoot)
    {
        if (powersRoot == null)
        {
            return;
        }

        for (int i = 0; i < 6; i++)
        {
            Transform child = powersRoot.Find($"{AbyssPowerRootName}_{i + 1}");
            if (child == null)
            {
                continue;
            }

            Vector3 legacyPosition = new Vector3(-3.0f + i * 1.2f, 1.8f, 0f);
            if (child.localPosition == legacyPosition)
            {
                Undo.RecordObject(child, "Repair Boss Abyss Power Position");
                child.localPosition = BossAbyssPowerLocalPosition;
                EditorUtility.SetDirty(child.gameObject);
            }
        }
    }

    private static ArenaDoorController[] FindSceneArenaDoors()
    {
        Transform arenaDoors = FindDeepChildInScene("ArenaDoors");
        if (arenaDoors == null)
        {
            return new ArenaDoorController[0];
        }

        List<ArenaDoorController> doorControllers = new List<ArenaDoorController>();
        for (int i = 0; i < arenaDoors.childCount; i++)
        {
            ArenaDoorController door = arenaDoors.GetChild(i).GetComponent<ArenaDoorController>();
            if (door != null)
            {
                doorControllers.Add(door);
            }
        }

        return doorControllers.ToArray();
    }

    private static void RemoveChildrenExcept(Transform parent, string childNameToKeep)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child != null && child.name != childNameToKeep)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
    }

    private static Transform FindDeepChildInScene(string childName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            return null;
        }

        GameObject[] rootObjects = activeScene.GetRootGameObjects();
        for (int i = 0; i < rootObjects.Length; i++)
        {
            Transform found = FindDeepChild(rootObjects[i].transform, childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static ArenaFloatingPlatformController CreateFloatingPlatform(Transform parent)
    {
        GameObject platformRoot = CreateGameObject("ArenaFloatingPlatform", parent);
        platformRoot.transform.localPosition = Vector3.zero;

        ArenaFloatingPlatformController platformController = Undo.AddComponent<ArenaFloatingPlatformController>(platformRoot);

        GameObject groundReferencePoint = EnsureGroundReferencePoint(platformRoot.transform);
        GameObject platform1Root = CreateFloatingPlatformLayer(platformRoot.transform, "Platform_1", true, 5);
        GameObject platform2Root = CreateFloatingPlatformLayer(platformRoot.transform, "Platform_2", true, 6);
        GameObject platform3Root = CreateFloatingPlatformLayer(platformRoot.transform, "Platform_3", true, 7);

        SerializedObject platformSo = new SerializedObject(platformController);
        SetObject(platformSo, "groundReferencePoint", groundReferencePoint != null ? groundReferencePoint.transform : null);
        SetObject(platformSo, "platform1Tilemap", platform1Root.GetComponent<Tilemap>());
        SetObject(platformSo, "platform2Tilemap", platform2Root.GetComponent<Tilemap>());
        SetObject(platformSo, "platform3Tilemap", platform3Root.GetComponent<Tilemap>());
        SetBool(platformSo, "startHidden", true);
        SetFloat(platformSo, "platformRiseDuration", 0.85f);
        platformSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(platformController);
        return platformController;
    }

    private static ArenaBossFloatingPlatformController FindOrCreateBossFloatingPlatform(Transform parent)
    {
        Transform existingPlatform = FindDeepChild(parent, "ArenaFloatingPlatform");
        if (existingPlatform == null)
        {
            return CreateBossFloatingPlatform(parent);
        }

        ArenaBossFloatingPlatformController platformController = existingPlatform.GetComponent<ArenaBossFloatingPlatformController>();
        if (platformController == null)
        {
            platformController = Undo.AddComponent<ArenaBossFloatingPlatformController>(existingPlatform.gameObject);
        }

        EnsureBossFloatingPlatformChildren(existingPlatform, platformController);
        return platformController;
    }

    private static ArenaBossFloatingPlatformController CreateBossFloatingPlatform(Transform parent)
    {
        GameObject platformRoot = CreateGameObject("ArenaFloatingPlatform", parent);
        platformRoot.transform.localPosition = Vector3.zero;

        ArenaBossFloatingPlatformController platformController = Undo.AddComponent<ArenaBossFloatingPlatformController>(platformRoot);

        GameObject groundReferencePoint = EnsureGroundReferencePoint(platformRoot.transform);
        GameObject platform1Root = CreateFloatingPlatformLayer(platformRoot.transform, "Platform_1", true, 5);
        GameObject platform2Root = CreateFloatingPlatformLayer(platformRoot.transform, "Platform_2", true, 6);
        GameObject platform3Root = CreateFloatingPlatformLayer(platformRoot.transform, "Platform_3", true, 7);
        GameObject platform4Root = CreateFloatingPlatformLayer(platformRoot.transform, "Platform_4", true, 8);
        GameObject platform5Root = CreateFloatingPlatformLayer(platformRoot.transform, "Platform_5", true, 9);
        GameObject platform6Root = CreateFloatingPlatformLayer(platformRoot.transform, "Platform_6", true, 10);

        SerializedObject platformSo = new SerializedObject(platformController);
        SetObject(platformSo, "groundReferencePoint", groundReferencePoint != null ? groundReferencePoint.transform : null);
        SetObject(platformSo, "platform1Tilemap", platform1Root.GetComponent<Tilemap>());
        SetObject(platformSo, "platform2Tilemap", platform2Root.GetComponent<Tilemap>());
        SetObject(platformSo, "platform3Tilemap", platform3Root.GetComponent<Tilemap>());
        SetObject(platformSo, "platform4Tilemap", platform4Root.GetComponent<Tilemap>());
        SetObject(platformSo, "platform5Tilemap", platform5Root.GetComponent<Tilemap>());
        SetObject(platformSo, "platform6Tilemap", platform6Root.GetComponent<Tilemap>());
        SetBool(platformSo, "startHidden", true);
        SetFloat(platformSo, "platformRiseInterval", 0.7f);
        SetFloat(platformSo, "platformRiseDuration", 0.85f);
        platformSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(platformController);
        return platformController;
    }

    private static GameObject FindOrCreateBossBackgrounds(Transform parent)
    {
        GameObject backgroundsRoot = FindOrCreateChild(parent, "BossBackgrounds");
        if (backgroundsRoot == null)
        {
            return null;
        }

        backgroundsRoot.transform.localPosition = Vector3.zero;
        backgroundsRoot.transform.localRotation = Quaternion.identity;
        backgroundsRoot.transform.localScale = Vector3.one;

        for (int i = 0; i < 6; i++)
        {
            string childName = $"Background{i + 1}";
            GameObject background = FindOrCreateChild(backgroundsRoot.transform, childName);
            EnsureBossBackgroundLayer(background, 3 + i);
        }

        return backgroundsRoot;
    }

    private static GameObject FindOrCreateAbyssPowers(Transform parent)
    {
        GameObject powersRoot = FindOrCreateChild(parent, AbyssPowerRootName);
        if (powersRoot == null)
        {
            return null;
        }

        for (int i = 0; i < 6; i++)
        {
            string childName = $"{AbyssPowerRootName}_{i + 1}";
            Transform existing = powersRoot.transform.Find(childName);
            bool created = existing == null;
            GameObject power = created ? FindOrCreateChild(powersRoot.transform, childName) : existing.gameObject;
            EnsureAbyssPowerVisual(power, created);
        }

        return powersRoot;
    }

    private static GameObject FindOrCreateBossTarget(Transform parent)
    {
        GameObject targetObject = FindDeepChild(parent, "BossTarget")?.gameObject
            ?? FindDeepChild(parent, "BossCameraTarget")?.gameObject
            ?? FindOrCreateChild(parent, "BossTarget");
        if (targetObject == null)
        {
            return null;
        }

        targetObject.name = "BossTarget";
        targetObject.transform.localPosition = Vector3.zero;
        targetObject.transform.localRotation = Quaternion.identity;
        targetObject.transform.localScale = Vector3.one;
        targetObject.layer = parent != null ? parent.gameObject.layer : targetObject.layer;
        return targetObject;
    }

    private static GameObject[] FindBossBackgroundChildren(Transform backgroundsRoot)
    {
        if (backgroundsRoot == null)
        {
            return new GameObject[0];
        }

        List<GameObject> result = new List<GameObject>();
        for (int i = 0; i < 6; i++)
        {
            Transform child = backgroundsRoot.Find($"Background{i + 1}");
            if (child != null)
            {
                result.Add(child.gameObject);
            }
        }

        return result.ToArray();
    }

    private static GameObject[] FindAbyssPowerChildren(Transform powersRoot)
    {
        if (powersRoot == null)
        {
            return new GameObject[0];
        }

        List<GameObject> result = new List<GameObject>();
        for (int i = 0; i < 6; i++)
        {
            Transform child = powersRoot.Find($"{AbyssPowerRootName}_{i + 1}");
            if (child != null)
            {
                result.Add(child.gameObject);
            }
        }

        return result.ToArray();
    }

    private static void EnsureBossBackgroundLayer(GameObject backgroundRoot, int sortingOrder)
    {
        if (backgroundRoot == null)
        {
            return;
        }

        backgroundRoot.layer = LayerMask.NameToLayer("Default");
        backgroundRoot.SetActive(true);

        if (backgroundRoot.GetComponent<Tilemap>() == null)
        {
            Undo.AddComponent<Tilemap>(backgroundRoot);
        }

        TilemapRenderer renderer = backgroundRoot.GetComponent<TilemapRenderer>();
        if (renderer == null)
        {
            renderer = Undo.AddComponent<TilemapRenderer>(backgroundRoot);
        }

        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = sortingOrder;
    }

    private static void EnsureAbyssPowerVisual(GameObject powerRoot, bool created)
    {
        if (powerRoot == null)
        {
            return;
        }

        powerRoot.layer = LayerMask.NameToLayer("Ignore Raycast");
        powerRoot.SetActive(true);

        if (powerRoot.GetComponent<Tilemap>() == null)
        {
            Undo.AddComponent<Tilemap>(powerRoot);
        }

        TilemapRenderer renderer = powerRoot.GetComponent<TilemapRenderer>();
        if (renderer == null)
        {
            renderer = Undo.AddComponent<TilemapRenderer>(powerRoot);
        }

        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = 12;

        if (created)
        {
            powerRoot.transform.localPosition = BossAbyssPowerLocalPosition;
            powerRoot.transform.localRotation = Quaternion.identity;
            powerRoot.transform.localScale = Vector3.one;
        }

        Collider2D[] colliders = powerRoot.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }

        Rigidbody2D[] bodies = powerRoot.GetComponentsInChildren<Rigidbody2D>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody2D body = bodies[i];
            if (body != null)
            {
                Undo.DestroyObjectImmediate(body);
            }
        }

        if (powerRoot.GetComponent<AbyssPower>() == null)
        {
            Undo.AddComponent<AbyssPower>(powerRoot);
        }

        if (created)
        {
            powerRoot.transform.localPosition = BossAbyssPowerLocalPosition;
            powerRoot.transform.localRotation = Quaternion.identity;
            powerRoot.transform.localScale = Vector3.one;
        }
    }

    private static GameObject CreateFloatingPlatformLayer(Transform parent, string childName, bool includeCollider, int sortingOrder)
    {
        GameObject layerRoot = CreateGameObject(childName, parent);
        layerRoot.layer = includeCollider ? LayerMask.NameToLayer(GroundLayerName) : LayerMask.NameToLayer("Default");

        Tilemap tilemap = Undo.AddComponent<Tilemap>(layerRoot);
        TilemapRenderer renderer = Undo.AddComponent<TilemapRenderer>(layerRoot);
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = sortingOrder;

        if (includeCollider)
        {
            TilemapCollider2D tilemapCollider = Undo.AddComponent<TilemapCollider2D>(layerRoot);
            tilemapCollider.usedByComposite = true;

            Rigidbody2D body = Undo.AddComponent<Rigidbody2D>(layerRoot);
            body.bodyType = RigidbodyType2D.Static;
            body.simulated = true;
            body.useAutoMass = false;

            CompositeCollider2D composite = Undo.AddComponent<CompositeCollider2D>(layerRoot);
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

            EditorUtility.SetDirty(tilemapCollider);
            EditorUtility.SetDirty(body);
            EditorUtility.SetDirty(composite);
        }

        EditorUtility.SetDirty(tilemap);
        EditorUtility.SetDirty(renderer);
        return layerRoot;
    }

    private static GameObject EnsureGroundReferencePoint(Transform parent)
    {
        if (parent == null)
        {
            return null;
        }

        Transform existing = parent.Find("GroundReferencePoint");
        GameObject childObject = existing != null ? existing.gameObject : new GameObject("GroundReferencePoint");
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(childObject, "Create GroundReferencePoint");
            Undo.SetTransformParent(childObject.transform, parent, "Parent GroundReferencePoint");
        }

        childObject.transform.localPosition = Vector3.zero;
        childObject.transform.localRotation = Quaternion.identity;
        childObject.transform.localScale = Vector3.one;
        childObject.layer = LayerMask.NameToLayer("Default");
        return childObject;
    }

    private static bool RepairFloatingPlatformLayerSet(ArenaFloatingPlatformController platform)
    {
        if (platform == null)
        {
            return false;
        }

        bool changed = false;
        Transform root = platform.transform;
        if (root == null)
        {
            return false;
        }

        GameObject groundReferencePoint = EnsureGroundReferencePoint(root);
        GameObject platform1 = FindOrCreateChild(root, "Platform_1");
        GameObject platform2 = FindOrCreateChild(root, "Platform_2");
        GameObject platform3 = FindOrCreateChild(root, "Platform_3");

        GameObject legacySolid = FindDeepChild(root, "Solid")?.gameObject;
        if (platform1 == null && legacySolid != null)
        {
            legacySolid.name = "Platform_1";
            platform1 = legacySolid;
            changed = true;
        }

        if (platform1 != null && platform1.name != "Platform_1")
        {
            platform1.name = "Platform_1";
            changed = true;
        }

        EnsureTilemapLayer(platform1, true, 5);
        EnsureTilemapLayer(platform2, true, 6);
        EnsureTilemapLayer(platform3, true, 7);

        SerializedObject platformSo = new SerializedObject(platform);
        SetObject(platformSo, "groundReferencePoint", groundReferencePoint != null ? groundReferencePoint.transform : null);
        SetObject(platformSo, "platform1Tilemap", platform1 != null ? platform1.GetComponent<Tilemap>() : null);
        SetObject(platformSo, "platform2Tilemap", platform2 != null ? platform2.GetComponent<Tilemap>() : null);
        SetObject(platformSo, "platform3Tilemap", platform3 != null ? platform3.GetComponent<Tilemap>() : null);
        SetFloat(platformSo, "platformRiseDuration", 0.85f);
        platformSo.ApplyModifiedPropertiesWithoutUndo();

        changed |= RemoveAndReport(FindDeepChild(root, "Background1") != null ? FindDeepChild(root, "Background1").gameObject : null);
        changed |= RemoveAndReport(FindDeepChild(root, "Background2") != null ? FindDeepChild(root, "Background2").gameObject : null);

        return changed;
    }

    private static bool RepairArenaBossFloatingPlatform(Transform encounterRoot)
    {
        if (encounterRoot == null)
        {
            return false;
        }

        ArenaBossFloatingPlatformController platform = FindOrCreateBossFloatingPlatform(encounterRoot);
        if (platform == null)
        {
            return false;
        }

        ArenaBossEncounterController controller = encounterRoot.GetComponent<ArenaBossEncounterController>();
        if (controller != null)
        {
            SerializedObject controllerSo = new SerializedObject(controller);
            SetObject(controllerSo, "bossFloatingPlatform", platform);
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        return true;
    }

    private static bool RemoveAndReport(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return false;
        }

        Undo.DestroyObjectImmediate(gameObject);
        return true;
    }

    private static bool SetLayerIfDifferent(GameObject gameObject, int layer)
    {
        if (gameObject == null || layer < 0 || gameObject.layer == layer)
        {
            return false;
        }

        gameObject.layer = layer;
        return true;
    }

    private static void EnsureFloatingPlatformChildren(Transform platformRoot, ArenaFloatingPlatformController platformController)
    {
        if (platformRoot == null || platformController == null)
        {
            return;
        }

        GameObject groundReferencePoint = EnsureGroundReferencePoint(platformRoot);
        GameObject platform1Root = FindOrCreateChild(platformRoot, "Platform_1");
        GameObject platform2Root = FindOrCreateChild(platformRoot, "Platform_2");
        GameObject platform3Root = FindOrCreateChild(platformRoot, "Platform_3");

        GameObject legacySolid = FindDeepChild(platformRoot, "Solid")?.gameObject;
        if (platform1Root == null && legacySolid != null)
        {
            legacySolid.name = "Platform_1";
            platform1Root = legacySolid;
        }

        if (platform1Root != null && platform1Root.name != "Platform_1")
        {
            platform1Root.name = "Platform_1";
        }

        if (platform2Root != null && platform2Root.name != "Platform_2")
        {
            platform2Root.name = "Platform_2";
        }

        if (platform3Root != null && platform3Root.name != "Platform_3")
        {
            platform3Root.name = "Platform_3";
        }

        EnsureTilemapLayer(platform1Root, true, 5);
        EnsureTilemapLayer(platform2Root, true, 6);
        EnsureTilemapLayer(platform3Root, true, 7);

        SerializedObject platformSo = new SerializedObject(platformController);
        if (IsObjectReferenceNull(platformSo, "groundReferencePoint"))
        {
            SetObject(platformSo, "groundReferencePoint", groundReferencePoint != null ? groundReferencePoint.transform : null);
        }

        if (IsObjectReferenceNull(platformSo, "platform1Tilemap"))
        {
            SetObject(platformSo, "platform1Tilemap", platform1Root.GetComponent<Tilemap>());
        }

        if (IsObjectReferenceNull(platformSo, "platform2Tilemap"))
        {
            SetObject(platformSo, "platform2Tilemap", platform2Root.GetComponent<Tilemap>());
        }

        if (IsObjectReferenceNull(platformSo, "platform3Tilemap"))
        {
            SetObject(platformSo, "platform3Tilemap", platform3Root.GetComponent<Tilemap>());
        }

        SetFloat(platformSo, "platformRiseDuration", 0.85f);

        platformSo.ApplyModifiedPropertiesWithoutUndo();

        RemoveChildIfExists(platformRoot, "Background1");
        RemoveChildIfExists(platformRoot, "Background2");
    }

    private static void RemoveChildIfExists(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return;
        }

        Transform child = parent.Find(childName);
        if (child != null)
        {
            Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    private static void EnsureBossFloatingPlatformChildren(Transform platformRoot, ArenaBossFloatingPlatformController platformController)
    {
        if (platformRoot == null || platformController == null)
        {
            return;
        }

        GameObject groundReferencePoint = EnsureGroundReferencePoint(platformRoot);
        GameObject platform1Root = FindOrCreateChild(platformRoot, "Platform_1");
        GameObject platform2Root = FindOrCreateChild(platformRoot, "Platform_2");
        GameObject platform3Root = FindOrCreateChild(platformRoot, "Platform_3");
        GameObject platform4Root = FindOrCreateChild(platformRoot, "Platform_4");
        GameObject platform5Root = FindOrCreateChild(platformRoot, "Platform_5");
        GameObject platform6Root = FindOrCreateChild(platformRoot, "Platform_6");

        EnsureTilemapLayer(platform1Root, true, 5);
        EnsureTilemapLayer(platform2Root, true, 6);
        EnsureTilemapLayer(platform3Root, true, 7);
        EnsureTilemapLayer(platform4Root, true, 8);
        EnsureTilemapLayer(platform5Root, true, 9);
        EnsureTilemapLayer(platform6Root, true, 10);

        SerializedObject platformSo = new SerializedObject(platformController);
        if (IsObjectReferenceNull(platformSo, "groundReferencePoint"))
        {
            SetObject(platformSo, "groundReferencePoint", groundReferencePoint != null ? groundReferencePoint.transform : null);
        }

        if (IsObjectReferenceNull(platformSo, "platform1Tilemap"))
        {
            SetObject(platformSo, "platform1Tilemap", platform1Root.GetComponent<Tilemap>());
        }

        if (IsObjectReferenceNull(platformSo, "platform2Tilemap"))
        {
            SetObject(platformSo, "platform2Tilemap", platform2Root.GetComponent<Tilemap>());
        }

        if (IsObjectReferenceNull(platformSo, "platform3Tilemap"))
        {
            SetObject(platformSo, "platform3Tilemap", platform3Root.GetComponent<Tilemap>());
        }

        if (IsObjectReferenceNull(platformSo, "platform4Tilemap"))
        {
            SetObject(platformSo, "platform4Tilemap", platform4Root.GetComponent<Tilemap>());
        }

        if (IsObjectReferenceNull(platformSo, "platform5Tilemap"))
        {
            SetObject(platformSo, "platform5Tilemap", platform5Root.GetComponent<Tilemap>());
        }

        if (IsObjectReferenceNull(platformSo, "platform6Tilemap"))
        {
            SetObject(platformSo, "platform6Tilemap", platform6Root.GetComponent<Tilemap>());
        }

        SetBool(platformSo, "startHidden", true);
        SetFloat(platformSo, "platformRiseInterval", 0.7f);
        SetFloat(platformSo, "platformRiseDuration", 0.85f);
        platformSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureTilemapLayer(GameObject layerRoot, bool includeCollider, int sortingOrder)
    {
        if (layerRoot == null)
        {
            return;
        }

        layerRoot.layer = includeCollider ? LayerMask.NameToLayer(GroundLayerName) : LayerMask.NameToLayer("Default");

        if (layerRoot.GetComponent<Tilemap>() == null)
        {
            Undo.AddComponent<Tilemap>(layerRoot);
        }

        TilemapRenderer renderer = layerRoot.GetComponent<TilemapRenderer>();
        if (renderer == null)
        {
            renderer = Undo.AddComponent<TilemapRenderer>(layerRoot);
        }

        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = sortingOrder;

        if (includeCollider)
        {
            if (layerRoot.GetComponent<TilemapCollider2D>() == null)
            {
                TilemapCollider2D tilemapCollider = Undo.AddComponent<TilemapCollider2D>(layerRoot);
                tilemapCollider.usedByComposite = true;
            }

            if (layerRoot.GetComponent<Rigidbody2D>() == null)
            {
                Rigidbody2D body = Undo.AddComponent<Rigidbody2D>(layerRoot);
                body.bodyType = RigidbodyType2D.Static;
                body.simulated = true;
                body.useAutoMass = false;
            }

            if (layerRoot.GetComponent<CompositeCollider2D>() == null)
            {
                CompositeCollider2D composite = Undo.AddComponent<CompositeCollider2D>(layerRoot);
                composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            }
        }
    }

    private static void EnsureDoorChildren(Transform doorRoot, ArenaDoorController doorController, float openVerticalOffset)
    {
        if (doorRoot == null || doorController == null)
        {
            return;
        }

        GameObject visualRoot = FindOrCreateChild(doorRoot, "Visual");
        GameObject blockingRoot = FindOrCreateChild(doorRoot, "Blocking_1");
        GameObject blockingDropRoot = FindOrCreateChild(doorRoot, "Blocking_2");

        if (visualRoot.GetComponent<Tilemap>() == null)
        {
            Undo.AddComponent<Tilemap>(visualRoot);
        }

        if (visualRoot.GetComponent<TilemapRenderer>() == null)
        {
            TilemapRenderer visualRenderer = Undo.AddComponent<TilemapRenderer>(visualRoot);
            visualRenderer.sortingLayerName = "Background";
            visualRenderer.sortingOrder = 100;
        }
        else
        {
            visualRoot.GetComponent<TilemapRenderer>().sortingLayerName = "Background";
        }

        if (blockingRoot.GetComponent<Tilemap>() == null)
        {
            Undo.AddComponent<Tilemap>(blockingRoot);
        }

        if (blockingRoot.GetComponent<TilemapRenderer>() == null)
        {
            TilemapRenderer blockingRenderer = Undo.AddComponent<TilemapRenderer>(blockingRoot);
            blockingRenderer.sortingLayerName = "Background";
            blockingRenderer.sortingOrder = 101;
        }
        else
        {
            blockingRoot.GetComponent<TilemapRenderer>().sortingLayerName = "Background";
        }

        if (blockingRoot.GetComponent<TilemapCollider2D>() == null)
        {
            TilemapCollider2D blockingTilemapCollider = Undo.AddComponent<TilemapCollider2D>(blockingRoot);
            blockingTilemapCollider.usedByComposite = true;
        }

        if (blockingRoot.GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D blockingBody = Undo.AddComponent<Rigidbody2D>(blockingRoot);
            blockingBody.bodyType = RigidbodyType2D.Static;
            blockingBody.simulated = true;
            blockingBody.useAutoMass = false;
        }

        if (blockingRoot.GetComponent<CompositeCollider2D>() == null)
        {
            CompositeCollider2D blockingComposite = Undo.AddComponent<CompositeCollider2D>(blockingRoot);
            blockingComposite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        }

        if (blockingDropRoot.GetComponent<Tilemap>() == null)
        {
            Undo.AddComponent<Tilemap>(blockingDropRoot);
        }

        if (blockingDropRoot.GetComponent<TilemapRenderer>() == null)
        {
            TilemapRenderer blockingDropRenderer = Undo.AddComponent<TilemapRenderer>(blockingDropRoot);
            blockingDropRenderer.sortingLayerName = "Background";
            blockingDropRenderer.sortingOrder = 102;
        }
        else
        {
            blockingDropRoot.GetComponent<TilemapRenderer>().sortingLayerName = "Background";
        }

        if (blockingDropRoot.GetComponent<TilemapCollider2D>() == null)
        {
            TilemapCollider2D blockingDropTilemapCollider = Undo.AddComponent<TilemapCollider2D>(blockingDropRoot);
            blockingDropTilemapCollider.usedByComposite = true;
        }

        if (blockingDropRoot.GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D blockingDropBody = Undo.AddComponent<Rigidbody2D>(blockingDropRoot);
            blockingDropBody.bodyType = RigidbodyType2D.Static;
            blockingDropBody.simulated = true;
            blockingDropBody.useAutoMass = false;
        }

        if (blockingDropRoot.GetComponent<CompositeCollider2D>() == null)
        {
            CompositeCollider2D blockingDropComposite = Undo.AddComponent<CompositeCollider2D>(blockingDropRoot);
            blockingDropComposite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        }

        SerializedObject doorSo = new SerializedObject(doorController);
        if (IsObjectReferenceNull(doorSo, "visualRoot"))
        {
            SetObject(doorSo, "visualRoot", visualRoot);
        }

        if (IsObjectReferenceNull(doorSo, "blockingRoot"))
        {
            SetObject(doorSo, "blockingRoot", blockingRoot);
        }

        if (IsObjectReferenceNull(doorSo, "blockingDropRoot"))
        {
            SetObject(doorSo, "blockingDropRoot", blockingDropRoot);
        }

        SetFloat(doorSo, "openVerticalOffset", openVerticalOffset);

        doorSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBossDoorBlockingLayers(Transform doorRoot)
    {
        if (doorRoot == null)
        {
            return;
        }

        int groundLayer = LayerMask.NameToLayer(GroundLayerName);
        if (groundLayer < 0)
        {
            return;
        }

        string[] blockingNames = { "Blocking", "Blocking_1", "Blocking_2" };
        for (int i = 0; i < blockingNames.Length; i++)
        {
            Transform child = doorRoot.Find(blockingNames[i]);
            if (child != null)
            {
                child.gameObject.layer = groundLayer;
            }
        }
    }

    private static GameObject CreateGameObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        if (parent != null)
        {
            Undo.SetTransformParent(go.transform, parent, $"Parent {name}");
        }

        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindDeepChild(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static Transform FindGridInActiveScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            return null;
        }

        GameObject[] rootObjects = activeScene.GetRootGameObjects();
        for (int i = 0; i < rootObjects.Length; i++)
        {
            Transform grid = FindDeepChild(rootObjects[i].transform, "Grid");
            if (grid != null)
            {
                return grid;
            }
        }

        return null;
    }

    private static bool ShouldFillArray(SerializedObject serializedObject, string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || !property.isArray)
        {
            return false;
        }

        return property.arraySize == 0;
    }

    private static bool IsObjectReferenceNull(SerializedObject serializedObject, string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        return property == null || property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null;
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetObjectArray(SerializedObject serializedObject, string propertyName, Object[] values)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || !property.isArray)
        {
            return;
        }

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.Boolean)
        {
            property.boolValue = value;
        }
    }

    private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.Float)
        {
            property.floatValue = value;
        }
    }

    private static void SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.String)
        {
            property.stringValue = value;
        }
    }
}
