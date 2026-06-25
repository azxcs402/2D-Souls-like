using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Bonfire : MonoBehaviour, ISaveable
{
    private const string DefaultLegacySaveId = "bonfire_default";
    private static readonly string[] DefaultSpriteNames =
    {
        "Bonfire_1",
        "Bonfire_2",
        "Bonfire_3",
        "Bonfire_4"
    };

    [Header("Activation")]
    [SerializeField] private bool bonfireActivated = true;

    [Header("Checkpoint")]
    [SerializeField] private string bonfireId = string.Empty;
    [SerializeField] private string bonfireDisplayName = string.Empty;
    [SerializeField] private bool restorePlayerOnRest = true;
    [SerializeField] private bool reloadSceneOnRest = true;
    [SerializeField, Min(0f)] private float restDelay = 0.05f;
    [SerializeField] private bool startLit = false;

    [Header("Audio")]
    [SerializeField] private AudioKey igniteSfxKey = AudioKey.BonfireIgnite;
    [SerializeField] private AudioKey restSfxKey = AudioKey.BonfireRest;
    [SerializeField] private AudioKey travelMenuOpenSfxKey = AudioKey.BonfireMenuOpen;
    [SerializeField] private AudioKey travelMenuCloseSfxKey = AudioKey.BonfireMenuClose;
    [SerializeField] private AudioKey travelConfirmSfxKey = AudioKey.BonfireTravel;

    [Header("Flame Audio")]
    [SerializeField] private AudioKey flameLoopSfxKey = AudioKey.AbyssFireFlameLoop;
    [SerializeField, Range(0f, 1f), Tooltip("Volume multiplier for the flame loop.")]
    private float flameLoopVolume = 1f;
    [SerializeField, Min(0f), Tooltip("Distance where the flame loop remains at full volume.")]
    private float flameLoopMinDistance = 0.75f;
    [SerializeField, Min(0f), Tooltip("Distance where the flame loop fades to silence.")]
    private float flameLoopMaxDistance = 4f;

    [Header("Animation")]
    [SerializeField] private Sprite[] flameFrames;
    [SerializeField, Min(1f)] private float framesPerSecond = 8f;
    [SerializeField] private bool loopAnimation = true;
    [SerializeField] private bool autoLoadDefaultFrames = true;
    [SerializeField] private Color unlitTint = new Color(0.65f, 0.65f, 0.65f, 0.9f);
    [SerializeField] private Color litTint = new Color(1f, 0.92f, 0.78f, 1f);
    [SerializeField, Min(0f)] private float ignitePulseScale = 1.1f;
    [SerializeField, Min(0.01f)] private float ignitePulseDuration = 0.35f;

    [Header("Interaction Prompt")]
    [SerializeField] private Font promptFont;
    [SerializeField] private string restPrompt = "Rest";
    [SerializeField] private string travelPrompt = "Travel";
    [SerializeField] private string ignitePrompt = "Press F to Light the Bonfire";
    [SerializeField] private string promptCursorSymbol = ">";
    [SerializeField] private Sprite restPromptIcon;
    [SerializeField] private Sprite travelPromptIcon;
    [SerializeField] private Sprite ignitePromptIcon;
    [SerializeField, Min(1f)] private float promptIconSize = 22f;
    [SerializeField, Min(1f)] private float promptRowHeight = 56f;
    [SerializeField, Min(0f)] private float promptRowSpacing = 8f;
    [SerializeField, Min(0f)] private float promptRowPaddingX = 14f;
    [SerializeField, Min(0f)] private float promptRowPaddingY = 4f;
    [SerializeField] private Color promptRowBackground = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Color promptRowSelectedBackground = new Color(0.34f, 0.34f, 0.12f, 0.35f);
    [SerializeField] private int playerSortingOrderOffset = 10;
    [SerializeField] private Vector3 promptLocalOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField, Min(1)] private int promptFontSize = 24;
    [SerializeField] private Color promptColor = Color.white;
    [SerializeField] private Color selectedPromptColor = new Color(1f, 0.92f, 0.48f, 1f);
    [SerializeField] private Color unselectedPromptColor = new Color(1f, 1f, 1f, 0.82f);

    [Header("Travel Menu")]
    [SerializeField] private Font travelMenuFont;
    [SerializeField] private string travelMenuTitle = "Teleport Destinations";
    [SerializeField] private string travelMenuHint = "W/S Select  F Teleport  ESC Close  Click to Select";
    [SerializeField] private string travelMenuEmptyText = "No bonfires available for travel";
    [SerializeField] private string travelMenuCloseLabel = "X";
    [SerializeField] private string travelMenuCursorSymbol = ">";
    [SerializeField] private Sprite travelMenuCloseIcon;
    [SerializeField] private Color travelMenuCloseButtonColor = new Color(0.25f, 0.25f, 0.3f, 0.95f);
    [SerializeField] private Color travelMenuBackdropColor = new Color(0.18f, 0.18f, 0.2f, 0.72f);
    [SerializeField] private Color travelMenuPanelColor = new Color(0.08f, 0.08f, 0.1f, 0.96f);
    [SerializeField] private Color travelMenuListColor = new Color(0.09f, 0.09f, 0.11f, 0.98f);
    [SerializeField] private Color travelMenuRowColor = new Color(0.19f, 0.19f, 0.24f, 1f);
    [SerializeField] private Color travelMenuRowSelectedColor = new Color(0.42f, 0.42f, 0.16f, 1f);
    [SerializeField] private Color travelMenuTitleColor = Color.white;
    [SerializeField] private Color travelMenuHintColor = new Color(1f, 1f, 1f, 0.84f);
    [SerializeField] private Color travelMenuEmptyColor = new Color(1f, 1f, 1f, 0.88f);
    [SerializeField] private Color travelMenuCursorColor = new Color(1f, 0.92f, 0.48f, 1f);
    [SerializeField] private Color travelMenuLabelSelectedColor = Color.white;
    [SerializeField] private Color travelMenuLabelUnselectedColor = new Color(1f, 1f, 1f, 0.82f);
    [SerializeField] private Vector2 travelMenuRootSize = new Vector2(900f, 840f);
    [SerializeField] private Vector2 travelMenuListSize = new Vector2(780f, 620f);
    [SerializeField] private Vector2 travelMenuCloseButtonSize = new Vector2(56f, 56f);
    [SerializeField] private Vector2 travelMenuCloseButtonOffset = new Vector2(-28f, -28f);
    [SerializeField, Min(1f)] private float travelMenuItemHeight = 72f;
    [SerializeField, Min(0f)] private float travelMenuItemSpacing = 10f;
    [SerializeField, Min(1)] private int travelMenuTitleFontSize = 54;
    [SerializeField, Min(1)] private int travelMenuHintFontSize = 24;
    [SerializeField, Min(1)] private int travelMenuItemFontSize = 30;
    [SerializeField, Min(1)] private int travelMenuCloseFontSize = 28;


    [Header("References")]
    [SerializeField] private SpriteRenderer flameRenderer;
    [SerializeField] private BoxCollider2D interactionCollider;
    [SerializeField] private AudioSource flameLoopSource;
    [SerializeField, HideInInspector] private string bonfireInstanceGuid = string.Empty;

    public string BonfireId => bonfireId;
    public string BonfireDisplayName => bonfireDisplayName;
    public string SceneName => GetSceneName();
    public string TravelKey => GetTravelKey();
    public string InteractPrompt => bonfireActivated ? RestPrompt : IgnitePrompt;
    public bool IsActivated => bonfireActivated;
    public bool IsLit => isLit;

    public Font PromptFont => promptFont;
    public Sprite RestPromptIcon => restPromptIcon;
    public Sprite TravelPromptIcon => travelPromptIcon;
    public Sprite IgnitePromptIcon => ignitePromptIcon;
    public float PromptIconSize => promptIconSize;
    public float PromptRowHeight => promptRowHeight;
    public float PromptRowSpacing => promptRowSpacing;
    public float PromptRowPaddingX => promptRowPaddingX;
    public float PromptRowPaddingY => promptRowPaddingY;
    public Color PromptRowBackground => promptRowBackground;
    public Color PromptRowSelectedBackground => promptRowSelectedBackground;
    public Color PromptSelectedColor => selectedPromptColor;
    public Color PromptUnselectedColor => unselectedPromptColor;
    public string RestPrompt => NormalizeLegacyPromptText(restPrompt, "Rest");
    public string TravelPrompt => NormalizeLegacyPromptText(travelPrompt, "Travel");
    public string IgnitePrompt => NormalizeLegacyPromptText(ignitePrompt, "Press F to Light the Bonfire");
    public string PromptCursorSymbol => promptCursorSymbol;
    public Font TravelMenuFont => travelMenuFont;
    public string TravelMenuTitle => NormalizeLegacyPromptText(travelMenuTitle, "Teleport Destinations");
    public string TravelMenuHint => NormalizeLegacyPromptText(travelMenuHint, "W/S Select  F Teleport  ESC Close  Click to Select");
    public string TravelMenuEmptyText => NormalizeLegacyPromptText(travelMenuEmptyText, "No bonfires available for travel");
    public string TravelMenuCloseLabel => travelMenuCloseLabel;
    public string TravelMenuCursorSymbol => travelMenuCursorSymbol;
    public Sprite TravelMenuCloseIcon => travelMenuCloseIcon;
    public Color TravelMenuCloseButtonColor => travelMenuCloseButtonColor;
    public Color TravelMenuBackdropColor => travelMenuBackdropColor;
    public Color TravelMenuPanelColor => travelMenuPanelColor;
    public Color TravelMenuListColor => travelMenuListColor;
    public Color TravelMenuRowColor => travelMenuRowColor;
    public Color TravelMenuRowSelectedColor => travelMenuRowSelectedColor;
    public Color TravelMenuTitleColor => travelMenuTitleColor;
    public Color TravelMenuHintColor => travelMenuHintColor;
    public Color TravelMenuEmptyColor => travelMenuEmptyColor;
    public Color TravelMenuCursorColor => travelMenuCursorColor;
    public Color TravelMenuLabelSelectedColor => travelMenuLabelSelectedColor;
    public Color TravelMenuLabelUnselectedColor => travelMenuLabelUnselectedColor;
    public Vector2 TravelMenuRootSize => travelMenuRootSize;
    public Vector2 TravelMenuListSize => travelMenuListSize;
    public Vector2 TravelMenuCloseButtonSize => travelMenuCloseButtonSize;
    public Vector2 TravelMenuCloseButtonOffset => travelMenuCloseButtonOffset;
    public float TravelMenuItemHeight => travelMenuItemHeight;
    public float TravelMenuItemSpacing => travelMenuItemSpacing;
    public int TravelMenuTitleFontSize => travelMenuTitleFontSize;
    public int TravelMenuHintFontSize => travelMenuHintFontSize;
    public int TravelMenuItemFontSize => travelMenuItemFontSize;
    public int TravelMenuCloseFontSize => travelMenuCloseFontSize;
    private Player currentPlayer;
    private bool isResting;
    private bool isLit;
    private int currentFrameIndex;
    private float frameTimer;
    private readonly List<PlayerRendererState> playerRendererStates = new();
    private Coroutine igniteCoroutine;
    private Canvas promptCanvas;
    private GameObject promptRoot;
    private GameObject ignitePromptRoot;
    private GameObject litPromptRoot;
    private Image ignitePromptIconImage;
    private Text ignitePromptText;
    private PromptOptionVisual[] litPromptOptions;
    private int selectedInteractionIndex;

    private struct PlayerRendererState
    {
        public SpriteRenderer renderer;
        public int sortingOrder;
    }

    private struct PromptOptionVisual
    {
        public GameObject root;
        public Button button;
        public Image background;
        public Image icon;
        public Text cursor;
        public Text label;
    }

    private void Awake()
    {
        CacheReferences();
        ConfigureCollider();
        EnsureActivationGuid();
        EnsurePromptVisual();
        TryLoadDefaultFrames();
        bonfireActivated = bonfireActivated || startLit;
        isLit = bonfireActivated || startLit;
        ApplyLitVisuals();
        UpdateFlameLoopAudio();
        UpdatePromptVisual();
        ApplyFrame(0);
    }

    private void OnEnable()
    {
        UpdateFlameLoopAudio();
    }

    private void OnValidate()
    {
        CacheReferences();
        ConfigureCollider();
        EnsureActivationGuid();
        ResolveActivationDuplicate();
        bonfireActivated = bonfireActivated || startLit;
        isLit = bonfireActivated;
        flameLoopMaxDistance = Mathf.Max(flameLoopMinDistance, flameLoopMaxDistance);
        framesPerSecond = Mathf.Max(1f, framesPerSecond);
        promptFontSize = Mathf.Max(1, promptFontSize);
        ignitePulseScale = Mathf.Max(1f, ignitePulseScale);
        ignitePulseDuration = Mathf.Max(0.01f, ignitePulseDuration);

        if (!Application.isPlaying)
        {
            return;
        }

        TryLoadDefaultFrames();
        ApplyLitVisuals();
        UpdateFlameLoopAudio();
        if (promptRoot != null)
        {
            EnsurePromptVisual();
            UpdatePromptVisual();
            ApplyFrame(0);
        }
    }

    private void Reset()
    {
        bonfireActivated = false;
        isLit = false;
        bonfireInstanceGuid = string.Empty;
        EnsureActivationGuid();
    }

    private void Update()
    {
        UpdateAnimation();

        if (currentPlayer == null || isResting)
        {
            UpdatePromptVisual();
            return;
        }

        if (currentPlayer.IsDead)
        {
            UpdatePromptVisual();
            return;
        }

        if (bonfireActivated && BonfireTravelMenu.IsOpen)
        {
            UpdatePromptVisual();
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            UpdatePromptVisual();
            return;
        }

        if (bonfireActivated)
        {
            if (keyboard.wKey.wasPressedThisFrame)
            {
                MoveInteractionSelection(-1);
            }

            if (keyboard.sKey.wasPressedThisFrame)
            {
                MoveInteractionSelection(1);
            }

            if (keyboard.fKey.wasPressedThisFrame)
            {
                if (selectedInteractionIndex <= 0)
                {
                    StartRestSequence();
                }
                else
                {
                    PlayAudio(travelMenuOpenSfxKey);
                    BonfireTravelMenu.Open(this);
                }
            }
        }
        else if (keyboard.fKey.wasPressedThisFrame)
        {
            StartRestSequence();
        }

        UpdatePromptVisual();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player != null)
        {
            currentPlayer = player;
            if (bonfireActivated)
            {
                selectedInteractionIndex = 0;
            }

            ApplyPlayerForeground(player);
            UpdatePromptVisual();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player != null && player == currentPlayer)
        {
            RestorePlayerForeground();
            currentPlayer = null;
            UpdatePromptVisual();
        }
    }

    public void StartRestSequence()
    {
        if (isResting || currentPlayer == null || currentPlayer.IsDead)
        {
            return;
        }

        PlayAudio(restSfxKey);

        StartCoroutine(RestAtBonfireRoutine());
        UpdatePromptVisual();
    }

    public GameData.BonfireRecord BuildTravelRecord()
    {
        return new GameData.BonfireRecord
        {
            sceneName = GetSceneName(),
            bonfireId = GetSaveId(),
            displayName = GetDisplayName(),
            worldPosition = transform.position
        };
    }

    public void RefreshInteractionPrompt()
    {
        UpdatePromptVisual();
    }

    public void SetSelectedInteractionIndex(int index)
    {
        selectedInteractionIndex = Mathf.Clamp(index, 0, 1);
        UpdatePromptVisual();
    }

    private IEnumerator RestAtBonfireRoutine()
    {
        isResting = true;

        if (!bonfireActivated || !isLit)
        {
            SetActivated(true);
            SetLit(true, true);
            PlayAudio(igniteSfxKey);
        }

        Player player = currentPlayer;
        if (player != null && restorePlayerOnRest)
        {
            player.RestoreHealthToFull();
            player.ResetForBonfire();
        }

        if (SaveManager.instance != null)
        {
            SaveManager.instance.SaveCheckpoint(transform.position, SceneManager.GetActiveScene().name);
        }

        if (restDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(restDelay);
        }

        if (reloadSceneOnRest)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (GameManager.instance != null)
            {
                GameManager.instance.ChangeScene(sceneName, false);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }
        else if (GameManager.instance != null)
        {
            GameManager.instance.SetGameplayControlsEnabled(true);
        }
        else if (player != null)
        {
            player.enabled = true;
            if (player.rb != null)
            {
                player.rb.simulated = true;
            }
        }

        RefreshInteractionPrompt();
        isResting = false;
    }

    public void PlayTravelMenuClosedSfx()
    {
        PlayAudio(travelMenuCloseSfxKey);
    }

    public void PlayTravelConfirmSfx()
    {
        PlayAudio(travelConfirmSfxKey);
    }

    private void UpdateFlameLoopAudio()
    {
        if (!bonfireActivated || !isLit)
        {
            StopFlameLoopAudio();
            return;
        }

        EnsureFlameLoopSource();
        if (flameLoopSource == null)
        {
            return;
        }

        AudioManager.instance?.PlayLoopingSFX(flameLoopSfxKey, flameLoopSource, flameLoopVolume);
    }

    private void StopFlameLoopAudio()
    {
        if (flameLoopSource == null)
        {
            return;
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.StopLoopingSFX(flameLoopSource);
            return;
        }

        if (flameLoopSource.isPlaying)
        {
            flameLoopSource.Stop();
        }
    }

    public void CopyFlameAudioSettingsFrom(Bonfire source)
    {
        if (source == null || source == this)
        {
            return;
        }

        flameLoopSfxKey = source.flameLoopSfxKey;
        flameLoopVolume = source.flameLoopVolume;
        flameLoopMinDistance = source.flameLoopMinDistance;
        flameLoopMaxDistance = source.flameLoopMaxDistance;

        if (Application.isPlaying)
        {
            UpdateFlameLoopAudio();
        }
    }

    private void EnsureFlameLoopSource()
    {
        if (flameLoopSource == null)
        {
            flameLoopSource = GetComponent<AudioSource>();
        }

        if (flameLoopSource == null && Application.isPlaying)
        {
            flameLoopSource = gameObject.AddComponent<AudioSource>();
        }

        if (flameLoopSource == null)
        {
            return;
        }

        flameLoopSource.playOnAwake = false;
        flameLoopSource.loop = true;
        flameLoopSource.spatialBlend = 1f;
        flameLoopSource.rolloffMode = AudioRolloffMode.Logarithmic;
        flameLoopSource.dopplerLevel = 0f;
        flameLoopSource.minDistance = flameLoopMinDistance;
        flameLoopSource.maxDistance = flameLoopMaxDistance;
    }

    public void LoadData(GameData data)
    {
        if (data == null)
        {
            return;
        }

        data.litBonfireRecords ??= new List<GameData.BonfireRecord>();
        string saveId = GetSaveId();
        string saveKey = GetTravelKey();
        string legacySaveId = GetLegacySaveId();
        string sceneName = GetSceneName();

        bool savedAsRecord = false;
        if (data.litBonfireRecords != null)
        {
            for (int i = 0; i < data.litBonfireRecords.Count; i++)
            {
                GameData.BonfireRecord record = data.litBonfireRecords[i];
                if (record == null)
                {
                    continue;
                }

                if (record.sceneName == sceneName && record.bonfireId == saveId)
                {
                    savedAsRecord = true;
                    break;
                }
            }
        }

        bool savedAsLegacyId = data.litBonfireIds != null
            && (data.litBonfireIds.Contains(saveId)
                || data.litBonfireIds.Contains(saveKey)
                || data.litBonfireIds.Contains(legacySaveId));

        bonfireActivated = savedAsRecord || savedAsLegacyId || startLit || bonfireActivated;
        isLit = bonfireActivated;

        if (bonfireActivated)
        {
            int recordIndex = -1;
            for (int i = 0; i < data.litBonfireRecords.Count; i++)
            {
                GameData.BonfireRecord record = data.litBonfireRecords[i];
                if (record != null && record.sceneName == sceneName && record.bonfireId == saveId)
                {
                    recordIndex = i;
                    break;
                }
            }

            GameData.BonfireRecord bonfireRecord = recordIndex >= 0
                ? data.litBonfireRecords[recordIndex]
                : new GameData.BonfireRecord();

            bonfireRecord.sceneName = sceneName;
            bonfireRecord.bonfireId = saveId;
            bonfireRecord.displayName = GetDisplayName();
            bonfireRecord.worldPosition = transform.position;

            if (recordIndex < 0)
            {
                data.litBonfireRecords.Add(bonfireRecord);
            }
        }

        ApplyLitVisuals();
        UpdateFlameLoopAudio();
    }

    public void SaveData(ref GameData data)
    {
        if (data == null)
        {
            data = new GameData();
        }

        data.litBonfireIds ??= new List<string>();
        data.litBonfireRecords ??= new List<GameData.BonfireRecord>();

        string saveId = GetSaveId();
        string saveKey = GetTravelKey();
        string sceneName = GetSceneName();
        string displayName = GetDisplayName();

        bool shouldSaveAsActivated = bonfireActivated || isLit;

        if (shouldSaveAsActivated && !data.litBonfireIds.Contains(saveId))
        {
            data.litBonfireIds.Add(saveId);
        }

        if (shouldSaveAsActivated && !data.litBonfireIds.Contains(saveKey))
        {
            data.litBonfireIds.Add(saveKey);
        }

        if (shouldSaveAsActivated)
        {
            int recordIndex = -1;
            for (int i = 0; i < data.litBonfireRecords.Count; i++)
            {
                GameData.BonfireRecord record = data.litBonfireRecords[i];
                if (record != null && record.sceneName == sceneName && record.bonfireId == saveId)
                {
                    recordIndex = i;
                    break;
                }
            }

            GameData.BonfireRecord bonfireRecord = recordIndex >= 0
                ? data.litBonfireRecords[recordIndex]
                : new GameData.BonfireRecord();

            bonfireRecord.sceneName = sceneName;
            bonfireRecord.bonfireId = saveId;
            bonfireRecord.displayName = displayName;
            bonfireRecord.worldPosition = transform.position;

            if (recordIndex < 0)
            {
                data.litBonfireRecords.Add(bonfireRecord);
            }
        }
    }

    private void EnsureActivationGuid()
    {
        if (!string.IsNullOrWhiteSpace(bonfireInstanceGuid))
        {
            return;
        }

        bonfireInstanceGuid = Guid.NewGuid().ToString("N");
    }

    private void ResolveActivationDuplicate()
    {
#if UNITY_EDITOR
        if (Application.isPlaying || string.IsNullOrWhiteSpace(bonfireInstanceGuid))
        {
            return;
        }

        Bonfire[] allBonfires = FindObjectsByType<Bonfire>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Bonfire conflict = null;
        for (int i = 0; i < allBonfires.Length; i++)
        {
            Bonfire candidate = allBonfires[i];
            if (candidate == null || candidate == this)
            {
                continue;
            }

            if (candidate.bonfireInstanceGuid == bonfireInstanceGuid)
            {
                conflict = candidate;
                break;
            }
        }

        if (conflict != null && GetInstanceID() > conflict.GetInstanceID())
        {
            bonfireActivated = false;
            isLit = false;
            bonfireInstanceGuid = Guid.NewGuid().ToString("N");
        }
#endif
    }

    private void CacheReferences()
    {
        if (flameRenderer == null)
        {
            flameRenderer = GetComponent<SpriteRenderer>();
        }

        if (interactionCollider == null)
        {
            interactionCollider = GetComponent<BoxCollider2D>();
        }
    }

    private void ConfigureCollider()
    {
        if (interactionCollider == null)
        {
            return;
        }

        interactionCollider.isTrigger = true;

        if (interactionCollider.size == Vector2.zero)
        {
            interactionCollider.size = new Vector2(1.25f, 1.75f);
        }

        if (interactionCollider.offset == Vector2.zero)
        {
            interactionCollider.offset = new Vector2(0f, 0.85f);
        }
    }

    private void ApplyPlayerForeground(Player player)
    {
        if (player == null)
        {
            return;
        }

        RestorePlayerForeground();
        playerRendererStates.Clear();

        SpriteRenderer[] renderers = player.GetComponentsInChildren<SpriteRenderer>(true);
        int baseOrder = GetForegroundOrderBase();

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            playerRendererStates.Add(new PlayerRendererState
            {
                renderer = renderer,
                sortingOrder = renderer.sortingOrder
            });

            renderer.sortingOrder = baseOrder + i;
        }
    }

    private void RestorePlayerForeground()
    {
        for (int i = 0; i < playerRendererStates.Count; i++)
        {
            PlayerRendererState state = playerRendererStates[i];
            if (state.renderer != null)
            {
                state.renderer.sortingOrder = state.sortingOrder;
            }
        }

        playerRendererStates.Clear();
    }

    private int GetForegroundOrderBase()
    {
        return (flameRenderer != null ? flameRenderer.sortingOrder : 0) + playerSortingOrderOffset;
    }

    private void TryLoadDefaultFrames()
    {
        if (!autoLoadDefaultFrames || (flameFrames != null && flameFrames.Length > 0))
        {
            return;
        }

        flameFrames = new Sprite[DefaultSpriteNames.Length];
        for (int i = 0; i < DefaultSpriteNames.Length; i++)
        {
            flameFrames[i] = LoadSpriteByName(DefaultSpriteNames[i]);
        }
    }

    private void UpdateAnimation()
    {
        if (flameRenderer == null || flameFrames == null || flameFrames.Length == 0)
        {
            return;
        }

        if (flameFrames.Length == 1)
        {
            ApplyFrame(0);
            return;
        }

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / framesPerSecond;

        if (frameTimer < frameDuration)
        {
            return;
        }

        frameTimer -= frameDuration;
        currentFrameIndex++;

        if (currentFrameIndex >= flameFrames.Length)
        {
            if (!loopAnimation)
            {
                currentFrameIndex = flameFrames.Length - 1;
                frameTimer = 0f;
                ApplyFrame(currentFrameIndex);
                return;
            }

            currentFrameIndex = 0;
        }

        ApplyFrame(currentFrameIndex);
    }

    private void ApplyFrame(int frameIndex)
    {
        if (flameRenderer == null || flameFrames == null || flameFrames.Length == 0)
        {
            return;
        }

        frameIndex = Mathf.Clamp(frameIndex, 0, flameFrames.Length - 1);
        Sprite frame = flameFrames[frameIndex];
        if (frame != null)
        {
            flameRenderer.sprite = frame;
        }
    }

    private void ApplyLitVisuals()
    {
        if (flameRenderer == null)
        {
            return;
        }

        flameRenderer.color = isLit ? litTint : unlitTint;
        flameRenderer.sortingOrder = isLit ? 12 : 10;
        UpdatePromptVisual();
    }

    private void SetLit(bool lit, bool playIgniteEffect)
    {
        bool changed = isLit != lit;
        isLit = lit;
        if (lit)
        {
            bonfireActivated = true;
        }
        ApplyLitVisuals();

        if (!changed)
        {
            return;
        }

        if (igniteCoroutine != null)
        {
            StopCoroutine(igniteCoroutine);
            igniteCoroutine = null;
        }

        if (lit && playIgniteEffect)
        {
            igniteCoroutine = StartCoroutine(IgnitePulseCo());
        }

        UpdateFlameLoopAudio();
    }

    private void SetActivated(bool activated, bool synchronizeLit = true)
    {
        bool changed = bonfireActivated != activated;
        bonfireActivated = activated;
        if (synchronizeLit)
        {
            isLit = activated;
        }

        ApplyLitVisuals();

        if (!changed)
        {
            return;
        }

        if (!activated && igniteCoroutine != null)
        {
            StopCoroutine(igniteCoroutine);
            igniteCoroutine = null;
        }

        UpdateFlameLoopAudio();
    }

    private IEnumerator IgnitePulseCo()
    {
        if (flameRenderer == null)
        {
            yield break;
        }

        Vector3 baseScale = transform.localScale;
        Vector3 pulseScale = baseScale * ignitePulseScale;
        float halfDuration = ignitePulseDuration * 0.5f;

        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            transform.localScale = Vector3.Lerp(baseScale, pulseScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            transform.localScale = Vector3.Lerp(pulseScale, baseScale, t);
            yield return null;
        }

        transform.localScale = baseScale;
        igniteCoroutine = null;
    }

    private Sprite LoadSpriteByName(string spriteName)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
        {
            return null;
        }

