using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrefabPlacementUtility
{
    [MenuItem("Tools/Prefabs/Spawn Selected Prefab At Selection")]
    public static void SpawnSelectedPrefabAtSelection()
    {
        SpawnSelectedPrefab(GetPlacementPositionFromSelection(), GetPlacementParentFromSelection());
    }

    [MenuItem("Tools/Prefabs/Spawn Selected Prefab At Scene View Pivot")]
    public static void SpawnSelectedPrefabAtSceneViewPivot()
    {
        Vector3 position = SceneView.lastActiveSceneView != null
            ? SceneView.lastActiveSceneView.pivot
            : Vector3.zero;
        SpawnSelectedPrefab(position, GetPlacementParentFromSelection());
    }

    [MenuItem("Tools/Prefabs/Spawn Selected Prefab At Selection", true)]
    [MenuItem("Tools/Prefabs/Spawn Selected Prefab At Scene View Pivot", true)]
    private static bool CanSpawnSelectedPrefab()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode
            && GetSelectedPrefabAsset() != null;
    }

    private static void SpawnSelectedPrefab(Vector3 position, Transform parent)
    {
        GameObject prefab = GetSelectedPrefabAsset();
        if (prefab == null)
        {
            Debug.LogWarning("Select a prefab asset in the Project window first.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (instance == null)
        {
            Debug.LogError($"Failed to instantiate prefab '{prefab.name}'.");
            return;
        }

        Undo.RegisterCreatedObjectUndo(instance, $"Spawn {prefab.name}");
        if (parent != null)
        {
            Undo.SetTransformParent(instance.transform, parent, $"Parent {prefab.name}");
        }

        instance.transform.position = position;
        instance.transform.rotation = Quaternion.identity;
        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static GameObject GetSelectedPrefabAsset()
    {
        GameObject selected = Selection.activeObject as GameObject;
        if (selected == null)
        {
            return null;
        }

        if (PrefabUtility.IsPartOfPrefabAsset(selected))
        {
            return selected;
        }

        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(selected);
        if (source != null && PrefabUtility.IsPartOfPrefabAsset(source))
        {
            return source;
        }

        return null;
    }

    private static Vector3 GetPlacementPositionFromSelection()
    {
        Transform selectedTransform = Selection.activeTransform;
        return selectedTransform != null ? selectedTransform.position : Vector3.zero;
    }

    private static Transform GetPlacementParentFromSelection()
    {
        Transform selectedTransform = Selection.activeTransform;
        if (selectedTransform == null)
        {
            return null;
        }

        return selectedTransform.gameObject.scene.IsValid() ? selectedTransform.parent : null;
    }
}
