using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemySkeletonPrefabConverter
{
    private const string TargetObjectName = "Enemy_Skeleton";
    private const string PrefabPath = "Assets/Prefabs/Enemy/Enemy_Skeleton.prefab";

    [MenuItem("Tools/Enemy/Convert Selected Skeleton To Prefab")]
    public static void ConvertSelectedSkeletonToPrefab()
    {
        GameObject target = Selection.activeGameObject != null
            ? Selection.activeGameObject
            : FindRootObjectByName(TargetObjectName);

        if (target == null)
        {
            Debug.LogError($"Could not find a root scene object named '{TargetObjectName}'. Select the skeleton object first and try again.");
            return;
        }

        if (target.transform.parent != null)
        {
            Debug.LogError("Select the root Enemy_Skeleton object, not a child object.");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        GameObject prefabInstance = PrefabUtility.SaveAsPrefabAssetAndConnect(
            target,
            PrefabPath,
            InteractionMode.UserAction
        );

        if (prefabInstance == null)
        {
            Debug.LogError($"Failed to create prefab at '{PrefabPath}'.");
            return;
        }

        Scene scene = target.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created prefab '{PrefabPath}' and converted '{target.name}' into a prefab instance.", prefabInstance);
    }

    private static GameObject FindRootObjectByName(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            if (root != null && root.name == objectName)
            {
                return root;
            }
        }

        return null;
    }
}
