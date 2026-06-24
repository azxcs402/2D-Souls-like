using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine.Serialization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class ArenaBossEncounterController : MonoBehaviour
{
    public static ArenaBossEncounterController Instance { get; private set; }
    private const int Skill4FireballCount = 6;
    private const float AbyssMageStunSkillPointCooldown = 5f;

    public static bool HasActiveEncounter => Instance != null && Instance.encounterStarted && !Instance.encounterCompleted;

    public static ArenaBossEncounterController GetActiveInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        Instance = FindObjectOfType<ArenaBossEncounterController>(true);
        return Instance;
    }

    [Serializable]
    public class BossPhaseState
    {
        [SerializeField] private BossPhaseAction[] actions = Array.Empty<BossPhaseAction>();

        public BossPhaseAction[] Actions => actions ?? Array.Empty<BossPhaseAction>();
    }

    [Serializable]
    public class BossPhaseAction
    {
        [SerializeField] private BossPhaseActionType actionType = BossPhaseActionType.SetGameObjectsActive;
        [SerializeField] private GameObject[] gameObjects = Array.Empty<GameObject>();
        [SerializeField] private bool active = true;
        [SerializeField] private FloatingPlatformState floatingPlatformState = FloatingPlatformState.AllVisible;
        [SerializeField, Min(0)] private int visibleCount;

        public BossPhaseActionType ActionType => actionType;
        public GameObject[] GameObjects => gameObjects ?? Array.Empty<GameObject>();
        public bool Active => active;
        public FloatingPlatformState FloatingPlatformState => floatingPlatformState;
        public int VisibleCount => visibleCount;
    }

    public enum BossPhaseActionType
    {
        SetGameObjectsActive = 0,
        SetFloatingPlatformState = 1,
        SetSkillPointBackgroundVisibleCount = 2,
        SetAbyssPowerVisibleCount = 3,
        SetAbyssFireVisibleCount = 4
    }

    public enum FloatingPlatformState
    {
        Hidden = 0,
        Platform1 = 1,
        Platform2 = 2,
        Platform3 = 3,
        AllVisible = 4,
        Platform4 = 5,
        Platform5 = 6,
        Platform6 = 7
    }

    public enum AbyssMageSkillPointPacePreset
    {
        Conservative = 0,
        Standard = 1,
        Aggressive = 2
    }

    [Header("Boss")]
    [SerializeField] private Enemy bossEnemy;
    [SerializeField] private GameObject bossPrefab;
    [FormerlySerializedAs("giantFireballExplosionRadiusOverride")]
    [SerializeField, Min(0f), Tooltip("Explosion damage radius used for giant fireballs spawned by this encounter.")]
    private float giantFireballExplosionRadius = 1.75f;
    [SerializeField, Range(1, 4)] private int bossPhaseCount = 4;
    [SerializeField, Range(0f, 1f)] private float phase2HealthThreshold = 0.75f;
    [SerializeField, Range(0f, 1f)] private float phase3HealthThreshold = 0.5f;
    [SerializeField, Range(0f, 1f)] private float phase4HealthThreshold = 0.25f;
    [SerializeField] private BossPhaseState[] bossPhaseStates = new BossPhaseState[4];

    [Header("Boss Floating Platform")]
    [SerializeField] private ArenaBossFloatingPlatformController bossFloatingPlatform;

    [Header("Abyss Fire")]
    [SerializeField] private AbyssFire[] abyssFires = Array.Empty<AbyssFire>();
    [SerializeField, Min(0f)] private float abyssFireRevealDelay = 0.5f;
    [SerializeField, Min(0f), Tooltip("Delay after the sixth abyss fire appears before lowering all abyss fire audio.")]
    private float abyssFireAudioReductionDelay = 1f;
    [SerializeField, Range(0f, 1f), Tooltip("Volume reduction applied after the delay. 0.75 means the audio is lowered to 25%.")]
    private float abyssFireAudioReduction = 0.75f;

    [Header("Abyss Power")]
    [SerializeField] private AbyssPower[] abyssPowers = Array.Empty<AbyssPower>();

    [Header("Abyss Mage Skill Point")]
    [SerializeField] private AbyssMageSkillPointPacePreset abyssMageSkillPointPacePreset = AbyssMageSkillPointPacePreset.Standard;
    [SerializeField, Range(0f, 100f)] private float abyssMageMeleeSkillPointChance = 40f;
    [SerializeField, Range(0f, 100f)] private float abyssMageFireballSummonSkillPointChance = 35f;
    [SerializeField, Min(0f)] private float abyssMageFireballSummonSkillPointCooldown = 3f;
    [SerializeField, Min(0f)] private float abyssMageFireballPlayerInteractionSkillPointCooldown = 6f;
    [SerializeField, Range(0f, 100f)] private float abyssMagePassiveSkillPointInitialChance = 1f;
    [SerializeField, Min(0f)] private float abyssMagePassiveSkillPointChanceIncrement = 5f;
    [SerializeField, Min(0.1f)] private float abyssMagePassiveSkillPointCheckInterval = 1f;
    [SerializeField, Min(0f)] private float giantHorizontalWallClearance = 0.2f;
    [SerializeField, Range(0.1f, 3f)] private float giantFireballColliderRadiusMultiplier = 0.6666667f;

    [Header("Enhanced Skill 2")]
    [SerializeField, Min(1)] private int normalSkill2CastsBeforeEnhanced = 2;

    [Header("Enhanced Skill 3")]
    [SerializeField] private Transform bossEnhancedSkill3CastPoint;
    [SerializeField, Min(1)] private int normalSkill3CastsBeforeEnhanced = 2;
    [SerializeField, Min(1)] private int enhancedSkill3GiantFireballCount = 3;
    [SerializeField, Min(0f)] private float enhancedSkill3FireballInterval = 0.8f;
    [SerializeField, Min(0f)] private float enhancedSkill3PlatformRiseStartDelay = 1f;
    [SerializeField, Min(0)] private int enhancedSkill3PlatformRiseCount = 3;
    [SerializeField, Min(0f)] private float enhancedSkill3PlatformRiseInterval = 0.6f;

    [Header("Skill 4")]
    [SerializeField, Min(0f)] private float skill4FireballSpawnOffsetStep = 0.08f;
    [SerializeField, Min(0f)] private float skill4FireballSpawnHeightOffset = 0.35f;

    [Header("Skill 5")]
    [SerializeField] private Transform bossSkill5CastPoint;
    [SerializeField, Range(0f, 1f)] private float skill5TriggerHealthPercent = 0.35f;
    [SerializeField, Range(0f, 1f)] private float skill5MaxRecoverHealthPercent = 0.65f;
    [SerializeField, Min(0f)] private float skill5HealPercentPerSecond = 0.012f;
    [SerializeField, Min(0.05f)] private float skill5GiantFireballInterval = 0.8f;
    [SerializeField, Min(1)] private int skill5MaxActiveGiantFireballs = 3;
    [SerializeField, Min(0f)] private float skill5PlatformDurationIncreasePerSecond = 1.1f;

    private const float Skill5HealPercentPerSecondRuntime = 0.02f;

    [Header("Skill Point Backgrounds")]
    [SerializeField] private GameObject[] skillPointBackgrounds = Array.Empty<GameObject>();

    [Header("UI")]
    [SerializeField] private UI_AbyssMageBossHealthBar bossHealthBar;

    [Header("Doors")]
    [SerializeField] private ArenaDoorController[] doors = Array.Empty<ArenaDoorController>();
    [SerializeField] private bool lockDoorsWhenEncounterStarts = true;

    [Header("Player / Camera")]
    [SerializeField] private Player player;
    [SerializeField, Min(0f)] private float bossIntroMoveDistance = 3f;
    [SerializeField, Min(0f)] private float bossIntroMoveSpeedMultiplier = 0.35f;
    [SerializeField] private Component bossCinemachineCamera;
    [SerializeField] private Transform bossTarget;
    [SerializeField, Min(0f)] private float cameraMoveToBossDuration = 0.25f;
    [SerializeField, Min(0f)] private float cameraMoveBackToPlayerDuration = 0.25f;
    [SerializeField, Min(0f)] private float cameraReturnToOriginalDuration = 0.25f;
    [SerializeField, Min(0f)] private float cameraReturnToOriginalSmoothDuration = 1.25f;
    [SerializeField] private AnimationCurve cameraReturnToOriginalCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField, Min(0f)] private float bossCameraOrthoSize = 12f;
    [SerializeField, Min(0f)] private float playerCameraOrthoSizeDuringBoss = 10f;
    [SerializeField] private float bossCameraCenterXOffset = 0f;

    [Header("Trigger")]
    [SerializeField] private bool startOnlyOnce = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Min(0f)] private float clearConfirmDelay = 0.25f;

    private readonly HashSet<Enemy> activeEnemies = new HashSet<Enemy>();
    private BoxCollider2D triggerCollider;
    private Coroutine encounterRoutine;
    private Coroutine abyssFireRevealRoutine;
    private Coroutine bossIntroRoutine;
    private Coroutine bossFloatingPlatformRoutine;
    private Coroutine bossEncounterIntroRoutine;
    private Coroutine bossCameraRoutine;
    private Coroutine abyssFireAudioReductionRoutine;
    private Component bossCameraRuntimeComponent;
    private int bossCameraOriginalPriority;
    private bool bossCameraOriginalPriorityCached;
    private bool bossCameraOriginalActiveSelf;
    private bool bossCameraOriginalActiveSelfCached;
    private Camera bossSceneCamera;
    private Vector3 bossSceneCameraOriginalPosition;
    private Quaternion bossSceneCameraOriginalRotation;
    private float bossSceneCameraOriginalOrthoSize;
    private bool bossSceneCameraStateCached;
    private Transform bossCameraOriginalFollowTarget;
    private bool bossCameraOriginalFollowTargetCached;
    private Transform bossCameraFollowProxy;
    private Enemy spawnedBossInstance;
    private Enemy_AbyssMage bossAbyssMage;
    private Entity_Health bossHealth;
    private IBossSkillPointSource bossSkillPointSource;
    private bool encounterStarted;
    private bool encounterCompleted;
    private int currentBossPhase = 1;
    private int currentSkillPoints;
    private bool abyssFireAudioReduced;
    private bool bossHealthBarBindingDeferred;
    private float passiveSkillPointTimer;
    private float passiveSkillPointChance = 1f;
    private float lastFireballSummonSkillPointTime = float.NegativeInfinity;
    private float lastFireballPlayerInteractionSkillPointTime = float.NegativeInfinity;
    private float lastAbyssMageStunSkillPointTime = float.NegativeInfinity;
    private bool pendingAbyssMageHybridSpellCastRequest;
    private bool pendingAbyssMageEnhancedHybridSpellCastRequest;
    private bool pendingAbyssMageGiantSpellCastRequest;
    private bool pendingAbyssMageSkill4Request;
    private bool skill5PendingStart;
    private int normalSkill2CastCounter;
    private int normalSkill3CastCounter;
    private bool enhancedSkill3Active;
    private Coroutine enhancedSkill3Routine;
    private bool skill5Triggered;
    private bool skill5Active;
    private bool skill5Ending;
    private bool skill5IgnoreNextHealthDrop;
    private int skill5LastObservedHealth;
    private Coroutine skill5Routine;

    public int CurrentBossPhase => currentBossPhase;
    public int CurrentSkillPoints => currentSkillPoints;
    public AbyssMageSkillPointPacePreset AbyssMageSkillPointPace => abyssMageSkillPointPacePreset;
    public string ActiveEnemySummary => BuildActiveEnemySummary();
    public float GiantHorizontalWallClearance => giantHorizontalWallClearance;
    public float GiantFireballColliderRadiusMultiplier => giantFireballColliderRadiusMultiplier;
    public float GiantFireballExplosionRadius => giantFireballExplosionRadius;

    private void Awake()
    {
        Instance = this;
        triggerCollider = GetComponent<BoxCollider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        NormalizeSerializedData();
        ResolveReferences();
        CacheAndBindBossEnemy(ResolveBossEnemy());
        ApplyIdleVisualState();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (Application.isPlaying)
        {
            CacheAndBindBossEnemy(ResolveBossEnemy());
        }
    }

    private void OnValidate()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        NormalizeSerializedData();
        ResolveReferences();
    }

    private void Update()
    {
        TryBeginEncounterFromPlayerPresence();
        HandleAbyssMageForceSpellCastInput();
        HandleAbyssMageForceEnhancedSkill2Input();
        HandleAbyssMageForceGiantSpellCastInput();
        HandleAbyssMageForceEnhancedSkill3Input();
        HandleAbyssMageForceSkill4Input();
        HandleAbyssMageForceSkill5HealthInput();
        TryStartPendingAbyssMageSkill5();
        HandleAbyssMageManualSpellCastInput();
        HandleAbyssMageDecrementSkillPointInput();
        UpdatePassiveAbyssMageSkillPointChance();
        UpdateBossFloatingPlatformMechanics();
    }

    private void OnDisable()
    {
        StopAllInternalRoutines();
        UnbindBossEnemy();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnDestroy()
    {
        StopAllInternalRoutines();
        UnbindBossEnemy();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        if (!encounterStarted)
        {
            BeginEncounter();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!encounterStarted && IsPlayerCollider(other))
        {
            BeginEncounter();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // The boss encounter continues after the player leaves the trigger.
    }

    public void BeginEncounter()
    {
        if (encounterStarted || (encounterCompleted && startOnlyOnce))
        {
            return;
        }

        NormalizeSerializedData();
        ResolveReferences();

        encounterStarted = true;
        encounterCompleted = false;
        currentBossPhase = Mathf.Clamp(currentBossPhase, 1, Mathf.Max(1, bossPhaseCount));
        currentSkillPoints = 0;
        normalSkill2CastCounter = 0;
        normalSkill3CastCounter = 0;
        ResetAbyssMageSkill5State();
        ResetPassiveAbyssMageSkillPointState();
        bossHealthBarBindingDeferred = true;

        AudioManager.instance?.StopBGM();

        SetDoorsLocked(lockDoorsWhenEncounterStarts);
        SetAbyssFireVisibleCount(0);
        PreviewSkillPointBackgroundVisibleCount(0);
        PreviewAbyssPowerVisibleCount(0);
        if (bossFloatingPlatform != null)
        {
            bossFloatingPlatform.ResetTimedPlatforms();
        }

        CacheAndBindBossEnemy(ResolveBossEnemy());
        if (bossEnemy != null)
        {
            bossEnemy.gameObject.SetActive(false);
        }

        CacheBossCameraComponent();
        PrepareBossCameraForIntro();
        BeginPlayerBossIntroMove();
        StartBossCameraIntro();

        if (bossEncounterIntroRoutine != null)
        {
            StopCoroutine(bossEncounterIntroRoutine);
            bossEncounterIntroRoutine = null;
        }

        bossEncounterIntroRoutine = StartCoroutine(RunBossEncounterIntroRoutine());

        if (bossEnemy == null)
        {
            // Spawn is deferred until the intro coroutine reaches the sixth abyss fire.
        }

        if (bossFloatingPlatformRoutine != null)
        {
            StopCoroutine(bossFloatingPlatformRoutine);
            bossFloatingPlatformRoutine = null;
        }
    }

    private void TryBeginEncounterFromPlayerPresence()
    {
        if (encounterStarted || (encounterCompleted && startOnlyOnce))
        {
            return;
        }

        ResolveReferences();

        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<BoxCollider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        if (triggerCollider == null)
        {
            return;
        }

        if (!TryGetPlayerEncounterBounds(out Bounds playerBounds))
        {
            return;
        }

        if (triggerCollider.bounds.Intersects(playerBounds))
        {
            BeginEncounter();
        }
    }

    public void ResetEncounter()
    {
        StopAllInternalRoutines();
        bossAbyssMage?.ResetDamageTeleportTriggerState();
        UnbindBossEnemy();
        bossHealthBarBindingDeferred = false;

        encounterStarted = false;
        encounterCompleted = false;
        currentBossPhase = 1;
        currentSkillPoints = 0;
        normalSkill2CastCounter = 0;
        normalSkill3CastCounter = 0;
        ResetAbyssMageSkill5State();
        ResetPassiveAbyssMageSkillPointState();

        SetDoorsLocked(false);
        SetAbyssFireVisibleCount(0);
        PreviewSkillPointBackgroundVisibleCount(0);
        PreviewAbyssPowerVisibleCount(0);
        ApplyPhaseState(currentBossPhase, preview: true);
        AudioManager.instance?.StartBGM(AudioKey.PlaylistLevels);
        if (bossFloatingPlatform != null)
        {
            bossFloatingPlatform.ResetTimedPlatforms();
        }
        RestoreBossCamera();
        ReleasePlayerAfterBossIntro();

        if (spawnedBossInstance != null)
        {
            if (Application.isPlaying)
            {
                Destroy(spawnedBossInstance.gameObject);
            }
            else
            {
                DestroyImmediate(spawnedBossInstance.gameObject);
            }

            spawnedBossInstance = null;
        }
    }

    public void PreviewBossPhaseState(int phase)
    {
        ApplyPhaseState(phase, preview: true);
    }

    public void PreviewSkillPointBackgroundVisibleCount(int visibleCount)
    {
        SetSkillPointBackgroundVisibleCount(Mathf.Clamp(visibleCount, 0, GetMaxVisualCount()));
    }

    public void PreviewAbyssPowerVisibleCount(int visibleCount)
    {
        SetAbyssPowerVisibleCount(Mathf.Clamp(visibleCount, 0, GetMaxVisualCount()));
    }

    public void SetPlatformHidden()
    {
        ApplyFloatingPlatformState(FloatingPlatformState.Hidden);
    }

    public void SetPlatformBackground1()
    {
        ApplyFloatingPlatformState(FloatingPlatformState.Platform1);
    }

    public void SetPlatformBackground2()
    {
        ApplyFloatingPlatformState(FloatingPlatformState.Platform2);
    }

    public void SetPlatformBackground3()
    {
        ApplyFloatingPlatformState(FloatingPlatformState.Platform3);
    }

    public void SetPlatformBackground4()
    {
        ApplyFloatingPlatformState(FloatingPlatformState.Platform4);
    }

    public void SetPlatformBackground5()
    {
        ApplyFloatingPlatformState(FloatingPlatformState.Platform5);
    }

    public void SetPlatformBackground6()
    {
        ApplyFloatingPlatformState(FloatingPlatformState.Platform6);
    }

    public void SetPlatformSolid()
    {
        ApplyFloatingPlatformState(FloatingPlatformState.AllVisible);
    }

    public bool TryForceTimedPlatformDown(Collider2D hitCollider)
    {
        if (bossFloatingPlatform == null || hitCollider == null)
        {
            return false;
        }

        return bossFloatingPlatform.TryForceTimedPlatformDown(hitCollider);
    }

    private void ResolveReferences()
    {
        if (bossPhaseStates == null || bossPhaseStates.Length != 4)
        {
            BossPhaseState[] newStates = new BossPhaseState[4];
            if (bossPhaseStates != null)
            {
                for (int i = 0; i < Mathf.Min(bossPhaseStates.Length, newStates.Length); i++)
                {
                    newStates[i] = bossPhaseStates[i];
                }
            }
            bossPhaseStates = newStates;
        }

        for (int i = 0; i < bossPhaseStates.Length; i++)
        {
            bossPhaseStates[i] ??= new BossPhaseState();
        }

        if (doors == null)
        {
            doors = Array.Empty<ArenaDoorController>();
        }

        if (abyssFires == null)
        {
            abyssFires = Array.Empty<AbyssFire>();
        }

        if (abyssPowers == null)
        {
            abyssPowers = Array.Empty<AbyssPower>();
        }

        abyssMageMeleeSkillPointChance = Mathf.Clamp(abyssMageMeleeSkillPointChance, 0f, 100f);
        abyssMageFireballSummonSkillPointChance = Mathf.Clamp(abyssMageFireballSummonSkillPointChance, 0f, 100f);
        abyssMageFireballSummonSkillPointCooldown = Mathf.Max(0f, abyssMageFireballSummonSkillPointCooldown);
        abyssMageFireballPlayerInteractionSkillPointCooldown = Mathf.Max(0f, abyssMageFireballPlayerInteractionSkillPointCooldown);
        abyssMagePassiveSkillPointInitialChance = Mathf.Clamp(abyssMagePassiveSkillPointInitialChance, 0f, 100f);
        abyssMagePassiveSkillPointChanceIncrement = Mathf.Max(0f, abyssMagePassiveSkillPointChanceIncrement);
        abyssMagePassiveSkillPointCheckInterval = Mathf.Max(0.1f, abyssMagePassiveSkillPointCheckInterval);
        giantHorizontalWallClearance = Mathf.Max(0f, giantHorizontalWallClearance);
        giantFireballColliderRadiusMultiplier = Mathf.Clamp(giantFireballColliderRadiusMultiplier, 0.1f, 3f);
        normalSkill2CastsBeforeEnhanced = Mathf.Max(1, normalSkill2CastsBeforeEnhanced);
        normalSkill3CastsBeforeEnhanced = Mathf.Max(1, normalSkill3CastsBeforeEnhanced);
        enhancedSkill3GiantFireballCount = Mathf.Max(1, enhancedSkill3GiantFireballCount);
        enhancedSkill3FireballInterval = Mathf.Max(0f, enhancedSkill3FireballInterval);
        enhancedSkill3PlatformRiseStartDelay = Mathf.Max(0f, enhancedSkill3PlatformRiseStartDelay);
        enhancedSkill3PlatformRiseCount = Mathf.Max(0, enhancedSkill3PlatformRiseCount);
        enhancedSkill3PlatformRiseInterval = Mathf.Max(0f, enhancedSkill3PlatformRiseInterval);
        skill4FireballSpawnOffsetStep = Mathf.Max(0f, skill4FireballSpawnOffsetStep);
        skill4FireballSpawnHeightOffset = Mathf.Max(0f, skill4FireballSpawnHeightOffset);
        skill5TriggerHealthPercent = Mathf.Clamp01(skill5TriggerHealthPercent);
        skill5MaxRecoverHealthPercent = Mathf.Clamp01(skill5MaxRecoverHealthPercent);
        skill5HealPercentPerSecond = Mathf.Max(0f, skill5HealPercentPerSecond);
        skill5GiantFireballInterval = Mathf.Max(0.05f, skill5GiantFireballInterval);
        skill5MaxActiveGiantFireballs = Mathf.Max(1, skill5MaxActiveGiantFireballs);
        skill5PlatformDurationIncreasePerSecond = Mathf.Max(0f, skill5PlatformDurationIncreasePerSecond);
        ApplyAbyssMageSkillPointPreset(abyssMageSkillPointPacePreset);

        if (skillPointBackgrounds == null)
        {
            skillPointBackgrounds = Array.Empty<GameObject>();
        }

        if (string.IsNullOrWhiteSpace(playerTag))
        {
            playerTag = "Player";
        }

        if (player == null && !string.IsNullOrWhiteSpace(playerTag))
        {
            GameObject playerObject = GameObject.FindWithTag(playerTag);
            if (playerObject != null)
            {
                player = playerObject.GetComponent<Player>();
            }
        }

        if (player == null)
        {
            player = FindObjectOfType<Player>(true);
        }

        if (bossTarget == null)
        {
            Transform foundTarget = FindDeepChild(transform, "BossTarget") ?? FindDeepChild(transform, "BossCameraTarget");
            if (foundTarget != null)
            {
                bossTarget = foundTarget;
            }
        }

        if (bossEnhancedSkill3CastPoint == null)
        {
            bossEnhancedSkill3CastPoint = FindDeepChild(transform, "BossEnhancedSkill3CastPoint");
        }

        if (bossSkill5CastPoint == null)
        {
            bossSkill5CastPoint = FindDeepChild(transform, "BossSkill5CastPoint");
        }

        if (bossHealthBar == null)
        {
            bossHealthBar = FindObjectOfType<UI_AbyssMageBossHealthBar>(true);
        }

        if (bossEnemy == null)
        {
            Enemy sceneEnemy = GetComponentInChildren<Enemy>(true);
            if (sceneEnemy != null)
            {
                bossEnemy = sceneEnemy;
            }
        }

        if (bossFloatingPlatform == null)
        {
            bossFloatingPlatform = GetComponentInChildren<ArenaBossFloatingPlatformController>(true);
        }

        if (bossCinemachineCamera == null)
        {
            bossCinemachineCamera = FindBossCameraComponent();
        }
    }

    private void NormalizeSerializedData()
    {
        bossPhaseCount = Mathf.Clamp(bossPhaseCount, 1, 4);
        giantFireballExplosionRadius = Mathf.Max(0f, giantFireballExplosionRadius);
        phase2HealthThreshold = Mathf.Clamp01(phase2HealthThreshold);
        phase3HealthThreshold = Mathf.Clamp01(phase3HealthThreshold);
        phase4HealthThreshold = Mathf.Clamp01(phase4HealthThreshold);

        if (phase2HealthThreshold < phase3HealthThreshold)
        {
            phase3HealthThreshold = phase2HealthThreshold;
        }

        if (phase3HealthThreshold < phase4HealthThreshold)
        {
            phase4HealthThreshold = phase3HealthThreshold;
        }

        abyssFireRevealDelay = Mathf.Max(0f, abyssFireRevealDelay);
        abyssMageSkillPointPacePreset = ClampPreset(abyssMageSkillPointPacePreset);
        bossIntroMoveDistance = Mathf.Max(0f, bossIntroMoveDistance);
        bossIntroMoveSpeedMultiplier = Mathf.Max(0f, bossIntroMoveSpeedMultiplier);
        cameraMoveToBossDuration = Mathf.Max(0f, cameraMoveToBossDuration);
        cameraMoveBackToPlayerDuration = Mathf.Max(0f, cameraMoveBackToPlayerDuration);
        cameraReturnToOriginalDuration = Mathf.Max(0f, cameraReturnToOriginalDuration);
        cameraReturnToOriginalSmoothDuration = Mathf.Max(0f, cameraReturnToOriginalSmoothDuration);
        bossCameraOrthoSize = Mathf.Max(0f, bossCameraOrthoSize);
        playerCameraOrthoSizeDuringBoss = Mathf.Max(0f, playerCameraOrthoSizeDuringBoss);
        clearConfirmDelay = Mathf.Max(0f, clearConfirmDelay);

        if (bossPhaseStates == null || bossPhaseStates.Length != 4)
        {
            BossPhaseState[] newStates = new BossPhaseState[4];
            if (bossPhaseStates != null)
            {
                for (int i = 0; i < Mathf.Min(bossPhaseStates.Length, newStates.Length); i++)
                {
                    newStates[i] = bossPhaseStates[i];
                }
            }
            bossPhaseStates = newStates;
        }

        for (int i = 0; i < bossPhaseStates.Length; i++)
        {
            bossPhaseStates[i] ??= new BossPhaseState();
        }
    }

    public void ApplyAbyssMageSkillPointPreset(AbyssMageSkillPointPacePreset preset)
    {
        abyssMageSkillPointPacePreset = ClampPreset(preset);

        switch (abyssMageSkillPointPacePreset)
        {
            case AbyssMageSkillPointPacePreset.Conservative:
                abyssMageMeleeSkillPointChance = 30f;
                abyssMageFireballSummonSkillPointChance = 20f;
                abyssMageFireballSummonSkillPointCooldown = 4f;
                abyssMageFireballPlayerInteractionSkillPointCooldown = 8f;
                abyssMagePassiveSkillPointInitialChance = 1f;
                abyssMagePassiveSkillPointChanceIncrement = 3f;
                abyssMagePassiveSkillPointCheckInterval = 1.25f;
                break;
            case AbyssMageSkillPointPacePreset.Aggressive:
                abyssMageMeleeSkillPointChance = 45f;
                abyssMageFireballSummonSkillPointChance = 40f;
                abyssMageFireballSummonSkillPointCooldown = 3f;
                abyssMageFireballPlayerInteractionSkillPointCooldown = 4f;
                abyssMagePassiveSkillPointInitialChance = 2f;
                abyssMagePassiveSkillPointChanceIncrement = 6f;
                abyssMagePassiveSkillPointCheckInterval = 1f;
                break;
            case AbyssMageSkillPointPacePreset.Standard:
            default:
                abyssMageMeleeSkillPointChance = 40f;
                abyssMageFireballSummonSkillPointChance = 35f;
                abyssMageFireballSummonSkillPointCooldown = 3f;
                abyssMageFireballPlayerInteractionSkillPointCooldown = 6f;
                abyssMagePassiveSkillPointInitialChance = 1f;
                abyssMagePassiveSkillPointChanceIncrement = 5f;
                abyssMagePassiveSkillPointCheckInterval = 1f;
                break;
        }
    }

    private static AbyssMageSkillPointPacePreset ClampPreset(AbyssMageSkillPointPacePreset preset)
    {
        return preset switch
        {
            AbyssMageSkillPointPacePreset.Conservative => AbyssMageSkillPointPacePreset.Conservative,
            AbyssMageSkillPointPacePreset.Standard => AbyssMageSkillPointPacePreset.Standard,
            AbyssMageSkillPointPacePreset.Aggressive => AbyssMageSkillPointPacePreset.Aggressive,
            _ => AbyssMageSkillPointPacePreset.Standard
        };
    }

    private void CacheAndBindBossEnemy(Enemy enemy)
    {
        if (bossEnemy == enemy)
        {
            if (bossHealth == null && bossEnemy != null)
            {
                bossHealth = ResolveBossHealth(bossEnemy);
            }

            if (bossEnemy != null)
            {
                RegisterBossEnemy(bossEnemy);
            }

            return;
        }

        UnbindBossEnemy();
        bossEnemy = enemy;

        if (bossEnemy != null)
        {
            RegisterBossEnemy(bossEnemy);
        }
    }

    private void RegisterBossEnemy(Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        activeEnemies.Add(enemy);

        enemy.OnDied -= HandleBossDied;
        enemy.OnDied += HandleBossDied;

        bossHealth = ResolveBossHealth(enemy);
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged -= HandleBossHealthChanged;
            bossHealth.OnHealthChanged += HandleBossHealthChanged;
            SyncBossEnemyFromHealth(enemy, bossHealth);
        }

        if (enemy is Enemy_AbyssMage abyssMage)
        {
            bossAbyssMage = abyssMage;
            bossSkillPointSource = abyssMage;
            bossAbyssMage.ResetDamageTeleportTriggerState();

            bossSkillPointSource.MeleeAttackCompleted -= HandleBossMeleeAttackCompleted;
            bossSkillPointSource.MeleeAttackCompleted += HandleBossMeleeAttackCompleted;

            bossAbyssMage.FireballSummoned -= HandleBossFireballSummoned;
            bossAbyssMage.FireballSummoned += HandleBossFireballSummoned;
            bossAbyssMage.FireballPlayerInteracted -= HandleBossFireballPlayerInteracted;
            bossAbyssMage.FireballPlayerInteracted += HandleBossFireballPlayerInteracted;

            ResetPassiveAbyssMageSkillPointState();
        }
        else if (enemy is IBossSkillPointSource skillPointSource)
        {
            bossSkillPointSource = skillPointSource;
            bossSkillPointSource.MeleeAttackCompleted -= HandleBossMeleeAttackCompleted;
            bossSkillPointSource.MeleeAttackCompleted += HandleBossMeleeAttackCompleted;
        }

        if (!bossHealthBarBindingDeferred && bossHealthBar != null && bossHealth != null)
        {
            bossHealthBar.BindBossHealth(bossHealth);
        }
    }

    private static void SyncBossEnemyFromHealth(Enemy enemy, Entity_Health health)
    {
        if (enemy == null || health == null)
        {
            return;
        }

        int desiredMaxHealth = Mathf.Max(1, health.MaxHealth);
        if (enemy.MaxHealth == desiredMaxHealth)
        {
            return;
        }

        enemy.SetMaxHealth(desiredMaxHealth);
    }

    private void UnbindBossEnemy()
    {
        pendingAbyssMageHybridSpellCastRequest = false;
        pendingAbyssMageEnhancedHybridSpellCastRequest = false;
        pendingAbyssMageGiantSpellCastRequest = false;

        if (bossAbyssMage != null)
        {
            bossAbyssMage.FireballSummoned -= HandleBossFireballSummoned;
            bossAbyssMage.FireballPlayerInteracted -= HandleBossFireballPlayerInteracted;
        }

        if (bossEnemy != null)
        {
            bossEnemy.OnDied -= HandleBossDied;
        }

        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged -= HandleBossHealthChanged;
        }

        if (bossSkillPointSource != null)
        {
            bossSkillPointSource.MeleeAttackCompleted -= HandleBossMeleeAttackCompleted;
        }

        bossAbyssMage = null;
        bossSkillPointSource = null;
        bossHealth = null;
        ResetPassiveAbyssMageSkillPointState();

        if (bossEnemy != null)
        {
            activeEnemies.Remove(bossEnemy);
        }
    }

    private Enemy ResolveBossEnemy()
    {
        if (bossEnemy != null)
        {
            return bossEnemy;
        }

        if (spawnedBossInstance != null)
        {
            return spawnedBossInstance;
        }

        return GetComponentInChildren<Enemy>(true);
    }

    private Entity_Health ResolveBossHealth(Enemy enemy)
    {
        if (enemy == null)
        {
            return null;
        }

        Entity_Health health = enemy.GetComponent<Entity_Health>();
        if (health != null)
        {
            return health;
        }

        return enemy.GetComponentInChildren<Entity_Health>(true);
    }

    private void SpawnBossFromPrefab()
    {
        if (bossPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = bossTarget != null ? bossTarget.position : transform.position;
        if (bossIntroMoveDistance > 0f)
        {
            spawnPosition += Vector3.right * bossIntroMoveDistance;
        }

        GameObject bossObject = Instantiate(bossPrefab, spawnPosition, Quaternion.identity, transform);
        bossObject.name = bossPrefab.name;

        spawnedBossInstance = bossObject.GetComponentInChildren<Enemy>(true);
        if (spawnedBossInstance == null)
        {
            spawnedBossInstance = bossObject.GetComponent<Enemy>();
        }

        bossEnemy = spawnedBossInstance;
        if (bossEnemy != null)
        {
            RegisterBossEnemy(bossEnemy);
        }

        if (!bossHealthBarBindingDeferred && bossEnemy != null && bossHealthBar != null)
        {
            bossHealthBar.BindBossHealth(ResolveBossHealth(bossEnemy));
        }
    }

    private void BeginPlayerBossIntroMove()
    {
        if (player == null)
        {
            return;
        }

        player.SetMovementLocked(true);

        if (bossTarget == null || bossIntroMoveDistance <= 0f)
        {
            return;
        }

        player.BeginBossIntroMoveTowards(bossTarget.position.x, bossIntroMoveDistance, bossIntroMoveSpeedMultiplier);
    }

    private IEnumerator RunBossEncounterIntroRoutine()
    {
        ShowBossHealthIntroProgress(0f);
        yield return RevealAbyssFiresAndSpawnBossRoutine();

        ShowBossHealthIntroProgress(100f);
        bossHealthBarBindingDeferred = false;
        BindBossHealthBarToCurrentBoss();

        StartBossCameraReturnToPlayer();

        ApplyPhaseState(currentBossPhase, preview: false);
    }

    private IEnumerator RevealAbyssFiresAndSpawnBossRoutine()
    {
        int abyssFireCount = abyssFires != null ? abyssFires.Length : 0;
        float revealStepDuration = Mathf.Max(0f, abyssFireRevealDelay);
        float totalIntroDuration = abyssFireCount > 0 ? abyssFireCount * revealStepDuration : 0f;
        float elapsed = 0f;

        if (abyssFireCount > 0)
        {
            for (int i = 0; i < abyssFireCount; i++)
            {
                SetAbyssFireVisible(i, true, true);
                if (i == 5)
                {
                    AudioManager.instance?.StartBGM(AudioKey.PlaylistBossBattle);
                }
                if (revealStepDuration > 0f)
                {
                    float stepEnd = elapsed + revealStepDuration;
                    while (elapsed < stepEnd)
                    {
                        elapsed += Time.deltaTime;
                        ShowBossHealthIntroProgress(GetBossHealthIntroPercent(elapsed, totalIntroDuration));
                        yield return null;
                    }

                    elapsed = stepEnd;
                }
                else
                {
                    ShowBossHealthIntroProgress(GetBossHealthIntroPercent(i + 1, abyssFireCount));
                    yield return null;
                }
            }
        }

        if (abyssFireCount <= 5)
        {
            AudioManager.instance?.StartBGM(AudioKey.PlaylistBossBattle);
        }

        ActivateBossAfterAbyssFires();
    }

    private void ActivateBossAfterAbyssFires()
    {
        if (bossEnemy == null)
        {
            SpawnBossFromPrefab();
            return;
        }

        if (bossEnemy.gameObject != null && !bossEnemy.gameObject.activeSelf)
        {
            bossEnemy.gameObject.SetActive(true);
            RegisterBossEnemy(bossEnemy);
        }
    }

    private void StartBossCameraIntro()
    {
        Component cameraComponent = GetBossCameraComponent();
        if (cameraComponent == null)
        {
            return;
        }

        if (bossCameraRoutine != null)
        {
            StopCoroutine(bossCameraRoutine);
            bossCameraRoutine = null;
        }

        bossCameraRoutine = StartCoroutine(TransitionBossCameraRoutine(
            cameraComponent,
            GetBossCameraIntroTargetPosition(),
            bossCameraOrthoSize,
            cameraMoveToBossDuration));
    }

    private void StartBossCameraReturnToPlayer()
    {
        Component cameraComponent = GetBossCameraComponent();
        if (cameraComponent == null)
        {
            ReleasePlayerAfterBossIntro();
            return;
        }

        if (bossCameraRoutine != null)
        {
            StopCoroutine(bossCameraRoutine);
            bossCameraRoutine = null;
        }

        bossCameraRoutine = StartCoroutine(TransitionBossCameraRoutine(
            cameraComponent,
            GetPlayerCameraTargetPosition(),
            playerCameraOrthoSizeDuringBoss,
            cameraMoveBackToPlayerDuration,
            onComplete: () =>
            {
                if (player != null)
                {
                    SetCameraFollowTarget(cameraComponent, player.transform);
                }
                else if (bossCameraOriginalFollowTargetCached)
                {
                    SetCameraFollowTarget(cameraComponent, bossCameraOriginalFollowTarget);
                }

                ReleasePlayerAfterBossIntro();
            }));
    }

    private IEnumerator TransitionBossCameraRoutine(Component cameraComponent, Vector3 targetPosition, float targetOrthoSize, float duration, Action onComplete = null)
    {
        if (cameraComponent == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        CacheBossCameraState(cameraComponent);
        EnsureBossCameraProxy();

        Transform followTarget = GetCameraFollowTarget(cameraComponent);
        Vector3 startPosition = followTarget != null ? followTarget.position : cameraComponent.transform.position;
        float startOrthoSize = GetCameraOrthoSize(cameraComponent);
        float effectiveDuration = duration <= 0f ? 0f : Mathf.Max(0.01f, duration);

        if (bossCameraFollowProxy != null)
        {
            bossCameraFollowProxy.position = startPosition;
            SetCameraFollowTarget(cameraComponent, bossCameraFollowProxy);
        }

        if (effectiveDuration <= 0f)
        {
            if (bossCameraFollowProxy != null)
            {
                bossCameraFollowProxy.position = new Vector3(targetPosition.x, startPosition.y, startPosition.z);
            }

            SetCameraOrthoSize(cameraComponent, targetOrthoSize);
            onComplete?.Invoke();
            yield break;
        }

        float elapsed = 0f;
        float startX = startPosition.x;
        float fixedY = startPosition.y;
        float fixedZ = startPosition.z;

        while (elapsed < effectiveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / effectiveDuration);
            float x = Mathf.Lerp(startX, targetPosition.x, t);
            if (bossCameraFollowProxy != null)
            {
                bossCameraFollowProxy.position = new Vector3(x, fixedY, fixedZ);
            }

            SetCameraOrthoSize(cameraComponent, Mathf.Lerp(startOrthoSize, targetOrthoSize, t));
            yield return null;
        }

        if (bossCameraFollowProxy != null)
        {
            bossCameraFollowProxy.position = new Vector3(targetPosition.x, fixedY, fixedZ);
        }

        SetCameraOrthoSize(cameraComponent, targetOrthoSize);
        onComplete?.Invoke();
    }

    private void CacheBossCameraComponent()
    {
        Component resolvedCamera = bossCinemachineCamera != null ? bossCinemachineCamera : FindBossCameraComponent();
        if (bossCameraRuntimeComponent != resolvedCamera)
        {
            bossCameraOriginalFollowTargetCached = false;
            bossSceneCameraStateCached = false;
        }

        bossCameraRuntimeComponent = resolvedCamera;
    }

    private void PrepareBossCameraForIntro()
    {
        Component cameraComponent = GetBossCameraComponent();
        if (cameraComponent == null)
        {
            return;
        }

        CacheBossCameraState(cameraComponent);
        EnsureBossCameraProxy();

        Transform followTarget = GetCameraFollowTarget(cameraComponent);
        Vector3 startPosition = followTarget != null ? followTarget.position : cameraComponent.transform.position;
        if (bossCameraFollowProxy != null)
        {
            bossCameraFollowProxy.position = startPosition;
        }

        SetCameraFollowTarget(cameraComponent, bossCameraFollowProxy);
    }

    private void RestoreBossCamera()
    {
        if (bossCameraRoutine != null)
        {
            StopCoroutine(bossCameraRoutine);
            bossCameraRoutine = null;
        }

        Component cameraComponent = GetBossCameraComponent();
        if (cameraComponent == null)
        {
            return;
        }

        if (bossCameraOriginalFollowTargetCached)
        {
            SetCameraFollowTarget(cameraComponent, bossCameraOriginalFollowTarget);
        }
        else if (player != null)
        {
            SetCameraFollowTarget(cameraComponent, player.transform);
        }

        if (bossSceneCameraStateCached)
        {
            SetCameraOrthoSize(cameraComponent, bossSceneCameraOriginalOrthoSize);
        }
    }

    private void CacheBossCameraState(Component cameraComponent)
    {
        if (cameraComponent == null)
        {
            return;
        }

        if (!bossCameraOriginalFollowTargetCached)
        {
            bossCameraOriginalFollowTarget = GetCameraFollowTarget(cameraComponent);
            bossCameraOriginalFollowTargetCached = true;
        }

        if (!bossSceneCameraStateCached)
        {
            bossSceneCameraOriginalOrthoSize = GetCameraOrthoSize(cameraComponent);
            bossSceneCameraStateCached = true;
        }
    }

    private Component FindBossCameraComponent()
    {
        Component[] components = UnityEngine.Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Component bestMatch = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null || component == this)
            {
                continue;
            }

            GameObject go = component.gameObject;
            if (go == null || go.scene != gameObject.scene)
            {
                continue;
            }

            string typeName = component.GetType().Name;
            if (typeName.IndexOf("Cinemachine", StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            int score = 0;
            if (typeName.IndexOf("VirtualCamera", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 10;
            }

            if (go.name.IndexOf("Boss", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 20;
            }

            if (go.name.IndexOf("Camera", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 5;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = component;
            }
        }

        return bestMatch;
    }

    private Component GetBossCameraComponent()
    {
        if (bossCameraRuntimeComponent != null)
        {
            return bossCameraRuntimeComponent;
        }

        if (bossCinemachineCamera != null)
        {
            bossCameraRuntimeComponent = bossCinemachineCamera;
            return bossCameraRuntimeComponent;
        }

        bossCameraRuntimeComponent = FindBossCameraComponent();
        return bossCameraRuntimeComponent;
    }

    private void EnsureBossCameraProxy()
    {
        if (bossCameraFollowProxy != null)
        {
            return;
        }

        GameObject proxyObject = new GameObject("BossCameraFollowProxy");
        proxyObject.hideFlags = HideFlags.HideAndDontSave;
        proxyObject.transform.SetParent(transform, true);
        bossCameraFollowProxy = proxyObject.transform;
    }

    private Vector3 GetBossCameraIntroTargetPosition()
    {
        Vector3 targetPosition = GetBossCameraTargetPosition();
        return targetPosition;
    }

    private Vector3 GetBossCameraTargetPosition()
    {
        Vector3 basePosition = bossTarget != null ? bossTarget.position : transform.position;
        basePosition.x += bossCameraCenterXOffset;
        return basePosition;
    }

    private Vector3 GetPlayerCameraTargetPosition()
    {
        if (player != null)
        {
            Vector3 playerPosition = player.transform.position;
            playerPosition.y = GetCurrentCameraHeight();
            return playerPosition;
        }

        return new Vector3(GetCurrentCameraX(), GetCurrentCameraHeight(), GetCurrentCameraZ());
    }

    private float GetCurrentCameraX()
    {
        Transform followTarget = GetCameraFollowTarget(GetBossCameraComponent());
        if (followTarget != null)
        {
            return followTarget.position.x;
        }

        Component cameraComponent = GetBossCameraComponent();
        return cameraComponent != null ? cameraComponent.transform.position.x : 0f;
    }

    private float GetCurrentCameraY()
    {
        Transform followTarget = GetCameraFollowTarget(GetBossCameraComponent());
        if (followTarget != null)
        {
            return followTarget.position.y;
        }

        Component cameraComponent = GetBossCameraComponent();
        return cameraComponent != null ? cameraComponent.transform.position.y : 0f;
    }

    private float GetCurrentCameraHeight()
    {
        return GetCurrentCameraY();
    }

    private float GetCurrentCameraZ()
    {
        Component cameraComponent = GetBossCameraComponent();
        return cameraComponent != null ? cameraComponent.transform.position.z : 0f;
    }

    private void ReleasePlayerAfterBossIntro()
    {
        CancelPlayerBossIntroMove();
        if (player != null)
        {
            player.SetMovementLocked(false);
        }
    }

    private void CancelPlayerBossIntroMove()
    {
        if (player == null)
        {
            return;
        }

        player.CancelBossIntroMove();
    }

    private Transform GetCameraFollowTarget(Component cameraComponent)
    {
        if (cameraComponent == null)
        {
            return null;
        }

        Type type = cameraComponent.GetType();
        PropertyInfo followProperty = type.GetProperty("Follow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (followProperty != null && followProperty.PropertyType == typeof(Transform) && followProperty.CanRead)
        {
            object value = followProperty.GetValue(cameraComponent);
            return value as Transform;
        }

        FieldInfo followField = type.GetField("m_Follow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (followField != null && followField.FieldType == typeof(Transform))
        {
            object value = followField.GetValue(cameraComponent);
            return value as Transform;
        }

        return null;
    }

    private void SetCameraFollowTarget(Component cameraComponent, Transform target)
    {
        if (cameraComponent == null)
        {
            return;
        }

        Type type = cameraComponent.GetType();
        PropertyInfo followProperty = type.GetProperty("Follow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (followProperty != null && followProperty.PropertyType == typeof(Transform) && followProperty.CanWrite)
        {
            followProperty.SetValue(cameraComponent, target);
            return;
        }

        FieldInfo followField = type.GetField("m_Follow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (followField != null && followField.FieldType == typeof(Transform))
        {
            followField.SetValue(cameraComponent, target);
        }
    }

    private float GetCameraOrthoSize(Component cameraComponent)
    {
        if (cameraComponent == null)
        {
            return 0f;
        }

        Type type = cameraComponent.GetType();
        FieldInfo lensField = type.GetField("m_Lens", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (lensField != null)
        {
            object lens = lensField.GetValue(cameraComponent);
            if (TryGetOrthographicSize(lens, out float size))
            {
                return size;
            }
        }

        Camera camera = cameraComponent as Camera;
        return camera != null && camera.orthographic ? camera.orthographicSize : 0f;
    }

    private void SetCameraOrthoSize(Component cameraComponent, float size)
    {
        if (cameraComponent == null)
        {
            return;
        }

        Type type = cameraComponent.GetType();
        FieldInfo lensField = type.GetField("m_Lens", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (lensField != null)
        {
            object lens = lensField.GetValue(cameraComponent);
            if (TrySetOrthographicSize(lens, size, out object updatedLens))
            {
                lensField.SetValue(cameraComponent, updatedLens);
                return;
            }
        }

        Camera camera = cameraComponent as Camera;
        if (camera != null && camera.orthographic)
        {
            camera.orthographicSize = size;
        }
    }

    private static bool TryGetOrthographicSize(object lens, out float size)
    {
        size = 0f;
        if (lens == null)
        {
            return false;
        }

        Type type = lens.GetType();
        FieldInfo field = type.GetField("OrthographicSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(float))
        {
            object value = field.GetValue(lens);
            if (value is float floatValue)
            {
                size = floatValue;
                return true;
            }
        }

        PropertyInfo property = type.GetProperty("OrthographicSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null && property.PropertyType == typeof(float) && property.CanRead)
        {
            object value = property.GetValue(lens);
            if (value is float floatValue)
            {
                size = floatValue;
                return true;
            }
        }

        return false;
    }

    private static bool TrySetOrthographicSize(object lens, float size, out object updatedLens)
    {
        updatedLens = lens;
        if (lens == null)
        {
            return false;
        }

        Type type = lens.GetType();
        FieldInfo field = type.GetField("OrthographicSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(float))
        {
            field.SetValue(updatedLens, size);
            return true;
        }

        PropertyInfo property = type.GetProperty("OrthographicSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null && property.PropertyType == typeof(float) && property.CanWrite)
        {
            property.SetValue(updatedLens, size);
            return true;
        }

        return false;
    }

    private void HandleBossHealthChanged(Entity_Health health)
    {
        if (health == null)
        {
            return;
        }

        int nextPhase = ResolveBossPhase(health.CurrentHealth, health.MaxHealth);
        if (nextPhase != currentBossPhase)
        {
            currentBossPhase = nextPhase;
            ApplyPhaseState(currentBossPhase, preview: false);
        }

        HandleAbyssMageSkill5HealthChanged(health);
    }

    private void HandleBossDied(Enemy enemy)
    {
        ResetAbyssMageSkill5State();
        bossAbyssMage?.ResetDamageTeleportTriggerState();
        encounterCompleted = true;
        SetDoorsLocked(false);
        RestoreBossCamera();
        ReleasePlayerAfterBossIntro();
        AudioManager.instance?.StartBGM(AudioKey.PlaylistLevels);

        if (bossHealthBar != null)
        {
            bossHealthBar.Hide();
        }
    }

    private void HandleBossMeleeAttackCompleted()
    {
        if (bossAbyssMage != null)
        {
            return;
        }

        AddSkillPoint(1);
    }

    public void NotifyAbyssMageMeleeAttackStarted(Enemy_AbyssMage mage)
    {
        if (mage == null || mage != bossAbyssMage)
        {
            return;
        }

        TryAwardSkillPointFromMeleeAttack();
    }

    private void HandleBossFireballSummoned()
    {
        TryAwardSkillPointFromFireballSummon();
    }

    private void HandleBossFireballPlayerInteracted()
    {
        TryAwardSkillPointFromFireballPlayerInteraction();
    }

    private void BindBossHealthBarToCurrentBoss()
    {
        if (bossHealthBar == null)
        {
            return;
        }

        bossHealth = ResolveBossHealth(bossEnemy);
        if (bossHealth != null)
        {
            bossHealthBar.BindBossHealth(bossHealth);
        }
    }

    private void ShowBossHealthIntroProgress(float progressPercent)
    {
        if (bossHealthBar == null)
        {
            return;
        }

        bossHealthBar.SetIntroProgressPercent(Mathf.Clamp(progressPercent, 0f, 100f));
    }

    private float GetBossHealthIntroPercent(float elapsed, float totalDuration)
    {
        if (totalDuration <= 0f)
        {
            return 100f;
        }

        return Mathf.Clamp01(elapsed / totalDuration) * 100f;
    }

    private void HandleAbyssMageManualSpellCastInput()
    {
        bool pressedByInputSystem = Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame;
        bool pressedByLegacyInput = Input.GetKeyDown(KeyCode.I);

        if (!pressedByInputSystem && !pressedByLegacyInput)
        {
            return;
        }

        TryForceAbyssMageHybridSpellCast();
    }

    private void HandleAbyssMageForceSpellCastInput()
    {
        bool pressedByInputSystem = Keyboard.current != null && Keyboard.current.oKey.wasPressedThisFrame;
        bool pressedByLegacyInput = Input.GetKeyDown(KeyCode.O);

        if (!pressedByInputSystem && !pressedByLegacyInput)
        {
            return;
        }

        string bossName = bossAbyssMage != null ? bossAbyssMage.name : "null";
        LogAbyssMageDebug($"O pressed. currentSkillPoints={currentSkillPoints}, boss={bossName}");
        TryForceAbyssMageHybridSpellCast(true);
    }

    private void HandleAbyssMageForceEnhancedSkill2Input()
    {
        bool pressedByInputSystem = Keyboard.current != null
            && (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame);
        bool pressedByLegacyInput = Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);

        if (!pressedByInputSystem && !pressedByLegacyInput)
        {
            return;
        }

        string bossName = bossAbyssMage != null ? bossAbyssMage.name : "null";
        LogAbyssMageDebug($"2 pressed. Force enhanced skill2 requested. boss={bossName}");
        TryForceAbyssMageEnhancedHybridSpellCast(true);
    }

    private void HandleAbyssMageForceGiantSpellCastInput()
    {
        bool pressedByInputSystem = Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame;
        bool pressedByLegacyInput = Input.GetKeyDown(KeyCode.L);

        if (!pressedByInputSystem && !pressedByLegacyInput)
        {
            return;
        }

        string bossName = bossAbyssMage != null ? bossAbyssMage.name : "null";
        LogAbyssMageDebug($"L pressed. currentSkillPoints={currentSkillPoints}, boss={bossName}");
        TryForceAbyssMageGiantSpellCast(true);
    }

    private void HandleAbyssMageForceEnhancedSkill3Input()
    {
        bool pressedByInputSystem = Keyboard.current != null
            && (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame);
        bool pressedByLegacyInput = Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);

        if (!pressedByInputSystem && !pressedByLegacyInput)
        {
            return;
        }

        string bossName = bossAbyssMage != null ? bossAbyssMage.name : "null";
        LogAbyssMageDebug($"3 pressed. Force enhanced skill3 requested. boss={bossName}");
        TryStartEnhancedAbyssMageSkill3(true);
    }

    private void HandleAbyssMageForceSkill4Input()
    {
        bool pressedByInputSystem = Keyboard.current != null
            && (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame);
        bool pressedByLegacyInput = Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4);

        if (!pressedByInputSystem && !pressedByLegacyInput)
        {
            return;
        }

        string bossName = bossAbyssMage != null ? bossAbyssMage.name : "null";
        LogAbyssMageSkill4Debug($"4 pressed. inputSystem={pressedByInputSystem}, legacy={pressedByLegacyInput}, cachedBoss={bossName}, encounterStarted={encounterStarted}, completed={encounterCompleted}");
        TryForceAbyssMageSkill4(true);
    }

    private void HandleAbyssMageDecrementSkillPointInput()
    {
        bool pressedByInputSystem = Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame;
        bool pressedByLegacyInput = Input.GetKeyDown(KeyCode.K);

        if (!pressedByInputSystem && !pressedByLegacyInput)
        {
            return;
        }

        if (TrySpendSkillPoints(1))
        {
            LogAbyssMageDebug($"K pressed. currentSkillPoints={currentSkillPoints}, boss={(bossAbyssMage != null ? bossAbyssMage.name : "null")}");
            return;
        }

        LogAbyssMageDebug($"K pressed but no skill point could be spent. currentSkillPoints={currentSkillPoints}, boss={(bossAbyssMage != null ? bossAbyssMage.name : "null")}");
    }

    private void AddSkillPoint(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int maxSkillPoints = GetMaxVisualCount();
        currentSkillPoints = Mathf.Clamp(currentSkillPoints + amount, 0, maxSkillPoints);
        SetSkillPointBackgroundVisibleCount(currentSkillPoints);
        RevealRandomAbyssPower();
    }

    private bool TrySpendSkillPoints(int amount)
    {
        if (skill5Active)
        {
            return false;
        }

        if (amount <= 0 || currentSkillPoints < amount)
        {
            return false;
        }

        currentSkillPoints -= amount;
        SetSkillPointBackgroundVisibleCount(currentSkillPoints);
        ConsumeRandomAbyssPowers(amount);
        return true;
    }

    public bool TryPrepareAbyssMageSpecialAttack(Enemy_AbyssMage mage, out Enemy_AbyssMage.AbyssMageSpecialAttackType attackType)
    {
        attackType = Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill1Fireball;

        if (!CanProcessAbyssMageSkillPointSource() || mage == null || mage != bossAbyssMage)
        {
            return false;
        }

        if (currentSkillPoints >= 6 && TrySpendSkillPoints(6))
        {
            attackType = Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill4SixGiantAbyssFireballs;
            return true;
        }

        if (currentSkillPoints >= 3)
        {
            if (TryRollPercent(50f) && TrySpendSkillPoints(2))
            {
                bool useEnhancedSkill2 = ShouldReplaceAbyssMageSkill2WithEnhanced(mage);
                attackType = useEnhancedSkill2
                    ? Enemy_AbyssMage.AbyssMageSpecialAttackType.EnhancedSkill2MixedFireball
                    : Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill2MixedFireball;

                if (useEnhancedSkill2)
                {
                    normalSkill2CastCounter = 0;
                }
                else
                {
                    normalSkill2CastCounter = Mathf.Min(normalSkill2CastsBeforeEnhanced, normalSkill2CastCounter + 1);
                }

                return true;
            }
        }

        if (currentSkillPoints >= 2 && !mage.IsSkill3OnCooldown && TrySpendSkillPoints(1))
        {
            attackType = Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill3GiantAbyssFireball;
        }

        return true;
    }

    private bool ShouldReplaceAbyssMageSkill2WithEnhanced(Enemy_AbyssMage mage)
    {
        return CanProcessAbyssMageSkillPointSource()
            && mage != null
            && mage == bossAbyssMage
            && normalSkill2CastCounter >= normalSkill2CastsBeforeEnhanced;
    }

    private void NotifyAbyssMageNormalSkill2Cast(Enemy_AbyssMage mage)
    {
        if (!CanProcessAbyssMageSkillPointSource() || mage == null || mage != bossAbyssMage)
        {
            return;
        }

        normalSkill2CastCounter = Mathf.Min(normalSkill2CastsBeforeEnhanced, normalSkill2CastCounter + 1);
    }

    public bool TryForceAbyssMageHybridSpellCast(bool ignoreRestrictions = false)
    {
        if (!CanProcessAbyssMageSkillPointSource() || bossAbyssMage == null)
        {
            LogAbyssMageDebug($"Force hybrid failed. CanProcess={CanProcessAbyssMageSkillPointSource()}, bossNull={(bossAbyssMage == null)}");
            return false;
        }

        if (ignoreRestrictions)
        {
            pendingAbyssMageHybridSpellCastRequest = false;
            pendingAbyssMageEnhancedHybridSpellCastRequest = false;
            pendingAbyssMageGiantSpellCastRequest = false;
            pendingAbyssMageSkill4Request = false;
            LogAbyssMageDebug("Force hybrid requested. Triggering boss ForceSpecialAttack(skill2).");
            bossAbyssMage.ForceSpecialAttack(Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill2MixedFireball);
            return true;
        }

        if (currentSkillPoints < 3)
        {
            LogAbyssMageDebug($"Hybrid request rejected. currentSkillPoints={currentSkillPoints}");
            return false;
        }

        pendingAbyssMageHybridSpellCastRequest = true;
        pendingAbyssMageEnhancedHybridSpellCastRequest = false;
        pendingAbyssMageGiantSpellCastRequest = false;
        pendingAbyssMageSkill4Request = false;

        if (!bossAbyssMage.CanEnterSpellCastState || !bossAbyssMage.CanSpellCast)
        {
            return true;
        }

        return bossAbyssMage.TryStartSpecialAttack(Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill2MixedFireball);
    }

    public bool TryForceAbyssMageEnhancedHybridSpellCast(bool ignoreRestrictions = false)
    {
        if (!CanProcessAbyssMageSkillPointSource() || bossAbyssMage == null)
        {
            LogAbyssMageDebug($"Force enhanced hybrid failed. CanProcess={CanProcessAbyssMageSkillPointSource()}, bossNull={(bossAbyssMage == null)}");
            return false;
        }

        if (ignoreRestrictions)
        {
            pendingAbyssMageHybridSpellCastRequest = false;
            pendingAbyssMageEnhancedHybridSpellCastRequest = false;
            pendingAbyssMageGiantSpellCastRequest = false;
            pendingAbyssMageSkill4Request = false;
            normalSkill2CastCounter = 0;
            LogAbyssMageDebug("Force enhanced hybrid requested. Triggering boss ForceSpecialAttack(enhanced skill2).");
            bossAbyssMage.ForceSpecialAttack(Enemy_AbyssMage.AbyssMageSpecialAttackType.EnhancedSkill2MixedFireball);
            return true;
        }

        normalSkill2CastCounter = 0;
        pendingAbyssMageHybridSpellCastRequest = false;
        pendingAbyssMageEnhancedHybridSpellCastRequest = true;
        pendingAbyssMageGiantSpellCastRequest = false;
        pendingAbyssMageSkill4Request = false;

        if (!bossAbyssMage.CanEnterSpellCastState || !bossAbyssMage.CanSpellCast)
        {
            return true;
        }

        return bossAbyssMage.TryStartSpecialAttack(Enemy_AbyssMage.AbyssMageSpecialAttackType.EnhancedSkill2MixedFireball);
    }

    public bool TryForceAbyssMageGiantSpellCast(bool ignoreRestrictions = false)
    {
        if (!CanProcessAbyssMageSkillPointSource() || bossAbyssMage == null)
        {
            LogAbyssMageDebug($"Force giant failed. CanProcess={CanProcessAbyssMageSkillPointSource()}, bossNull={(bossAbyssMage == null)}");
            return false;
        }

        if (ignoreRestrictions)
        {
            if (bossAbyssMage.IsSkill3OnCooldown)
            {
                LogAbyssMageDebug("Force giant rejected. Skill3 is on cooldown.");
                return false;
            }

            pendingAbyssMageHybridSpellCastRequest = false;
            pendingAbyssMageEnhancedHybridSpellCastRequest = false;
            pendingAbyssMageGiantSpellCastRequest = false;
            pendingAbyssMageSkill4Request = false;
            LogAbyssMageDebug("Force giant requested. Triggering boss ForceSpecialAttack(skill3).");
            bossAbyssMage.ForceSpecialAttack(Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill3GiantAbyssFireball);
            return true;
        }

        if (bossAbyssMage.IsSkill3OnCooldown)
        {
            LogAbyssMageDebug("Giant request rejected. Skill3 is on cooldown.");
            return false;
        }

        if (currentSkillPoints < 2)
        {
            LogAbyssMageDebug($"Giant request rejected. currentSkillPoints={currentSkillPoints}");
            return false;
        }

        pendingAbyssMageGiantSpellCastRequest = true;
        pendingAbyssMageHybridSpellCastRequest = false;
        pendingAbyssMageSkill4Request = false;

        if (!bossAbyssMage.CanEnterSpellCastState || !bossAbyssMage.CanSpellCast)
        {
            return true;
        }

        return bossAbyssMage.TryStartSpecialAttack(Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill3GiantAbyssFireball);
    }

    public bool TryForceAbyssMageSkill4(bool ignoreRestrictions = false)
    {
        ResolveAbyssMageForSkill4Debug();

        if (skill5Active)
        {
            LogAbyssMageSkill4Debug("Force failed because skill5 is active.");
            return false;
        }

        if ((!ignoreRestrictions && !CanProcessAbyssMageSkillPointSource()) || bossAbyssMage == null)
        {
            LogAbyssMageSkill4Debug($"Force failed. CanProcess={CanProcessAbyssMageSkillPointSource()}, bossNull={(bossAbyssMage == null)}");
            return false;
        }

        if (ignoreRestrictions)
        {
            if (bossAbyssMage.gameObject == null || !bossAbyssMage.gameObject.activeInHierarchy || bossAbyssMage.IsDead)
            {
                LogAbyssMageSkill4Debug($"Force failed because boss is inactive or dead. active={(bossAbyssMage.gameObject != null && bossAbyssMage.gameObject.activeInHierarchy)}, dead={bossAbyssMage.IsDead}");
                return false;
            }

            pendingAbyssMageHybridSpellCastRequest = false;
            pendingAbyssMageEnhancedHybridSpellCastRequest = false;
            pendingAbyssMageGiantSpellCastRequest = false;
            pendingAbyssMageSkill4Request = false;
            LogAbyssMageSkill4Debug("Force accepted. Spawning skill4 directly.");
            StartCoroutine(RunAbyssMageSkill4Routine(ignoreProcessingGate: true));
            return true;
        }

        if (currentSkillPoints < 6)
        {
            LogAbyssMageDebug($"Skill4 request rejected. currentSkillPoints={currentSkillPoints}");
            return false;
        }

        pendingAbyssMageSkill4Request = true;
        pendingAbyssMageHybridSpellCastRequest = false;
        pendingAbyssMageEnhancedHybridSpellCastRequest = false;
        pendingAbyssMageGiantSpellCastRequest = false;

        if (!bossAbyssMage.CanEnterSpellCastState || !bossAbyssMage.CanSpellCast)
        {
            return true;
        }

        return bossAbyssMage.TryStartSpecialAttack(Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill4SixGiantAbyssFireballs);
    }

    private void HandleAbyssMageForceSkill5HealthInput()
    {
        bool pressedByInputSystem = Keyboard.current != null
            && (Keyboard.current.digit5Key.wasPressedThisFrame || Keyboard.current.numpad5Key.wasPressedThisFrame);
        bool pressedByLegacyInput = Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5);

        if (!pressedByInputSystem && !pressedByLegacyInput)
        {
            return;
        }

        ResolveAbyssMageForSkill4Debug();
        ForceBossHealthPercent(0.34f);
        LogAbyssMageDebug($"5 pressed. Boss health forced to 34%. boss={(bossAbyssMage != null ? bossAbyssMage.name : "null")}");
    }

    private void ForceBossHealthPercent(float percent)
    {
        if (bossHealth == null)
        {
            bossHealth = ResolveBossHealth(bossEnemy);
        }

        int maxHealth = bossHealth != null ? bossHealth.MaxHealth : (bossEnemy != null ? bossEnemy.MaxHealth : 1);
        int targetHealth = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(1, maxHealth) * Mathf.Clamp01(percent)), 1, Mathf.Max(1, maxHealth));

        if (bossEnemy != null)
        {
            bossEnemy.SetCurrentHealth(targetHealth);
        }

        if (bossHealth != null)
        {
            skill5IgnoreNextHealthDrop = true;
            bossHealth.SetCurrentHealth(targetHealth);
        }
    }

    private void HandleAbyssMageSkill5HealthChanged(Entity_Health health)
    {
        if (health == null || health.MaxHealth <= 0 || bossAbyssMage == null || bossAbyssMage.IsDead)
        {
            return;
        }

        if (skill5IgnoreNextHealthDrop)
        {
            skill5IgnoreNextHealthDrop = false;
            skill5LastObservedHealth = health.CurrentHealth;
        }
        else if (skill5Active && !skill5Ending && health.CurrentHealth < skill5LastObservedHealth)
        {
            LogAbyssMageSkill5Debug($"Ending because boss took damage. previous={skill5LastObservedHealth}, current={health.CurrentHealth}");
            EndAbyssMageSkill5(AbyssMageSkill5EndReason.PlayerDamage);
        }

        skill5LastObservedHealth = health.CurrentHealth;

        float healthPercent = health.CurrentHealth / (float)health.MaxHealth;
        if (!skill5Triggered
            && !skill5Active
            && encounterStarted
            && !encounterCompleted
            && healthPercent <= skill5TriggerHealthPercent)
        {
            if (bossAbyssMage != null && bossAbyssMage.CanEnterSpellCastState)
            {
                TryStartAbyssMageSkill5();
            }
            else
            {
                skill5PendingStart = true;
            }
        }
    }

    private bool TryStartAbyssMageSkill5()
    {
        if (skill5Triggered || skill5Active || bossAbyssMage == null || bossAbyssMage.IsDead)
        {
            skill5PendingStart = false;
            return false;
        }

        if (!bossAbyssMage.CanEnterSpellCastState)
        {
            skill5PendingStart = true;
            return false;
        }

        ResolveReferences();
        if (bossSkill5CastPoint == null)
        {
            LogAbyssMageSkill5Debug("Skill5 rejected. BossSkill5CastPoint is missing.");
            skill5PendingStart = false;
            return false;
        }

        skill5Triggered = true;
        skill5Active = true;
        skill5Ending = false;
        skill5PendingStart = false;
        skill5LastObservedHealth = bossHealth != null ? bossHealth.CurrentHealth : 0;

        pendingAbyssMageHybridSpellCastRequest = false;
        pendingAbyssMageEnhancedHybridSpellCastRequest = false;
        pendingAbyssMageGiantSpellCastRequest = false;
        pendingAbyssMageSkill4Request = false;

        if (enhancedSkill3Routine != null)
        {
            EndEnhancedSkill3State();
        }

        bossAbyssMage.AbortSpellCastForEnhancedSkill3();
        skill5Routine = StartCoroutine(RunAbyssMageSkill5Routine());
        return true;
    }

    private IEnumerator RunAbyssMageSkill5Routine()
    {
        List<Enemy_AbyssMageFireball> activeFireballs = new List<Enemy_AbyssMageFireball>();
        float nextSpawnTime = 0f;
        float healAccumulator = 0f;

        void HandleFireballResolved(Enemy_AbyssMageFireball projectile)
        {
            if (projectile != null)
            {
                projectile.ImpactResolved -= HandleFireballResolved;
                activeFireballs.Remove(projectile);
            }

            nextSpawnTime = Time.time;
        }

        try
        {
            LogAbyssMageSkill5Debug("Skill5 begin.");
            bossAbyssMage.BeginEnhancedSkill3CastHold(bossSkill5CastPoint.position);

            if (bossFloatingPlatform != null)
            {
                bossFloatingPlatform.BeginSkill5PlatformLock();
            }

            AddSkillPoint(6);
            RevealAllAbyssPowers();

            Transform ceilingReferencePoint = GetSkill4CeilingReferencePoint();
            while (skill5Active && !skill5Ending && bossHealth != null && bossHealth.CurrentHealth < GetSkill5RecoveryCapHealth())
            {
                CleanupResolvedSkill5Fireballs(activeFireballs, HandleFireballResolved);

                if (activeFireballs.Count < skill5MaxActiveGiantFireballs && Time.time >= nextSpawnTime)
                {
                    Enemy_AbyssMageFireball projectile = SpawnSkill5GiantAbyssFireball(ceilingReferencePoint);
                    if (projectile != null)
                    {
                        activeFireballs.Add(projectile);
                        projectile.ImpactResolved += HandleFireballResolved;
                    }

                    nextSpawnTime = Time.time + skill5GiantFireballInterval;
                }

                healAccumulator += bossHealth.MaxHealth * Skill5HealPercentPerSecondRuntime * Time.deltaTime;
                int healAmount = Mathf.FloorToInt(healAccumulator);
                if (healAmount > 0)
                {
                    healAccumulator -= healAmount;
                    HealBossForSkill5(healAmount);
                }

                if (bossHealth.CurrentHealth >= GetSkill5RecoveryCapHealth())
                {
                    EndAbyssMageSkill5(AbyssMageSkill5EndReason.RecoveredToCap);
                    break;
                }

                yield return null;
            }

            if (skill5Active && !skill5Ending)
            {
                EndAbyssMageSkill5(AbyssMageSkill5EndReason.RecoveredToCap);
            }
        }
        finally
        {
            for (int i = 0; i < activeFireballs.Count; i++)
            {
                Enemy_AbyssMageFireball projectile = activeFireballs[i];
                if (projectile != null)
                {
                    projectile.ImpactResolved -= HandleFireballResolved;
                }
            }

            skill5Routine = null;
        }
    }

    private enum AbyssMageSkill5EndReason
    {
        PlayerDamage = 0,
        RecoveredToCap = 1
    }

    private void EndAbyssMageSkill5(AbyssMageSkill5EndReason reason)
    {
        if (!skill5Active || skill5Ending)
        {
            return;
        }

        skill5Ending = true;
        StartCoroutine(EndAbyssMageSkill5Routine(reason));
    }

    private IEnumerator EndAbyssMageSkill5Routine(AbyssMageSkill5EndReason reason)
    {
        LogAbyssMageSkill5Debug($"Skill5 ending. reason={reason}");

        if (bossFloatingPlatform != null)
        {
            bossFloatingPlatform.EndSkill5PlatformLock();
            yield return bossFloatingPlatform.ForceAllTimedPlatformsDownRoutine();
        }

        SpendSkillPointsForSkill5End(6);

        if (bossAbyssMage != null)
        {
            bossAbyssMage.EndEnhancedSkill3CastHold();
        }

        if (reason == AbyssMageSkill5EndReason.RecoveredToCap)
        {
            yield return RunAbyssMageSkill4Routine(ignoreProcessingGate: true, forceRandomPlatformRise: false);
        }

        skill5Active = false;
        skill5Ending = false;
        ResetPassiveAbyssMageSkillPointState();
    }

    private Enemy_AbyssMageFireball SpawnSkill5GiantAbyssFireball(Transform ceilingReferencePoint)
    {
        if (bossAbyssMage == null || bossFloatingPlatform == null)
        {
            return null;
        }

        if (!TryGetRandomSkill4PlatformSpawnX(out float spawnX))
        {
            return null;
        }

        float spawnY = ceilingReferencePoint != null
            ? ceilingReferencePoint.position.y
            : transform.position.y;
        Vector2 spawnPosition = new Vector2(spawnX, spawnY);
        Enemy_AbyssMageFireball projectile = bossAbyssMage.SpawnSkill4GiantAbyssFireball(
            spawnPosition,
            ceilingReferencePoint,
            UnityEngine.Random.Range(0f, skill4FireballSpawnOffsetStep * Skill4FireballCount),
            giantFireballExplosionRadius,
            1.5f);
        LogAbyssMageSkill5Debug($"Spawned skill5 giant fireball at {spawnPosition}, projectile={(projectile != null ? projectile.name : "null")}");
        return projectile;
    }

    private void CleanupResolvedSkill5Fireballs(List<Enemy_AbyssMageFireball> fireballs, System.Action<Enemy_AbyssMageFireball> handler)
    {
        for (int i = fireballs.Count - 1; i >= 0; i--)
        {
            Enemy_AbyssMageFireball projectile = fireballs[i];
            if (projectile == null)
            {
                fireballs.RemoveAt(i);
                continue;
            }
        }
    }

    private void HealBossForSkill5(int amount)
    {
        if (amount <= 0 || bossHealth == null)
        {
            return;
        }

        int cap = GetSkill5RecoveryCapHealth();
        int targetHealth = Mathf.Min(cap, bossHealth.CurrentHealth + amount);
        if (targetHealth <= bossHealth.CurrentHealth)
        {
            return;
        }

        if (bossEnemy != null)
        {
            bossEnemy.SetCurrentHealth(targetHealth);
        }

        bossHealth.SetCurrentHealth(targetHealth);
        skill5LastObservedHealth = targetHealth;
    }

    private int GetSkill5RecoveryCapHealth()
    {
        int maxHealth = bossHealth != null ? bossHealth.MaxHealth : (bossEnemy != null ? bossEnemy.MaxHealth : 1);
        return Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(1, maxHealth) * skill5MaxRecoverHealthPercent), 1, Mathf.Max(1, maxHealth));
    }

    private void SpendSkillPointsForSkill5End(int amount)
    {
        currentSkillPoints = Mathf.Max(0, currentSkillPoints - Mathf.Max(0, amount));
        SetSkillPointBackgroundVisibleCount(currentSkillPoints);
        ConsumeRandomAbyssPowers(amount);
    }

    private bool TryGetRandomSkill4PlatformSpawnX(out float spawnX)
    {
        spawnX = 0f;

        List<float> platformXs = new List<float>();
        for (int i = 0; i < Skill4FireballCount; i++)
        {
            if (TryGetSkill4PlatformSpawnBounds(i, out Bounds platformBounds))
            {
                platformXs.Add(platformBounds.center.x);
            }
        }

        if (platformXs.Count == 0)
        {
            return false;
        }

        spawnX = platformXs[UnityEngine.Random.Range(0, platformXs.Count)];
        return true;
    }

    public bool ShouldReplaceAbyssMageSkill3WithEnhanced(Enemy_AbyssMage mage)
    {
        return CanProcessAbyssMageSkillPointSource()
            && mage != null
            && mage == bossAbyssMage
            && normalSkill3CastCounter >= normalSkill3CastsBeforeEnhanced;
    }

    public void NotifyAbyssMageNormalSkill3Cast(Enemy_AbyssMage mage)
    {
        if (!CanProcessAbyssMageSkillPointSource() || mage == null || mage != bossAbyssMage)
        {
            return;
        }

        normalSkill3CastCounter = Mathf.Min(normalSkill3CastsBeforeEnhanced, normalSkill3CastCounter + 1);
    }

    public bool TryStartEnhancedAbyssMageSkill3(bool force = false)
    {
        if (enhancedSkill3Active || enhancedSkill3Routine != null)
        {
            return false;
        }

        if (!CanProcessAbyssMageSkillPointSource() || bossAbyssMage == null)
        {
            return false;
        }

        ResolveReferences();
        if (bossEnhancedSkill3CastPoint == null)
        {
            LogAbyssMageDebug("Enhanced skill3 rejected. BossEnhancedSkill3CastPoint is missing.");
            return false;
        }

        bossAbyssMage.AbortSpellCastForEnhancedSkill3();

        pendingAbyssMageHybridSpellCastRequest = false;
        pendingAbyssMageGiantSpellCastRequest = false;
        normalSkill3CastCounter = 0;
        enhancedSkill3Routine = StartCoroutine(RunEnhancedAbyssMageSkill3Routine());
        return true;
    }

    private IEnumerator RunEnhancedAbyssMageSkill3Routine()
    {
        enhancedSkill3Active = true;
        int completedGiantFireballs = 0;
        int spawnedGiantFireballs = 0;
        float nextWaitLogTime = 0f;
        List<Enemy_AbyssMageFireball> spawnedProjectiles = new List<Enemy_AbyssMageFireball>();

        void HandleGiantFireballImpact(Enemy_AbyssMageFireball projectile)
        {
            if (projectile != null)
            {
                projectile.ImpactResolved -= HandleGiantFireballImpact;
            }

            completedGiantFireballs++;
            LogAbyssMageDebug($"Enhanced skill3 projectile resolved. completed={completedGiantFireballs}, spawned={spawnedGiantFireballs}");
        }

        try
        {
            Vector2 castPosition = bossEnhancedSkill3CastPoint.position;
            LogAbyssMageDebug($"Enhanced skill3 begin hold at {castPosition}.");
            bossAbyssMage.BeginEnhancedSkill3CastHold(castPosition);

        if (bossFloatingPlatform != null)
        {
            yield return bossFloatingPlatform.ForceAllTimedPlatformsDownRoutine();
            bossFloatingPlatform.ResetTimedPlatforms();
        }

            if (enhancedSkill3PlatformRiseStartDelay > 0f)
            {
                yield return new WaitForSeconds(enhancedSkill3PlatformRiseStartDelay);
            }

            if (bossFloatingPlatform != null)
            {
                int requestedPlatformCount = Mathf.Max(3, enhancedSkill3PlatformRiseCount);
                yield return bossFloatingPlatform.ActivateRandomTimedPlatformsRoutine(
                    requestedPlatformCount,
                    enhancedSkill3PlatformRiseInterval);
            }

            Transform target = player != null ? player.transform : null;
            if (target == null)
            {
                GameObject playerObject = !string.IsNullOrWhiteSpace(playerTag) ? GameObject.FindWithTag(playerTag) : null;
                target = playerObject != null ? playerObject.transform : bossTarget;
            }

            for (int i = 0; i < enhancedSkill3GiantFireballCount; i++)
            {
                Enemy_AbyssMageFireball projectile = bossAbyssMage.SpawnEnhancedSkill3GiantFireball(target);
                if (projectile != null)
                {
                    spawnedGiantFireballs++;
                    spawnedProjectiles.Add(projectile);
                    projectile.ImpactResolved += HandleGiantFireballImpact;
                }
                else
                {
                    completedGiantFireballs++;
                }

                if (i < enhancedSkill3GiantFireballCount - 1 && enhancedSkill3FireballInterval > 0f)
                {
                    yield return new WaitForSeconds(enhancedSkill3FireballInterval);
                }
            }

            while (completedGiantFireballs < Mathf.Max(1, spawnedGiantFireballs))
            {
                if (Time.time >= nextWaitLogTime)
                {
                    nextWaitLogTime = Time.time + 0.5f;
                    LogAbyssMageDebug($"Enhanced skill3 waiting fireballs. completed={completedGiantFireballs}, spawned={spawnedGiantFireballs}");
                }

                yield return null;
            }

            LogAbyssMageDebug("Enhanced skill3 all fireballs resolved. Ending boss hold.");
            bossAbyssMage.EndEnhancedSkill3CastHold();

            if (bossFloatingPlatform != null)
            {
                LogAbyssMageDebug("Enhanced skill3 forcing all timed platforms down after boss hold ended.");
                yield return bossFloatingPlatform.ForceAllTimedPlatformsDownRoutine();
            }
        }
        finally
        {
            LogAbyssMageDebug($"Enhanced skill3 finally. completed={completedGiantFireballs}, spawned={spawnedGiantFireballs}");
            for (int i = 0; i < spawnedProjectiles.Count; i++)
            {
                Enemy_AbyssMageFireball projectile = spawnedProjectiles[i];
                if (projectile != null)
                {
                    projectile.ImpactResolved -= HandleGiantFireballImpact;
                }
            }

            EndEnhancedSkill3State(false);
        }
    }

    private void EndEnhancedSkill3State(bool stopRoutine = true)
    {
        LogAbyssMageDebug($"EndEnhancedSkill3State called. stopRoutine={stopRoutine}, routineNull={enhancedSkill3Routine == null}, active={enhancedSkill3Active}");
        if (stopRoutine && enhancedSkill3Routine != null)
        {
            StopCoroutine(enhancedSkill3Routine);
        }

        enhancedSkill3Routine = null;

        if (bossAbyssMage != null)
        {
            bossAbyssMage.EndEnhancedSkill3CastHold();
            bossAbyssMage.ResetTeleportProbabilities();
        }

        ResetPassiveAbyssMageSkillPointState();
        enhancedSkill3Active = false;
    }

    private void ResetAbyssMageSkill5State()
    {
        if (skill5Routine != null)
        {
            StopCoroutine(skill5Routine);
            skill5Routine = null;
        }

        if (bossFloatingPlatform != null)
        {
            bossFloatingPlatform.EndSkill5PlatformLock();
        }

        if (skill5Active && bossAbyssMage != null)
        {
            bossAbyssMage.EndEnhancedSkill3CastHold();
        }

        skill5Triggered = false;
        skill5Active = false;
        skill5Ending = false;
        skill5IgnoreNextHealthDrop = false;
        skill5LastObservedHealth = 0;
        skill5PendingStart = false;
    }

    private void TryStartPendingAbyssMageSkill5()
    {
        if (!skill5PendingStart
            || skill5Triggered
            || skill5Active
            || !encounterStarted
            || encounterCompleted
            || bossAbyssMage == null
            || !bossAbyssMage.CanEnterSpellCastState)
        {
            return;
        }

        TryStartAbyssMageSkill5();
    }

    public IEnumerator RunAbyssMageSkill4Routine(bool ignoreProcessingGate = false, bool forceRandomPlatformRise = true)
    {
        if ((!ignoreProcessingGate && !CanProcessAbyssMageSkillPointSource()) || bossAbyssMage == null)
        {
            LogAbyssMageSkill4Debug($"Routine aborted. ignoreGate={ignoreProcessingGate}, CanProcess={CanProcessAbyssMageSkillPointSource()}, bossNull={(bossAbyssMage == null)}");
            yield break;
        }

        ResolveReferences();
        Transform ceilingReferencePoint = GetSkill4CeilingReferencePoint();
        float referenceY = ceilingReferencePoint != null
            ? ceilingReferencePoint.position.y
            : (bossEnhancedSkill3CastPoint != null ? bossEnhancedSkill3CastPoint.position.y : transform.position.y);
        float fallbackSpawnY = GetSkill4SpawnY(referenceY);
        LogAbyssMageSkill4Debug($"Routine begin. boss={bossAbyssMage.name}, spellPrefab={(bossAbyssMage.SpellPrefab != null ? bossAbyssMage.SpellPrefab.name : "null")}, ceiling={(ceilingReferencePoint != null ? ceilingReferencePoint.name : "null")}, referenceY={referenceY:0.###}, fallbackSpawnY={fallbackSpawnY:0.###}, tileSpawnHeightOffset={skill4FireballSpawnHeightOffset:0.###}");

        if (forceRandomPlatformRise && bossFloatingPlatform != null && bossFloatingPlatform.TryForceRandomTimedPlatformRise())
        {
            LogAbyssMageSkill4Debug("Random platform rise forced for skill4.");
        }
        else if (forceRandomPlatformRise)
        {
            LogAbyssMageSkill4Debug("Random platform rise failed for skill4.");
        }
        else
        {
            LogAbyssMageSkill4Debug("Random platform rise skipped for skill4.");
        }

        int spawnedCount = 0;
        for (int i = 0; i < Skill4FireballCount; i++)
        {
            bool hasPlatformBounds = TryGetSkill4PlatformSpawnBounds(i, out Bounds platformBounds);
            float spawnX;
            if (!hasPlatformBounds)
            {
                spawnX = GetSkill4FallbackSpawnX(i);
            }
            else
            {
                spawnX = platformBounds.center.x;
            }

            float spawnY = fallbackSpawnY;
            Vector2 spawnPosition = new Vector2(spawnX, spawnY);
            Enemy_AbyssMageFireball projectile = bossAbyssMage.SpawnSkill4GiantAbyssFireball(
                spawnPosition,
                ceilingReferencePoint,
                skill4FireballSpawnOffsetStep * i,
                giantFireballExplosionRadius);
            if (projectile != null)
            {
                spawnedCount++;
            }

            string source = hasPlatformBounds
                ? $"Platform_{i + 1} boundsCenter={platformBounds.center}, boundsMin={platformBounds.min}, boundsMax={platformBounds.max}"
                : "fallback";
            LogAbyssMageSkill4Debug($"Spawn #{i + 1}. position={spawnPosition}, source={source}, projectile={(projectile != null ? projectile.name : "null")}");
        }

        LogAbyssMageSkill4Debug($"Routine end. spawned={spawnedCount}/{Skill4FireballCount}");
        yield return null;
    }

    private Transform GetSkill4CeilingReferencePoint()
    {
        return bossFloatingPlatform != null
            ? bossFloatingPlatform.transform.Find("CeilingReferencePoint")
            : null;
    }

    private float GetSkill4SpawnY(float referenceY)
    {
        return referenceY;
    }

    private void ResolveAbyssMageForSkill4Debug()
    {
        ResolveReferences();

        if (bossAbyssMage != null && bossAbyssMage.gameObject != null && bossAbyssMage.gameObject.activeInHierarchy && !bossAbyssMage.IsDead)
        {
            LogAbyssMageSkill4Debug($"Resolve boss: using cached {bossAbyssMage.name}");
            return;
        }

        Enemy resolvedEnemy = ResolveBossEnemy();
        CacheAndBindBossEnemy(resolvedEnemy);
        if (bossAbyssMage != null && bossAbyssMage.gameObject != null && bossAbyssMage.gameObject.activeInHierarchy && !bossAbyssMage.IsDead)
        {
            LogAbyssMageSkill4Debug($"Resolve boss: bound from bossEnemy {bossAbyssMage.name}");
            return;
        }

        Enemy_AbyssMage childMage = GetComponentInChildren<Enemy_AbyssMage>(true);
        if (childMage != null && childMage.gameObject.activeInHierarchy && !childMage.IsDead)
        {
            bossAbyssMage = childMage;
            LogAbyssMageSkill4Debug($"Resolve boss: found active child {bossAbyssMage.name}");
            return;
        }

        Enemy_AbyssMage[] sceneMages = FindObjectsOfType<Enemy_AbyssMage>(true);
        for (int i = 0; i < sceneMages.Length; i++)
        {
            Enemy_AbyssMage sceneMage = sceneMages[i];
            if (sceneMage != null && sceneMage.gameObject.activeInHierarchy && !sceneMage.IsDead)
            {
                bossAbyssMage = sceneMage;
                LogAbyssMageSkill4Debug($"Resolve boss: scene active result={bossAbyssMage.name}");
                return;
            }
        }

        bossAbyssMage = childMage != null ? childMage : (sceneMages.Length > 0 ? sceneMages[0] : null);
        LogAbyssMageSkill4Debug($"Resolve boss: fallback result={(bossAbyssMage != null ? bossAbyssMage.name : "null")}");
    }

    private bool TryGetSkill4PlatformSpawnBounds(int platformIndex, out Bounds platformBounds)
    {
        platformBounds = default;

        if (bossFloatingPlatform != null
            && bossFloatingPlatform.TryGetTeleportPlatformBounds(platformIndex, out platformBounds))
        {
            return true;
        }

        return false;
    }

    private float GetSkill4FallbackSpawnX(int platformIndex)
    {
        if (TryGetBossArenaBounds(out Bounds arenaBounds))
        {
            float t = (platformIndex + 0.5f) / Mathf.Max(1f, Skill4FireballCount);
            return Mathf.Lerp(arenaBounds.min.x, arenaBounds.max.x, t);
        }

        return bossTarget != null ? bossTarget.position.x : transform.position.x;
    }

    public bool PreviewAbyssMageGiantSpellCast(float explosionRadiusOverride = -1f)
    {
        bool spawnedTemporaryMage = false;
        Enemy_AbyssMage previewMage = ResolvePreviewAbyssMageForSpellPreview(true, out spawnedTemporaryMage);
        if (previewMage == null)
        {
            LogAbyssMageDebug("Preview giant failed. bossNull=True");
            return false;
        }

        Transform previewTarget = player != null ? player.transform : bossTarget;
        bool spawned = previewMage.PreviewGiantAbyssFireball(previewTarget, explosionRadiusOverride);

        if (spawnedTemporaryMage && previewMage != null)
        {
            Destroy(previewMage.transform.root.gameObject);
        }

        LogAbyssMageDebug($"Preview giant requested. spawned={spawned}");
        return spawned;
    }

    private void LogAbyssMageDebug(string message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[AbyssMageBoss] {message}", this);
#endif
    }

    private void LogAbyssMageSkill4Debug(string message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[AbyssMageSkill4] {message}", this);
#endif
    }

    private void LogAbyssMageSkill5Debug(string message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[AbyssMageSkill5] {message}", this);
#endif
    }

    public Enemy_AbyssMage ResolvePreviewAbyssMageForSpellPreview(bool allowTemporarySpawn, out bool spawnedTemporaryMage)
    {
        spawnedTemporaryMage = false;

        if (bossAbyssMage != null)
        {
            return bossAbyssMage;
        }

        if (bossEnemy is Enemy_AbyssMage assignedMage)
        {
            return assignedMage;
        }

        if (bossEnemy != null)
        {
            Enemy_AbyssMage childMage = bossEnemy.GetComponentInChildren<Enemy_AbyssMage>(true);
            if (childMage != null)
            {
                return childMage;
            }
        }

        Enemy_AbyssMage sceneMage = GetComponentInChildren<Enemy_AbyssMage>(true);
        if (sceneMage != null)
        {
            return sceneMage;
        }

        sceneMage = FindObjectOfType<Enemy_AbyssMage>(true);
        if (sceneMage != null)
        {
            return sceneMage;
        }

        if (!allowTemporarySpawn || bossPrefab == null)
        {
            return null;
        }

        Vector3 spawnPosition = bossTarget != null ? bossTarget.position : transform.position;
        GameObject previewBossObject = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
        Enemy_AbyssMage previewMage = previewBossObject.GetComponentInChildren<Enemy_AbyssMage>(true);
        if (previewMage == null)
        {
            previewMage = previewBossObject.GetComponent<Enemy_AbyssMage>();
        }

        if (previewMage == null)
        {
            Destroy(previewBossObject);
            return null;
        }

        spawnedTemporaryMage = true;
        return previewMage;
    }

    public Enemy_AbyssMage.AbyssMageSpecialAttackType ResolveAbyssMageSpecialAttackType(Enemy_AbyssMage mage)
    {
        if (mage == null)
        {
            return Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill1Fireball;
        }

        ArenaBossEncounterController activeController = GetActiveInstance();
        if (activeController != this)
        {
            return Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill1Fireball;
        }

        if (!CanProcessAbyssMageSkillPointSource() || mage == null || mage != bossAbyssMage)
        {
            return Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill1Fireball;
        }

        if (pendingAbyssMageSkill4Request)
        {
            if (currentSkillPoints >= 6 && TrySpendSkillPoints(6))
            {
                pendingAbyssMageSkill4Request = false;
                return Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill4SixGiantAbyssFireballs;
            }

            pendingAbyssMageSkill4Request = false;
        }

        if (currentSkillPoints >= 6 && TrySpendSkillPoints(6))
        {
            return Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill4SixGiantAbyssFireballs;
        }

        if (pendingAbyssMageEnhancedHybridSpellCastRequest)
        {
            if (currentSkillPoints >= 3 && TrySpendSkillPoints(2))
            {
                pendingAbyssMageEnhancedHybridSpellCastRequest = false;
                normalSkill2CastCounter = 0;
                return Enemy_AbyssMage.AbyssMageSpecialAttackType.EnhancedSkill2MixedFireball;
            }

            pendingAbyssMageEnhancedHybridSpellCastRequest = false;
        }

        if (pendingAbyssMageHybridSpellCastRequest)
        {
            if (currentSkillPoints >= 3 && TrySpendSkillPoints(2))
            {
                pendingAbyssMageHybridSpellCastRequest = false;
                if (ShouldReplaceAbyssMageSkill2WithEnhanced(mage))
                {
                    normalSkill2CastCounter = 0;
                    return Enemy_AbyssMage.AbyssMageSpecialAttackType.EnhancedSkill2MixedFireball;
                }

                NotifyAbyssMageNormalSkill2Cast(mage);
                return Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill2MixedFireball;
            }

            pendingAbyssMageHybridSpellCastRequest = false;
        }

        if (pendingAbyssMageGiantSpellCastRequest)
        {
            if (ShouldReplaceAbyssMageSkill3WithEnhanced(mage))
            {
                pendingAbyssMageGiantSpellCastRequest = false;
                return Enemy_AbyssMage.AbyssMageSpecialAttackType.EnhancedSkill3GiantAbyssFireball;
            }

            if (!mage.IsSkill3OnCooldown && currentSkillPoints >= 2 && TrySpendSkillPoints(1))
            {
                pendingAbyssMageGiantSpellCastRequest = false;
                return Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill3GiantAbyssFireball;
            }

            pendingAbyssMageGiantSpellCastRequest = false;
        }

        if (currentSkillPoints >= 3)
        {
            if (TryRollPercent(50f) && TrySpendSkillPoints(2))
            {
                if (ShouldReplaceAbyssMageSkill2WithEnhanced(mage))
                {
                    normalSkill2CastCounter = 0;
                    return Enemy_AbyssMage.AbyssMageSpecialAttackType.EnhancedSkill2MixedFireball;
                }

                NotifyAbyssMageNormalSkill2Cast(mage);
                return Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill2MixedFireball;
            }
        }

        if (currentSkillPoints >= 2 && ShouldReplaceAbyssMageSkill3WithEnhanced(mage))
        {
            return Enemy_AbyssMage.AbyssMageSpecialAttackType.EnhancedSkill3GiantAbyssFireball;
        }

        if (!mage.IsSkill3OnCooldown && currentSkillPoints >= 2 && TrySpendSkillPoints(1))
        {
            return Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill3GiantAbyssFireball;
        }

        return Enemy_AbyssMage.AbyssMageSpecialAttackType.Skill1Fireball;
    }

    private void TryAwardSkillPointFromMeleeAttack()
    {
        if (!CanProcessAbyssMageSkillPointSource())
        {
            return;
        }

        if (TryRollPercent(abyssMageMeleeSkillPointChance))
        {
            AddSkillPoint(1);
        }
    }

    private void TryAwardSkillPointFromFireballSummon()
    {
        if (!CanProcessAbyssMageSkillPointSource())
        {
            return;
        }

        if (Time.time < lastFireballSummonSkillPointTime + abyssMageFireballSummonSkillPointCooldown)
        {
            return;
        }

        if (TryRollPercent(abyssMageFireballSummonSkillPointChance))
        {
            lastFireballSummonSkillPointTime = Time.time;
            AddSkillPoint(1);
        }
    }

    private void TryAwardSkillPointFromFireballPlayerInteraction()
    {
        if (!CanProcessAbyssMageSkillPointSource())
        {
            return;
        }

        if (Time.time < lastFireballPlayerInteractionSkillPointTime + abyssMageFireballPlayerInteractionSkillPointCooldown)
        {
            return;
        }

        lastFireballPlayerInteractionSkillPointTime = Time.time;
        AddSkillPoint(1);
    }

    public void NotifyAbyssMageStunned(Enemy_AbyssMage mage)
    {
        if (mage == null || mage != bossAbyssMage)
        {
            return;
        }

        if (Time.time < lastAbyssMageStunSkillPointTime + AbyssMageStunSkillPointCooldown)
        {
            return;
        }

        lastAbyssMageStunSkillPointTime = Time.time;
        AddSkillPoint(1);
    }

    private void UpdatePassiveAbyssMageSkillPointChance()
    {
        if (!CanProcessAbyssMageSkillPointSource())
        {
            return;
        }

        passiveSkillPointTimer += Time.deltaTime;
        while (passiveSkillPointTimer >= abyssMagePassiveSkillPointCheckInterval)
        {
            passiveSkillPointTimer -= abyssMagePassiveSkillPointCheckInterval;

            if (TryRollPercent(passiveSkillPointChance))
            {
                AddSkillPoint(1);
                passiveSkillPointChance = abyssMagePassiveSkillPointInitialChance;
                continue;
            }

            passiveSkillPointChance = Mathf.Min(100f, passiveSkillPointChance + abyssMagePassiveSkillPointChanceIncrement);
        }
    }

    private bool CanProcessAbyssMageSkillPointSource()
    {
        return encounterStarted
            && !encounterCompleted
            && !enhancedSkill3Active
            && !skill5Active
            && bossAbyssMage != null
            && bossAbyssMage.gameObject != null
            && bossAbyssMage.gameObject.activeInHierarchy
            && !bossAbyssMage.IsDead;
    }

    private void ResetPassiveAbyssMageSkillPointState()
    {
        passiveSkillPointTimer = 0f;
        passiveSkillPointChance = abyssMagePassiveSkillPointInitialChance;
        lastFireballSummonSkillPointTime = float.NegativeInfinity;
        lastFireballPlayerInteractionSkillPointTime = float.NegativeInfinity;
        lastAbyssMageStunSkillPointTime = float.NegativeInfinity;
        pendingAbyssMageHybridSpellCastRequest = false;
        pendingAbyssMageEnhancedHybridSpellCastRequest = false;
        pendingAbyssMageGiantSpellCastRequest = false;
        pendingAbyssMageSkill4Request = false;
    }

    private static bool TryRollPercent(float chancePercent)
    {
        return UnityEngine.Random.value <= Mathf.Clamp01(chancePercent / 100f);
    }

    public Transform GetAbyssMageCeilingReferencePoint()
    {
        if (bossFloatingPlatform != null)
        {
            Transform referencePoint = bossFloatingPlatform.transform.Find("CeilingReferencePoint");
            if (referencePoint != null)
            {
                return referencePoint;
            }
        }

        GameObject ceilingObject = GameObject.Find("CeilingReferencePoint");
        return ceilingObject != null ? ceilingObject.transform : null;
    }

    public bool TryGetBossArenaHorizontalBounds(out float leftX, out float rightX)
    {
        leftX = 0f;
        rightX = 0f;

        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<BoxCollider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        if (triggerCollider == null)
        {
            return false;
        }

        Bounds bounds = triggerCollider.bounds;
        leftX = bounds.min.x;
        rightX = bounds.max.x;
        return rightX >= leftX;
    }

    public bool TryGetBossArenaBounds(out Bounds bounds)
    {
        bounds = default;

        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<BoxCollider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        if (triggerCollider == null)
        {
            return false;
        }

        bounds = triggerCollider.bounds;
        return bounds.size.x > 0f && bounds.size.y > 0f;
    }

    public bool TryGetAbyssMageRandomPlatformTeleportDestination(Enemy_AbyssMage mage, out Vector2 destination)
    {
        destination = Vector2.zero;

        if (!IsActiveAbyssMageBoss(mage) || bossFloatingPlatform == null)
        {
            return false;
        }

        return bossFloatingPlatform.TryGetRandomTeleportDestination(
            mage.transform,
            mage.TeleportMaxPlacementAttempts,
            out destination);
    }

    public bool IsActiveAbyssMageBoss(Enemy_AbyssMage mage)
    {
        return CanProcessAbyssMageSkillPointSource()
            && mage != null
            && mage == bossAbyssMage;
    }

    private void RevealRandomAbyssPower()
    {
        if (abyssPowers == null || abyssPowers.Length == 0)
        {
            return;
        }

        List<int> hiddenIndices = new List<int>();
        for (int i = 0; i < abyssPowers.Length; i++)
        {
            if (!IsAbyssPowerVisible(abyssPowers[i]))
            {
                hiddenIndices.Add(i);
            }
        }

        if (hiddenIndices.Count == 0)
        {
            return;
        }

        int chosenIndex = hiddenIndices[UnityEngine.Random.Range(0, hiddenIndices.Count)];
        SetAbyssPowerVisible(chosenIndex, true);
        ActivateAbyssPowerPlatform(chosenIndex);
    }

    private void RevealAllAbyssPowers()
    {
        if (abyssPowers == null || abyssPowers.Length == 0)
        {
            return;
        }

        for (int i = 0; i < abyssPowers.Length; i++)
        {
            SetAbyssPowerVisible(i, true);
        }
    }

    private void ActivateAbyssPowerPlatform(int abyssPowerIndex)
    {
        if (bossFloatingPlatform == null)
        {
            return;
        }

        bossFloatingPlatform.ActivateTimedPlatform(abyssPowerIndex);
    }

    private void UpdateBossFloatingPlatformMechanics()
    {
        if (!Application.isPlaying || bossFloatingPlatform == null)
        {
            return;
        }

        Collider2D bossCollider = bossAbyssMage != null ? bossAbyssMage.GetComponent<Collider2D>() : null;
        bossFloatingPlatform.TickTimedPlatforms(Time.deltaTime, bossCollider);
    }

    private void ConsumeRandomAbyssPowers(int amount)
    {
        if (amount <= 0 || abyssPowers == null || abyssPowers.Length == 0)
        {
            return;
        }

        for (int consumed = 0; consumed < amount; consumed++)
        {
            List<int> visibleIndices = new List<int>();
            for (int i = 0; i < abyssPowers.Length; i++)
            {
                if (IsAbyssPowerVisible(abyssPowers[i]))
                {
                    visibleIndices.Add(i);
                }
            }

            if (visibleIndices.Count == 0)
            {
                return;
            }

            int chosenIndex = visibleIndices[UnityEngine.Random.Range(0, visibleIndices.Count)];
            SetAbyssPowerVisible(chosenIndex, false);
        }
    }

    private IEnumerator RevealAbyssFiresRoutine()
    {
        if (abyssFires == null || abyssFires.Length == 0)
        {
            yield break;
        }

        for (int i = 0; i < abyssFires.Length; i++)
        {
            SetAbyssFireVisible(i, true, true);
            if (abyssFireRevealDelay > 0f)
            {
                yield return new WaitForSeconds(abyssFireRevealDelay);
            }
        }
    }

    private IEnumerator MoveBossIntoPositionRoutine(Transform bossTransform, Vector3 targetPosition)
    {
        if (bossTransform == null)
        {
            yield break;
        }

        Vector3 startPosition = bossTransform.position;
        float duration = bossIntroMoveDistance <= 0f
            ? 0f
            : Mathf.Max(0.05f, bossIntroMoveDistance * Mathf.Max(0.01f, bossIntroMoveSpeedMultiplier));

        if (duration <= 0f)
        {
            bossTransform.position = targetPosition;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            bossTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        bossTransform.position = targetPosition;
    }

    private void ApplyIdleVisualState()
    {
        SetDoorsLocked(false);
        SetAbyssFireVisibleCount(0);
        PreviewSkillPointBackgroundVisibleCount(0);
        PreviewAbyssPowerVisibleCount(0);
        if (bossFloatingPlatform != null)
        {
            bossFloatingPlatform.SetHidden();
        }
        RestoreBossCamera();
        ReleasePlayerAfterBossIntro();
    }

    private void ApplyPhaseState(int phase, bool preview)
    {
        int clampedPhase = Mathf.Clamp(phase, 1, Mathf.Max(1, bossPhaseCount));
        currentBossPhase = clampedPhase;

        BossPhaseState phaseState = GetPhaseState(clampedPhase);
        if (phaseState == null)
        {
            ApplyFloatingPlatformFallback(clampedPhase);
            return;
        }

        BossPhaseAction[] actions = phaseState.Actions;
        if (actions.Length == 0)
        {
            ApplyFloatingPlatformFallback(clampedPhase);
            return;
        }

        for (int i = 0; i < actions.Length; i++)
        {
            BossPhaseAction action = actions[i];
            if (action == null)
            {
                continue;
            }

            switch (action.ActionType)
            {
                case BossPhaseActionType.SetGameObjectsActive:
                    SetGameObjectsActive(action.GameObjects, action.Active);
                    break;
                case BossPhaseActionType.SetFloatingPlatformState:
                    ApplyFloatingPlatformState(action.FloatingPlatformState);
                    break;
                case BossPhaseActionType.SetSkillPointBackgroundVisibleCount:
                    SetSkillPointBackgroundVisibleCount(action.VisibleCount);
                    break;
                case BossPhaseActionType.SetAbyssPowerVisibleCount:
                    SetAbyssPowerVisibleCount(action.VisibleCount);
                    break;
                case BossPhaseActionType.SetAbyssFireVisibleCount:
                    SetAbyssFireVisibleCount(action.VisibleCount);
                    break;
            }
        }
    }

    private BossPhaseState GetPhaseState(int phase)
    {
        if (bossPhaseStates == null || bossPhaseStates.Length == 0)
        {
            return null;
        }

        int index = Mathf.Clamp(phase - 1, 0, bossPhaseStates.Length - 1);
        return bossPhaseStates[index];
    }

    private int ResolveBossPhase(int currentHealth, int maxHealth)
    {
        if (maxHealth <= 0)
        {
            return 1;
        }

        float normalized = Mathf.Clamp01(currentHealth / (float)maxHealth);
        if (bossPhaseCount >= 4 && normalized <= phase4HealthThreshold)
        {
            return 4;
        }

        if (bossPhaseCount >= 3 && normalized <= phase3HealthThreshold)
        {
            return 3;
        }

        if (bossPhaseCount >= 2 && normalized <= phase2HealthThreshold)
        {
            return 2;
        }

        return 1;
    }

    private void ApplyFloatingPlatformFallback(int phase)
    {
        if (phase <= 1)
        {
            SetPlatformHidden();
        }
        else if (phase == 2)
        {
            SetPlatformBackground1();
        }
        else if (phase == 3)
        {
            SetPlatformBackground2();
        }
        else
        {
            SetPlatformSolid();
        }
    }

    private void ApplyFloatingPlatformState(FloatingPlatformState state)
    {
        ArenaBossFloatingPlatformController platform = FindFloatingPlatformController();
        if (platform == null)
        {
            return;
        }

        switch (state)
        {
            case FloatingPlatformState.Hidden:
                platform.SetHidden();
                break;
            case FloatingPlatformState.Platform1:
                platform.SetPlatform1();
                break;
            case FloatingPlatformState.Platform2:
                platform.SetPlatform2();
                break;
            case FloatingPlatformState.Platform3:
                platform.SetPlatform3();
                break;
            case FloatingPlatformState.Platform4:
                platform.SetPlatform4();
                break;
            case FloatingPlatformState.Platform5:
                platform.SetPlatform5();
                break;
            case FloatingPlatformState.Platform6:
                platform.SetPlatform6();
                break;
            case FloatingPlatformState.AllVisible:
                platform.SetAllVisible();
                break;
        }
    }

    private ArenaBossFloatingPlatformController FindFloatingPlatformController()
    {
        ArenaBossFloatingPlatformController platform = GetComponentInChildren<ArenaBossFloatingPlatformController>(true);
        if (platform != null)
        {
            return platform;
        }

        return null;
    }

    private void SetGameObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject obj = objects[i];
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }
    }

    private void SetSkillPointBackgroundVisibleCount(int visibleCount)
    {
        visibleCount = Mathf.Clamp(visibleCount, 0, GetMaxVisualCount());
        for (int i = 0; i < skillPointBackgrounds.Length; i++)
        {
            GameObject background = skillPointBackgrounds[i];
            if (background != null)
            {
                background.SetActive(i < visibleCount);
            }
        }
    }

    private void SetAbyssPowerVisibleCount(int visibleCount)
    {
        visibleCount = Mathf.Clamp(visibleCount, 0, GetMaxVisualCount());
        for (int i = 0; i < abyssPowers.Length; i++)
        {
            SetAbyssPowerVisible(i, i < visibleCount);
        }
    }

    private void SetAbyssPowerVisible(int index, bool visible)
    {
        if (abyssPowers == null || index < 0 || index >= abyssPowers.Length)
        {
            return;
        }

        AbyssPower power = abyssPowers[index];
        if (power == null)
        {
            return;
        }

        power.SetVisible(visible);
    }

    private bool IsAbyssPowerVisible(AbyssPower power)
    {
        if (power == null)
        {
            return false;
        }

        return power.gameObject.activeSelf;
    }

    private void SetAbyssFireVisibleCount(int visibleCount, bool playAudio = false)
    {
        visibleCount = Mathf.Clamp(visibleCount, 0, abyssFires != null ? abyssFires.Length : 0);
        for (int i = 0; i < abyssFires.Length; i++)
        {
            SetAbyssFireVisible(i, i < visibleCount, playAudio, false);
        }

        UpdateAbyssFireAudioReductionState(visibleCount);
    }

    private void SetAbyssFireVisible(int index, bool visible, bool playAudio = false, bool refreshAudioState = true)
    {
        if (abyssFires == null || index < 0 || index >= abyssFires.Length)
        {
            return;
        }

        AbyssFire fire = abyssFires[index];
        if (fire == null)
        {
            return;
        }

        bool isCurrentlyVisible = fire.gameObject.activeSelf;
        if (isCurrentlyVisible == visible)
        {
            return;
        }

        fire.SetVisible(visible, playAudio);

        if (refreshAudioState)
        {
            UpdateAbyssFireAudioReductionState(GetAbyssFireVisibleCount());
        }
    }

    private int GetAbyssFireVisibleCount()
    {
        if (abyssFires == null || abyssFires.Length == 0)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < abyssFires.Length; i++)
        {
            AbyssFire fire = abyssFires[i];
            if (fire != null && fire.gameObject.activeSelf)
            {
                count++;
            }
        }

        return count;
    }

    private void UpdateAbyssFireAudioReductionState(int visibleCount)
    {
        if (abyssFires == null || abyssFires.Length == 0)
        {
            return;
        }

        if (visibleCount < 6)
        {
            CancelAbyssFireAudioReduction();
            return;
        }

        if (abyssFireAudioReduced)
        {
            ApplyAbyssFireAudioVolumeMultiplier(1f - abyssFireAudioReduction);
            return;
        }

        if (abyssFireAudioReductionRoutine != null)
        {
            return;
        }

        abyssFireAudioReductionRoutine = StartCoroutine(ApplyAbyssFireAudioReductionAfterDelayCo());
    }

    private IEnumerator ApplyAbyssFireAudioReductionAfterDelayCo()
    {
        if (abyssFireAudioReductionDelay > 0f)
        {
            yield return new WaitForSeconds(abyssFireAudioReductionDelay);
        }

        abyssFireAudioReductionRoutine = null;

        if (GetAbyssFireVisibleCount() < 6)
        {
            yield break;
        }

        abyssFireAudioReduced = true;
        ApplyAbyssFireAudioVolumeMultiplier(1f - abyssFireAudioReduction);
    }

    private void CancelAbyssFireAudioReduction()
    {
        if (abyssFireAudioReductionRoutine != null)
        {
            StopCoroutine(abyssFireAudioReductionRoutine);
            abyssFireAudioReductionRoutine = null;
        }

        abyssFireAudioReduced = false;
        ApplyAbyssFireAudioVolumeMultiplier(1f);
    }

    private void ApplyAbyssFireAudioVolumeMultiplier(float multiplier)
    {
        if (abyssFires == null || abyssFires.Length == 0)
        {
            return;
        }

        for (int i = 0; i < abyssFires.Length; i++)
        {
            AbyssFire fire = abyssFires[i];
            if (fire != null)
            {
                fire.SetTransitionAudioVolumeMultiplier(multiplier);
            }
        }
    }

    private void SetDoorsLocked(bool locked)
    {
        if (doors == null)
        {
            return;
        }

        for (int i = 0; i < doors.Length; i++)
        {
            ArenaDoorController door = doors[i];
            if (door != null)
            {
                door.SetLocked(locked);
            }
        }
    }

    private void StopAllInternalRoutines()
    {
        ResetAbyssMageSkill5State();
        EndEnhancedSkill3State();

        if (encounterRoutine != null)
        {
            StopCoroutine(encounterRoutine);
            encounterRoutine = null;
        }

        if (abyssFireRevealRoutine != null)
        {
            StopCoroutine(abyssFireRevealRoutine);
            abyssFireRevealRoutine = null;
        }

        if (bossIntroRoutine != null)
        {
            StopCoroutine(bossIntroRoutine);
            bossIntroRoutine = null;
        }

        if (bossFloatingPlatformRoutine != null)
        {
            StopCoroutine(bossFloatingPlatformRoutine);
            bossFloatingPlatformRoutine = null;
        }

        if (bossEncounterIntroRoutine != null)
        {
            StopCoroutine(bossEncounterIntroRoutine);
            bossEncounterIntroRoutine = null;
        }

        if (bossCameraRoutine != null)
        {
            StopCoroutine(bossCameraRoutine);
            bossCameraRoutine = null;
        }

        CancelAbyssFireAudioReduction();
    }

    private string BuildActiveEnemySummary()
    {
        if (activeEnemies.Count == 0)
        {
            return "None";
        }

        StringBuilder builder = new StringBuilder();
        bool first = true;
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy == null)
            {
                continue;
            }

            if (!first)
            {
                builder.Append(", ");
            }

            builder.Append(enemy.name);
            first = false;
        }

        return first ? "None" : builder.ToString();
    }

    private int GetMaxVisualCount()
    {
        int count = 0;
        count = Math.Max(count, skillPointBackgrounds != null ? skillPointBackgrounds.Length : 0);
        count = Math.Max(count, abyssPowers != null ? abyssPowers.Length : 0);
        count = Math.Max(count, abyssFires != null ? abyssFires.Length : 0);
        return Math.Max(count, 6);
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        Player otherPlayer = other.GetComponentInParent<Player>();

        if (player != null && otherPlayer == player)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(playerTag) && other.CompareTag(playerTag))
        {
            return true;
        }

        return otherPlayer != null;
    }

    private bool TryGetPlayerEncounterBounds(out Bounds bounds)
    {
        bounds = default;

        if (player == null)
        {
            player = FindObjectOfType<Player>(true);
        }

        if (player == null)
        {
            return false;
        }

        if (player.TryGetActiveColliderBounds(out bounds))
        {
            return true;
        }

        Collider2D[] colliders = player.GetComponentsInChildren<Collider2D>(true);
        bool hasBounds = false;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null || !collider.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return hasBounds;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform nestedChild = FindDeepChild(child, childName);
            if (nestedChild != null)
            {
                return nestedChild;
            }
        }

        return null;
    }
}

