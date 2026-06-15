using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProjectBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateProjectManagers()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            return;
        }

        if (Object.FindFirstObjectByType<GameManager>() != null || Object.FindFirstObjectByType<SaveManager>() != null)
        {
            return;
        }

        GameObject managers = new GameObject("ProjectManagers");
        managers.AddComponent<GameManager>();
        managers.AddComponent<SaveManager>();
    }
}
