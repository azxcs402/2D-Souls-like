using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class ArenaEncounterBootstrapper
{
    private const string WaveSetAssetPath = "Assets/Combat/ArenaEncounter/ArenaWaveSet.asset";
    private const string GroundLayerName = "Ground";

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

        ArenaDoorController leftDoor = FindOrCreateDoor(doorGroup.transform, "Door_Left", new Vector3(-6f, 0f, 0f));
        ArenaDoorController rightDoor = FindOrCreateDoor(doorGroup.transform, "Door_Right", new Vector3(6f, 0f, 0f));
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
            SetObjectArray(controllerSo, "doors", new Object[] { leftDoor, rightDoor });
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

    private static ArenaDoorController FindOrCreateDoor(Transform parent, string doorName, Vector3 localPosition)
    {
        Transform existingDoor = parent.Find(doorName);
        if (existingDoor == null)
        {
            return CreateDoor(parent, doorName, localPosition);
        }

        ArenaDoorController doorController = existingDoor.GetComponent<ArenaDoorController>();
        if (doorController == null)
        {
            doorController = Undo.AddComponent<ArenaDoorController>(existingDoor.gameObject);
        }

        EnsureDoorChildren(existingDoor, doorController);
        return doorController;
    }

    private static ArenaDoorController CreateDoor(Transform parent, string doorName, Vector3 localPosition)
    {
        GameObject doorRoot = CreateGameObject(doorName, parent);
        doorRoot.transform.localPosition = localPosition;

        ArenaDoorController doorController = Undo.AddComponent<ArenaDoorController>(doorRoot);

        GameObject visualRoot = CreateGameObject("Visual", doorRoot.transform);
        GameObject blockingRoot = CreateGameObject("Blocking", doorRoot.transform);

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

        SerializedObject doorSo = new SerializedObject(doorController);
        SetObject(doorSo, "visualRoot", visualRoot);
        SetObject(doorSo, "blockingRoot", blockingRoot);
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

    private static ArenaFloatingPlatformController CreateFloatingPlatform(Transform parent)
    {
        GameObject platformRoot = CreateGameObject("ArenaFloatingPlatform", parent);
        platformRoot.transform.localPosition = Vector3.zero;

        ArenaFloatingPlatformController platformController = Undo.AddComponent<ArenaFloatingPlatformController>(platformRoot);

        GameObject background1Root = CreateFloatingPlatformLayer(platformRoot.transform, "Background1", false, 3);
        GameObject background2Root = CreateFloatingPlatformLayer(platformRoot.transform, "Background2", false, 4);
        GameObject solidRoot = CreateFloatingPlatformLayer(platformRoot.transform, "Solid", true, 5);

        SerializedObject platformSo = new SerializedObject(platformController);
        SetObject(platformSo, "background1Tilemap", background1Root.GetComponent<Tilemap>());
        SetObject(platformSo, "background2Tilemap", background2Root.GetComponent<Tilemap>());
        SetObject(platformSo, "solidTilemap", solidRoot.GetComponent<Tilemap>());
        SetBool(platformSo, "startHidden", true);
        platformSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(platformController);
        return platformController;
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

        GameObject background1 = FindDeepChild(root, "Background1") != null ? FindDeepChild(root, "Background1").gameObject : null;
        GameObject background2 = FindDeepChild(root, "Background2") != null ? FindDeepChild(root, "Background2").gameObject : null;
        GameObject solid = FindDeepChild(root, "Solid") != null ? FindDeepChild(root, "Solid").gameObject : null;

        changed |= SetLayerIfDifferent(background1, LayerMask.NameToLayer("Default"));
        changed |= SetLayerIfDifferent(background2, LayerMask.NameToLayer("Default"));
        changed |= SetLayerIfDifferent(solid, LayerMask.NameToLayer(GroundLayerName));

        return changed;
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

        GameObject background1Root = FindOrCreateChild(platformRoot, "Background1");
        GameObject background2Root = FindOrCreateChild(platformRoot, "Background2");
        GameObject solidRoot = FindOrCreateChild(platformRoot, "Solid");

        EnsureTilemapLayer(background1Root, false, 3);
        EnsureTilemapLayer(background2Root, false, 4);
        EnsureTilemapLayer(solidRoot, true, 5);

        SerializedObject platformSo = new SerializedObject(platformController);
        if (IsObjectReferenceNull(platformSo, "background1Tilemap"))
        {
            SetObject(platformSo, "background1Tilemap", background1Root.GetComponent<Tilemap>());
        }

        if (IsObjectReferenceNull(platformSo, "background2Tilemap"))
        {
            SetObject(platformSo, "background2Tilemap", background2Root.GetComponent<Tilemap>());
        }

        if (IsObjectReferenceNull(platformSo, "solidTilemap"))
        {
            SetObject(platformSo, "solidTilemap", solidRoot.GetComponent<Tilemap>());
        }

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

    private static void EnsureDoorChildren(Transform doorRoot, ArenaDoorController doorController)
    {
        if (doorRoot == null || doorController == null)
        {
            return;
        }

        GameObject visualRoot = FindOrCreateChild(doorRoot, "Visual");
        GameObject blockingRoot = FindOrCreateChild(doorRoot, "Blocking");

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

        SerializedObject doorSo = new SerializedObject(doorController);
        if (IsObjectReferenceNull(doorSo, "visualRoot"))
        {
            SetObject(doorSo, "visualRoot", visualRoot);
        }

        if (IsObjectReferenceNull(doorSo, "blockingRoot"))
        {
            SetObject(doorSo, "blockingRoot", blockingRoot);
        }

        doorSo.ApplyModifiedPropertiesWithoutUndo();
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
