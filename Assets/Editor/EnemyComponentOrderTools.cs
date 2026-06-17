using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal static class EnemyComponentOrderTools
{
    public static void MoveComponentToTop(Component component)
    {
        if (component == null)
        {
            return;
        }

        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Move Script Component Up only persists in Edit Mode. Play Mode changes revert when you stop.");
            return;
        }

        GameObject targetGameObject = component.gameObject;
        System.Type componentType = component.GetType();

        if (PrefabUtility.IsPartOfPrefabInstance(targetGameObject))
        {
            MoveComponentInPrefabAsset(targetGameObject, componentType);
            return;
        }

        MoveComponentInSceneObject(targetGameObject, componentType);
    }

    private static void MoveComponentInSceneObject(GameObject targetGameObject, System.Type componentType)
    {
        if (targetGameObject == null || componentType == null)
        {
            return;
        }

        Component liveComponent = targetGameObject.GetComponent(componentType);
        if (liveComponent == null)
        {
            return;
        }

        Undo.RegisterCompleteObjectUndo(targetGameObject, "Move Component To Top");

        int safetyCounter = 64;
        while (safetyCounter-- > 0 && UnityEditorInternal.ComponentUtility.MoveComponentUp(liveComponent))
        {
            // Keep moving until the component can no longer go higher.
        }

        EditorSceneManager.MarkSceneDirty(targetGameObject.scene);
        EditorUtility.SetDirty(targetGameObject);
        EditorApplication.RepaintHierarchyWindow();
        EditorApplication.RepaintProjectWindow();
    }

    private static void MoveComponentInPrefabAsset(GameObject targetGameObject, System.Type componentType)
    {
        string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targetGameObject);
        if (string.IsNullOrWhiteSpace(prefabPath))
        {
            MoveComponentInSceneObject(targetGameObject, componentType);
            return;
        }

        GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabContents == null)
        {
            return;
        }

        try
        {
            Component assetComponent = prefabContents.GetComponent(componentType);
            if (assetComponent == null)
            {
                return;
            }

            Undo.RegisterCompleteObjectUndo(prefabContents, "Move Component To Top");

            int safetyCounter = 64;
            while (safetyCounter-- > 0 && UnityEditorInternal.ComponentUtility.MoveComponentUp(assetComponent))
            {
                // Keep moving until the component can no longer go higher.
            }

            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
            EditorUtility.SetDirty(prefabContents);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorApplication.RepaintHierarchyWindow();
            EditorApplication.RepaintProjectWindow();
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }
}
