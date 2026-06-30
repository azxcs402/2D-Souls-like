using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuSceneGenerator
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string MainMenuSceneName = "MainMenu";
    private const string MainMenuRootName = "MainMenuRoot";
    private const string MainPanelName = "MainPanel";
    private const string OptionsPanelName = "OptionsPanel";
    private const string MainFrameName = "Frame";
    private const string MainContentName = "Content";
    private const string HeaderContainerName = "Header";
    private const string ActionsContainerName = "Actions";
    private const string FooterContainerName = "Footer";
    private const string SettingsContainerName = "Settings";

    [MenuItem("Tools/2D Souls-like/Generate Main Menu Scene")]
    public static void GenerateMainMenuScene()
    {
        EnsureSceneAsset();
        EnsureBuildSettings();
        Debug.Log("MainMenu scene generated.");
    }

    private static void EnsureSceneAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) != null)
        {
            return;
        }

        string scenesFolder = Path.GetDirectoryName(MainMenuScenePath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(scenesFolder) && !AssetDatabase.IsValidFolder(scenesFolder))
        {
            Directory.CreateDirectory(scenesFolder);
            AssetDatabase.Refresh();
        }

        Scene originalScene = SceneManager.GetActiveScene();
        Scene menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(menuScene);

        BuildMenuScene();

        EditorSceneManager.SaveScene(menuScene, MainMenuScenePath);
        EditorSceneManager.CloseScene(menuScene, true);

        if (originalScene.IsValid())
        {
            EditorSceneManager.SetActiveScene(originalScene);
        }

        AssetDatabase.Refresh();
    }

    private static void BuildMenuScene()
    {
        CreateCamera();
        CreateEventSystem();

        GameObject managers = new GameObject("ProjectManagers");
        GameManager gameManager = managers.AddComponent<GameManager>();
        SaveManager saveManager = managers.AddComponent<SaveManager>();

        SerializedObject gameManagerObject = new SerializedObject(gameManager);
        gameManagerObject.FindProperty("gameplaySceneName").stringValue = "SampleScene";
        gameManagerObject.FindProperty("mainMenuSceneName").stringValue = MainMenuSceneName;
        gameManagerObject.FindProperty("showMenuOnStart").boolValue = false;
        gameManagerObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject saveManagerObject = new SerializedObject(saveManager);
        saveManagerObject.FindProperty("fileName").stringValue = "savegame.json";
        saveManagerObject.FindProperty("encryptData").boolValue = true;
        saveManagerObject.ApplyModifiedPropertiesWithoutUndo();

        Canvas canvas = CreateCanvas();
        CreateFadeScreen(canvas.transform);

        GameObject menuRoot = new GameObject(MainMenuRootName, typeof(RectTransform), typeof(UI_MainMenu));
        menuRoot.transform.SetParent(canvas.transform, false);
        StretchFull(menuRoot.GetComponent<RectTransform>());

        GameObject backdrop = CreateOrGetPanel(menuRoot.transform, "Backdrop", new Color(0.02f, 0.02f, 0.03f, 0.9f));
        StretchFull(backdrop.GetComponent<RectTransform>());
        CreateBackdropAccent(menuRoot.transform, "BackdropAccentLeft", new Vector2(0f, 0.5f), new Vector2(18f, 760f), new Vector2(68f, 0f), new Color(0.85f, 0.74f, 0.35f, 0.06f));
        CreateBackdropAccent(menuRoot.transform, "BackdropAccentRight", new Vector2(1f, 0.5f), new Vector2(18f, 760f), new Vector2(-68f, 0f), new Color(0.85f, 0.74f, 0.35f, 0.035f));

        GameObject mainPanel = CreateOrGetPanel(menuRoot.transform, MainPanelName, new Color(0.06f, 0.06f, 0.08f, 0.98f));
        SetupPanelRect(mainPanel.GetComponent<RectTransform>(), new Vector2(720f, 780f));
        SetupPanelStack(mainPanel, 14f, new RectOffset(44, 52, 46, 40));
        CreatePanelFrame(mainPanel.transform, MainFrameName, new Color(0.12f, 0.12f, 0.14f, 0.96f));
        GameObject mainContent = CreateOrGetContainer(mainPanel.transform, MainContentName);
        SetupStack(mainContent, 18f);

        GameObject mainHeader = CreateOrGetContainer(mainContent.transform, HeaderContainerName);
        SetupStack(mainHeader, 10f);

        GameObject mainActions = CreateOrGetContainer(mainContent.transform, ActionsContainerName);
        SetupStack(mainActions, 14f);

        GameObject mainFooter = CreateOrGetContainer(mainContent.transform, FooterContainerName);
        SetupStack(mainFooter, 8f);

        CreateOrMoveText(mainHeader.transform, "Tagline", "ADVENTURE / ACTION RPG", 18, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.83f, 0.76f, 0.55f, 0.92f), 28f);
        CreateOrMoveDivider(mainHeader.transform, "TitleRuleTop", new Color(0.85f, 0.74f, 0.35f, 0.35f), 2f, 170f);
        CreateOrMoveText(mainHeader.transform, "Title", "2D Souls-like", 62, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.98f, 0.94f, 0.8f, 1f), 120f);
        CreateOrMoveText(mainHeader.transform, "Subtitle", "Main Menu", 22, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.9f, 0.9f, 0.92f, 0.78f), 38f);
        CreateOrMoveDivider(mainHeader.transform, "TitleRuleBottom", new Color(0.85f, 0.74f, 0.35f, 0.22f), 1f, 210f);

        UI_MainMenu mainMenu = menuRoot.GetComponent<UI_MainMenu>();
        mainMenu.Configure(mainPanel, null);

        Button newGameButton = CreateOrMoveButton(mainActions.transform, "NewGameButton", "New Game");
        Button continueButton = CreateOrMoveButton(mainActions.transform, "ContinueButton", "Continue");
        Button optionsButton = CreateOrMoveButton(mainActions.transform, "OptionsButton", "Options");
        Button quitButton = CreateOrMoveButton(mainActions.transform, "QuitButton", "Quit");
        DecorateMainButton(newGameButton, true);
        DecorateMainButton(continueButton, false);
        DecorateMainButton(optionsButton, false);
        DecorateMainButton(quitButton, false);

        CreateOrMoveText(mainFooter.transform, "Hint", "Enter the world, adjust settings, or exit cleanly.", 16, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.88f, 0.62f), 26f);

        GameObject optionsPanel = CreateOrGetPanel(menuRoot.transform, OptionsPanelName, new Color(0.06f, 0.06f, 0.08f, 0.98f));
        SetupPanelRect(optionsPanel.GetComponent<RectTransform>(), new Vector2(680f, 760f));
        SetupPanelStack(optionsPanel, 16f, new RectOffset(44, 48, 44, 40));
        CreatePanelFrame(optionsPanel.transform, MainFrameName, new Color(0.12f, 0.12f, 0.14f, 0.96f));
        GameObject optionsContent = CreateOrGetContainer(optionsPanel.transform, MainContentName);
        SetupStack(optionsContent, 16f);

        GameObject optionsHeader = CreateOrGetContainer(optionsContent.transform, HeaderContainerName);
        SetupStack(optionsHeader, 8f);

        GameObject optionsSettings = CreateOrGetContainer(optionsContent.transform, SettingsContainerName);
        SetupStack(optionsSettings, 18f);

        GameObject optionsFooter = CreateOrGetContainer(optionsContent.transform, FooterContainerName);
        SetupStack(optionsFooter, 8f);

        CreateOrMoveText(optionsHeader.transform, "OptionsTitle", "Options", 50, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.98f, 0.94f, 0.8f, 1f), 92f);
        CreateOrMoveDivider(optionsHeader.transform, "OptionsRule", new Color(0.85f, 0.74f, 0.35f, 0.25f), 1f, 180f);

        UI_Options options = optionsPanel.GetComponent<UI_Options>();
        if (options == null)
        {
            options = optionsPanel.AddComponent<UI_Options>();
        }

        CreateOrMoveSliderRow(optionsSettings.transform, "Master Volume", out Slider volumeSlider);
        CreateOrMoveSliderRow(optionsSettings.transform, "BGM Volume", out Slider bgmVolumeSlider);
        CreateOrMoveSliderRow(optionsSettings.transform, "SFX Volume", out Slider sfxVolumeSlider);
        Button resetButton = CreateOrMoveButton(optionsFooter.transform, "ResetVolumeButton", "Restore Defaults");
        Button backButton = CreateOrMoveButton(optionsFooter.transform, "BackButton", "Back");
        DecorateMainButton(backButton, false);

        options.Configure(mainPanel);
        options.Bind(volumeSlider, bgmVolumeSlider, sfxVolumeSlider);
        optionsPanel.SetActive(false);

        mainMenu.Configure(mainPanel, optionsPanel);
        SerializedObject mainMenuObject = new SerializedObject(mainMenu);
        mainMenuObject.FindProperty("continueButton").objectReferenceValue = continueButton;
        mainMenuObject.ApplyModifiedPropertiesWithoutUndo();

        BindMenuButtons(newGameButton, continueButton, optionsButton, quitButton, resetButton, backButton, options, volumeSlider, bgmVolumeSlider, sfxVolumeSlider);

        EditorUtility.SetDirty(gameManager);
        EditorUtility.SetDirty(saveManager);
        EditorUtility.SetDirty(mainMenu);
        EditorUtility.SetDirty(options);
        EditorUtility.SetDirty(menuRoot);
        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f);
        camera.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static UI_FadeScreen CreateFadeScreen(Transform parent)
    {
        GameObject fadeObject = new GameObject("FadeScreen", typeof(RectTransform), typeof(Image), typeof(UI_FadeScreen));
        fadeObject.transform.SetParent(parent, false);
        StretchFull(fadeObject.GetComponent<RectTransform>());

        Image fadeImage = fadeObject.GetComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;

        UI_FadeScreen fadeScreen = fadeObject.GetComponent<UI_FadeScreen>();
        SerializedObject fadeObjectSerialized = new SerializedObject(fadeScreen);
        fadeObjectSerialized.FindProperty("fadeImage").objectReferenceValue = fadeImage;
        fadeObjectSerialized.ApplyModifiedPropertiesWithoutUndo();
        return fadeScreen;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);

        Image image = panel.GetComponent<Image>();
        image.color = color;

        return panel;
    }

    private static GameObject CreateOrGetPanel(Transform parent, string name, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject panel = existing != null ? existing.gameObject : CreatePanel(parent, name, color);
        Image image = panel.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
        }

        return panel;
    }

    private static GameObject CreateOrGetContainer(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        GameObject container = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        container.transform.SetParent(parent, false);
        return container;
    }

    private static void SetupPanelRect(RectTransform rectTransform, Vector2 sizeDelta)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private static void SetupPanelStack(GameObject panel, float spacing, RectOffset padding)
    {
        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = panel.AddComponent<VerticalLayoutGroup>();
        }

        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = spacing;
        layout.padding = padding;

        ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = panel.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static void SetupStack(GameObject container, float spacing)
    {
        VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = container.AddComponent<VerticalLayoutGroup>();
        }

        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = spacing;

        ContentSizeFitter fitter = container.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = container.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static GameObject CreatePanelFrame(Transform parent, string name, Color color)
    {
        GameObject frame = CreateOrGetPanel(parent, name, color);
        RectTransform rect = frame.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(10f, 10f);
        rect.offsetMax = new Vector2(-10f, -10f);

        Outline outline = frame.GetComponent<Outline>();
        if (outline == null)
        {
            outline = frame.AddComponent<Outline>();
        }

        outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
        outline.effectDistance = new Vector2(2f, -2f);
        return frame;
    }

    private static GameObject CreateBackdropAccent(Transform parent, string name, Vector2 anchor, Vector2 sizeDelta, Vector2 anchoredPosition, Color color)
    {
        GameObject accent = CreateOrGetPanel(parent, name, color);
        RectTransform rect = accent.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;
        return accent;
    }

    private static Text CreateOrMoveText(Transform parent, string name, string value, int size, FontStyle style, TextAnchor alignment, Color color, float minHeight)
    {
        Transform existing = FindDeepChild(parent.root, name);
        Text text = existing != null ? existing.GetComponent<Text>() : null;
        if (text == null)
        {
            text = CreateText(parent, name, value, size, style, alignment, color);
        }
        else
        {
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
            {
                text.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.transform.SetParent(parent, false);
        }

        LayoutElement layoutElement = text.gameObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = text.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minHeight = minHeight;

        Shadow shadow = text.gameObject.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = text.gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    private static Image CreateOrMoveDivider(Transform parent, string name, Color color, float height, float preferredWidth)
    {
        Transform existing = FindDeepChild(parent.root, name);
        GameObject dividerObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
        dividerObject.transform.SetParent(parent, false);

        RectTransform rect = dividerObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, height);

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

    private static Button CreateOrMoveButton(Transform parent, string name, string label)
    {
        Transform existing = FindDeepChild(parent.root, name);
        GameObject buttonObject = existing != null ? existing.gameObject : null;
        if (buttonObject == null)
        {
            return CreateButton(parent, name, label);
        }

        buttonObject.transform.SetParent(parent, false);
        Text labelText = buttonObject.transform.Find("Label")?.GetComponent<Text>();
        if (labelText != null)
        {
            labelText.text = label;
        }

        DecorateMainButton(buttonObject.GetComponent<Button>(), label == "New Game");
        return buttonObject.GetComponent<Button>();
    }

    private static GameObject CreateOrMoveSliderRow(Transform parent, string label, out Slider slider)
    {
        string rowName = label.Replace(" ", "") + "Row";
        Transform existing = FindDeepChild(parent.root, rowName);
        if (existing == null)
        {
            return CreateSliderRow(parent, label, out slider);
        }

        existing.SetParent(parent, false);
        slider = existing.Find(label + "Slider")?.GetComponent<Slider>();
        if (slider == null)
        {
            GameObject createdRow = CreateSliderRow(parent, label, out slider);
            return createdRow;
        }

        return existing.gameObject;
    }

    private static void DecorateMainButton(Button button, bool isPrimary)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = isPrimary
                ? new Color(0.22f, 0.22f, 0.26f, 1f)
                : new Color(0.15f, 0.15f, 0.18f, 1f);
        }

        ColorBlock colors = button.colors;
        colors.normalColor = image != null ? image.color : colors.normalColor;
        colors.highlightedColor = isPrimary
            ? new Color(0.3f, 0.26f, 0.16f, 1f)
            : new Color(0.24f, 0.24f, 0.29f, 1f);
        colors.selectedColor = isPrimary
            ? new Color(0.34f, 0.3f, 0.2f, 1f)
            : new Color(0.27f, 0.24f, 0.18f, 1f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.12f, 1f);
        button.colors = colors;

        Outline outline = button.GetComponent<Outline>();
        if (outline == null)
        {
            outline = button.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
        outline.effectDistance = new Vector2(2f, -2f);

        Text labelText = button.GetComponentInChildren<Text>();
        if (labelText != null)
        {
            Shadow shadow = labelText.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = labelText.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(1f, -1f);
        }
    }

    private static void MoveIfExists(Transform searchRoot, string objectName, Transform newParent)
    {
        Transform existing = FindDeepChild(searchRoot, objectName);
        if (existing != null)
        {
            existing.SetParent(newParent, false);
        }
    }

    private static void BindMenuButtons(
        Button newGameButton,
        Button continueButton,
        Button optionsButton,
        Button quitButton,
        Button resetButton,
        Button backButton,
        UI_Options options,
        Slider volumeSlider,
        Slider bgmVolumeSlider,
        Slider sfxVolumeSlider)
    {
        UI_MainMenu mainMenu = newGameButton != null ? newGameButton.GetComponentInParent<UI_MainMenu>() : null;
        if (mainMenu == null && quitButton != null)
        {
            mainMenu = quitButton.GetComponentInParent<UI_MainMenu>();
        }

        if (mainMenu != null)
        {
            if (newGameButton != null)
            {
                ClearPersistentListeners(newGameButton.onClick);
                UnityEventTools.AddPersistentListener(newGameButton.onClick, mainMenu.NewGameBTN);
            }

            if (continueButton != null)
            {
                ClearPersistentListeners(continueButton.onClick);
                UnityEventTools.AddPersistentListener(continueButton.onClick, mainMenu.ContinueBTN);
            }

            if (optionsButton != null)
            {
                ClearPersistentListeners(optionsButton.onClick);
                UnityEventTools.AddPersistentListener(optionsButton.onClick, mainMenu.OptionsBTN);
            }

            if (quitButton != null)
            {
                ClearPersistentListeners(quitButton.onClick);
                UnityEventTools.AddPersistentListener(quitButton.onClick, mainMenu.QuitGameBTN);
            }
        }

        if (backButton != null && options != null)
        {
            ClearPersistentListeners(backButton.onClick);
            UnityEventTools.AddPersistentListener(backButton.onClick, options.CloseBTN);
        }

        if (resetButton != null && options != null)
        {
            ClearPersistentListeners(resetButton.onClick);
            UnityEventTools.AddPersistentListener(resetButton.onClick, options.ResetVolumeDefaults);
        }

        if (volumeSlider != null && options != null)
        {
            ClearPersistentListeners(volumeSlider.onValueChanged);
            UnityEventTools.AddPersistentListener(volumeSlider.onValueChanged, options.MasterVolumeValue);
        }

        if (bgmVolumeSlider != null && options != null)
        {
            ClearPersistentListeners(bgmVolumeSlider.onValueChanged);
            UnityEventTools.AddPersistentListener(bgmVolumeSlider.onValueChanged, options.BgmVolumeValue);
        }

        if (sfxVolumeSlider != null && options != null)
        {
            ClearPersistentListeners(sfxVolumeSlider.onValueChanged);
            UnityEventTools.AddPersistentListener(sfxVolumeSlider.onValueChanged, options.SfxVolumeValue);
        }
    }

    private static void ClearPersistentListeners(UnityEngine.Events.UnityEventBase unityEvent)
    {
        if (unityEvent == null)
        {
            return;
        }

        for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(unityEvent, i);
        }
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

    private static Text CreateText(Transform parent, string name, string value, int size, FontStyle style, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
        {
            text.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        }
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(0f, 0f);
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(0f, 72f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.22f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.18f, 0.18f, 0.22f, 1f);
        colors.highlightedColor = new Color(0.26f, 0.26f, 0.32f, 1f);
        colors.selectedColor = new Color(0.32f, 0.28f, 0.18f, 1f);
        colors.pressedColor = new Color(0.12f, 0.12f, 0.15f, 1f);
        button.colors = colors;

        Text labelText = CreateText(buttonObject.transform, "Label", label, 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        DecorateMainButton(button, label == "New Game");
        return button;
    }

    private static GameObject CreateSliderRow(Transform parent, string label, out Slider slider)
    {
        GameObject row = new GameObject(label.Replace(" ", "") + "Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 12f;

        CreateText(row.transform, label + "Label", label, 24, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white)
            .gameObject.AddComponent<LayoutElement>().preferredWidth = 180f;

        GameObject sliderObject = new GameObject(label + "Slider", typeof(RectTransform), typeof(Image), typeof(Slider));
        sliderObject.transform.SetParent(row.transform, false);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(0f, 36f);

        Image sliderBackground = sliderObject.GetComponent<Image>();
        sliderBackground.color = new Color(0.16f, 0.16f, 0.18f, 1f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(10f, 0f);
        fillAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = new Color(0.85f, 0.75f, 0.35f, 1f);

        slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.6f;
        slider.direction = Slider.Direction.LeftToRight;
        slider.transition = Selectable.Transition.ColorTint;
        slider.targetGraphic = fillImage;
        slider.fillRect = fill.GetComponent<RectTransform>();

        return row;
    }

    private static GameObject CreateToggleRow(Transform parent, string label, out Toggle toggle)
    {
        GameObject row = new GameObject(label.Replace(" ", "") + "Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.spacing = 12f;

        CreateText(row.transform, label + "Label", label, 24, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white)
            .gameObject.AddComponent<LayoutElement>().preferredWidth = 180f;

        GameObject toggleObject = new GameObject(label + "Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
        toggleObject.transform.SetParent(row.transform, false);
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(36f, 36f);

        Image background = toggleObject.GetComponent<Image>();
        background.color = new Color(0.16f, 0.16f, 0.18f, 1f);

        GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkmark.transform.SetParent(toggleObject.transform, false);
        Image checkmarkImage = checkmark.GetComponent<Image>();
        checkmarkImage.color = new Color(0.85f, 0.75f, 0.35f, 1f);
        RectTransform checkRect = checkmark.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.25f, 0.25f);
        checkRect.anchorMax = new Vector2(0.75f, 0.75f);
        checkRect.offsetMin = Vector2.zero;
        checkRect.offsetMax = Vector2.zero;

        toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = checkmarkImage;
        toggle.isOn = true;

        return row;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureBuildSettings()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) == null)
        {
            return;
        }

        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(scene => scene.path == MainMenuScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(MainMenuScenePath, true));

        string sampleScenePath = "Assets/Scenes/SampleScene.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(sampleScenePath) != null && scenes.FindIndex(scene => scene.path == sampleScenePath) < 0)
        {
            scenes.Add(new EditorBuildSettingsScene(sampleScenePath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