#if UNITY_EDITOR
        string[] candidatePaths =
        {
            "Assets/Graphics/Decorations/Bonfire/Bonfire_1.png",
            "Assets/Graphics/Decorations/Bonfire/Bonfire_2.png",
            "Assets/Graphics/Decorations/Bonfire/Bonfire_3.png",
            "Assets/Graphics/Decorations/Bonfire/Bonfire_4.png"
        };

        foreach (string path in candidatePaths)
        {
            Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null && sprite.name == spriteName)
            {
                return sprite;
            }
        }
#endif

        return null;
    }

    private string GetSaveId()
    {
        string trimmedId = string.IsNullOrWhiteSpace(bonfireId) ? string.Empty : bonfireId.Trim();
        if (!string.IsNullOrEmpty(trimmedId) && !string.Equals(trimmedId, DefaultLegacySaveId, System.StringComparison.Ordinal))
        {
            return trimmedId;
        }

        return BuildFallbackSaveId();
    }

    private string GetLegacySaveId()
    {
        return string.IsNullOrWhiteSpace(bonfireId) ? DefaultLegacySaveId : bonfireId.Trim();
    }

    private string GetDisplayName()
    {
        string displayName = string.IsNullOrWhiteSpace(bonfireDisplayName) ? string.Empty : bonfireDisplayName.Trim();
        if (!string.IsNullOrWhiteSpace(displayName) && !LooksLikeMojibake(displayName))
        {
            return displayName;
        }

        return gameObject != null ? gameObject.name : string.Empty;
    }

    private string GetSceneName()
    {
        if (gameObject.scene.IsValid())
        {
            return gameObject.scene.name;
        }

        return SceneManager.GetActiveScene().name;
    }

    private string GetTravelKey()
    {
        return $"{GetSceneName()}|{GetSaveId()}";
    }

    private string BuildFallbackSaveId()
    {
        return $"{GetSceneName()}|{GetHierarchyPath(transform)}";
    }

    private static string GetHierarchyPath(Transform target)
    {
        if (target == null)
        {
            return string.Empty;
        }

        List<string> parts = new();
        Transform current = target;
        while (current != null)
        {
            parts.Add($"{current.name}[{current.GetSiblingIndex()}]");
            current = current.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    private static bool LooksLikeMojibake(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        int suspiciousCount = 0;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '\uFFFD' || char.IsControl(c))
            {
                return true;
            }

            if (c >= '\u00C0' && c <= '\u024F')
            {
                suspiciousCount++;
            }
        }

        return suspiciousCount >= 2;
    }

    private void EnsurePromptVisual()
    {
        if (promptRoot == null)
        {
            Transform existing = transform.Find("Prompt");
            promptRoot = existing != null ? existing.gameObject : new GameObject("Prompt");

            if (existing == null)
            {
                promptRoot.transform.SetParent(transform, false);
            }
        }

        if (promptCanvas == null)
        {
            promptCanvas = promptRoot.GetComponent<Canvas>();
        }

        if (promptCanvas == null)
        {
            promptCanvas = promptRoot.AddComponent<Canvas>();
        }

        if (promptRoot.GetComponent<GraphicRaycaster>() == null)
        {
            promptRoot.AddComponent<GraphicRaycaster>();
        }

        promptCanvas.renderMode = RenderMode.WorldSpace;
        promptCanvas.overrideSorting = true;
        promptCanvas.sortingOrder = 50;

        RectTransform rect = promptRoot.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = promptRoot.AddComponent<RectTransform>();
        }

        rect.localPosition = promptLocalOffset;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one * 0.01f;
        rect.sizeDelta = new Vector2(560f, 180f);

        EnsureIgnitePromptVisual();
        EnsureLitPromptVisual();
        UpdatePromptOptionVisuals();
    }

    private void UpdatePromptVisual()
    {
        EnsurePromptVisual();

        if (promptRoot == null)
        {
            return;
        }

        bool hasPlayer = currentPlayer != null && !isResting && !currentPlayer.IsDead;
        bool showLitPrompt = hasPlayer && bonfireActivated && !BonfireTravelMenu.IsOpen;
        bool showIgnitePrompt = hasPlayer && !bonfireActivated && !BonfireTravelMenu.IsOpen;

        promptRoot.SetActive(hasPlayer);

        if (ignitePromptRoot != null)
        {
            ignitePromptRoot.SetActive(showIgnitePrompt);
        }

        if (litPromptRoot != null)
        {
            litPromptRoot.SetActive(showLitPrompt);
        }

        if (showLitPrompt)
        {
            UpdatePromptOptionVisuals();
        }

    }

    private void EnsureIgnitePromptVisual()
    {
        if (ignitePromptRoot == null)
        {
            ignitePromptRoot = new GameObject("IgnitePrompt", typeof(RectTransform));
            ignitePromptRoot.transform.SetParent(promptRoot.transform, false);
        }

        RectTransform rootRect = ignitePromptRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        RectTransform contentRect;
        Transform contentTransform = ignitePromptRoot.transform.Find("Content");
        GameObject contentObject;
        if (contentTransform == null)
        {
            contentObject = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            contentObject.transform.SetParent(ignitePromptRoot.transform, false);
        }
        else
        {
            contentObject = contentTransform.gameObject;
        }

        contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(360f, promptRowHeight);
        contentRect.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup layout = contentObject.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = promptRowSpacing;
        layout.padding = new RectOffset(Mathf.RoundToInt(promptRowPaddingX), Mathf.RoundToInt(promptRowPaddingX), Mathf.RoundToInt(promptRowPaddingY), Mathf.RoundToInt(promptRowPaddingY));

        if (ignitePromptIconImage == null)
        {
            Transform iconTransform = contentObject.transform.Find("Icon");
            if (iconTransform != null)
            {
                ignitePromptIconImage = iconTransform.GetComponent<Image>();
            }
        }

        if (ignitePromptIconImage == null)
        {
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(contentObject.transform, false);
            ignitePromptIconImage = iconObject.GetComponent<Image>();
        }

        RectTransform iconRect = ignitePromptIconImage.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(promptIconSize, promptIconSize);
        ignitePromptIconImage.sprite = ignitePromptIcon;
        ignitePromptIconImage.color = Color.white;
        ignitePromptIconImage.enabled = ignitePromptIconImage.sprite != null;

        if (ignitePromptText == null)
        {
            Transform textTransform = contentObject.transform.Find("Text");
            if (textTransform != null)
            {
                ignitePromptText = textTransform.GetComponent<Text>();
            }
        }

        if (ignitePromptText == null)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(contentObject.transform, false);
            ignitePromptText = textObject.GetComponent<Text>();
        }

        RectTransform textRect = ignitePromptText.GetComponent<RectTransform>();
        textRect.sizeDelta = Vector2.zero;

        ignitePromptText.font = LoadPromptFont();
        ignitePromptText.fontSize = promptFontSize;
        ignitePromptText.alignment = TextAnchor.MiddleCenter;
        ignitePromptText.color = promptColor;
        ignitePromptText.horizontalOverflow = HorizontalWrapMode.Overflow;
        ignitePromptText.verticalOverflow = VerticalWrapMode.Overflow;
        ignitePromptText.text = IgnitePrompt;
    }

    private void PlayAudio(AudioKey audioKey)
    {
        if (AudioManager.instance == null)
        {
            return;
        }

        EnsureFlameLoopSource();
        if (flameLoopSource != null && AudioManager.instance.PlayLocalizedSFX(audioKey, flameLoopSource, flameLoopMaxDistance))
        {
            return;
        }

        AudioManager.instance.PlayGlobalSFX(audioKey);
    }

    private void EnsureLitPromptVisual()
    {
        if (litPromptRoot == null)
        {
            litPromptRoot = new GameObject("LitPrompt", typeof(RectTransform));
            litPromptRoot.transform.SetParent(promptRoot.transform, false);
        }

        RectTransform rootRect = litPromptRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        if (litPromptOptions == null || litPromptOptions.Length != 2)
        {
            litPromptOptions = new PromptOptionVisual[2];
        }

        if (litPromptOptions[0].root == null)
        {
            GameObject panel = new GameObject("PromptPanel", typeof(RectTransform));
            panel.transform.SetParent(litPromptRoot.transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(480f, (promptRowHeight * 2f) + promptRowSpacing + 24f);
            panelRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.spacing = promptRowSpacing;
            layout.padding = new RectOffset(Mathf.RoundToInt(promptRowPaddingX), Mathf.RoundToInt(promptRowPaddingX), Mathf.RoundToInt(promptRowPaddingY), Mathf.RoundToInt(promptRowPaddingY));

            ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            litPromptOptions[0] = CreatePromptOption(panel.transform, "RestOption", RestPrompt, restPromptIcon, 0, OnRestSelected);
            litPromptOptions[1] = CreatePromptOption(panel.transform, "TravelOption", TravelPrompt, travelPromptIcon, 1, OnTravelSelected);
        }

        UpdatePromptOptionVisuals();
    }

    private PromptOptionVisual CreatePromptOption(Transform parent, string name, string label, Sprite iconSprite, int index, System.Action onClick)
    {
        GameObject rootObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        rootObject.transform.SetParent(parent, false);

        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(0f, promptRowHeight);

        LayoutElement layout = rootObject.AddComponent<LayoutElement>();
        layout.minHeight = promptRowHeight;
        layout.flexibleWidth = 1f;

        Image background = rootObject.GetComponent<Image>();
        background.color = promptRowBackground;

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
        rowLayout.spacing = promptRowSpacing;
        rowLayout.padding = new RectOffset(Mathf.RoundToInt(promptRowPaddingX), Mathf.RoundToInt(promptRowPaddingX), Mathf.RoundToInt(promptRowPaddingY), Mathf.RoundToInt(promptRowPaddingY));

        Text cursor = CreateText(rootObject.transform, "Cursor", string.IsNullOrWhiteSpace(promptCursorSymbol) ? ">" : promptCursorSymbol, promptFontSize, FontStyle.Bold, TextAnchor.MiddleCenter, promptFont);
        cursor.color = selectedPromptColor;
        cursor.GetComponent<RectTransform>().sizeDelta = new Vector2(promptFontSize, 0f);

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(rootObject.transform, false);
        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = iconSprite;
        icon.color = Color.white;
        icon.enabled = iconSprite != null;
        icon.GetComponent<RectTransform>().sizeDelta = new Vector2(promptIconSize, promptIconSize);

        Text labelText = CreateText(rootObject.transform, "Label", label, promptFontSize, FontStyle.Bold, TextAnchor.MiddleLeft, promptFont);
        labelText.color = unselectedPromptColor;
        labelText.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        return new PromptOptionVisual
        {
            root = rootObject,
            button = button,
            background = background,
            icon = icon,
            cursor = cursor,
            label = labelText
        };
    }

    private void UpdatePromptOptionVisuals()
    {
        if (litPromptOptions == null || litPromptOptions.Length < 2)
        {
            return;
        }

        for (int i = 0; i < litPromptOptions.Length; i++)
        {
            PromptOptionVisual option = litPromptOptions[i];
            if (option.root == null)
            {
                continue;
            }

            bool selected = i == Mathf.Clamp(selectedInteractionIndex, 0, 1);
            if (option.background != null)
            {
                option.background.color = selected
                    ? promptRowSelectedBackground
                    : promptRowBackground;
            }

            if (option.icon != null)
            {
                option.icon.sprite = i == 0 ? restPromptIcon : travelPromptIcon;
                option.icon.enabled = option.icon.sprite != null;
            }

            if (option.cursor != null)
            {
                Color color = option.cursor.color;
                color.a = selected ? 1f : 0f;
                option.cursor.color = color;
            }

            if (option.label != null)
            {
                option.label.text = i == 0 ? RestPrompt : TravelPrompt;
                option.label.color = selected ? selectedPromptColor : unselectedPromptColor;
            }
        }
    }

    private void MoveInteractionSelection(int delta)
    {
        if (selectedInteractionIndex < 0)
        {
            selectedInteractionIndex = 0;
        }
        else
        {
            selectedInteractionIndex = (selectedInteractionIndex + delta) % 2;
            if (selectedInteractionIndex < 0)
            {
                selectedInteractionIndex += 2;
            }
        }

        UpdatePromptOptionVisuals();
    }

    private void OnRestSelected()
    {
        selectedInteractionIndex = 0;
        StartRestSequence();
    }

    private void OnTravelSelected()
    {
        selectedInteractionIndex = 1;
        BonfireTravelMenu.Open(this);
    }

    private Font LoadPromptFont()
    {
        if (promptFont != null)
        {
            return promptFont;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        return Font.CreateDynamicFontFromOSFont("Arial", 16);
    }

    private static string NormalizeLegacyPromptText(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        string trimmed = value.Trim();
        return trimmed switch
        {
            "\u4f11\u606f" => "Rest",
            "\u4f20\u9001" => "Travel",
            "\u6309\u4e0bF\u6fc0\u6d3b\u7bdd\u706b" => "Press F to Light the Bonfire",
            "\u4f20\u9001\u5730\u70b9" => "Teleport Destinations",
            "W/S \u9009\u62e9  F \u4f20\u9001  ESC \u5173\u95ed  \u9f20\u6807\u70b9\u51fb\u53ef\u9009\u4e2d" => "W/S Select  F Teleport  ESC Close  Click to Select",
            "\u6ca1\u6709\u53ef\u4f20\u9001\u7684\u7bdd\u706b" => "No bonfires available for travel",
            _ => trimmed
        };
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

    private void OnDisable()
    {
        RestorePlayerForeground();
        StopFlameLoopAudio();
        UpdatePromptVisual();
    }

    private void OnDestroy()
    {
        RestorePlayerForeground();
        StopFlameLoopAudio();
    }
}
