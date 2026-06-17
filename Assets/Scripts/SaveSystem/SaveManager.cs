using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    [SerializeField] private string fileName = "savegame.json";
    [SerializeField] private bool encryptData = true;

    private FileDataHandler dataHandler;
    private GameData gameData;
    private List<ISaveable> allSaveables = new();
    private bool initialLoadCompleted;

    public GameData GetGameData() => gameData;

    public bool HasSaveFile => dataHandler != null && dataHandler.HasFile();
    public bool HasSaveData => gameData != null
        && gameData.hasLastPlayerPosition
        && !string.IsNullOrEmpty(gameData.lastScenePlayed);

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);
        SceneManager.sceneLoaded += HandleSceneLoaded;
        LoadGame();
        initialLoadCompleted = true;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LoadGame();
        initialLoadCompleted = true;
    }

    private void Start()
    {
        if (!initialLoadCompleted)
        {
            LoadGame();
            initialLoadCompleted = true;
        }
    }

    public void LoadGame()
    {
        if (dataHandler == null)
        {
            dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, encryptData);
        }

        gameData = dataHandler.LoadData() ?? new GameData();
        allSaveables = FindSaveables();

        foreach (ISaveable saveable in allSaveables)
        {
            saveable.LoadData(gameData);
        }

        RestorePlayerState();
    }

    public void SaveGame()
    {
        SaveGame(true);
    }

    public void SaveGame(bool capturePlayerState)
    {
        if (gameData == null)
        {
            gameData = new GameData();
        }

        allSaveables = FindSaveables();
        foreach (ISaveable saveable in allSaveables)
        {
            saveable.SaveData(ref gameData);
        }

        if (capturePlayerState)
        {
            CapturePlayerState();
        }

        dataHandler.SaveData(gameData);
    }

    public void SaveCheckpoint(Vector3 checkpointPosition, string sceneName = null)
    {
        if (gameData == null)
        {
            gameData = new GameData();
        }

        gameData.lastScenePlayed = string.IsNullOrWhiteSpace(sceneName)
            ? SceneManager.GetActiveScene().name
            : sceneName;
        gameData.lastPlayerPosition = checkpointPosition;
        gameData.hasLastPlayerPosition = true;

        SaveGame(false);
    }

    public void ResetGameData()
    {
        gameData = new GameData();
        dataHandler.SaveData(gameData);
    }

    [ContextMenu("*** Delete save data ***")]
    public void DeleteSaveData()
    {
        dataHandler ??= new FileDataHandler(Application.persistentDataPath, fileName, encryptData);
        dataHandler.Delete();
        gameData = new GameData();
    }

    private List<ISaveable> FindSaveables()
    {
        return FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OfType<ISaveable>()
            .ToList();
    }

    private void CapturePlayerState()
    {
        Player player = FindFirstObjectByType<Player>();
        if (player == null)
        {
            return;
        }

        gameData.lastScenePlayed = SceneManager.GetActiveScene().name;
        gameData.lastPlayerPosition = player.transform.position;
        gameData.hasLastPlayerPosition = true;
    }

    private void RestorePlayerState()
    {
        if (gameData == null || !gameData.hasLastPlayerPosition)
        {
            return;
        }

        Player player = FindFirstObjectByType<Player>();
        if (player == null)
        {
            return;
        }

        player.transform.position = gameData.lastPlayerPosition;

        if (player.rb != null)
        {
            player.rb.velocity = Vector2.zero;
            player.rb.angularVelocity = 0f;
        }

        Physics2D.SyncTransforms();
    }
}
