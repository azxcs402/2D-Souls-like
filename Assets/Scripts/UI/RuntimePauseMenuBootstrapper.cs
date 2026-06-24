using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

[DefaultExecutionOrder(-1000)]
public class RuntimePauseMenuBootstrapper : MonoBehaviour
{
    private readonly Color panelColor = new Color(0.08f, 0.08f, 0.1f, 0.94f);
    private readonly Color backdropColor = new Color(0f, 0f, 0f, 0.78f);
    private readonly Color buttonColor = new Color(0.2f, 0.2f, 0.24f, 1f);
    private readonly Color selectedButtonColor = new Color(0.35f, 0.3f, 0.12f, 1f);
    private readonly Color unselectedButtonColor = new Color(0.2f, 0.2f, 0.24f, 1f);
    private readonly Color sliderBackgroundColor = new Color(0.16f, 0.16f, 0.18f, 1f);
    private readonly Color sliderFillColor = new Color(0.85f, 0.75f, 0.35f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateBootstrapper()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            return;
        }

        if (Object.FindFirstObjectByType<UI_PauseMenu>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        if (Object.FindFirstObjectByType<RuntimePauseMenuBootstrapper>() != null)
        {
            return;
        }

        GameObject bootstrapper = new GameObject(nameof(RuntimePauseMenuBootstrapper));
        bootstrapper.AddComponent<RuntimePauseMenuBootstrapper>();
    }

    private GameObject menuRoot;
    private GameObject mainPanel;
    private GameObject volumePanel;
    private GameObject difficultyPanel;
    private Button resumeButton;
    private Button mainMenuButton;
    private Button volumeButton;
    private Button difficultyButton;
    private Slider masterSlider;
    private Slider bgmSlider;
    private Slider sfxSlider;
    private Button easyButton;
    private Button normalButton;
    private Image easyButtonImage;
    private Image normalButtonImage;
    private bool cursorStateCaptured;
    private bool originalCursorVisible;
    private CursorLockMode originalCursorLockState;
    private Coroutine initRoutine;

    private void Start()
    {
        if (initRoutine != null)
        {
            return;
        }

        initRoutine = StartCoroutine(WaitForGameManagerAndBuild());
    }

    private void Update()
    {
        if (GameManager.instance == null
            || GameManager.instance.IsMenuScene)
        {
            return;
        }

        bool escapePressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            || Input.GetKeyDown(KeyCode.Escape);
        if (!escapePressed)
        {
            return;
        }

        HandleEscape();
    }

    private IEnumerator WaitForGameManagerAndBuild()
    {
        while (GameManager.instance == null)
        {
            yield return null;
        }

        if (GameManager.instance.IsMenuScene)
        {
            Destroy(gameObject);
            yield break;
        }

        BuildMenu();
        initRoutine = null;
    }

