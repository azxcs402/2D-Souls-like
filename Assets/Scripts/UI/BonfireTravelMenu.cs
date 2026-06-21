using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BonfireTravelMenu : MonoBehaviour
{
    private sealed class MenuRow
    {
        public GameObject root;
        public Button button;
        public Image background;
        public Text cursor;
        public Text label;
    }

    private sealed class DestinationRow
    {
        public GameData.BonfireRecord record;
        public MenuRow row;
    }

    private static BonfireTravelMenu instance;

    public static bool IsOpen => instance != null && instance.isOpen;

    public static void Open(Bonfire sourceBonfire)
    {
        if (sourceBonfire == null)
        {
            return;
        }

        EnsureInstance();
        instance.Show(sourceBonfire);
    }

    private Canvas canvas;
    private GameObject root;
    private GameObject travelPage;
    private Image backdropImage;
    private Image travelPanelImage;
    private Image travelListImage;
    private RectTransform travelPanelRect;
    private RectTransform travelListRect;
    private RectTransform closeButtonRect;
    private RectTransform pageTitleRect;
    private RectTransform pageHintRect;
    private Button closeButton;
    private Image closeButtonImage;
    private Image closeButtonIconImage;
    private Text closeButtonText;
    private RectTransform travelListRoot;
    private Text travelEmptyText;
    private Text pageTitleText;
    private Text pageHintText;
    private VerticalLayoutGroup travelListLayoutGroup;
    private readonly List<DestinationRow> destinationRows = new();
    private readonly List<GameData.BonfireRecord> destinationRecords = new();
    private Bonfire sourceBonfire;
    private int selectedIndex = -1;
    private bool isOpen;
    private int openedFrame = -1;

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject(nameof(BonfireTravelMenu));
        instance = go.AddComponent<BonfireTravelMenu>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUi();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        if (Time.frameCount <= openedFrame)
        {
            return;
        }

        HandleKeyboardInput();
    }

    private void Show(Bonfire bonfire)
    {
        EnsureUi();
        sourceBonfire = bonfire;
        openedFrame = Time.frameCount;
        AudioManager.instance?.PlayGlobalSFX(AudioKey.BonfireMenuOpen);
        ShowTravelPage();
        ApplyMenuTheme();
        SetVisible(true);

        if (GameManager.instance != null)
        {
            GameManager.instance.SetGameplayControlsEnabled(false);
        }

        if (sourceBonfire != null)
        {
            sourceBonfire.RefreshInteractionPrompt();
            sourceBonfire.PlayTravelMenuClosedSfx();
        }
    }

    private void CloseMenu(bool restoreGameplayControls)
    {
        if (!isOpen)
        {
            return;
        }

        SetVisible(false);

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }

        if (restoreGameplayControls && GameManager.instance != null)
        {
            GameManager.instance.SetGameplayControlsEnabled(true);
        }

        if (sourceBonfire != null)
        {
            sourceBonfire.RefreshInteractionPrompt();
        }

        sourceBonfire = null;
        destinationRecords.Clear();
        ClearDestinationRows();
        openedFrame = -1;
        selectedIndex = -1;
    }

    private void TriggerRefresh()
    {
        Bonfire bonfire = sourceBonfire;
        if (bonfire == null)
        {
            CloseMenu(true);
            return;
        }

        CloseMenu(true);
        bonfire.PlayTravelConfirmSfx();
        bonfire.StartRestSequence();
    }

    private void TriggerTeleport(GameData.BonfireRecord targetRecord)
    {
        if (targetRecord == null || string.IsNullOrWhiteSpace(targetRecord.sceneName))
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(targetRecord.sceneName))
        {
            return;
        }

        CloseMenu(true);
        AudioManager.instance?.PlayGlobalSFX(AudioKey.BonfireTravel);

        if (SaveManager.instance != null)
        {
            SaveManager.instance.SaveCheckpoint(targetRecord.worldPosition, targetRecord.sceneName);
        }

        Player player = FindFirstObjectByType<Player>();
        string currentScene = SceneManager.GetActiveScene().name;
        bool sameScene = string.Equals(currentScene, targetRecord.sceneName, StringComparison.Ordinal);

        if (sameScene && player != null)
        {
            player.transform.position = targetRecord.worldPosition;
            if (player.rb != null)
            {
                player.rb.velocity = Vector2.zero;
                player.rb.angularVelocity = 0f;
            }

            Physics2D.SyncTransforms();

            if (GameManager.instance != null)
            {
                GameManager.instance.SetGameplayControlsEnabled(true);
            }
            else
            {
                player.enabled = true;
                if (player.rb != null)
                {
                    player.rb.simulated = true;
                }
            }

            return;
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.ChangeScene(targetRecord.sceneName, false);
        }
        else
        {
            SceneManager.LoadScene(targetRecord.sceneName);
        }
    }

    private void HandleKeyboardInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            AudioManager.instance?.PlayGlobalSFX(AudioKey.BonfireMenuClose);
            CloseMenu(true);
            return;
        }

        bool moveUp = keyboard.wKey.wasPressedThisFrame;
        bool moveDown = keyboard.sKey.wasPressedThisFrame;
        if (moveUp ^ moveDown)
        {
            int direction = moveUp ? -1 : 1;
            MoveSelection(direction, destinationRecords.Count);
        }

        if (keyboard.fKey.wasPressedThisFrame)
        {
            ExecuteSelected();
        }
    }

    private void ExecuteSelected()
    {
        if (!isOpen)
        {
            return;
        }

        if (selectedIndex < 0 || selectedIndex >= destinationRecords.Count)
        {
            return;
        }

        TriggerTeleport(destinationRecords[selectedIndex]);
    }

    private void MoveSelection(int direction, int itemCount)
    {
        if (itemCount <= 0)
        {
            selectedIndex = -1;
            ApplySelectionVisuals();
            return;
        }

        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }
        else
        {
            selectedIndex = (selectedIndex + direction) % itemCount;
            if (selectedIndex < 0)
            {
                selectedIndex += itemCount;
            }
        }

        ApplySelectionVisuals();
    }

    private void SelectIndex(int index)
    {
        selectedIndex = index;
        ApplySelectionVisuals();
    }

    private void ApplySelectionVisuals()
    {
        if (!isOpen)
        {
            return;
        }

        for (int i = 0; i < destinationRows.Count; i++)
        {
            SetRowSelected(destinationRows[i].row, i == selectedIndex);
        }

        if (selectedIndex >= 0 && selectedIndex < destinationRows.Count)
        {
            SelectUiObject(destinationRows[selectedIndex].row.button);
        }
    }

    private static void SelectUiObject(Selectable selectable)
    {
        if (selectable == null)
        {
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.currentSelectedGameObject != selectable.gameObject)
        {
            eventSystem.SetSelectedGameObject(selectable.gameObject);
        }
    }
    private void ShowTravelPage()
    {
        travelPage.SetActive(true);
        pageTitleText.text = GetTravelMenuTitle();
        pageHintText.text = GetTravelMenuHint();

        RebuildDestinationRows();
        if (destinationRecords.Count == 0)
        {
            selectedIndex = -1;
            if (travelEmptyText != null)
            {
                travelEmptyText.gameObject.SetActive(true);
            }
        }
        else
        {
            if (travelEmptyText != null)
            {
                travelEmptyText.gameObject.SetActive(false);
            }

            SelectIndex(0);
        }
    }

    private void ApplyMenuTheme()
    {
        if (sourceBonfire == null)
        {
            return;
        }

        if (backdropImage != null)
        {
            backdropImage.color = sourceBonfire.TravelMenuBackdropColor;
        }

        if (travelPanelImage != null)
        {
            travelPanelImage.color = sourceBonfire.TravelMenuPanelColor;
        }

        if (travelPanelRect != null)
        {
            travelPanelRect.sizeDelta = sourceBonfire.TravelMenuRootSize;
        }

        if (travelListImage != null)
        {
            travelListImage.color = sourceBonfire.TravelMenuListColor;
        }

        if (travelListRect != null)
        {
            travelListRect.sizeDelta = sourceBonfire.TravelMenuListSize;
        }

        if (closeButtonImage != null)
        {
            closeButtonImage.color = sourceBonfire.TravelMenuCloseButtonColor;
            closeButtonImage.sprite = null;
            closeButtonImage.type = Image.Type.Simple;
        }

        if (closeButtonIconImage != null)
        {
            closeButtonIconImage.sprite = sourceBonfire.TravelMenuCloseIcon;
            closeButtonIconImage.enabled = sourceBonfire.TravelMenuCloseIcon != null;
        }

        if (closeButtonText != null)
        {
            closeButtonText.gameObject.SetActive(sourceBonfire.TravelMenuCloseIcon == null);
            closeButtonText.text = string.IsNullOrWhiteSpace(sourceBonfire.TravelMenuCloseLabel) ? "X" : sourceBonfire.TravelMenuCloseLabel;
            closeButtonText.fontSize = sourceBonfire.TravelMenuCloseFontSize;
            closeButtonText.font = sourceBonfire.TravelMenuFont != null ? sourceBonfire.TravelMenuFont : closeButtonText.font;
        }

        if (pageTitleText != null)
        {
            pageTitleText.text = GetTravelMenuTitle();
            pageTitleText.fontSize = sourceBonfire.TravelMenuTitleFontSize;
            pageTitleText.color = sourceBonfire.TravelMenuTitleColor;
            pageTitleText.font = sourceBonfire.TravelMenuFont != null ? sourceBonfire.TravelMenuFont : pageTitleText.font;
        }

        if (pageHintText != null)
        {
            pageHintText.text = GetTravelMenuHint();
            pageHintText.fontSize = sourceBonfire.TravelMenuHintFontSize;
            pageHintText.color = sourceBonfire.TravelMenuHintColor;
            pageHintText.font = sourceBonfire.TravelMenuFont != null ? sourceBonfire.TravelMenuFont : pageHintText.font;
        }

        if (pageTitleRect != null)
        {
            pageTitleRect.sizeDelta = new Vector2(0f, Mathf.Max(72f, sourceBonfire.TravelMenuTitleFontSize * 1.4f));
        }

        if (pageHintRect != null)
        {
            pageHintRect.sizeDelta = new Vector2(0f, Mathf.Max(42f, sourceBonfire.TravelMenuHintFontSize * 1.5f));
        }

        if (travelListLayoutGroup != null)
        {
            travelListLayoutGroup.spacing = sourceBonfire.TravelMenuItemSpacing;
        }

        if (closeButtonRect != null)
        {
            closeButtonRect.sizeDelta = sourceBonfire.TravelMenuCloseButtonSize;
            closeButtonRect.anchoredPosition = sourceBonfire.TravelMenuCloseButtonOffset;
        }

        ApplySelectionVisuals();
    }
    private void RefreshDestinationRecords()
    {
        destinationRecords.Clear();
        HashSet<string> seenKeys = new();
        string currentKey = sourceBonfire != null ? sourceBonfire.TravelKey : string.Empty;

        Bonfire[] liveBonfires = FindObjectsByType<Bonfire>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < liveBonfires.Length; i++)
        {
            Bonfire bonfire = liveBonfires[i];
            if (bonfire == null || !bonfire.IsActivated)
            {
                continue;
            }

            GameData.BonfireRecord record = bonfire.BuildTravelRecord();
            if (record == null)
            {
                continue;
            }

            string key = record.GetKey();
            if (key == currentKey || !seenKeys.Add(key))
            {
                continue;
            }

            destinationRecords.Add(record);
        }

        GameData gameData = SaveManager.instance != null ? SaveManager.instance.GetGameData() : null;
        if (gameData != null && gameData.litBonfireRecords != null)
        {
            for (int i = 0; i < gameData.litBonfireRecords.Count; i++)
            {
                GameData.BonfireRecord record = gameData.litBonfireRecords[i];
                if (record == null
                    || string.IsNullOrWhiteSpace(record.sceneName)
                    || string.IsNullOrWhiteSpace(record.bonfireId))
                {
                    continue;
                }

                string key = record.GetKey();
                if (key == currentKey || !seenKeys.Add(key))
                {
                    continue;
                }

                destinationRecords.Add(CloneRecord(record));
            }
        }

        destinationRecords.Sort(CompareRecords);
    }

    private static int CompareRecords(GameData.BonfireRecord a, GameData.BonfireRecord b)
    {
        if (a == null && b == null)
        {
            return 0;
        }

        if (a == null)
        {
            return 1;
        }

        if (b == null)
        {
            return -1;
        }

        int sceneCompare = string.Compare(a.sceneName, b.sceneName, StringComparison.OrdinalIgnoreCase);
        if (sceneCompare != 0)
        {
            return sceneCompare;
        }

        int nameCompare = string.Compare(ResolveRecordName(a), ResolveRecordName(b), StringComparison.OrdinalIgnoreCase);
        if (nameCompare != 0)
        {
            return nameCompare;
        }

        int idCompare = string.Compare(a.bonfireId, b.bonfireId, StringComparison.OrdinalIgnoreCase);
        if (idCompare != 0)
        {
            return idCompare;
        }

        return a.worldPosition.y.CompareTo(b.worldPosition.y);
    }

    private void RebuildDestinationRows()
    {
        ClearDestinationRows();
        RefreshDestinationRecords();

        if (destinationRecords.Count == 0)
        {
            travelEmptyText = CreateText(travelListRoot, "Empty", GetTravelMenuEmptyText(), 30, FontStyle.Bold, TextAnchor.MiddleCenter, sourceBonfire != null ? sourceBonfire.TravelMenuFont : null);
            RectTransform emptyRect = travelEmptyText.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0f, 0.5f);
            emptyRect.anchorMax = new Vector2(1f, 0.5f);
            emptyRect.sizeDelta = new Vector2(0f, 72f);
            travelEmptyText.color = sourceBonfire != null ? sourceBonfire.TravelMenuEmptyColor : new Color(1f, 1f, 1f, 0.88f);
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        for (int i = 0; i < destinationRecords.Count; i++)
        {
            int index = i;
            GameData.BonfireRecord record = destinationRecords[i];
            string label = ResolveLabel(record, index, currentScene);
            DestinationRow destinationRow = new DestinationRow
            {
                record = record,
                row = CreateOptionRow(travelListRoot, $"Destination_{index + 1}", label, () => SelectIndex(index))
            };

            destinationRows.Add(destinationRow);
            SetRowSelected(destinationRow.row, index == selectedIndex);
        }
    }

    private void ClearDestinationRows()
    {
        for (int i = 0; i < destinationRows.Count; i++)
        {
            if (destinationRows[i]?.row?.root != null)
            {
                Destroy(destinationRows[i].row.root);
            }
        }

        destinationRows.Clear();

        if (travelEmptyText != null)
        {
            Destroy(travelEmptyText.gameObject);
            travelEmptyText = null;
        }
    }

    private void BuildUi()
    {
        CreateEventSystemIfNeeded();

        canvas = CreateCanvas("BonfireTravelCanvas");
        DontDestroyOnLoad(canvas.gameObject);
        root = new GameObject("BonfireTravelRoot", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        backdropImage = CreateImage(root.transform, "Backdrop", new Color(0.18f, 0.18f, 0.2f, 0.72f));
        StretchFull(backdropImage.rectTransform);

        closeButton = CreateCloseButton(root.transform);
        closeButtonRect = closeButton.GetComponent<RectTransform>();
        closeButtonRect.anchorMin = new Vector2(1f, 1f);
        closeButtonRect.anchorMax = new Vector2(1f, 1f);
        closeButtonRect.pivot = new Vector2(1f, 1f);
        closeButtonRect.sizeDelta = new Vector2(56f, 56f);
        closeButtonRect.anchoredPosition = new Vector2(-28f, -28f);

        travelPage = CreatePage(root.transform, "TravelPage");

        BuildTravelPage(travelPage.transform);
    }

    private void EnsureUi()
    {
        if (canvas != null && root != null && travelPage != null)
        {
            return;
        }

        BuildUi();
        SetVisible(false);
    }
    private void BuildTravelPage(Transform parent)
    {
        GameObject panel = CreatePanel(parent, "TravelPanel", new Vector2(0.5f, 0.5f), new Vector2(900f, 840f));
        travelPanelImage = panel.GetComponent<Image>();
        travelPanelRect = panel.GetComponent<RectTransform>();
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 16f;
        layout.padding = new RectOffset(36, 36, 32, 32);

        ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;


        pageTitleText = CreateText(panel.transform, "Title", "\u4f20\u9001\u5730\u70b9", 54, FontStyle.Bold, TextAnchor.MiddleCenter);
        pageTitleRect = pageTitleText.GetComponent<RectTransform>();
        pageTitleText.gameObject.AddComponent<LayoutElement>().minHeight = 72f;
        pageHintText = CreateText(panel.transform, "Hint", "W/S \u9009\u62e9  F \u4f20\u9001  ESC \u5173\u95ed  \u9f20\u6807\u70b9\u51fb\u53ef\u9009\u4e2d", 24, FontStyle.Normal, TextAnchor.MiddleCenter);
        pageHintRect = pageHintText.GetComponent<RectTransform>();
        pageHintText.color = new Color(1f, 1f, 1f, 0.84f);
        pageHintText.gameObject.AddComponent<LayoutElement>().minHeight = 42f;
        GameObject listPanel = CreatePanel(panel.transform, "ListPanel", new Vector2(0.5f, 0.5f), new Vector2(780f, 620f));
        travelListImage = listPanel.GetComponent<Image>();
        travelListRect = listPanel.GetComponent<RectTransform>();
        travelListImage.color = new Color(0.09f, 0.09f, 0.11f, 0.98f);
        LayoutElement listLayout = listPanel.AddComponent<LayoutElement>();
        listLayout.minHeight = 620f;
        listLayout.minWidth = 780f;

        travelListLayoutGroup = listPanel.AddComponent<VerticalLayoutGroup>();
        travelListLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        travelListLayoutGroup.childForceExpandHeight = false;
        travelListLayoutGroup.childForceExpandWidth = true;
        travelListLayoutGroup.childControlHeight = true;
        travelListLayoutGroup.childControlWidth = true;
        travelListLayoutGroup.spacing = 10f;
        travelListLayoutGroup.padding = new RectOffset(22, 22, 22, 22);

        ContentSizeFitter listFitter = listPanel.AddComponent<ContentSizeFitter>();
        listFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        travelListRoot = travelListRect;
    }

    private MenuRow CreateOptionRow(Transform parent, string name, string label, Action onClick)
    {
        float itemHeight = sourceBonfire != null ? sourceBonfire.TravelMenuItemHeight : 72f;
        int itemFontSize = sourceBonfire != null ? sourceBonfire.TravelMenuItemFontSize : 30;
        string cursorSymbol = GetTravelMenuCursorSymbol();

        GameObject rootObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        rootObject.transform.SetParent(parent, false);

        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(0f, itemHeight);

        LayoutElement layout = rootObject.AddComponent<LayoutElement>();
        layout.minHeight = itemHeight;
        layout.flexibleWidth = 1f;

        Image background = rootObject.GetComponent<Image>();
        background.color = sourceBonfire != null ? sourceBonfire.TravelMenuRowColor : new Color(0.19f, 0.19f, 0.24f, 1f);

        Button button = rootObject.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        if (onClick != null)
        {
            button.onClick.AddListener(() => onClick());
        }

        HorizontalLayoutGroup rowLayout = rootObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childForceExpandHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childControlHeight = true;
        rowLayout.childControlWidth = true;
        rowLayout.spacing = 18f;
        rowLayout.padding = new RectOffset(22, 22, 10, 10);

        Text cursor = CreateText(rootObject.transform, "Cursor", cursorSymbol, itemFontSize, FontStyle.Bold, TextAnchor.MiddleCenter, sourceBonfire != null ? sourceBonfire.TravelMenuFont : null);
        cursor.color = sourceBonfire != null ? sourceBonfire.TravelMenuCursorColor : new Color(1f, 0.92f, 0.48f, 1f);
        cursor.GetComponent<RectTransform>().sizeDelta = new Vector2(itemFontSize, 0f);

        Text labelText = CreateText(rootObject.transform, "Label", label, itemFontSize, FontStyle.Bold, TextAnchor.MiddleLeft, sourceBonfire != null ? sourceBonfire.TravelMenuFont : null);
        labelText.color = sourceBonfire != null ? sourceBonfire.TravelMenuLabelUnselectedColor : new Color(1f, 1f, 1f, 0.82f);
        labelText.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        return new MenuRow
        {
            root = rootObject,
            button = button,
            background = background,
            cursor = cursor,
            label = labelText
        };
    }

    private Button CreateCloseButton(Transform parent)
    {
        GameObject go = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        closeButtonRect = go.GetComponent<RectTransform>();
        closeButtonImage = go.GetComponent<Image>();
        closeButtonImage.color = new Color(0.25f, 0.25f, 0.3f, 0.95f);

        Button button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() =>
        {
            if (instance != null)
            {
                instance.CloseMenu(true);
            }
        });

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(go.transform, false);
        closeButtonIconImage = iconObject.GetComponent<Image>();
        RectTransform iconRect = closeButtonIconImage.GetComponent<RectTransform>();
        StretchFull(iconRect);
        iconRect.offsetMin = new Vector2(10f, 10f);
        iconRect.offsetMax = new Vector2(-10f, -10f);
        closeButtonIconImage.enabled = false;

        closeButtonText = CreateText(go.transform, "Label", "X", 28, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform labelRect = closeButtonText.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }

    private static GameObject CreatePage(Transform parent, string name)
    {
        GameObject page = new GameObject(name, typeof(RectTransform));
        page.transform.SetParent(parent, false);

        RectTransform rect = page.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return page;
    }

    private static Canvas CreateCanvas(string name)
    {
        GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 7000;

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
        DontDestroyOnLoad(eventSystem);
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

        panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.96f);
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

    private static Text CreateText(Transform parent, string name, string text, int fontSize, FontStyle style, TextAnchor alignment, Font font = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        Text uiText = go.GetComponent<Text>();
        uiText.text = text;
        uiText.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

        return uiText;
    }

    private void SetVisible(bool visible)
    {
        isOpen = visible;
        if (root != null)
        {
            root.SetActive(visible);
        }
    }

    private void SetRowSelected(MenuRow row, bool selected)
    {
        if (row == null)
        {
            return;
        }

        row.background.color = selected
            ? (sourceBonfire != null ? sourceBonfire.TravelMenuRowSelectedColor : new Color(0.42f, 0.42f, 0.16f, 1f))
            : (sourceBonfire != null ? sourceBonfire.TravelMenuRowColor : new Color(0.19f, 0.19f, 0.24f, 1f));

        if (row.cursor != null)
        {
            Color cursorColor = row.cursor.color;
            cursorColor.a = selected ? 1f : 0f;
            row.cursor.color = cursorColor;
        }

        if (row.label != null)
        {
            row.label.color = selected
                ? (sourceBonfire != null ? sourceBonfire.TravelMenuLabelSelectedColor : Color.white)
                : (sourceBonfire != null ? sourceBonfire.TravelMenuLabelUnselectedColor : new Color(1f, 1f, 1f, 0.82f));
        }
    }

    private string GetTravelMenuTitle()
    {
        if (sourceBonfire != null && !string.IsNullOrWhiteSpace(sourceBonfire.TravelMenuTitle))
        {
            return sourceBonfire.TravelMenuTitle;
        }

        return "\u4f20\u9001\u5730\u70b9";
    }

    private string GetTravelMenuHint()
    {
        if (sourceBonfire != null && !string.IsNullOrWhiteSpace(sourceBonfire.TravelMenuHint))
        {
            return sourceBonfire.TravelMenuHint;
        }

        return "W/S \u9009\u62e9  F \u4f20\u9001  ESC \u5173\u95ed  \u9f20\u6807\u70b9\u51fb\u53ef\u9009\u4e2d";
    }

    private string GetTravelMenuEmptyText()
    {
        if (sourceBonfire != null && !string.IsNullOrWhiteSpace(sourceBonfire.TravelMenuEmptyText))
        {
            return sourceBonfire.TravelMenuEmptyText;
        }

        return "\u6ca1\u6709\u53ef\u4f20\u9001\u7684\u7bdd\u706b";
    }

    private string GetTravelMenuCursorSymbol()
    {
        if (sourceBonfire != null && !string.IsNullOrWhiteSpace(sourceBonfire.TravelMenuCursorSymbol))
        {
            return sourceBonfire.TravelMenuCursorSymbol;
        }

        return ">";
    }

    private static string ResolveRecordName(GameData.BonfireRecord record)
    {
        if (record == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(record.displayName))
        {
            return record.displayName.Trim();
        }

        string readableName = ExtractReadableName(record.bonfireId);
        return string.IsNullOrWhiteSpace(readableName) ? string.Empty : readableName;
    }

    private static string ResolveLabel(GameData.BonfireRecord record, int index, string currentScene)
    {
        string label = ResolveRecordName(record);
        if (string.IsNullOrWhiteSpace(label))
        {
            label = (index + 1).ToString();
        }

        return label;
    }

    private static string ExtractReadableName(string bonfireId)
    {
        if (string.IsNullOrWhiteSpace(bonfireId))
        {
            return string.Empty;
        }

        string value = bonfireId.Trim();
        int separatorIndex = value.LastIndexOf('|');
        if (separatorIndex >= 0 && separatorIndex < value.Length - 1)
        {
            value = value.Substring(separatorIndex + 1);
        }

        int bracketIndex = value.IndexOf('[');
        if (bracketIndex > 0)
        {
            value = value.Substring(0, bracketIndex);
        }

        return value.Trim();
    }

    private static GameData.BonfireRecord CloneRecord(GameData.BonfireRecord source)
    {
        return new GameData.BonfireRecord
        {
            sceneName = source.sceneName,
            bonfireId = source.bonfireId,
            displayName = source.displayName,
            worldPosition = source.worldPosition
        };
    }

    private void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
