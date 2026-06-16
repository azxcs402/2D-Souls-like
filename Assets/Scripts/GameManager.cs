using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Scenes")]
    [SerializeField] private string gameplaySceneName = "SampleScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Boot")]
    [SerializeField] private bool showMenuOnStart = false;

    [Header("Death Sequence")]
    [SerializeField] private float deathSequenceDuration = 4f;
    [SerializeField, Min(0f)] private float deathTextLeadInDuration = 0.5f;
    [SerializeField, Min(0f)] private float respawnFlashDuration = 0.12f;
    [SerializeField] private Color deathBackdropColor = Color.black;
    [SerializeField] private Color respawnFlashColor = Color.white;
    [SerializeField] private Color deathTextColor = new Color(0.9f, 0.1f, 0.1f, 1f);
    [SerializeField] private int deathTextFontSize = 72;
    [SerializeField] private Font deathTextFont;
    [SerializeField, Min(0f)] private float hazardBlackInDuration = 0.08f;
    [SerializeField, Min(0f)] private float hazardBlackHoldDuration = 0.04f;
    [SerializeField, Min(0f)] private float hazardBlackOutDuration = 0.08f;

    public string GameplaySceneName => gameplaySceneName;
    public string MainMenuSceneName => mainMenuSceneName;
    public bool ShouldShowMenuOnStart => showMenuOnStart;
    public bool IsMenuScene => SceneManager.GetActiveScene().name == mainMenuSceneName;

    private Coroutine sceneChangeCoroutine;
    private Coroutine deathSequenceCoroutine;
    private Coroutine hazardSequenceCoroutine;
    private bool saveBeforeSceneChange = true;
    private bool gameplayControlsPaused;
    private Image deathBackdrop;
    private Image respawnFlash;
    private Text deathText;
    private GameObject deathOverlayRoot;
    private bool deathOverlayVisible;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureDeathOverlay();
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

    public void BeginPlayerDeathSequence(bool freezeTime = false)
    {
        if (deathSequenceCoroutine != null)
        {
            return;
        }

        deathSequenceCoroutine = StartCoroutine(PlayerDeathSequenceCo(freezeTime));
    }

    public void BeginHazardRecoverySequence(Player player, Vector3 respawnPosition)
    {
        if (player == null
            || player.IsHazardRecoveryActive
            || hazardSequenceCoroutine != null
            || deathSequenceCoroutine != null)
        {
            return;
        }

        hazardSequenceCoroutine = StartCoroutine(HazardRecoverySequenceCo(player, respawnPosition));
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

    private IEnumerator PlayerDeathSequenceCo(bool freezeTime)
    {
        EnsureDeathOverlay();
        SetDeathOverlayVisible(true);
        SetDeathTextVisible(true);
        SetDeathOverlayAlpha(0f);
        float previousTimeScale = Time.timeScale;

        if (freezeTime)
        {
            Time.timeScale = 0f;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        float elapsed = 0f;
        float totalDuration = Mathf.Max(0.01f, deathSequenceDuration);
        float textLeadInDuration = Mathf.Clamp(deathTextLeadInDuration, 0f, totalDuration);
        float fadeDuration = Mathf.Max(0.01f, totalDuration - textLeadInDuration);

        while (elapsed < textLeadInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        elapsed = 0f;
        SetDeathOverlayAlpha(0f);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetDeathOverlayAlpha(Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        SetDeathOverlayAlpha(1f);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        if (loadOperation != null)
        {
            while (!loadOperation.isDone)
            {
                yield return null;
            }
        }

        yield return null;

        if (freezeTime)
        {
            Time.timeScale = previousTimeScale;
        }

        SetDeathOverlayAlpha(0f);
        yield return StartCoroutine(RespawnFlashCo());

        SetDeathOverlayVisible(false);
        deathSequenceCoroutine = null;
    }

    private IEnumerator HazardRecoverySequenceCo(Player player, Vector3 respawnPosition)
    {
        EnsureDeathOverlay();
        SetDeathOverlayVisible(true);
        SetDeathTextVisible(false);
        SetDeathOverlayAlpha(0f);
        player.BeginHazardRecovery();

        float blackInDuration = Mathf.Max(0.01f, hazardBlackInDuration);
        float blackHoldDuration = Mathf.Max(0f, hazardBlackHoldDuration);
        float blackOutDuration = Mathf.Max(0.01f, hazardBlackOutDuration);

        float elapsed = 0f;
        while (elapsed < blackInDuration)
        {
            if (player == null)
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            SetDeathOverlayAlpha(Mathf.Clamp01(elapsed / blackInDuration));
            yield return null;
        }

        SetDeathOverlayAlpha(1f);

        if (player != null)
        {
            player.RecoverFromHazard(respawnPosition);
        }

        elapsed = 0f;
        while (elapsed < blackHoldDuration)
        {
            if (player == null)
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < blackOutDuration)
        {
            if (player == null)
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            SetDeathOverlayAlpha(1f - Mathf.Clamp01(elapsed / blackOutDuration));
            yield return null;
        }

        SetDeathOverlayAlpha(0f);
        SetDeathTextVisible(true);

        if (player != null)
        {
            player.EndHazardRecovery();
        }

        SetDeathOverlayVisible(false);
        hazardSequenceCoroutine = null;
    }

    private IEnumerator RespawnFlashCo()
    {
        EnsureDeathOverlay();

        if (respawnFlash == null)
        {
            yield break;
        }

        SetDeathOverlayVisible(true);
        SetRespawnFlashAlpha(1f);

        float elapsed = 0f;
        float flashDuration = Mathf.Max(0.01f, respawnFlashDuration);
        while (elapsed < flashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetRespawnFlashAlpha(1f - Mathf.Clamp01(elapsed / flashDuration));
            yield return null;
        }

        SetRespawnFlashAlpha(0f);
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

    private void EnsureDeathOverlay()
    {
        if (deathOverlayRoot != null)
        {
            return;
        }

        deathOverlayRoot = new GameObject("DeathOverlay", typeof(RectTransform));
        DontDestroyOnLoad(deathOverlayRoot);

        Canvas deathCanvas = deathOverlayRoot.AddComponent<Canvas>();
        deathCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        deathCanvas.sortingOrder = 6000;

        CanvasScaler scaler = deathOverlayRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        deathOverlayRoot.AddComponent<GraphicRaycaster>();

        GameObject backdropObject = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        backdropObject.transform.SetParent(deathOverlayRoot.transform, false);
        deathBackdrop = backdropObject.GetComponent<Image>();
        deathBackdrop.color = new Color(deathBackdropColor.r, deathBackdropColor.g, deathBackdropColor.b, 0f);

        RectTransform backdropRect = backdropObject.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        GameObject flashObject = new GameObject("RespawnFlash", typeof(RectTransform), typeof(Image));
        flashObject.transform.SetParent(deathOverlayRoot.transform, false);
        respawnFlash = flashObject.GetComponent<Image>();
        respawnFlash.color = new Color(respawnFlashColor.r, respawnFlashColor.g, respawnFlashColor.b, 0f);
        respawnFlash.gameObject.SetActive(true);

        RectTransform flashRect = flashObject.GetComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(deathOverlayRoot.transform, false);
        deathText = textObject.GetComponent<Text>();
        deathText.text = "YOU DIED";
        deathText.font = deathTextFont != null ? deathTextFont : LoadUIFont();
        deathText.fontSize = deathTextFontSize;
        deathText.fontStyle = FontStyle.Bold;
        deathText.alignment = TextAnchor.MiddleCenter;
        deathText.color = new Color(deathTextColor.r, deathTextColor.g, deathTextColor.b, 1f);
        deathText.horizontalOverflow = HorizontalWrapMode.Overflow;
        deathText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(1200f, 240f);
        textRect.anchoredPosition = Vector2.zero;

        deathOverlayRoot.SetActive(false);
    }

    private void SetDeathOverlayVisible(bool visible)
    {
        EnsureDeathOverlay();

        if (deathOverlayVisible == visible && deathOverlayRoot != null)
        {
            return;
        }

        deathOverlayVisible = visible;
        if (deathOverlayRoot != null)
        {
            deathOverlayRoot.SetActive(visible);
        }
    }

    private void SetDeathTextVisible(bool visible)
    {
        EnsureDeathOverlay();

        if (deathText != null && deathText.gameObject.activeSelf != visible)
        {
            deathText.gameObject.SetActive(visible);
        }
    }

    private void SetDeathOverlayAlpha(float alpha)
    {
        EnsureDeathOverlay();

        float clampedAlpha = Mathf.Clamp01(alpha);

        if (deathBackdrop != null)
        {
            Color color = deathBackdrop.color;
            color.a = clampedAlpha;
            deathBackdrop.color = color;
        }

        if (deathText != null)
        {
            Color color = deathText.color;
            color.a = 1f;
            deathText.color = color;
        }
    }

    private void SetRespawnFlashAlpha(float alpha)
    {
        EnsureDeathOverlay();

        if (respawnFlash == null)
        {
            return;
        }

        Color color = respawnFlash.color;
        color.a = Mathf.Clamp01(alpha);
        respawnFlash.color = color;
    }

    private static Font LoadUIFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
