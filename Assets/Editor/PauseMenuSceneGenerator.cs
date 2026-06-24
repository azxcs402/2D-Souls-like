using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PauseMenuSceneGenerator
{
    private const string PauseMenuRootName = "PauseMenuRoot";
    private const string MenuRootName = "MenuRoot";
    private const string MainPanelName = "MainPanel";
    private const string VolumePanelName = "VolumePanel";
    private const string DifficultyPanelName = "DifficultyPanel";

    [MenuItem("Tools/2D Souls-like/Create Pause Menu In Active Scene")]
    public static void CreatePauseMenuInActiveScene()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("Pause menu layout can only be created while the editor is idle.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            Debug.LogError("No active scene is loaded.");
            return;
        }

        EnsureEventSystem();
        Canvas canvas = EnsureCanvas();
        GameObject pauseMenuRoot = EnsurePauseMenuRoot(canvas.transform);
        BuildOrUpdatePauseMenu(pauseMenuRoot.transform);

        EditorSceneManager.MarkSceneDirty(activeScene);
        Debug.Log("Pause menu hierarchy created or updated.");
    }

    [MenuItem("Tools/2D Souls-like/Rebuild Pause Menu Layout")]
    public static void RebuildPauseMenuLayout()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            Debug.LogError("No active scene is loaded.");
            return;
        }

        GameObject pauseMenuRoot = GameObject.Find(PauseMenuRootName);
        if (pauseMenuRoot == null)
        {
            Debug.LogError("PauseMenuRoot was not found in the active scene.");
            return;
        }

        BuildOrUpdatePauseMenu(pauseMenuRoot.transform);
        EditorSceneManager.MarkSceneDirty(activeScene);
        Debug.Log("Pause menu layout rebuilt.");
    }

    private static void BuildOrUpdatePauseMenu(Transform pauseMenuRoot)
    {
        UI_PauseMenu pauseMenu = pauseMenuRoot.GetComponent<UI_PauseMenu>();
        if (pauseMenu == null)
        {
            pauseMenu = pauseMenuRoot.gameObject.AddComponent<UI_PauseMenu>();
        }

        GameObject menuRoot = CreateOrGetContainer(pauseMenuRoot, MenuRootName);
        StretchFull(menuRoot.GetComponent<RectTransform>());

        GameObject backdrop = CreateOrGetImage(menuRoot.transform, "Backdrop", new Color(0.03f, 0.03f, 0.04f, 0.82f));
        StretchFull(backdrop.GetComponent<RectTransform>());

        CreateBackdropAccent(menuRoot.transform, "BackdropAccentLeft", new Vector2(0f, 0.5f), new Vector2(18f, 760f), new Vector2(68f, 0f), new Color(0.85f, 0.74f, 0.35f, 0.06f));
        CreateBackdropAccent(menuRoot.transform, "BackdropAccentRight", new Vector2(1f, 0.5f), new Vector2(18f, 760f), new Vector2(-68f, 0f), new Color(0.85f, 0.74f, 0.35f, 0.035f));

        GameObject mainPanel = CreateOrGetPanel(menuRoot.transform, MainPanelName, new Color(0.08f, 0.08f, 0.1f, 0.95f));
        SetupPanel(mainPanel.GetComponent<RectTransform>(), new Vector2(620f, 700f));
        CreateVerticalPanelLayout(mainPanel.transform, 12f);
        GameObject mainContent = CreateOrGetContainer(mainPanel.transform, "Content");
        CreateVerticalPanelLayout(mainContent.transform, 18f);
        GameObject mainHeader = CreateOrGetContainer(mainContent.transform, "Header");
        CreateVerticalPanelLayout(mainHeader.transform, 10f);
        GameObject mainActions = CreateOrGetContainer(mainContent.transform, "Actions");
        CreateVerticalPanelLayout(mainActions.transform, 14f);
        GameObject mainFooter = CreateOrGetContainer(mainContent.transform, "Footer");
        CreateVerticalPanelLayout(mainFooter.transform, 8f);

        CreateText(mainHeader.transform, "Tagline", "PAUSE MENU", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        CreateDivider(mainHeader.transform, "TitleRuleTop", new Color(0.85f, 0.74f, 0.35f, 0.35f), 2f, 170f);
        CreateText(mainHeader.transform, "Title", "Paused", 62, FontStyle.Bold, TextAnchor.MiddleCenter);
        CreateText(mainHeader.transform, "Subtitle", "Adjust settings or return to the game", 22, FontStyle.Normal, TextAnchor.MiddleCenter);
        CreateDivider(mainHeader.transform, "TitleRuleBottom", new Color(0.85f, 0.74f, 0.35f, 0.22f), 1f, 210f);

        CreateButton(mainActions.transform, "ResumeButton", "Resume");
        CreateButton(mainActions.transform, "MainMenuButton", "Main Menu");
        CreateButton(mainActions.transform, "VolumeButton", "Volume");
        CreateButton(mainActions.transform, "DifficultyButton", "Difficulty");

        CreateText(mainFooter.transform, "Hint", "Esc to resume. Main Menu will leave the current run.", 16, FontStyle.Normal, TextAnchor.MiddleCenter);

        GameObject volumePanel = CreateOrGetPanel(menuRoot.transform, VolumePanelName, new Color(0.08f, 0.08f, 0.1f, 0.95f));
        SetupPanel(volumePanel.GetComponent<RectTransform>(), new Vector2(620f, 620f));
        CreateVerticalPanelLayout(volumePanel.transform, 16f);
        GameObject volumeContent = CreateOrGetContainer(volumePanel.transform, "Content");
        CreateVerticalPanelLayout(volumeContent.transform, 16f);
        GameObject volumeHeader = CreateOrGetContainer(volumeContent.transform, "Header");
        CreateVerticalPanelLayout(volumeHeader.transform, 8f);
        GameObject volumeSettings = CreateOrGetContainer(volumeContent.transform, "Settings");
        CreateVerticalPanelLayout(volumeSettings.transform, 18f);
        GameObject volumeFooter = CreateOrGetContainer(volumeContent.transform, "Footer");
        CreateVerticalPanelLayout(volumeFooter.transform, 8f);

        CreateText(volumeHeader.transform, "Title", "Volume", 48, FontStyle.Bold, TextAnchor.MiddleCenter);
        CreateDivider(volumeHeader.transform, "OptionsRule", new Color(0.85f, 0.74f, 0.35f, 0.25f), 1f, 180f);
        CreateSliderRow(volumeSettings.transform, "Master Volume");
        CreateSliderRow(volumeSettings.transform, "BGM Volume");
        CreateSliderRow(volumeSettings.transform, "SFX Volume");
        CreateButton(volumeFooter.transform, "BackButton", "Back");

        GameObject difficultyPanel = CreateOrGetPanel(menuRoot.transform, DifficultyPanelName, new Color(0.08f, 0.08f, 0.1f, 0.95f));
        SetupPanel(difficultyPanel.GetComponent<RectTransform>(), new Vector2(620f, 460f));
        CreateVerticalPanelLayout(difficultyPanel.transform, 16f);
        GameObject difficultyContent = CreateOrGetContainer(difficultyPanel.transform, "Content");
        CreateVerticalPanelLayout(difficultyContent.transform, 16f);
        GameObject difficultyHeader = CreateOrGetContainer(difficultyContent.transform, "Header");
        CreateVerticalPanelLayout(difficultyHeader.transform, 8f);
        GameObject difficultyActions = CreateOrGetContainer(difficultyContent.transform, "Actions");
        CreateHorizontalRow(difficultyActions.transform, "DifficultyChoices");
        GameObject difficultyFooter = CreateOrGetContainer(difficultyContent.transform, "Footer");
        CreateVerticalPanelLayout(difficultyFooter.transform, 8f);

        CreateText(difficultyHeader.transform, "Title", "Difficulty", 48, FontStyle.Bold, TextAnchor.MiddleCenter);
        CreateDivider(difficultyHeader.transform, "OptionsRule", new Color(0.85f, 0.74f, 0.35f, 0.25f), 1f, 180f);
        Transform choices = difficultyActions.transform.Find("DifficultyChoices");
        if (choices != null)
        {
            CreateButton(choices, "EasyButton", "Easy");
            CreateButton(choices, "NormalButton", "Normal");
        }
        CreateButton(difficultyFooter.transform, "BackButton", "Back");

        SerializedObject pauseMenuObject = new SerializedObject(pauseMenu);
        pauseMenuObject.FindProperty("menuRoot").objectReferenceValue = menuRoot;
        pauseMenuObject.FindProperty("mainPanel").objectReferenceValue = mainPanel;
        pauseMenuObject.FindProperty("volumePanel").objectReferenceValue = volumePanel;
        pauseMenuObject.FindProperty("difficultyPanel").objectReferenceValue = difficultyPanel;
        pauseMenuObject.FindProperty("resumeButton").objectReferenceValue = FindButton(mainPanel.transform, "ResumeButton");
        pauseMenuObject.FindProperty("mainMenuButton").objectReferenceValue = FindButton(mainPanel.transform, "MainMenuButton");
        pauseMenuObject.FindProperty("volumeButton").objectReferenceValue = FindButton(mainPanel.transform, "VolumeButton");
        pauseMenuObject.FindProperty("difficultyButton").objectReferenceValue = FindButton(mainPanel.transform, "DifficultyButton");
        pauseMenuObject.FindProperty("masterSlider").objectReferenceValue = FindSlider(volumePanel.transform, "Master VolumeSlider");
        pauseMenuObject.FindProperty("bgmSlider").objectReferenceValue = FindSlider(volumePanel.transform, "BGM VolumeSlider");
        pauseMenuObject.FindProperty("sfxSlider").objectReferenceValue = FindSlider(volumePanel.transform, "SFX VolumeSlider");
        pauseMenuObject.FindProperty("volumeBackButton").objectReferenceValue = FindButton(volumePanel.transform, "BackButton");
        pauseMenuObject.FindProperty("easyButton").objectReferenceValue = FindButton(difficultyPanel.transform, "EasyButton");
        pauseMenuObject.FindProperty("normalButton").objectReferenceValue = FindButton(difficultyPanel.transform, "NormalButton");
        pauseMenuObject.FindProperty("difficultyBackButton").objectReferenceValue = FindButton(difficultyPanel.transform, "BackButton");
        pauseMenuObject.FindProperty("easyButtonImage").objectReferenceValue = FindButton(difficultyPanel.transform, "EasyButton")?.GetComponent<Image>();
        pauseMenuObject.FindProperty("normalButtonImage").objectReferenceValue = FindButton(difficultyPanel.transform, "NormalButton")?.GetComponent<Image>();
        pauseMenuObject.ApplyModifiedPropertiesWithoutUndo();

        menuRoot.SetActive(false);
        difficultyPanel.SetActive(false);
        volumePanel.SetActive(false);
        mainPanel.SetActive(false);

        EditorUtility.SetDirty(pauseMenu);
    }

    private static Canvas EnsureCanvas()
    {
        GameObject existingCanvasObject = GameObject.Find("PauseMenuCanvas");
        if (existingCanvasObject != null)
        {
            Canvas existingCanvas = existingCanvasObject.GetComponent<Canvas>();
            if (existingCanvas != null)
            {
                return existingCanvas;
            }
        }

        GameObject canvasObject = new GameObject("PauseMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static GameObject EnsurePauseMenuRoot(Transform parent)
    {
        Transform existing = parent.Find(PauseMenuRootName);
        GameObject root = existing != null ? existing.gameObject : new GameObject(PauseMenuRootName, typeof(RectTransform), typeof(UI_PauseMenu));
        root.transform.SetParent(parent, false);
        StretchFull(root.GetComponent<RectTransform>());
        return root;
    }

    private static GameObject CreateOrGetPanel(Transform parent, string name, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject panel = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(parent, false);

        Image image = panel.GetComponent<Image>();
        if (image == null)
        {
            image = panel.AddComponent<Image>();
        }

        image.color = color;
        image.raycastTarget = true;
        return panel;
    }

    private static GameObject CreateOrGetContainer(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        GameObject container = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        container.transform.SetParent(parent, false);
        return container;
    }

    private static GameObject CreateOrGetImage(Transform parent, string name, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject imageObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return imageObject;
    }

    private static GameObject CreateBackdropAccent(Transform parent, string name, Vector2 anchor, Vector2 sizeDelta, Vector2 anchoredPosition, Color color)
    {
        GameObject accent = CreateOrGetImage(parent, name, color);
        RectTransform rect = accent.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;
        return accent;
    }

    private static void SetupPanel(RectTransform rect, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
    }

    private static void CreateVerticalPanelLayout(Transform parent, float spacing)
    {
        VerticalLayoutGroup layout = parent.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = spacing;
        layout.padding = new RectOffset(36, 36, 42, 42);

        ContentSizeFitter fitter = parent.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = parent.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static void CreateHorizontalRow(Transform parent, string name)
    {
        GameObject row = parent.Find(name) != null ? parent.Find(name).gameObject : new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 16f;
    }

    private static void CreatePanelFrame(Transform parent, string name, Color color)
    {
        GameObject frame = CreateOrGetImage(parent, name, color);
        RectTransform rect = frame.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(10f, 10f);
        rect.offsetMax = new Vector2(-10f, -10f);

        LayoutElement layoutElement = frame.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = frame.AddComponent<LayoutElement>();
        }

        layoutElement.ignoreLayout = true;
    }

    private static Text CreateText(Transform parent, string name, string text, int fontSize, FontStyle style, TextAnchor alignment)
    {
        GameObject existing = parent.Find(name) != null ? parent.Find(name).gameObject : null;
        GameObject go = existing != null ? existing : new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        Text uiText = go.GetComponent<Text>();
        if (uiText == null)
        {
            uiText = go.AddComponent<Text>();
        }

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

        LayoutElement layoutElement = uiText.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = uiText.gameObject.AddComponent<LayoutElement>();
        }

        if (name == "Title")
        {
            layoutElement.minHeight = 90f;
            layoutElement.preferredWidth = 540f;
        }

        if (name == "Tagline")
        {
            layoutElement.minHeight = 28f;
            layoutElement.preferredWidth = 540f;
        }

        if (name == "Subtitle")
        {
            layoutElement.minHeight = 38f;
            layoutElement.preferredWidth = 540f;
        }

        if (name == "Hint")
        {
            layoutElement.minHeight = 26f;
            layoutElement.preferredWidth = 540f;
        }

        return uiText;
    }

    private static Button CreateButton(Transform parent, string name, string label)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(420f, 72f);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.24f, 1f);

        Button button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        UIRectangularButtonUtility.MakeRectangular(button);

        LayoutElement layoutElement = go.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = go.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = 420f;
        layoutElement.preferredHeight = 72f;
        layoutElement.minHeight = 72f;

        Text labelText = go.transform.Find("Label") != null
            ? go.transform.Find("Label").GetComponent<Text>()
            : null;
        if (labelText == null)
        {
            labelText = CreateText(go.transform, "Label", label, 30, FontStyle.Bold, TextAnchor.MiddleCenter);
        }
        else
        {
            labelText.text = label;
        }

        RectTransform textRect = labelText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private static GameObject CreateSliderRow(Transform parent, string label)
    {
        string rowName = label.Replace(" ", "") + "Row";
        Transform existing = parent.Find(rowName);
        GameObject row = existing != null ? existing.gameObject : new GameObject(rowName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 12f;

        CreateText(row.transform, label + "Label", label, 24, FontStyle.Normal, TextAnchor.MiddleLeft)
            .gameObject.GetComponent<LayoutElement>().preferredWidth = 180f;

        LayoutElement rowLayoutElement = row.GetComponent<LayoutElement>();
        if (rowLayoutElement == null)
        {
            rowLayoutElement = row.AddComponent<LayoutElement>();
        }

        rowLayoutElement.preferredWidth = 540f;
        rowLayoutElement.preferredHeight = 60f;
        rowLayoutElement.minHeight = 60f;

        GameObject sliderObject = row.transform.Find(label + "Slider") != null
            ? row.transform.Find(label + "Slider").gameObject
            : new GameObject(label + "Slider", typeof(RectTransform), typeof(Image), typeof(Slider));
        sliderObject.transform.SetParent(row.transform, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(0f, 36f);

        Image sliderBackground = sliderObject.GetComponent<Image>();
        sliderBackground.color = new Color(0.16f, 0.16f, 0.18f, 1f);

        GameObject fillArea = sliderObject.transform.Find("Fill Area") != null
            ? sliderObject.transform.Find("Fill Area").gameObject
            : new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(10f, 0f);
        fillAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject fill = fillArea.transform.Find("Fill") != null
            ? fillArea.transform.Find("Fill").gameObject
            : new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = new Color(0.85f, 0.75f, 0.35f, 1f);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.6f;
        slider.direction = Slider.Direction.LeftToRight;
        slider.transition = Selectable.Transition.ColorTint;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fillImage;

        return row;
    }

    private static Image CreateDivider(Transform parent, string name, Color color, float height, float preferredWidth)
    {
        GameObject dividerObject = parent.Find(name) != null ? parent.Find(name).gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
        dividerObject.transform.SetParent(parent, false);

        RectTransform rect = dividerObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(540f, height);

        Image image = dividerObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        LayoutElement layoutElement = dividerObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = dividerObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = preferredWidth;
        layoutElement.minHeight = height;
        return image;
    }

    private static Slider FindSlider(Transform root, string sliderName)
    {
        Transform child = FindDeepChild(root, sliderName);
        return child != null ? child.GetComponent<Slider>() : null;
    }

    private static Button FindButton(Transform root, string buttonName)
    {
        Transform child = FindDeepChild(root, buttonName);
        return child != null ? child.GetComponent<Button>() : null;
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

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
