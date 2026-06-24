using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public class RuntimeMenuBootstrapper : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateBootstrapper()
    {
        if (GameManager.instance == null || !GameManager.instance.ShouldShowMenuOnStart)
        {
            return;
        }

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == GameManager.instance.MainMenuSceneName)
        {
            return;
        }

        if (FindFirstObjectByType<RuntimeMenuBootstrapper>() != null)
        {
            return;
        }

        GameObject bootstrapper = new GameObject(nameof(RuntimeMenuBootstrapper));
        bootstrapper.AddComponent<RuntimeMenuBootstrapper>();
    }

    private void Start()
    {
        if (GameManager.instance == null || !GameManager.instance.ShouldShowMenuOnStart)
        {
            Destroy(gameObject);
            return;
        }

        BuildMenu();
    }

    private void BuildMenu()
    {
        CreateEventSystemIfNeeded();

        Canvas canvas = CreateCanvas("RuntimeMenuCanvas");
        GameObject root = new GameObject("MenuRoot", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image backdrop = CreateImage(root.transform, "Backdrop", new Color(0f, 0f, 0f, 0.78f));
        StretchFull(backdrop.rectTransform);

        GameObject panel = CreatePanel(root.transform, "MainPanel", new Vector2(0.5f, 0.5f), new Vector2(540f, 640f));
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 18f;
        layout.padding = new RectOffset(36, 36, 42, 42);

        ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject titleObj = CreateText(panel.transform, "Title", "2D Souls-like", 62, FontStyle.Bold, TextAnchor.MiddleCenter);
        LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
        titleLayout.minHeight = 120f;

        GameObject subtitleObj = CreateText(panel.transform, "Subtitle", "Press Play to begin", 24, FontStyle.Normal, TextAnchor.MiddleCenter);
        subtitleObj.AddComponent<LayoutElement>().minHeight = 48f;

        UI_MainMenu mainMenu = root.AddComponent<UI_MainMenu>();
        mainMenu.Configure(panel, null);

        UI_Options options = CreateOptionsPanel(root.transform, panel);
        mainMenu.Configure(panel, options.gameObject);
        options.Configure(panel);
        options.gameObject.SetActive(false);

        Button playButton = CreateButton(panel.transform, "Play", "Play");
        Button continueButton = CreateButton(panel.transform, "Continue", "Continue");
        Button newGameButton = CreateButton(panel.transform, "NewGame", "New Game");
        Button optionsButton = CreateButton(panel.transform, "Options", "Options");
        Button quitButton = CreateButton(panel.transform, "Quit", "Quit");

        bool hasSave = SaveManager.instance != null && SaveManager.instance.HasSaveData;
        continueButton.interactable = hasSave;

        playButton.onClick.AddListener(() =>
        {
            Destroy(root);
            mainMenu.PlayBTN();
        });

        continueButton.onClick.AddListener(() =>
        {
            Destroy(root);
            mainMenu.ContinueBTN();
        });

        newGameButton.onClick.AddListener(() =>
        {
            Destroy(root);
            mainMenu.NewGameBTN();
        });

        optionsButton.onClick.AddListener(mainMenu.OptionsBTN);
        quitButton.onClick.AddListener(mainMenu.QuitGameBTN);

        GameObject optionsPanel = options.gameObject;
        optionsPanel.transform.SetParent(root.transform, false);
        RectTransform optionsRect = optionsPanel.GetComponent<RectTransform>();
        optionsRect.anchorMin = Vector2.zero;
        optionsRect.anchorMax = Vector2.one;
        optionsRect.offsetMin = Vector2.zero;
        optionsRect.offsetMax = Vector2.zero;

        GameManager.instance.ResumeMenu();
    }

    private UI_Options CreateOptionsPanel(Transform parent, GameObject mainPanel)
    {
        GameObject panel = CreatePanel(parent, "OptionsPanel", new Vector2(0.5f, 0.5f), new Vector2(540f, 640f));
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 20f;
        layout.padding = new RectOffset(36, 36, 42, 42);

        ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateText(panel.transform, "OptionsTitle", "Options", 48, FontStyle.Bold, TextAnchor.MiddleCenter)
            .AddComponent<LayoutElement>().minHeight = 90f;

        UI_Options options = panel.AddComponent<UI_Options>();
        CreateLabeledSlider(panel.transform, "Master Volume", out Slider volumeSlider);
        CreateLabeledSlider(panel.transform, "BGM Volume", out Slider bgmSlider);
        CreateLabeledSlider(panel.transform, "SFX Volume", out Slider sfxSlider);

        options.Bind(volumeSlider, bgmSlider, sfxSlider);

        Button resetButton = CreateButton(panel.transform, "ResetVolume", "Restore Defaults");
        Button backButton = CreateButton(panel.transform, "Back", "Back");
        resetButton.onClick.AddListener(options.ResetVolumeDefaults);
        backButton.onClick.AddListener(() =>
        {
            options.CloseBTN();
            if (mainPanel != null)
            {
                mainPanel.SetActive(true);
            }
        });

        return options;
    }

    private static Canvas CreateCanvas(string name)
    {
        GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static void CreateEventSystemIfNeeded()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Object.DontDestroyOnLoad(eventSystem);
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.1f, 0.94f);
        return panel;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static GameObject CreateText(Transform parent, string name, string text, int fontSize, FontStyle style, TextAnchor alignment)
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

    private static Button CreateButton(Transform parent, string name, string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 74f);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.24f, 1f);

        Button button = go.GetComponent<Button>();

        GameObject textObj = CreateText(go.transform, "Label", label, 30, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private static GameObject CreateLabeledSlider(Transform parent, string label, out Slider slider)
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
        bg.color = new Color(0.16f, 0.16f, 0.18f, 1f);

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
        fillImage.color = new Color(0.85f, 0.75f, 0.35f, 1f);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fillImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.transition = Selectable.Transition.ColorTint;

        RectTransform sliderRect = sliderGo.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(0f, 36f);

        return row;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
