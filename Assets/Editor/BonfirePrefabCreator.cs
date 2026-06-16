using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BonfirePrefabCreator
{
    private const string BonfirePrefabPath = "Assets/Prefabs/Interactables/Bonfire.prefab";
    private static readonly string[] BonfireFramePaths =
    {
        "Assets/Graphics/Decorations/Bonfire/Bonfire_1.png",
        "Assets/Graphics/Decorations/Bonfire/Bonfire_2.png",
        "Assets/Graphics/Decorations/Bonfire/Bonfire_3.png",
        "Assets/Graphics/Decorations/Bonfire/Bonfire_4.png"
    };

    [MenuItem("Tools/Bonfire/Create Bonfire Prefab")]
    public static void CreateBonfirePrefab()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(BonfirePrefabPath));

        if (!EnsureFrameImports())
        {
            return;
        }

        Sprite[] frames = BonfireFramePaths
            .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
            .ToArray();

        if (frames.Any(frame => frame == null))
        {
            Debug.LogError("Failed to load one or more bonfire sprites.");
            return;
        }

        GameObject root = new GameObject(
            "Bonfire",
            typeof(SpriteRenderer),
            typeof(BoxCollider2D),
            typeof(Bonfire)
        );

        try
        {
            ConfigureBonfireObject(root, frames);
            PrefabUtility.SaveAsPrefabAsset(root, BonfirePrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BonfirePrefabPath);
        if (prefab != null)
        {
            EditorGUIUtility.PingObject(prefab);
        }

        Debug.Log($"Created bonfire prefab at {BonfirePrefabPath}");
    }

    [MenuItem("Tools/Bonfire/Spawn Bonfire In Active Scene")]
    public static void SpawnBonfireInActiveScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BonfirePrefabPath);
        if (prefab == null)
        {
            CreateBonfirePrefab();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BonfirePrefabPath);
            if (prefab == null)
            {
                return;
            }
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene()) as GameObject;
        if (instance == null)
        {
            Debug.LogError("Failed to spawn bonfire prefab into the active scene.");
            return;
        }

        instance.transform.position = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void ConfigureBonfireObject(GameObject root, Sprite[] frames)
    {
        root.name = "Bonfire";
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        root.layer = 0;

        SpriteRenderer spriteRenderer = root.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = frames[0];
        spriteRenderer.sortingLayerName = "Default";
        spriteRenderer.sortingOrder = 10;

        BoxCollider2D collider = root.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1.25f, 1.75f);
        collider.offset = new Vector2(0f, 0.85f);

        Bonfire bonfire = root.GetComponent<Bonfire>();
        SerializedObject bonfireSo = new SerializedObject(bonfire);
        SetString(bonfireSo, "bonfireId", "bonfire_default");
        SetBool(bonfireSo, "restorePlayerOnRest", true);
        SetBool(bonfireSo, "reloadSceneOnRest", true);
        SetFloat(bonfireSo, "restDelay", 0.05f);
        SetString(bonfireSo, "interactPrompt", "Press F to rest");
        SetFloat(bonfireSo, "framesPerSecond", 8f);
        SetBool(bonfireSo, "loopAnimation", true);
        SetBool(bonfireSo, "autoLoadDefaultFrames", false);
        SetObject(bonfireSo, "flameRenderer", spriteRenderer);

        SerializedProperty framesProperty = bonfireSo.FindProperty("flameFrames");
        if (framesProperty != null && framesProperty.isArray)
        {
            framesProperty.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
            {
                SerializedProperty element = framesProperty.GetArrayElementAtIndex(i);
                element.objectReferenceValue = frames[i];
            }
        }

        SetObject(bonfireSo, "interactionCollider", collider);
        bonfireSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(bonfire);
        EditorUtility.SetDirty(spriteRenderer);
        EditorUtility.SetDirty(collider);
    }

    private static bool EnsureFrameImports()
    {
        bool changed = false;

        foreach (string framePath in BonfireFramePaths)
        {
            changed |= EnsureSpriteImport(framePath);
        }

        if (changed)
        {
            AssetDatabase.Refresh();
        }

        return true;
    }

    private static bool EnsureSpriteImport(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Could not read importer for bonfire sprite: {assetPath}");
            return false;
        }

        bool changed = false;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }

        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (importer.filterMode != FilterMode.Point)
        {
            importer.filterMode = FilterMode.Point;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }

        return changed;
    }

    private static void SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.String)
        {
            property.stringValue = value;
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

    private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
        {
            property.objectReferenceValue = value;
        }
    }
}
