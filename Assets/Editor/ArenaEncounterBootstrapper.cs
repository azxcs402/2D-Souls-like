using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class ArenaEncounterBootstrapper
{
    private const string WaveSetAssetPath = "Assets/Combat/ArenaEncounter/ArenaWaveSet.asset";

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

        GameObject encounterRoot = CreateGameObject("ArenaEncounter", selected != null ? selected.transform : null);
        BoxCollider2D triggerCollider = Undo.AddComponent<BoxCollider2D>(encounterRoot);
        triggerCollider.isTrigger = true;

        ArenaEncounterController encounterController = Undo.AddComponent<ArenaEncounterController>(encounterRoot);

        GameObject spawnRoot = CreateGameObject("SpawnPoints", encounterRoot.transform);
        Transform[] spawnPoints = new Transform[4];
        Vector3[] spawnOffsets =
        {
            new Vector3(-4f, 1f, 0f),
            new Vector3(-2f, 1f, 0f),
            new Vector3(2f, 1f, 0f),
            new Vector3(4f, 1f, 0f)
        };

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            GameObject spawnPoint = CreateGameObject($"SpawnPoint_{i + 1}", spawnRoot.transform);
            spawnPoint.transform.localPosition = spawnOffsets[i];
            spawnPoints[i] = spawnPoint.transform;
        }

        Transform gridTransform = selected != null
            ? FindDeepChild(selected.transform.root, "Grid")
            : FindGridInActiveScene();
        Transform doorParent = gridTransform != null ? gridTransform : encounterRoot.transform;
        GameObject doorGroup = CreateGameObject("ArenaDoors", doorParent);

        ArenaDoorController leftDoor = CreateDoor(doorGroup.transform, "Door_Left", new Vector3(-6f, 0f, 0f));
        ArenaDoorController rightDoor = CreateDoor(doorGroup.transform, "Door_Right", new Vector3(6f, 0f, 0f));

        ArenaWaveSet waveSet = EnsureWaveSetAsset();

        SerializedObject controllerSo = new SerializedObject(encounterController);
        SetObject(controllerSo, "waveSet", waveSet);
        SetObjectArray(controllerSo, "spawnPoints", spawnPoints);
        SetObjectArray(controllerSo, "doors", new Object[] { leftDoor, rightDoor });
        SetBool(controllerSo, "lockDoorsWhenEncounterStarts", true);
        SetBool(controllerSo, "startOnlyOnce", true);
        SetFloat(controllerSo, "defaultDelayBeforeSpawn", 0.25f);
        SetFloat(controllerSo, "defaultDelayAfterClear", 0.75f);
        SetFloat(controllerSo, "clearConfirmDelay", 0.25f);
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = encounterRoot;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Arena encounter setup created. Fill the wave set, then assign enemy prefabs and door tiles.");
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

    private static ArenaWaveSet EnsureWaveSetAsset()
    {
        ArenaWaveSet waveSet = AssetDatabase.LoadAssetAtPath<ArenaWaveSet>(WaveSetAssetPath);
        if (waveSet != null)
        {
            return waveSet;
        }

        waveSet = ScriptableObject.CreateInstance<ArenaWaveSet>();
        AssetDatabase.CreateAsset(waveSet, WaveSetAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.SetDirty(waveSet);
        return waveSet;
    }

    private static ArenaDoorController CreateDoor(Transform parent, string doorName, Vector3 localPosition)
    {
        GameObject doorRoot = CreateGameObject(doorName, parent);
        doorRoot.transform.localPosition = localPosition;

        ArenaDoorController doorController = Undo.AddComponent<ArenaDoorController>(doorRoot);

        GameObject visualRoot = CreateGameObject("Visual", doorRoot.transform);

        Tilemap tilemap = Undo.AddComponent<Tilemap>(visualRoot);
        TilemapRenderer renderer = Undo.AddComponent<TilemapRenderer>(visualRoot);
        TilemapCollider2D tilemapCollider = Undo.AddComponent<TilemapCollider2D>(visualRoot);
        Rigidbody2D body = Undo.AddComponent<Rigidbody2D>(visualRoot);
        CompositeCollider2D composite = Undo.AddComponent<CompositeCollider2D>(visualRoot);

        body.bodyType = RigidbodyType2D.Static;
        body.simulated = true;
        body.useAutoMass = false;

        tilemapCollider.usedByComposite = true;
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

        renderer.sortingOrder = 100;

        SerializedObject doorSo = new SerializedObject(doorController);
        SetObject(doorSo, "doorRoot", visualRoot);
        SetBool(doorSo, "openOnStart", true);
        SetString(doorSo, "openBoolParameter", "open");
        doorSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(doorController);
        EditorUtility.SetDirty(tilemap);
        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(tilemapCollider);
        EditorUtility.SetDirty(body);
        EditorUtility.SetDirty(composite);

        return doorController;
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