public interface IArenaFloatingPlatformPreviewable
{
    IEnumerator PlayAppearanceSequence();
    IEnumerator PlayDisappearanceSequence();
}

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class ArenaBossFloatingPlatformController : MonoBehaviour, IArenaFloatingPlatformPreviewable
{
    private const string GroundLayerName = "Ground";
    private const int PlatformCount = 6;

    [Header("References")]
    [SerializeField] private Transform groundReferencePoint;
    [SerializeField] private Tilemap platform1Tilemap;
    [SerializeField] private Tilemap platform2Tilemap;
    [SerializeField] private Tilemap platform3Tilemap;
    [SerializeField] private Tilemap platform4Tilemap;
    [SerializeField] private Tilemap platform5Tilemap;
    [SerializeField] private Tilemap platform6Tilemap;

    [Header("Appearance Sequence")]
    [SerializeField, Min(0f)] private float appearanceStartDelay = 0f;
    [SerializeField, Min(0f)] private float platformRiseInterval = 0.7f;
    [SerializeField, Min(0f)] private float platformRiseDistance = 1.5f;
    [SerializeField, Min(0f)] private float platformRiseDuration = 0.85f;
    [SerializeField] private AnimationCurve platformRiseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Disappearance Sequence")]
    [SerializeField, Min(0f)] private float disappearanceStartDelay = 0f;

    [Header("Timed Platform Rules")]
    [SerializeField, Min(0f)] private float initialPlatformDuration = 5f;
    [SerializeField, Min(0f)] private float repeatedPlatformDurationBonus = 3f;
    [SerializeField, Min(0f)] private float maxPlatformDuration = 9f;
    [SerializeField, Min(0f)] private float minimumTeleportPlatformDuration = 3f;
    [SerializeField, Min(0f)] private float bossStandDurationBonusPerSecond = 1.2f;
    [SerializeField, Min(0f)] private float bossStandSurfaceTolerance = 0.2f;

    [Header("Editor")]
    [SerializeField] private bool startHidden = true;

    private Tilemap[] platformTilemaps;
    private TilemapRenderer[] platformRenderers;
    private TilemapCollider2D[] platformColliders;
    private Rigidbody2D[] platformBodies;
    private CompositeCollider2D[] platformComposites;
    private Coroutine[] platformTransitionRoutines;
    private readonly bool[] timedPlatformActive = new bool[PlatformCount];
    private readonly float[] timedPlatformRemainingDurations = new float[PlatformCount];
    private readonly float[] bossStandDurationAccumulators = new float[PlatformCount];
    private Vector3[] platformRestWorldPositions = new Vector3[PlatformCount];
    private bool[] platformRestWorldPositionCached = new bool[PlatformCount];
    private bool skill5PlatformLockActive;
#if UNITY_EDITOR
    private double lastEditorTimeSample;
#endif

    public bool IsVisible { get; private set; }

    private void Awake()
    {
        ResolveReferences();
        CachePlatformRestWorldPositions();

        if (startHidden)
        {
            SetHidden();
        }
        else
        {
            SetAllVisible();
        }
    }

    private void OnValidate()
    {
        ResolveReferences();

        appearanceStartDelay = Mathf.Max(0f, appearanceStartDelay);
        platformRiseInterval = Mathf.Max(0f, platformRiseInterval);
        platformRiseDistance = Mathf.Max(0f, platformRiseDistance);
        platformRiseDuration = Mathf.Max(0f, platformRiseDuration);
        disappearanceStartDelay = Mathf.Max(0f, disappearanceStartDelay);
        initialPlatformDuration = Mathf.Max(0f, initialPlatformDuration);
        repeatedPlatformDurationBonus = Mathf.Max(0f, repeatedPlatformDurationBonus);
        maxPlatformDuration = Mathf.Max(0f, maxPlatformDuration);
        minimumTeleportPlatformDuration = Mathf.Max(0f, minimumTeleportPlatformDuration);
        bossStandDurationBonusPerSecond = Mathf.Max(0f, bossStandDurationBonusPerSecond);
        bossStandSurfaceTolerance = Mathf.Max(0f, bossStandSurfaceTolerance);

        if (!Application.isPlaying)
        {
            CachePlatformRestWorldPositions();
        }
    }

    public IEnumerator PlayAppearanceSequence()
    {
        SetHidden();
        if (appearanceStartDelay > 0f)
        {
            yield return new WaitForSeconds(appearanceStartDelay);
        }

        for (int i = 0; i < PlatformCount; i++)
        {
            yield return PlayPlatformRiseSequence(i);
            if (platformRiseInterval > 0f && i < PlatformCount - 1)
            {
                yield return new WaitForSeconds(platformRiseInterval);
            }
        }
    }

    public IEnumerator PlayDisappearanceSequence()
    {
        SetAllVisible();
        if (disappearanceStartDelay > 0f)
        {
            yield return new WaitForSeconds(disappearanceStartDelay);
        }

        for (int i = PlatformCount - 1; i >= 0; i--)
        {
            yield return PlayPlatformSinkSequence(i);
            if (platformRiseInterval > 0f && i > 0)
            {
                yield return new WaitForSeconds(platformRiseInterval);
            }
        }
    }

    public void SetHidden()
    {
        IsVisible = false;
        StopPlatformTransitions();
        ResetPlatformWorldPositions();
        for (int i = 0; i < PlatformCount; i++)
        {
            SetPlatformVisible(i, false);
            SetPlatformPhysics(i, false);
        }
    }

    public void SetAllVisible()
    {
        IsVisible = true;
        StopPlatformTransitions();
        ResetPlatformWorldPositions();
        for (int i = 0; i < PlatformCount; i++)
        {
            SetPlatformVisible(i, true);
            SetPlatformPhysics(i, true);
        }
    }

    public void SetPlatform1() => SetOnlyPlatformVisible(0);
    public void SetPlatform2() => SetOnlyPlatformVisible(1);
    public void SetPlatform3() => SetOnlyPlatformVisible(2);
    public void SetPlatform4() => SetOnlyPlatformVisible(3);
    public void SetPlatform5() => SetOnlyPlatformVisible(4);
    public void SetPlatform6() => SetOnlyPlatformVisible(5);

    public void ResetTimedPlatforms()
    {
        skill5PlatformLockActive = false;
        for (int i = 0; i < PlatformCount; i++)
        {
            timedPlatformActive[i] = false;
            timedPlatformRemainingDurations[i] = 0f;
            bossStandDurationAccumulators[i] = 0f;
        }

        SetHidden();
    }

    public void ActivateTimedPlatform(int platformIndex)
    {
        if (!IsValidPlatformIndex(platformIndex))
        {
            return;
        }

        ResolveReferences();
        if (maxPlatformDuration <= 0f)
        {
            return;
        }

        float durationToAdd = timedPlatformActive[platformIndex] ? repeatedPlatformDurationBonus : initialPlatformDuration;
        timedPlatformRemainingDurations[platformIndex] = Mathf.Min(
            maxPlatformDuration,
            timedPlatformRemainingDurations[platformIndex] + durationToAdd);
        timedPlatformActive[platformIndex] = timedPlatformRemainingDurations[platformIndex] > 0f;
        bossStandDurationAccumulators[platformIndex] = 0f;

        if (timedPlatformActive[platformIndex] && !IsPlatformPhysicsEnabled(platformIndex))
        {
            StartPlatformTransition(platformIndex, rise: true);
        }
    }

    public void TickTimedPlatforms(float deltaTime, Collider2D bossCollider)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        ResolveReferences();
        if (skill5PlatformLockActive)
        {
            HoldPlatformsDuringSkill5();
            return;
        }

        ExtendPlatformUnderBoss(deltaTime, bossCollider);

        for (int i = 0; i < PlatformCount; i++)
        {
            if (!timedPlatformActive[i])
            {
                continue;
            }

            timedPlatformRemainingDurations[i] -= deltaTime;
            if (timedPlatformRemainingDurations[i] > 0f)
            {
                continue;
            }

            timedPlatformRemainingDurations[i] = 0f;
            timedPlatformActive[i] = false;
            bossStandDurationAccumulators[i] = 0f;
            StartPlatformTransition(i, rise: false);
        }
    }

    public bool TryGetRandomTeleportDestination(Transform teleporter, int maxAttempts, out Vector2 destination)
    {
        destination = Vector2.zero;
        ResolveReferences();

        List<int> eligibleIndices = new List<int>();
        for (int i = 0; i < PlatformCount; i++)
        {
            if (IsPlatformEligibleForTeleport(i, minimumTeleportPlatformDuration))
            {
                eligibleIndices.Add(i);
            }
        }

        if (eligibleIndices.Count == 0)
        {
            return false;
        }

        int attempts = Mathf.Max(1, maxAttempts);
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            int platformIndex = eligibleIndices[UnityEngine.Random.Range(0, eligibleIndices.Count)];
            if (!TryGetPlatformBounds(platformIndex, out Bounds bounds))
            {
                continue;
            }

            float inset = Mathf.Min(.35f, bounds.size.x * .2f);
            float minX = bounds.min.x + inset;
            float maxX = bounds.max.x - inset;
            if (minX > maxX)
            {
                minX = bounds.min.x;
                maxX = bounds.max.x;
            }

            float x = UnityEngine.Random.Range(minX, maxX);
            destination = BuildTeleportRootPosition(teleporter, x, bounds.max.y);
            return true;
        }

        return false;
    }

    public bool TryGetTeleportPlatformCenterX(int platformIndex, out float centerX)
    {
        centerX = 0f;
        ResolveReferences();

        if (!TryGetPlatformBounds(platformIndex, out Bounds bounds))
        {
            return false;
        }

        centerX = bounds.center.x;
        return true;
    }

    public bool TryGetTeleportPlatformBounds(int platformIndex, out Bounds bounds)
    {
        ResolveReferences();
        return TryGetPlatformBounds(platformIndex, out bounds);
    }

    public bool TryForceRandomTimedPlatformRise()
    {
        ResolveReferences();

        if (platformTilemaps == null)
        {
            return false;
        }

        List<int> availableIndices = new List<int>();
        for (int i = 0; i < PlatformCount; i++)
        {
            if (platformTilemaps[i] != null)
            {
                availableIndices.Add(i);
            }
        }

        if (availableIndices.Count == 0)
        {
            return false;
        }

        int platformIndex = availableIndices[UnityEngine.Random.Range(0, availableIndices.Count)];
        ActivateTimedPlatform(platformIndex);
        return true;
    }

    public void BeginSkill5PlatformLock()
    {
        ResolveReferences();
        skill5PlatformLockActive = true;
        StopPlatformTransitions();
        ResetPlatformWorldPositions();
        IsVisible = true;

        for (int i = 0; i < PlatformCount; i++)
        {
            if (platformTilemaps == null || platformTilemaps[i] == null)
            {
                continue;
            }

            timedPlatformActive[i] = true;
            timedPlatformRemainingDurations[i] = Mathf.Max(maxPlatformDuration, initialPlatformDuration, 1f);
            bossStandDurationAccumulators[i] = 0f;
            SetPlatformVisible(i, true);
            SetPlatformPhysics(i, true);
        }
    }

    public void EndSkill5PlatformLock()
    {
        skill5PlatformLockActive = false;
    }

    public IEnumerator ForceAllTimedPlatformsDownRoutine()
    {
        ResolveReferences();
        StopPlatformTransitions();

        bool hasAnyPlatformToLower = false;
        for (int i = 0; i < PlatformCount; i++)
        {
            timedPlatformActive[i] = false;
            timedPlatformRemainingDurations[i] = 0f;
            bossStandDurationAccumulators[i] = 0f;

            if (!IsPlatformVisibleOrActive(i))
            {
                SetPlatformPhysics(i, false);
                SetPlatformVisible(i, false);
                continue;
            }

            hasAnyPlatformToLower = true;
            StartPlatformTransition(i, rise: false);
        }

        if (!hasAnyPlatformToLower)
        {
            IsVisible = false;
            yield break;
        }

        while (HasRunningPlatformTransition())
        {
            yield return null;
        }

        IsVisible = false;
    }

    public bool TryForceTimedPlatformDown(Collider2D hitCollider)
    {
        if (skill5PlatformLockActive)
        {
            return false;
        }

        if (hitCollider == null)
        {
            return false;
        }

        ResolveReferences();

        for (int i = 0; i < PlatformCount; i++)
        {
            if (!IsPlatformColliderMatch(i, hitCollider))
            {
                continue;
            }

            return ForceTimedPlatformDown(i);
        }

        return false;
    }

    public IEnumerator ActivateRandomTimedPlatformsRoutine(int platformCount, float interval)
    {
        ResolveReferences();

        int count = Mathf.Clamp(platformCount, 0, PlatformCount);
        if (count <= 0)
        {
            yield break;
        }

        List<int> availableIndices = new List<int>();
        for (int i = 0; i < PlatformCount; i++)
        {
            if (platformTilemaps != null && platformTilemaps[i] != null)
            {
                availableIndices.Add(i);
            }
        }

        for (int activated = 0; activated < count && availableIndices.Count > 0; activated++)
        {
            int listIndex = UnityEngine.Random.Range(0, availableIndices.Count);
            int platformIndex = availableIndices[listIndex];
            availableIndices.RemoveAt(listIndex);
            ActivateTimedPlatform(platformIndex);

            if (activated < count - 1 && interval > 0f)
            {
                yield return new WaitForSeconds(interval);
            }
        }

        while (HasRunningPlatformTransition())
        {
            yield return null;
        }
    }

    private IEnumerator PlayPlatformRiseSequence(int platformIndex)
    {
        if (!IsValidPlatformIndex(platformIndex))
        {
            yield break;
        }

        Tilemap tilemap = platformTilemaps[platformIndex];
        if (tilemap == null)
        {
            yield break;
        }

        Vector3 restPosition = GetPlatformRestWorldPosition(platformIndex);
        Vector3 startPosition = GetPlatformRiseStartWorldPosition(platformIndex, restPosition);

        if (platformRiseDistance <= 0f || platformRiseDuration <= 0f)
        {
            tilemap.transform.position = restPosition;
            SetPlatformVisible(platformIndex, true);
            SetPlatformPhysics(platformIndex, true);
            yield break;
        }

        tilemap.transform.position = startPosition;
        SetPlatformVisible(platformIndex, true);
        SetPlatformPhysics(platformIndex, false);
        BeginPlatformRiseTiming();

        float elapsed = 0f;
        while (elapsed < platformRiseDuration)
        {
            elapsed += GetPlatformRiseDeltaTime();
            float t = Mathf.Clamp01(elapsed / platformRiseDuration);
            float easedT = EvaluatePlatformRiseCurve(t);
            tilemap.transform.position = Vector3.LerpUnclamped(startPosition, restPosition, easedT);
            yield return null;
        }

        tilemap.transform.position = restPosition;
        SetPlatformPhysics(platformIndex, true);
    }

    private IEnumerator PlayPlatformSinkSequence(int platformIndex)
    {
        if (skill5PlatformLockActive)
        {
            yield break;
        }

        if (!IsValidPlatformIndex(platformIndex))
        {
            yield break;
        }

        Tilemap tilemap = platformTilemaps[platformIndex];
        if (tilemap == null)
        {
            yield break;
        }

        Vector3 restPosition = GetPlatformRestWorldPosition(platformIndex);
        Vector3 startPosition = GetPlatformRiseStartWorldPosition(platformIndex, restPosition);

        if (platformRiseDistance <= 0f || platformRiseDuration <= 0f)
        {
            tilemap.transform.position = restPosition;
            SetPlatformVisible(platformIndex, false);
            SetPlatformPhysics(platformIndex, false);
            yield break;
        }

        tilemap.transform.position = restPosition;
        SetPlatformVisible(platformIndex, true);
        SetPlatformPhysics(platformIndex, false);
        BeginPlatformRiseTiming();

        float elapsed = 0f;
        while (elapsed < platformRiseDuration)
        {
            elapsed += GetPlatformRiseDeltaTime();
            float t = Mathf.Clamp01(elapsed / platformRiseDuration);
            float easedT = EvaluatePlatformRiseCurve(t);
            tilemap.transform.position = Vector3.LerpUnclamped(restPosition, startPosition, easedT);
            yield return null;
        }

        tilemap.transform.position = restPosition;
        SetPlatformVisible(platformIndex, false);
        SetPlatformPhysics(platformIndex, false);
    }

    private void ResolveReferences()
    {
        if (groundReferencePoint == null)
        {
            Transform child = transform.Find("GroundReferencePoint");
            if (child != null)
            {
                groundReferencePoint = child;
            }
        }

        if (platform1Tilemap == null)
        {
            platform1Tilemap = FindTilemap("Platform_1");
        }

        if (platform2Tilemap == null)
        {
            platform2Tilemap = FindTilemap("Platform_2");
        }

        if (platform3Tilemap == null)
        {
            platform3Tilemap = FindTilemap("Platform_3");
        }

        if (platform4Tilemap == null)
        {
            platform4Tilemap = FindTilemap("Platform_4");
        }

        if (platform5Tilemap == null)
        {
            platform5Tilemap = FindTilemap("Platform_5");
        }

        if (platform6Tilemap == null)
        {
            platform6Tilemap = FindTilemap("Platform_6");
        }

        platformTilemaps ??= new Tilemap[PlatformCount];
        platformRenderers ??= new TilemapRenderer[PlatformCount];
        platformColliders ??= new TilemapCollider2D[PlatformCount];
        platformBodies ??= new Rigidbody2D[PlatformCount];
        platformComposites ??= new CompositeCollider2D[PlatformCount];
        platformTransitionRoutines ??= new Coroutine[PlatformCount];

        platformTilemaps[0] = platform1Tilemap;
        platformTilemaps[1] = platform2Tilemap;
        platformTilemaps[2] = platform3Tilemap;
        platformTilemaps[3] = platform4Tilemap;
        platformTilemaps[4] = platform5Tilemap;
        platformTilemaps[5] = platform6Tilemap;

        for (int i = 0; i < PlatformCount; i++)
        {
            Tilemap tilemap = platformTilemaps[i];
            platformRenderers[i] = tilemap != null ? tilemap.GetComponent<TilemapRenderer>() : null;
            platformColliders[i] = tilemap != null ? tilemap.GetComponent<TilemapCollider2D>() : null;
            platformBodies[i] = tilemap != null ? tilemap.GetComponent<Rigidbody2D>() : null;
            platformComposites[i] = tilemap != null ? tilemap.GetComponent<CompositeCollider2D>() : null;
            EnsurePlatformUsesGroundLayer(tilemap);
        }

        if (!Application.isPlaying)
        {
            CachePlatformRestWorldPositions();
        }
    }

    private Tilemap FindTilemap(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<Tilemap>() : null;
    }

    private void SetRendererAndCollider(TilemapRenderer renderer, Collider2D collider2D, bool enabled)
    {
        if (renderer != null)
        {
            renderer.enabled = enabled;
        }

        if (collider2D != null)
        {
            collider2D.enabled = enabled;
        }
    }

    private void SetPlatformVisible(int platformIndex, bool visible)
    {
        if (!IsValidPlatformIndex(platformIndex))
        {
            return;
        }

        TilemapRenderer renderer = platformRenderers[platformIndex];
        TilemapCollider2D collider2D = platformColliders[platformIndex];
        SetRendererAndCollider(renderer, collider2D, visible);
    }

    private void SetPlatformPhysics(int platformIndex, bool enabled)
    {
        if (!IsValidPlatformIndex(platformIndex))
        {
            return;
        }

        TilemapCollider2D collider2D = platformColliders[platformIndex];
        if (collider2D != null)
        {
            collider2D.enabled = enabled;
        }

        CompositeCollider2D composite = platformComposites[platformIndex];
        if (composite != null)
        {
            composite.enabled = enabled;
        }

        Rigidbody2D body = platformBodies[platformIndex];
        if (body != null)
        {
            body.simulated = enabled;
        }
    }

    private void SetOnlyPlatformVisible(int platformIndex)
    {
        if (!IsValidPlatformIndex(platformIndex))
        {
            return;
        }

        IsVisible = true;
        StopPlatformTransitions();
        ResetPlatformWorldPositions();

        for (int i = 0; i < PlatformCount; i++)
        {
            bool visible = i == platformIndex;
            SetPlatformVisible(i, visible);
            SetPlatformPhysics(i, visible);
        }
    }

    private void ResetPlatformWorldPositions()
    {
        for (int i = 0; i < PlatformCount; i++)
        {
            if (platformTilemaps[i] != null)
            {
                platformTilemaps[i].transform.position = GetPlatformRestWorldPosition(i);
            }
        }
    }

    private Vector3 GetPlatformRestWorldPosition(int platformIndex)
    {
        if (!IsValidPlatformIndex(platformIndex))
        {
            return Vector3.zero;
        }

        if (!platformRestWorldPositionCached[platformIndex])
        {
            if (!Application.isPlaying)
            {
                CachePlatformRestWorldPositions();
            }
        }

        return platformRestWorldPositions[platformIndex];
    }

    private Vector3 GetPlatformRiseStartWorldPosition(int platformIndex, Vector3 restPosition)
    {
        float groundY = groundReferencePoint != null ? groundReferencePoint.position.y : restPosition.y;
        return new Vector3(restPosition.x, groundY - platformRiseDistance, restPosition.z);
    }

    private void CachePlatformRestWorldPositions()
    {
        for (int i = 0; i < PlatformCount; i++)
        {
            if (platformTilemaps != null && i < platformTilemaps.Length && platformTilemaps[i] != null)
            {
                platformRestWorldPositions[i] = platformTilemaps[i].transform.position;
                platformRestWorldPositionCached[i] = true;
            }
            else
            {
                platformRestWorldPositions[i] = Vector3.zero;
                platformRestWorldPositionCached[i] = false;
            }
        }
    }

    private void BeginPlatformRiseTiming()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            lastEditorTimeSample = EditorApplication.timeSinceStartup;
        }
