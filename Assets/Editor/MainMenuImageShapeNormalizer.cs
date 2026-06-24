using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MainMenuImageShapeNormalizer
{
    private const string MainMenuSceneName = "MainMenu";
    private const string MainMenuRootName = "MainMenuRoot";

    static MainMenuImageShapeNormalizer()
    {
        EditorApplication.delayCall += NormalizeActiveMainMenuOnce;
    }

    [MenuItem("Tools/2D Souls-like/Fix Main Menu Image Shapes")]
    public static void NormalizeActiveMainMenu()
    {
        if (!TryGetMainMenuRoot(out Transform menuRoot))
        {
            Debug.LogError("Open the MainMenu scene before fixing its image shapes.");
            return;
        }

        int changedCount = NormalizeImages(menuRoot);
        if (changedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        Debug.Log($"MainMenu image shapes normalized. Changed Images: {changedCount}");
    }

    private static void NormalizeActiveMainMenuOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        if (!TryGetMainMenuRoot(out Transform menuRoot))
        {
            return;
        }

        int changedCount = NormalizeImages(menuRoot);
        if (changedCount <= 0)
        {
            return;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"MainMenu image shapes normalized automatically. Changed Images: {changedCount}");
    }

    private static bool TryGetMainMenuRoot(out Transform menuRoot)
    {
        menuRoot = null;

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded || activeScene.name != MainMenuSceneName)
        {
            return false;
        }

        GameObject rootObject = GameObject.Find(MainMenuRootName);
        if (rootObject == null)
        {
            return false;
        }

        menuRoot = rootObject.transform;
        return true;
    }

    private static int NormalizeImages(Transform menuRoot)
    {
        Image[] images = menuRoot.GetComponentsInChildren<Image>(true);
        int changedCount = 0;

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null)
            {
                continue;
            }

            bool needsChange = image.sprite != null
                || image.type != Image.Type.Simple
                || image.useSpriteMesh
                || image.preserveAspect;

            if (!needsChange)
            {
                continue;
            }

            Undo.RecordObject(image, "Normalize Main Menu Image Shape");
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.useSpriteMesh = false;
            image.preserveAspect = false;
            EditorUtility.SetDirty(image);
            changedCount++;
        }

        return changedCount;
    }
}