    private void HandleEscape()
    {
        if (menuRoot == null)
        {
            return;
        }

        if (!menuRoot.activeSelf)
        {
            if (Time.timeScale <= 0f && (GameManager.instance == null || !GameManager.instance.IsGameplayPaused))
            {
                return;
            }

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

    private void BuildMenu()
    {
        CreateEventSystemIfNeeded();

        Canvas canvas = CreateCanvas("PauseMenuCanvas");
        menuRoot = new GameObject("PauseMenuRoot", typeof(RectTransform));
        menuRoot.transform.SetParent(canvas.transform, false);
        StretchFull(menuRoot.GetComponent<RectTransform>());
        menuRoot.SetActive(false);

        Image backdrop = CreateImage(menuRoot.transform, "Backdrop", backdropColor);
        StretchFull(backdrop.rectTransform);

        mainPanel = CreatePanel(menuRoot.transform, "MainPanel", new Vector2(0.5f, 0.5f), new Vector2(560f, 620f));
        volumePanel = CreatePanel(menuRoot.transform, "VolumePanel", new Vector2(0.5f, 0.5f), new Vector2(620f, 620f));
        difficultyPanel = CreatePanel(menuRoot.transform, "DifficultyPanel", new Vector2(0.5f, 0.5f), new Vector2(560f, 460f));

        BuildMainPanel(mainPanel.transform);
        BuildVolumePanel(volumePanel.transform);
        BuildDifficultyPanel(difficultyPanel.transform);

        ShowMainPanel();
    }

    private void BuildMainPanel(Transform parent)
    {
        VerticalLayoutGroup layout = parent.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 18f;
        layout.padding = new RectOffset(36, 36, 42, 42);

        ContentSizeFitter fitter = parent.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateText(parent, "Title", "Paused", 60, FontStyle.Bold, TextAnchor.MiddleCenter)
            .AddComponent<LayoutElement>().minHeight = 110f;

        resumeButton = CreateButton(parent, "ResumeButton", "Resume");
        mainMenuButton = CreateButton(parent, "MainMenuButton", "Main Menu");
        volumeButton = CreateButton(parent, "VolumeButton", "Volume");
        difficultyButton = CreateButton(parent, "DifficultyButton", "Difficulty");

        resumeButton.onClick.AddListener(CloseMenu);
        mainMenuButton.onClick.AddListener(GoMainMenu);
        volumeButton.onClick.AddListener(ShowVolumePanel);
        difficultyButton.onClick.AddListener(ShowDifficultyPanel);
    }

    private void BuildVolumePanel(Transform parent)
    {
        VerticalLayoutGroup layout = parent.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 18f;
        layout.padding = new RectOffset(36, 36, 42, 42);

        ContentSizeFitter fitter = parent.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateText(parent, "Title", "Volume", 52, FontStyle.Bold, TextAnchor.MiddleCenter)
            .AddComponent<LayoutElement>().minHeight = 92f;

        CreateLabeledSlider(parent, "Master Volume", out masterSlider);
        CreateLabeledSlider(parent, "BGM Volume", out bgmSlider);
        CreateLabeledSlider(parent, "SFX Volume", out sfxSlider);

        Button backButton = CreateButton(parent, "BackButton", "Back");
        backButton.onClick.AddListener(ShowMainPanel);
    }

    private void BuildDifficultyPanel(Transform parent)
    {
        VerticalLayoutGroup layout = parent.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 18f;
        layout.padding = new RectOffset(36, 36, 42, 42);

        ContentSizeFitter fitter = parent.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateText(parent, "Title", "Difficulty", 52, FontStyle.Bold, TextAnchor.MiddleCenter)
            .AddComponent<LayoutElement>().minHeight = 92f;

        GameObject row = new GameObject("DifficultyChoices", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childControlWidth = true;
        rowLayout.spacing = 16f;

        easyButton = CreateButton(row.transform, "EasyButton", "Easy");
        normalButton = CreateButton(row.transform, "NormalButton", "Normal");
        easyButtonImage = easyButton.GetComponent<Image>();
        normalButtonImage = normalButton.GetComponent<Image>();

        easyButton.onClick.AddListener(SetEasyDifficulty);
        normalButton.onClick.AddListener(SetNormalDifficulty);

        Button backButton = CreateButton(parent, "BackButton", "Back");
        backButton.onClick.AddListener(ShowMainPanel);
    }

    private void OpenMenu()
    {
        if (menuRoot == null)
        {
            return;
        }

        CaptureCursorState();
        menuRoot.SetActive(true);
        GameManager.instance?.PauseGameplay();
        ShowMainPanel();
    }

    private void CloseMenu()
    {
        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }

        GameManager.instance?.ResumeGameplay();
        RestoreCursorState();
    }

    private void GoMainMenu()
    {
        GameManager.instance?.LoadMainMenu();
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

        if (menuRoot != null && menuRoot.activeSelf && resumeButton != null && EventSystem.current != null)
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

        if (menuRoot != null && menuRoot.activeSelf && masterSlider != null && EventSystem.current != null)
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

        if (menuRoot != null && menuRoot.activeSelf && easyButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(easyButton.gameObject);
        }
    }

    private void RefreshVolumeUI()
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

    private void RefreshDifficultyUI()
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

    private void SetEasyDifficulty()
    {
        GameDifficultySettings.SetDifficulty(GameDifficulty.Easy);
        RefreshDifficultyUI();
    }

    private void SetNormalDifficulty()
    {
        GameDifficultySettings.SetDifficulty(GameDifficulty.Normal);
        RefreshDifficultyUI();
    }

    private void OnMasterVolumeChanged(float value)
    {
        float clampedValue = Mathf.Clamp01(value);
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetMasterVolume(clampedValue);
            return;
        }

        ApplyFallbackVolume(AudioVolumeKeys.Master, clampedValue);
    }

    private void OnBgmVolumeChanged(float value)
    {
        float clampedValue = Mathf.Clamp01(value);
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetBgmVolume(clampedValue);
            return;
        }

        ApplyFallbackVolume(AudioVolumeKeys.Bgm, clampedValue);
    }

