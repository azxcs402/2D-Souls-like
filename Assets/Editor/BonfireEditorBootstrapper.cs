using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BonfireEditorBootstrapper
{
    static BonfireEditorBootstrapper()
    {
        EditorApplication.delayCall += EnsureBonfireAssets;
    }

    [MenuItem("Tools/Bonfire/Ensure Bonfire Assets")]
    public static void EnsureBonfireAssetsFromMenu()
    {
        EnsureBonfireAssets();
    }

    private static void EnsureBonfireAssets()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        GameObject bonfirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Interactables/Bonfire.prefab");
        if (bonfirePrefab != null)
        {
            return;
        }

        BonfirePrefabCreator.CreateBonfirePrefab();
    }
}
