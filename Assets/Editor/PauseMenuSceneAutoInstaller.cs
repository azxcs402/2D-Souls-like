using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

[InitializeOnLoad]
public static class PauseMenuSceneAutoInstaller
{
#if UNITY_EDITOR
    private const string SampleSceneName = "SampleScene";
    static PauseMenuSceneAutoInstaller()
    {
        EditorApplication.delayCall += TryInstallPauseMenuInActiveScene;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    [MenuItem("Tools/2D Souls-like/Restore Pause Menu In Active Sample Scene")]
    public static void RestorePauseMenuInActiveSampleScene()
    {
        TryInstallPauseMenuInActiveScene();
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        TryInstallPauseMenuInActiveScene();
    }

    private static void TryInstallPauseMenuInActiveScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded || activeScene.name != SampleSceneName)
        {
            return;
        }

        if (Object.FindFirstObjectByType<UI_PauseMenu>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        PauseMenuSceneGenerator.CreatePauseMenuInActiveScene();
    }
#endif
}