    private void OnSfxVolumeChanged(float value)
    {
        float clampedValue = Mathf.Clamp01(value);
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetSfxVolume(clampedValue);
            return;
        }

        ApplyFallbackVolume(AudioVolumeKeys.Sfx, clampedValue);
    }

    private void ApplyFallbackVolume(string prefsKey, float value)
    {
        if (prefsKey == AudioVolumeKeys.Master)
        {
            AudioListener.volume = value;
        }

        PlayerPrefs.SetFloat(prefsKey, value);
        PlayerPrefs.Save();
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

    private void WireSlider(Slider slider, System.Action<float> handler)
    {
        if (slider == null || handler == null)
        {
            return;
        }

        slider.onValueChanged.AddListener(value => handler(value));
    }

    private void CreateEventSystemIfNeeded()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystem);
    }

    private Canvas CreateCanvas(string name)
    {
        GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6000;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        image.color = panelColor;

        return panel;
    }

    private Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private GameObject CreateText(Transform parent, string name, string text, int fontSize, FontStyle style, TextAnchor alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text uiText = go.GetComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiText.font == null)
        {
            uiText.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        }

        uiText.fontSize = fontSize;
        uiText.fontStyle = style;
        uiText.alignment = alignment;
        uiText.color = Color.white;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
        return go;
    }

    private Button CreateButton(Transform parent, string name, string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 74f);

        Image image = go.GetComponent<Image>();
        image.color = buttonColor;

        Button button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        UIRectangularButtonUtility.MakeRectangular(button);

        GameObject textObj = CreateText(go.transform, "Label", label, 30, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private void CreateLabeledSlider(Transform parent, string label, out Slider slider)
    {
        GameObject row = new GameObject(label.Replace(" ", "") + "Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 12f;

        CreateText(row.transform, label + "Label", label, 24, FontStyle.Normal, TextAnchor.MiddleLeft)
            .AddComponent<LayoutElement>().preferredWidth = 180f;

        GameObject sliderGo = new GameObject(label + "Slider", typeof(RectTransform), typeof(Slider), typeof(Image));
        sliderGo.transform.SetParent(row.transform, false);
        slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.6f;

        Image bg = sliderGo.GetComponent<Image>();
        bg.color = sliderBackgroundColor;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(10f, 0f);
        fillAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = sliderFillColor;

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fillImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.transition = Selectable.Transition.ColorTint;

        RectTransform sliderRect = sliderGo.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(0f, 36f);

        if (label == "Master Volume")
        {
            WireSlider(slider, OnMasterVolumeChanged);
        }
        else if (label == "BGM Volume")
        {
            WireSlider(slider, OnBgmVolumeChanged);
        }
        else if (label == "SFX Volume")
        {
            WireSlider(slider, OnSfxVolumeChanged);
        }
    }

    private void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
