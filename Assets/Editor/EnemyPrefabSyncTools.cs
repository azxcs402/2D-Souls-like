using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class EnemyPrefabSyncTools
{
    private static readonly string[] DefaultEnemyRootNames =
    {
        "Enemy_Skeleton",
        "Enemy_Slime",
        "Enemy_Mage",
        "Enemy_Reaper"
    };

    [MenuItem("Tools/Enemy/Apply Selected Enemy Instances To Prefabs")]
    public static void ApplySelectedEnemyInstancesToPrefabs()
    {
        List<GameObject> targets = GetSelectedOrDefaultEnemyRoots();
        if (targets.Count == 0)
        {
            Debug.LogWarning("No enemy prefab instances were found. Select the scene instances first, or make sure the active scene contains Enemy_Skeleton, Enemy_Slime, Enemy_Mage, and Enemy_Reaper roots.");
            return;
        }

        int appliedCount = 0;
        foreach (GameObject target in targets)
        {
            if (target == null)
            {
                continue;
            }

            if (!PrefabUtility.IsPartOfPrefabInstance(target))
            {
                Debug.LogWarning($"Skipped '{target.name}' because it is not a prefab instance.");
                continue;
            }

            GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(target);
            if (root == null)
            {
                Debug.LogWarning($"Skipped '{target.name}' because Unity could not resolve the prefab instance root.");
                continue;
            }

            Undo.RegisterFullObjectHierarchyUndo(root, $"Apply {root.name} To Prefab");
            PrefabUtility.ApplyPrefabInstance(root, InteractionMode.UserAction);
            EditorUtility.SetDirty(root);
            appliedCount++;
            Debug.Log($"Applied scene instance '{root.name}' back to its prefab asset.", root);
        }

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Finished applying {appliedCount} enemy instance(s) to their prefab assets.");
    }

    private static List<GameObject> GetSelectedOrDefaultEnemyRoots()
    {
        List<GameObject> targets = new List<GameObject>();

        foreach (GameObject selected in Selection.gameObjects)
        {
            if (selected == null)
            {
                continue;
            }

            GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(selected);
            if (root != null && !targets.Contains(root))
            {
                targets.Add(root);
            }
        }

        if (targets.Count > 0)
        {
            return targets;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            return targets;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
            {
                continue;
            }

            if (!IsDefaultEnemyRootName(root.name))
            {
                continue;
            }

            if (!PrefabUtility.IsPartOfPrefabInstance(root))
            {
                continue;
            }

            targets.Add(root);
        }

        return targets;
    }

    private static bool IsDefaultEnemyRootName(string name)
    {
        for (int i = 0; i < DefaultEnemyRootNames.Length; i++)
        {
            if (string.Equals(name, DefaultEnemyRootNames[i], StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
