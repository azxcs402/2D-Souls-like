using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class SampleSceneSerializationRepair
{
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    [MenuItem("Tools/2D Souls-like/Repair Sample Scene Serialization")]
    public static void RepairFromMenu()
    {
        RepairSampleScene();
    }

    public static void RepairCommandLine()
    {
        RepairSampleScene();
    }

    private static void RepairSampleScene()
    {
        Scene scene = SceneManager.GetSceneByPath(SampleScenePath);
        bool openedForRepair = !scene.IsValid() || !scene.isLoaded;
        if (openedForRepair)
        {
            scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Additive);
        }

        int repairedMatrixCount = 0;
        int missingScriptCount = 0;
        int removedMissingScriptCount = 0;
        int removedOrphanComponentCount = 0;
        int rebuiltPotionIconCount = RebuildHealingPotionWorldIcon(scene);
        int rebuiltPotionUiCount = RebuildHealingPotionUi(scene);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                int objectMissingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                if (objectMissingScriptCount > 0)
                {
                    missingScriptCount += objectMissingScriptCount;
                    Debug.LogError($"Missing scripts found on '{GetHierarchyPath(child)}': {objectMissingScriptCount}.", child.gameObject);
                }

                Component[] components = child.GetComponents<Component>();
                int nullComponentCount = 0;
                foreach (Component component in components)
                {
                    if (component == null)
                    {
                        nullComponentCount++;
                    }
                }

                if (nullComponentCount > 0)
                {
                    int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
                    removedMissingScriptCount += removed;
                    Debug.Log($"Removed {removed} missing component slot(s) from '{GetHierarchyPath(child)}'.", child.gameObject);
                }

                removedOrphanComponentCount += RemoveOrphanComponentSlots(child.gameObject, child);
            }

            foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>(true))
            {
                repairedMatrixCount += RepairUnusedTileMatrices(tilemap);
            }
        }

        if (repairedMatrixCount > 0
            || removedMissingScriptCount > 0
            || removedOrphanComponentCount > 0
            || rebuiltPotionIconCount > 0
            || rebuiltPotionUiCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Failed to save repaired scene: {SampleScenePath}");
            }
        }

        if (openedForRepair)
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        Debug.Log($"Sample scene serialization repair completed. Repaired matrices: {repairedMatrixCount}, missing scripts reported: {missingScriptCount}, missing script slots removed: {removedMissingScriptCount}, orphan component slots removed: {removedOrphanComponentCount}, potion icons rebuilt: {rebuiltPotionIconCount}, potion UI components rebuilt: {rebuiltPotionUiCount}.");
    }

    private static int RepairUnusedTileMatrices(Tilemap tilemap)
    {
        SerializedObject serializedTilemap = new SerializedObject(tilemap);
        SerializedProperty matrices = serializedTilemap.FindProperty("m_TileMatrixArray");
        if (matrices == null || !matrices.isArray)
        {
            return 0;
        }

        int repairedCount = 0;
        for (int i = 0; i < matrices.arraySize; i++)
        {
            SerializedProperty entry = matrices.GetArrayElementAtIndex(i);
            SerializedProperty refCount = entry.FindPropertyRelative("m_RefCount");
            SerializedProperty matrixData = entry.FindPropertyRelative("m_Data");
            if (refCount == null || matrixData == null || refCount.intValue > 0)
            {
                continue;
            }

            Matrix4x4 matrix = ReadMatrix(matrixData);
            if (matrix == Matrix4x4.identity)
            {
                continue;
            }

            WriteIdentityMatrix(matrixData);
            repairedCount++;
            Debug.Log($"Repaired unused Tilemap matrix on '{tilemap.name}' at index {i}.", tilemap);
        }

        if (repairedCount > 0)
        {
            serializedTilemap.ApplyModifiedPropertiesWithoutUndo();
        }

        return repairedCount;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = $"{transform.name}/{path}";
        }

        return path;
    }

    private static int RemoveOrphanComponentSlots(GameObject gameObject, Transform transform)
    {
        SerializedObject serializedGameObject = new SerializedObject(gameObject);
        SerializedProperty components = serializedGameObject.FindProperty("m_Component");
        if (components == null || !components.isArray)
        {
            return 0;
        }

        int removedCount = 0;
        Component[] runtimeComponents = gameObject.GetComponents<Component>();
        for (int i = components.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty componentReference = components.GetArrayElementAtIndex(i).FindPropertyRelative("component");
            bool runtimeComponentMissing = i < runtimeComponents.Length && runtimeComponents[i] == null;
            bool serializedReferenceMissing = componentReference != null && componentReference.objectReferenceValue == null;
            if (runtimeComponentMissing && componentReference != null)
            {
                UnityEngine.Object nativeComponent = componentReference.objectReferenceValue;
                if (!ReferenceEquals(nativeComponent, null))
                {
                    UnityEngine.Object.DestroyImmediate(nativeComponent, true);
                    removedCount++;
                    continue;
                }
            }

            if (runtimeComponentMissing || serializedReferenceMissing)
            {
                int previousSize = components.arraySize;
                components.DeleteArrayElementAtIndex(i);
                if (components.arraySize == previousSize)
                {
                    components.DeleteArrayElementAtIndex(i);
                }
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            serializedGameObject.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"Removed {removedCount} orphan component reference(s) from '{GetHierarchyPath(transform)}'.", gameObject);
        }

        return removedCount;
    }

    private static int RebuildHealingPotionWorldIcon(Scene scene)
    {
        Player player = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            player = root.GetComponentInChildren<Player>(true);
            if (player != null)
            {
                break;
            }
        }

        if (player == null)
        {
            return 0;
        }

        Transform iconTransform = player.transform.Find("HealingPotionWorldIcon");
        if (iconTransform == null)
        {
            return 0;
        }

        PlayerHealingPotionWorldIcon icon = iconTransform.GetComponent<PlayerHealingPotionWorldIcon>();
        MonoScript iconScript = icon != null ? MonoScript.FromMonoBehaviour(icon) : null;
        string iconScriptPath = iconScript != null ? AssetDatabase.GetAssetPath(iconScript) : string.Empty;
        bool hasNullComponent = Array.Exists(iconTransform.GetComponents<Component>(), component => component == null);
        MonoBehaviour[] iconBehaviours = iconTransform.GetComponents<MonoBehaviour>();
        bool hasUnexpectedBehaviourCount = iconBehaviours.Length != 1;
        if (!hasNullComponent
            && !hasUnexpectedBehaviourCount
            && iconScriptPath == "Assets/Scripts/Player/PlayerHealingPotionWorldIcon.cs")
        {
            return 0;
        }

        GameObject oldIconObject = iconTransform.gameObject;
        SpriteRenderer oldRenderer = oldIconObject.GetComponent<SpriteRenderer>();
        string rendererJson = oldRenderer != null ? EditorJsonUtility.ToJson(oldRenderer) : null;
        int siblingIndex = iconTransform.GetSiblingIndex();
        Vector3 localPosition = iconTransform.localPosition;
        Quaternion localRotation = iconTransform.localRotation;
        Vector3 localScale = iconTransform.localScale;
        int layer = oldIconObject.layer;
        string tag = oldIconObject.tag;
        bool active = oldIconObject.activeSelf;
        StaticEditorFlags staticFlags = GameObjectUtility.GetStaticEditorFlags(oldIconObject);

        UnityEngine.Object.DestroyImmediate(oldIconObject);

        GameObject rebuiltIconObject = new GameObject("HealingPotionWorldIcon");
        rebuiltIconObject.transform.SetParent(player.transform, false);
        rebuiltIconObject.transform.SetSiblingIndex(siblingIndex);
        rebuiltIconObject.transform.localPosition = localPosition;
        rebuiltIconObject.transform.localRotation = localRotation;
        rebuiltIconObject.transform.localScale = localScale;
        rebuiltIconObject.layer = layer;
        rebuiltIconObject.tag = tag;
        GameObjectUtility.SetStaticEditorFlags(rebuiltIconObject, staticFlags);

        SpriteRenderer rebuiltRenderer = rebuiltIconObject.AddComponent<SpriteRenderer>();
        if (!string.IsNullOrEmpty(rendererJson))
        {
            EditorJsonUtility.FromJsonOverwrite(rendererJson, rebuiltRenderer);
        }

        PlayerHealingPotionWorldIcon rebuiltIcon = rebuiltIconObject.AddComponent<PlayerHealingPotionWorldIcon>();
        rebuiltIcon.Configure(player, rebuiltRenderer);
        rebuiltIconObject.SetActive(active);
        Debug.Log("Rebuilt Player/HealingPotionWorldIcon with an asset-backed MonoScript.", rebuiltIconObject);
        return 1;
    }

    private static int RebuildHealingPotionUi(Scene scene)
    {
        Transform slot = FindTransformByName(scene, "UI_HealingPotionSlot");
        if (slot == null)
        {
            return 0;
        }

        UI_PlayerHealingPotion[] potionComponents = slot.GetComponents<UI_PlayerHealingPotion>();
        MonoScript currentScript = potionComponents.Length > 0 ? MonoScript.FromMonoBehaviour(potionComponents[0]) : null;
        string currentScriptPath = currentScript != null ? AssetDatabase.GetAssetPath(currentScript) : string.Empty;
        bool hasNullComponent = Array.Exists(slot.GetComponents<Component>(), component => component == null);
        if (!hasNullComponent
            && potionComponents.Length == 1
            && currentScriptPath == "Assets/Scripts/Player/UI_PlayerHealingPotion.cs")
        {
            return 0;
        }

        UnityEngine.Object player = null;
        UnityEngine.Object playerHealth = null;
        UnityEngine.Object potionImage = null;
        UnityEngine.Object cooldownImage = null;
        UnityEngine.Object countText = null;
        Vector2 countTextOffset = new Vector2(0f, -10f);
        if (potionComponents.Length > 0)
        {
            SerializedObject source = new SerializedObject(potionComponents[0]);
            player = source.FindProperty("player")?.objectReferenceValue;
            playerHealth = source.FindProperty("playerHealth")?.objectReferenceValue;
            potionImage = source.FindProperty("potionImage")?.objectReferenceValue;
            cooldownImage = source.FindProperty("cooldownImage")?.objectReferenceValue;
            countText = source.FindProperty("countText")?.objectReferenceValue;
            SerializedProperty offset = source.FindProperty("countTextOffset");
            if (offset != null)
            {
                countTextOffset = offset.vector2Value;
            }
        }

        foreach (UI_PlayerHealingPotion component in potionComponents)
        {
            UnityEngine.Object.DestroyImmediate(component, true);
        }

        RemoveOrphanComponentSlots(slot.gameObject, slot);
        UI_PlayerHealingPotion rebuilt = slot.gameObject.AddComponent<UI_PlayerHealingPotion>();
        SerializedObject destination = new SerializedObject(rebuilt);
        destination.FindProperty("player").objectReferenceValue = player;
        destination.FindProperty("playerHealth").objectReferenceValue = playerHealth;
        destination.FindProperty("potionImage").objectReferenceValue = potionImage;
        destination.FindProperty("cooldownImage").objectReferenceValue = cooldownImage;
        destination.FindProperty("countText").objectReferenceValue = countText;
        destination.FindProperty("countTextOffset").vector2Value = countTextOffset;
        destination.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("Rebuilt UI_HealingPotionSlot with an asset-backed MonoScript.", slot.gameObject);
        return 1;
    }

    private static Transform FindTransformByName(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                {
                    return child;
                }
            }
        }

        return null;
    }

    private static Matrix4x4 ReadMatrix(SerializedProperty matrixData)
    {
        Matrix4x4 matrix = new Matrix4x4();
        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                SerializedProperty element = matrixData.FindPropertyRelative($"e{row}{column}");
                matrix[row, column] = element != null ? element.floatValue : 0f;
            }
        }

        return matrix;
    }

    private static void WriteIdentityMatrix(SerializedProperty matrixData)
    {
        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                SerializedProperty element = matrixData.FindPropertyRelative($"e{row}{column}");
                if (element != null)
                {
                    element.floatValue = row == column ? 1f : 0f;
                }
            }
        }
    }
}
