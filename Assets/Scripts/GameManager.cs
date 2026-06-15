using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Scenes")]
    [SerializeField] private string gameplaySceneName = "SampleScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Boot")]
    [SerializeField] private bool showMenuOnStart = false;

    public string GameplaySceneName => gameplaySceneName;
    public string MainMenuSceneName => mainMenuSceneName;
    public bool ShouldShowMenuOnStart => showMenuOnStart;
    public bool IsMenuScene => SceneManager.GetActiveScene().name == mainMenuSceneName;

    private Coroutine sceneChangeCoroutine;
    private bool saveBeforeSceneChange = true;
    private bool gameplayControlsPaused;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (showMenuOnStart)
        {
            Time.timeScale = 0f;
            SetGameplayControlsEnabled(false);
        }
    }

    public void PlayGame()
    {
        showMenuOnStart = false;

        if (sceneChangeCoroutine != null)
        {
            StopCoroutine(sceneChangeCoroutine);
            sceneChangeCoroutine = null;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        GameData data = SaveManager.instance != null ? SaveManager.instance.GetGameData() : null;
        bool hasSave = SaveManager.instance != null && SaveManager.instance.HasSaveData;
        bool canContinueCurrentScene = hasSave && data != null && data.lastScenePlayed == currentScene;

        if (canContinueCurrentScene)
        {
            ResumeGameplay();
            return;
        }

        if (hasSave && data != null && !string.IsNullOrEmpty(data.lastScenePlayed) && data.lastScenePlayed != currentScene)
        {
            if (Application.CanStreamedLevelBeLoaded(data.lastScenePlayed))
            {
                ChangeScene(data.lastScenePlayed, false);
            }
            else
            {
                StartNewGame();
            }
            return;
        }

        StartNewGame();
    }

    public void StartNewGame()
    {
        showMenuOnStart = false;

        if (SaveManager.instance != null)
        {
            SaveManager.instance.ResetGameData();
        }

        ChangeScene(gameplaySceneName, false);
    }

    public void ContinuePlay()
    {
        showMenuOnStart = false;
        PlayGame();
    }

    public void LoadMainMenu()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName) || !Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            ResumeMenu();
            return;
        }

        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            ResumeMenu();
            return;
        }

        ChangeScene(mainMenuSceneName, true);
    }

    public void RestartScene()
    {
        ChangeScene(SceneManager.GetActiveScene().name, true);
    }

    public void ChangeScene(string sceneName)
    {
        ChangeScene(sceneName, true);
    }

    public void ChangeScene(string sceneName, bool saveCurrentGame)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            return;
        }

        if (sceneChangeCoroutine != null)
        {
            StopCoroutine(sceneChangeCoroutine);
        }

        saveBeforeSceneChange = saveCurrentGame;
        sceneChangeCoroutine = StartCoroutine(ChangeSceneCo(sceneName));
    }

    public void ResumeGameplay()
    {
        Time.timeScale = 1f;
        SetGameplayControlsEnabled(true);
    }

    public void ResumeMenu()
    {
        Time.timeScale = 0f;
        SetGameplayControlsEnabled(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private IEnumerator ChangeSceneCo(string sceneName)
    {
        Time.timeScale = 1f;

        if (saveBeforeSceneChange && SaveManager.instance != null)
        {
            SaveManager.instance.SaveGame();
        }

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        if (loadOperation == null)
        {
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        sceneChangeCoroutine = null;
    }

    private void SetGameplayControlsEnabled(bool enabled)
    {
        if (gameplayControlsPaused == !enabled)
        {
            return;
        }

        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.enabled = enabled;
        }

        gameplayControlsPaused = !enabled;
    }
}