#endif
    }

    private float GetPlatformRiseDeltaTime()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            double now = EditorApplication.timeSinceStartup;
            float delta = lastEditorTimeSample > 0d ? (float)(now - lastEditorTimeSample) : 0f;
            lastEditorTimeSample = now;
            return Mathf.Max(0f, delta);
        }
#endif
        return Time.deltaTime;
    }

    private float EvaluatePlatformRiseCurve(float t)
    {
        if (platformRiseCurve == null || platformRiseCurve.length == 0)
        {
            return Mathf.SmoothStep(0f, 1f, t);
        }

        return Mathf.Clamp01(platformRiseCurve.Evaluate(Mathf.Clamp01(t)));
    }

    private void EnsurePlatformUsesGroundLayer(Tilemap tilemap)
    {
        if (tilemap == null)
        {
            return;
        }

        int groundLayer = LayerMask.NameToLayer(GroundLayerName);
        if (groundLayer >= 0 && tilemap.gameObject.layer != groundLayer)
        {
            tilemap.gameObject.layer = groundLayer;
        }
    }

    private bool IsValidPlatformIndex(int platformIndex)
    {
        return platformIndex >= 0 && platformIndex < PlatformCount;
    }

    private void ExtendPlatformUnderBoss(float deltaTime, Collider2D bossCollider)
    {
        if (bossCollider == null || bossStandDurationBonusPerSecond <= 0f || maxPlatformDuration <= 0f)
        {
            return;
        }

        for (int i = 0; i < PlatformCount; i++)
        {
            if (!timedPlatformActive[i] || !IsBossStandingOnPlatform(i, bossCollider))
            {
                bossStandDurationAccumulators[i] = 0f;
                continue;
            }

            bossStandDurationAccumulators[i] += deltaTime;
            while (bossStandDurationAccumulators[i] >= 1f)
            {
                bossStandDurationAccumulators[i] -= 1f;
                timedPlatformRemainingDurations[i] = Mathf.Min(
                    maxPlatformDuration,
                    timedPlatformRemainingDurations[i] + bossStandDurationBonusPerSecond);
            }
        }
    }

    private bool IsBossStandingOnPlatform(int platformIndex, Collider2D bossCollider)
    {
        if (!IsPlatformPhysicsEnabled(platformIndex) || !TryGetPlatformBounds(platformIndex, out Bounds platformBounds))
        {
            return false;
        }

        Bounds bossBounds = bossCollider.bounds;
        bool overlapsHorizontally = bossBounds.max.x >= platformBounds.min.x && bossBounds.min.x <= platformBounds.max.x;
        if (!overlapsHorizontally)
        {
            return false;
        }

        float verticalDistance = bossBounds.min.y - platformBounds.max.y;
        return verticalDistance >= -.02f && verticalDistance <= bossStandSurfaceTolerance;
    }

    private void StartPlatformTransition(int platformIndex, bool rise)
    {
        if (!IsValidPlatformIndex(platformIndex))
        {
            return;
        }

        if (skill5PlatformLockActive)
        {
            return;
        }

        if (platformTransitionRoutines == null)
        {
            platformTransitionRoutines = new Coroutine[PlatformCount];
        }

        if (platformTransitionRoutines[platformIndex] != null)
        {
            StopCoroutine(platformTransitionRoutines[platformIndex]);
        }

        platformTransitionRoutines[platformIndex] = StartCoroutine(PlayTimedPlatformTransition(platformIndex, rise));
    }

    private bool ForceTimedPlatformDown(int platformIndex)
    {
        if (!IsValidPlatformIndex(platformIndex))
        {
            return false;
        }

        ResolveReferences();
        StopPlatformTransition(platformIndex);

        timedPlatformActive[platformIndex] = false;
        timedPlatformRemainingDurations[platformIndex] = 0f;
        bossStandDurationAccumulators[platformIndex] = 0f;

        if (!IsPlatformVisibleOrActive(platformIndex))
        {
            SetPlatformPhysics(platformIndex, false);
            SetPlatformVisible(platformIndex, false);
            return true;
        }

        StartPlatformTransition(platformIndex, rise: false);
        return true;
    }

    private void StopPlatformTransition(int platformIndex)
    {
        if (!IsValidPlatformIndex(platformIndex) || platformTransitionRoutines == null)
        {
            return;
        }

        if (platformTransitionRoutines[platformIndex] != null)
        {
            StopCoroutine(platformTransitionRoutines[platformIndex]);
            platformTransitionRoutines[platformIndex] = null;
        }
    }

    private IEnumerator PlayTimedPlatformTransition(int platformIndex, bool rise)
    {
        if (rise)
        {
            yield return PlayPlatformRiseSequence(platformIndex);
        }
        else
        {
            yield return PlayPlatformSinkSequence(platformIndex);
        }

        if (platformTransitionRoutines != null && IsValidPlatformIndex(platformIndex))
        {
            platformTransitionRoutines[platformIndex] = null;
        }
    }

    private void StopPlatformTransitions()
    {
        if (platformTransitionRoutines == null)
        {
            return;
        }

        for (int i = 0; i < platformTransitionRoutines.Length; i++)
        {
            if (platformTransitionRoutines[i] != null)
            {
                StopCoroutine(platformTransitionRoutines[i]);
                platformTransitionRoutines[i] = null;
            }
        }
    }

    private void HoldPlatformsDuringSkill5()
    {
        StopPlatformTransitions();
        ResetPlatformWorldPositions();

        for (int i = 0; i < PlatformCount; i++)
        {
            if (platformTilemaps == null || platformTilemaps[i] == null)
            {
                continue;
            }

            timedPlatformActive[i] = true;
            timedPlatformRemainingDurations[i] = Mathf.Max(maxPlatformDuration, initialPlatformDuration, 1f);
            bossStandDurationAccumulators[i] = 0f;
            SetPlatformVisible(i, true);
            SetPlatformPhysics(i, true);
        }
    }

    private bool IsPlatformEligibleForTeleport(int platformIndex, float minimumRemainingDuration)
    {
        return IsValidPlatformIndex(platformIndex)
            && timedPlatformActive[platformIndex]
            && timedPlatformRemainingDurations[platformIndex] >= minimumRemainingDuration
            && platformTransitionRoutines != null
            && platformTransitionRoutines[platformIndex] == null
            && IsPlatformPhysicsEnabled(platformIndex);
    }

    private bool HasRunningPlatformTransition()
    {
        if (platformTransitionRoutines == null)
        {
            return false;
        }

        for (int i = 0; i < platformTransitionRoutines.Length; i++)
        {
            if (platformTransitionRoutines[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPlatformVisibleOrActive(int platformIndex)
    {
        if (!IsValidPlatformIndex(platformIndex))
        {
            return false;
        }

        if (timedPlatformActive[platformIndex])
        {
            return true;
        }

        TilemapRenderer renderer = platformRenderers != null ? platformRenderers[platformIndex] : null;
        TilemapCollider2D collider2D = platformColliders != null ? platformColliders[platformIndex] : null;
        return renderer != null && renderer.enabled || collider2D != null && collider2D.enabled;
    }

    private bool IsPlatformColliderMatch(int platformIndex, Collider2D hitCollider)
    {
        if (!IsValidPlatformIndex(platformIndex) || hitCollider == null || platformTilemaps == null)
        {
            return false;
        }

        Tilemap tilemap = platformTilemaps[platformIndex];
        if (tilemap == null)
        {
            return false;
        }

        return hitCollider.transform == tilemap.transform
            || hitCollider.transform.IsChildOf(tilemap.transform)
            || tilemap.transform.IsChildOf(hitCollider.transform);
    }

    private bool IsPlatformPhysicsEnabled(int platformIndex)
    {
        if (!IsValidPlatformIndex(platformIndex))
        {
            return false;
        }

        TilemapCollider2D collider2D = platformColliders != null ? platformColliders[platformIndex] : null;
        return collider2D != null && collider2D.enabled;
    }

    private bool TryGetPlatformBounds(int platformIndex, out Bounds bounds)
    {
        bounds = default;
        if (!IsValidPlatformIndex(platformIndex) || platformTilemaps == null || platformTilemaps[platformIndex] == null)
        {
            return false;
        }

        Tilemap tilemap = platformTilemaps[platformIndex];
        if (!TryGetTilemapWorldBounds(tilemap, out bounds))
        {
            return false;
        }

        return bounds.size.x > 0f && bounds.size.y > 0f;
    }

    private bool TryGetTilemapWorldBounds(Tilemap tilemap, out Bounds bounds)
    {
        bounds = default;
        if (tilemap == null)
        {
            return false;
        }

        BoundsInt cellBounds = tilemap.cellBounds;
        if (cellBounds.size.x <= 0 || cellBounds.size.y <= 0)
        {
            return false;
        }

        Vector3 cellSize = tilemap.layoutGrid != null ? tilemap.layoutGrid.cellSize : Vector3.one;
        Vector3 scale = tilemap.transform.lossyScale;
        Vector3 halfCellSize = new Vector3(
            Mathf.Abs(cellSize.x * scale.x) * 0.5f,
            Mathf.Abs(cellSize.y * scale.y) * 0.5f,
            Mathf.Abs(cellSize.z * scale.z) * 0.5f);

        bool hasAnyTile = false;
        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;

        foreach (Vector3Int cell in cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(cell))
            {
                continue;
            }

            Vector3 worldCenter = tilemap.GetCellCenterWorld(cell);
            Vector3 cellMin = worldCenter - halfCellSize;
            Vector3 cellMax = worldCenter + halfCellSize;

            if (!hasAnyTile)
            {
                min = cellMin;
                max = cellMax;
                hasAnyTile = true;
                continue;
            }

            min = Vector3.Min(min, cellMin);
            max = Vector3.Max(max, cellMax);
        }

        if (!hasAnyTile)
        {
            return false;
        }

        bounds.SetMinMax(min, max);
        return true;
    }

    private Vector2 BuildTeleportRootPosition(Transform teleporter, float groundX, float groundY)
    {
        if (teleporter == null)
        {
            return new Vector2(groundX, groundY);
        }

        Collider2D collider2D = teleporter.GetComponent<Collider2D>();
        if (collider2D == null)
        {
            return new Vector2(groundX, groundY);
        }

        float rootToColliderCenterX = teleporter.position.x - collider2D.bounds.center.x;
        float rootToColliderBottomY = teleporter.position.y - collider2D.bounds.min.y;
        return new Vector2(groundX + rootToColliderCenterX, groundY + rootToColliderBottomY + .02f);
    }

}
