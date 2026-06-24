using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UI_PauseMenu : MonoBehaviour
{
    [Header("Fallback Scenes")]
    [SerializeField] private string fallbackMainMenuSceneName = "MainMenu";

    [Header("Root Panels")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject volumePanel;
    [SerializeField] private GameObject difficultyPanel;

    [Header("Main Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button volumeButton;
    [SerializeField] private Button difficultyButton;

    [Header("Volume Controls")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button volumeBackButton;

    [Header("Difficulty Controls")]
    [SerializeField] private Button easyButton;
    [SerializeField] private Button normalButton;
    [SerializeField] private Button difficultyBackButton;

    [Header("Visual State")]
    [SerializeField] private Image easyButtonImage;
    [SerializeField] private Image normalButtonImage;
    [SerializeField] private Color selectedButtonColor = new Color(0.35f, 0.3f, 0.12f, 1f);
    [SerializeField] private Color unselectedButtonColor = new Color(0.2f, 0.2f, 0.24f, 1f);

    private bool cursorStateCaptured;
    private bool originalCursorVisible;
    private CursorLockMode originalCursorLockState;

    private void Awake()
    {
        ResolveHierarchyReferences();
        WireButtons();
    }

    private void Start()
    {
        ResolveHierarchyReferences();
        WireButtons();
        HideMenu();
    }

    public void OpenMenu()
    {
        if (menuRoot == null)
        {
            return;
        }

        CaptureCursorState();
        menuRoot.SetActive(true);
        ShowMainPanel();
        PauseGameplayContext();
    }

    public void CloseMenu()
    {
        HideMenu();
        ResumeGameplayContext();
        RestoreCursorState();
    }

    public void GoMainMenu()
    {
        RestoreCursorState();
        if (GameManager.instance != null)
        {
            GameManager.instance.LoadMainMenu();
            return;
        }

        if (!string.IsNullOrWhiteSpace(fallbackMainMenuSceneName))
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(fallbackMainMenuSceneName);
        }
    }

    public void SetEasyDifficulty()
    {
        GameDifficultySettings.SetDifficulty(GameDifficulty.Easy);
        RefreshDifficultyUI();
    }

    public void SetNormalDifficulty()
    {
        GameDifficultySettings.SetDifficulty(GameDifficulty.Normal);
        RefreshDifficultyUI();
    }

    public void RefreshVolumeUI()
    {
        float master = AudioManager.instance != null
            ? AudioManager.instance.GetMasterVolume()
            : PlayerPrefs.GetFloat(AudioVolumeKeys.Master, AudioVolumeDefaults.Master);
        float bgm = AudioManager.instance != null
            ? AudioManager.instance.GetBgmVolume()
            : PlayerPrefs.GetFloat(AudioVolumeKeys.Bgm, AudioVolumeDefaults.Bgm);
        float sfx = AudioManager.instance != null
            ? AudioManager.instance.GetSfxVolume()
            : PlayerPrefs.GetFloat(AudioVolumeKeys.Sfx, AudioVolumeDefaults.Sfx);

        masterSlider?.SetValueWithoutNotify(master);
        bgmSlider?.SetValueWithoutNotify(bgm);
        sfxSlider?.SetValueWithoutNotify(sfx);
    }

    public void RefreshDifficultyUI()
    {
        bool easySelected = GameDifficultySettings.IsEasy;

        if (easyButtonImage != null)
        {
            easyButtonImage.color = easySelected ? selectedButtonColor : unselectedButtonColor;
        }

        if (normalButtonImage != null)
        {
            normalButtonImage.color = easySelected ? unselectedButtonColor : selectedButtonColor;
        }
    }

    public void MasterVolumeValue(float value)
    {
        ApplyVolume(AudioVolumeKeys.Master, Mathf.Clamp01(value), volume => AudioManager.instance?.SetMasterVolume(volume));
    }

    public void BgmVolumeValue(float value)
    {
        ApplyVolume(AudioVolumeKeys.Bgm, Mathf.Clamp01(value), volume => AudioManager.instance?.SetBgmVolume(volume));
    }

    public void SfxVolumeValue(float value)
    {
        ApplyVolume(AudioVolumeKeys.Sfx, Mathf.Clamp01(value), volume => AudioManager.instance?.SetSfxVolume(volume));
    }

    public void HandleEscapeRequest()
    {
        if (GameManager.instance != null
            && GameManager.instance.IsMenuScene)
        {
            return;
        }

        HandleEscape();
    }

    private void HandleEscape()
    {
        if (!IsMenuVisible())
        {
            OpenMenu();
            return;
        }

        if (volumePanel != null && volumePanel.activeSelf)
        {
            ShowMainPanel();
            return;
        }

        if (difficultyPanel != null && difficultyPanel.activeSelf)
        {
            ShowMainPanel();
            return;
        }

        CloseMenu();
    }

    private void HideMenu()
    {
        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }

        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        if (volumePanel != null)
        {
            volumePanel.SetActive(false);
        }

        if (difficultyPanel != null)
        {
            difficultyPanel.SetActive(false);
        }
    }

    private void ShowMainPanel()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }

        if (volumePanel != null)
        {
            volumePanel.SetActive(false);
        }

        if (difficultyPanel != null)
        {
            difficultyPanel.SetActive(false);
        }

        if (IsMenuVisible() && resumeButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }
    }

    private void ShowVolumePanel()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        if (volumePanel != null)
        {
            volumePanel.SetActive(true);
        }

        if (difficultyPanel != null)
        {
            difficultyPanel.SetActive(false);
        }

        RefreshVolumeUI();

        if (IsMenuVisible() && masterSlider != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(masterSlider.gameObject);
        }
    }

    private void ShowDifficultyPanel()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        if (volumePanel != null)
        {
            volumePanel.SetActive(false);
        }

        if (difficultyPanel != null)
        {
            difficultyPanel.SetActive(true);
        }

        RefreshDifficultyUI();

        if (IsMenuVisible() && easyButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(easyButton.gameObject);
        }
    }

    private void ResolveHierarchyReferences()
    {
        if (menuRoot == null)
        {
            menuRoot = transform.Find("MenuRoot")?.gameObject;
        }

        if (mainPanel == null)
        {
            mainPanel = menuRoot != null ? menuRoot.transform.Find("MainPanel")?.gameObject : transform.Find("MainPanel")?.gameObject;
        }

        if (volumePanel == null)
        {
            volumePanel = menuRoot != null ? menuRoot.transform.Find("VolumePanel")?.gameObject : transform.Find("VolumePanel")?.gameObject;
        }

        if (difficultyPanel == null)
        {
            difficultyPanel = menuRoot != null ? menuRoot.transform.Find("DifficultyPanel")?.gameObject : transform.Find("DifficultyPanel")?.gameObject;
        }

        resumeButton ??= FindButton(mainPanel != null ? mainPanel.transform : null, "ResumeButton");
        mainMenuButton ??= FindButton(mainPanel != null ? mainPanel.transform : null, "MainMenuButton");
        volumeButton ??= FindButton(mainPanel != null ? mainPanel.transform : null, "VolumeButton");
        difficultyButton ??= FindButton(mainPanel != null ? mainPanel.transform : null, "DifficultyButton");

        volumeBackButton ??= FindButton(volumePanel != null ? volumePanel.transform : null, "BackButton");
        difficultyBackButton ??= FindButton(difficultyPanel != null ? difficultyPanel.transform : null, "BackButton");

        masterSlider ??= FindSlider(volumePanel != null ? volumePanel.transform : null, "Master VolumeSlider");
        bgmSlider ??= FindSlider(volumePanel != null ? volumePanel.transform : null, "BGM VolumeSlider");
        sfxSlider ??= FindSlider(volumePanel != null ? volumePanel.transform : null, "SFX VolumeSlider");

        easyButton ??= FindButton(difficultyPanel != null ? difficultyPanel.transform : null, "EasyButton");
        normalButton ??= FindButton(difficultyPanel != null ? difficultyPanel.transform : null, "NormalButton");

        easyButtonImage ??= easyButton != null ? easyButton.GetComponent<Image>() : null;
        normalButtonImage ??= normalButton != null ? normalButton.GetComponent<Image>() : null;
    }

    private void WireButtons()
    {
        if (resumeButton != null && resumeButton.onClick.GetPersistentEventCount() == 0)
        {
            resumeButton.onClick.AddListener(CloseMenu);
        }

        if (mainMenuButton != null && mainMenuButton.onClick.GetPersistentEventCount() == 0)
        {
            mainMenuButton.onClick.AddListener(GoMainMenu);
        }

        if (volumeButton != null && volumeButton.onClick.GetPersistentEventCount() == 0)
        {
            volumeButton.onClick.AddListener(ShowVolumePanel);
        }

        if (difficultyButton != null && difficultyButton.onClick.GetPersistentEventCount() == 0)
        {
            difficultyButton.onClick.AddListener(ShowDifficultyPanel);
        }

        if (volumeBackButton != null && volumeBackButton.onClick.GetPersistentEventCount() == 0)
        {
            volumeBackButton.onClick.AddListener(ShowMainPanel);
        }

        if (difficultyBackButton != null && difficultyBackButton.onClick.GetPersistentEventCount() == 0)
        {
            difficultyBackButton.onClick.AddListener(ShowMainPanel);
        }

        if (easyButton != null && easyButton.onClick.GetPersistentEventCount() == 0)
        {
            easyButton.onClick.AddListener(SetEasyDifficulty);
        }

        if (normalButton != null && normalButton.onClick.GetPersistentEventCount() == 0)
        {
            normalButton.onClick.AddListener(SetNormalDifficulty);
        }

        if (masterSlider != null && masterSlider.onValueChanged.GetPersistentEventCount() == 0)
        {
            masterSlider.onValueChanged.AddListener(MasterVolumeValue);
        }

        if (bgmSlider != null && bgmSlider.onValueChanged.GetPersistentEventCount() == 0)
        {
            bgmSlider.onValueChanged.AddListener(BgmVolumeValue);
        }

        if (sfxSlider != null && sfxSlider.onValueChanged.GetPersistentEventCount() == 0)
        {
            sfxSlider.onValueChanged.AddListener(SfxVolumeValue);
        }
    }

    private void CaptureCursorState()
    {
        if (cursorStateCaptured)
        {
            return;
        }

        originalCursorVisible = Cursor.visible;
        originalCursorLockState = Cursor.lockState;
        cursorStateCaptured = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void RestoreCursorState()
    {
        if (!cursorStateCaptured)
        {
            return;
        }

        Cursor.visible = originalCursorVisible;
        Cursor.lockState = originalCursorLockState;
    }

    private static Button FindButton(Transform root, string buttonName)
    {
        Transform child = FindDeepChild(root, buttonName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private static Slider FindSlider(Transform root, string sliderName)
    {
        Transform child = FindDeepChild(root, sliderName);
        return child != null ? child.GetComponent<Slider>() : null;
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindDeepChild(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static void ApplyVolume(string prefsKey, float value, System.Action<float> applyRuntime)
    {
        if (applyRuntime != null)
        {
            applyRuntime(value);
        }

        if (prefsKey == AudioVolumeKeys.Master)
        {
            AudioListener.volume = value;
        }

        PlayerPrefs.SetFloat(prefsKey, value);
        PlayerPrefs.Save();
    }

    private bool IsMenuVisible()
    {
        return menuRoot != null && menuRoot.activeSelf;
    }

    private void PauseGameplayContext()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.PauseGameplay();
            return;
        }

        Time.timeScale = 0f;
    }

    private void ResumeGameplayContext()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.ResumeGameplay();
        }
        else
        {
            Time.timeScale = 1f;
        }
    }
}
