using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
public class PauseMenuSceneRepairInstaller : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "SampleScene";

    private const string PauseMenuCanvasName = "PauseMenuCanvas";
    private const string PauseMenuRootName = "PauseMenuRoot";
    private const string MenuRootName = "MenuRoot";

    private readonly Color backdropColor = new Color(0.03f, 0.03f, 0.04f, 0.82f);
    private readonly Color panelColor = new Color(0.08f, 0.08f, 0.1f, 0.95f);
    private readonly Color buttonColor = new Color(0.2f, 0.2f, 0.24f, 1f);
    private readonly Color sliderBackgroundColor = new Color(0.16f, 0.16f, 0.18f, 1f);
    private readonly Color sliderFillColor = new Color(0.85f, 0.75f, 0.35f, 1f);

    private void OnEnable()
    {
        TryInstall();
    }

    private void OnValidate()
    {
        TryInstall();
    }

    private void TryInstall()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(targetSceneName) && SceneManager.GetActiveScene().name != targetSceneName)
        {
            return;
        }

        if (Object.FindFirstObjectByType<UI_PauseMenu>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        EnsureEventSystem();

        Canvas canvas = CreateCanvas(PauseMenuCanvasName);
        GameObject root = new GameObject(PauseMenuRootName, typeof(RectTransform), typeof(UI_PauseMenu));
        root.transform.SetParent(canvas.transform, false);
        StretchFull(root.GetComponent<RectTransform>());

        PauseMenuInputRelay inputRelay = canvas.gameObject.AddComponent<PauseMenuInputRelay>();
        inputRelay.Initialize(root.GetComponent<UI_PauseMenu>());

        Image backdrop = CreateImage(root.transform, "Backdrop", backdropColor);
        StretchFull(backdrop.rectTransform);

        GameObject menuRoot = CreateContainer(root.transform, MenuRootName);
        StretchFull(menuRoot.GetComponent<RectTransform>());

        GameObject mainPanel = CreatePanel(menuRoot.transform, "MainPanel");
        GameObject volumePanel = CreatePanel(menuRoot.transform, "VolumePanel");
        GameObject difficultyPanel = CreatePanel(menuRoot.transform, "DifficultyPanel");

        BuildMainPanel(mainPanel.transform);
        BuildVolumePanel(volumePanel.transform);
        BuildDifficultyPanel(difficultyPanel.transform);

        menuRoot.SetActive(false);
        mainPanel.SetActive(false);
        volumePanel.SetActive(false);
        difficultyPanel.SetActive(false);
    }

    private void BuildMainPanel(Transform parent)
    {
        ConfigureVerticalPanel(parent.GetComponent<VerticalLayoutGroup>(), parent.GetComponent<ContentSizeFitter>(), 18f);
        CreateText(parent, "Title", "Paused", 60, FontStyle.Bold, TextAnchor.MiddleCenter);
        CreateButton(parent, "ResumeButton", "Resume");
        CreateButton(parent, "MainMenuButton", "Main Menu");
        CreateButton(parent, "VolumeButton", "Volume");
        CreateButton(parent, "DifficultyButton", "Difficulty");
    }

    private void BuildVolumePanel(Transform parent)
    {
        ConfigureVerticalPanel(parent.GetComponent<VerticalLayoutGroup>(), parent.GetComponent<ContentSizeFitter>(), 18f);
        CreateText(parent, "Title", "Volume", 52, FontStyle.Bold, TextAnchor.MiddleCenter);
        CreateSliderRow(parent, "Master Volume");
        CreateSliderRow(parent, "BGM Volume");
        CreateSliderRow(parent, "SFX Volume");
        CreateButton(parent, "BackButton", "Back");
    }

    private void BuildDifficultyPanel(Transform parent)
    {
        ConfigureVerticalPanel(parent.GetComponent<VerticalLayoutGroup>(), parent.GetComponent<ContentSizeFitter>(), 18f);
        CreateText(parent, "Title", "Difficulty", 52, FontStyle.Bold, TextAnchor.MiddleCenter);

        GameObject row = CreateHorizontalRow(parent, "DifficultyChoices");
        HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childControlHeight = true;
        rowLayout.childControlWidth = true;
        rowLayout.spacing = 16f;

        CreateButton(row.transform, "EasyButton", "Easy");
        CreateButton(row.transform, "NormalButton", "Normal");
        CreateButton(parent, "BackButton", "Back");
    }

    private void ConfigureVerticalPanel(VerticalLayoutGroup layout, ContentSizeFitter fitter, float spacing)
    {
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = spacing;
        layout.padding = new RectOffset(36, 36, 42, 42);

        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private Canvas CreateCanvas(string name)
    {
        GameObject canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = name == "MainPanel" ? new Vector2(620f, 700f) : name == "VolumePanel" ? new Vector2(620f, 620f) : new Vector2(620f, 460f);
        rect.anchoredPosition = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        image.color = panelColor;
        return panel;
    }

    private GameObject CreateContainer(Transform parent, string name)
    {
        GameObject container = new GameObject(name, typeof(RectTransform));
        container.transform.SetParent(parent, false);
        return container;
    }

    private GameObject CreateHorizontalRow(Transform parent, string name)
    {
        GameObject container = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        container.transform.SetParent(parent, false);
        return container;
    }

    private Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void CreateText(Transform parent, string name, string text, int fontSize, FontStyle style, TextAnchor alignment)
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

        LayoutElement layoutElement = go.AddComponent<LayoutElement>();
        if (name == "Title")
        {
            layoutElement.minHeight = 90f;
        }
    }

    private Button CreateButton(Transform parent, string name, string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(420f, 72f);

        Image image = go.GetComponent<Image>();
        image.color = buttonColor;

        Button button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        UIRectangularButtonUtility.MakeRectangular(button);

        CreateText(go.transform, "Label", label, 30, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform textRect = go.transform.Find("Label").GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private void CreateSliderRow(Transform parent, string label)
    {
        GameObject row = new GameObject(label.Replace(" ", "") + "Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 12f;

        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.preferredWidth = 540f;
        rowLayout.preferredHeight = 60f;
        rowLayout.minHeight = 60f;

        GameObject labelObject = new GameObject(label + "Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        labelObject.transform.SetParent(row.transform, false);
        Text labelText = labelObject.GetComponent<Text>();
        labelText.text = label;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (labelText.font == null)
        {
            labelText.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        }
        labelText.fontSize = 24;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.color = Color.white;

        LayoutElement labelLayout = labelObject.GetComponent<LayoutElement>();
        labelLayout.preferredWidth = 180f;

        GameObject sliderGo = new GameObject(label + "Slider", typeof(RectTransform), typeof(Slider), typeof(Image));
        sliderGo.transform.SetParent(row.transform, false);
        RectTransform sliderRect = sliderGo.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(0f, 36f);

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

        Slider slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.6f;
        slider.direction = Slider.Direction.LeftToRight;
        slider.transition = Selectable.Transition.ColorTint;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fillImage;
    }

    private void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
