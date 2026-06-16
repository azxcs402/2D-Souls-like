using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class SpikeHazardEditorBootstrapper
{
    private const string GridObjectName = "Grid";
    private const string SpikesObjectName = "Spikes";

    static SpikeHazardEditorBootstrapper()
    {
        EditorApplication.delayCall += EnsureSpikeTilemap;
        EditorSceneManager.sceneOpened += (_, _) => EditorApplication.delayCall += EnsureSpikeTilemap;
    }

    [MenuItem("Tools/Environment/Ensure Spike Tilemap")]
    public static void EnsureSpikeTilemapFromMenu()
    {
        EnsureSpikeTilemap();
    }

    private static void EnsureSpikeTilemap()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            return;
        }

        GameObject gridObject = GameObject.Find(GridObjectName);
        if (gridObject == null)
        {
            return;
        }

        Transform existing = gridObject.transform.Find(SpikesObjectName);
        if (existing != null)
        {
            return;
        }

        GameObject spikesObject = new GameObject(SpikesObjectName);
        Undo.RegisterCreatedObjectUndo(spikesObject, "Create Spike Tilemap");
        spikesObject.transform.SetParent(gridObject.transform, false);
        spikesObject.transform.localPosition = Vector3.zero;
        spikesObject.transform.localRotation = Quaternion.identity;
        spikesObject.transform.localScale = Vector3.one;

        spikesObject.AddComponent<TilemapRenderer>();
        spikesObject.AddComponent<Tilemap>();
        spikesObject.AddComponent<Rigidbody2D>();
        spikesObject.AddComponent<TilemapCollider2D>();
        spikesObject.AddComponent<SpikeHazard>();

        TilemapRenderer renderer = spikesObject.GetComponent<TilemapRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 1;
        }

        Rigidbody2D body = spikesObject.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Static;
            body.simulated = true;
        }

        TilemapCollider2D collider = spikesObject.GetComponent<TilemapCollider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        Selection.activeGameObject = spikesObject;
    }
}
