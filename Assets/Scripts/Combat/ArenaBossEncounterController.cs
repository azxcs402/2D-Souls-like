using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class ArenaBossEncounterController : MonoBehaviour
{
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

    [Header("Boss")]
    [SerializeField] private Enemy bossEnemy;
    [SerializeField] private GameObject bossPrefab;
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

    [Header("Abyss Power")]
    [SerializeField] private AbyssPower[] abyssPowers = Array.Empty<AbyssPower>();

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
    private Entity_Health bossHealth;
    private IBossSkillPointSource bossSkillPointSource;
    private bool encounterStarted;
    private bool encounterCompleted;
    private int currentBossPhase = 1;
    private int currentSkillPoints;

    public int CurrentBossPhase => currentBossPhase;
    public int CurrentSkillPoints => currentSkillPoints;
    public string ActiveEnemySummary => BuildActiveEnemySummary();

    private void Awake()
    {
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

    private void OnDisable()
    {
        UnbindBossEnemy();
        StopAllInternalRoutines();
    }

    private void OnDestroy()
    {
        UnbindBossEnemy();
        StopAllInternalRoutines();
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

        SetDoorsLocked(lockDoorsWhenEncounterStarts);
        SetAbyssFireVisibleCount(0);
        PreviewSkillPointBackgroundVisibleCount(0);
        PreviewAbyssPowerVisibleCount(0);
        if (bossFloatingPlatform != null)
        {
            bossFloatingPlatform.SetHidden();
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

        if (bossFloatingPlatform != null)
        {
            bossFloatingPlatformRoutine = StartCoroutine(bossFloatingPlatform.PlayAppearanceSequence());
        }
    }

    public void ResetEncounter()
    {
        StopAllInternalRoutines();
        UnbindBossEnemy();

        encounterStarted = false;
        encounterCompleted = false;
        currentBossPhase = 1;
        currentSkillPoints = 0;

        SetDoorsLocked(false);
        SetAbyssFireVisibleCount(0);
        PreviewSkillPointBackgroundVisibleCount(0);
        PreviewAbyssPowerVisibleCount(0);
        ApplyPhaseState(currentBossPhase, preview: true);
        if (bossFloatingPlatform != null)
        {
            bossFloatingPlatform.SetHidden();
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

        if (bossTarget == null)
        {
            Transform foundTarget = FindDeepChild(transform, "BossTarget") ?? FindDeepChild(transform, "BossCameraTarget");
            if (foundTarget != null)
            {
                bossTarget = foundTarget;
            }
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
        }

        if (enemy is IBossSkillPointSource skillPointSource)
        {
            bossSkillPointSource = skillPointSource;
            bossSkillPointSource.MeleeAttackCompleted -= HandleBossMeleeAttackCompleted;
            bossSkillPointSource.MeleeAttackCompleted += HandleBossMeleeAttackCompleted;
        }

        if (bossHealthBar != null && bossHealth != null)
        {
            bossHealthBar.BindBossHealth(bossHealth);
        }
    }

    private void UnbindBossEnemy()
    {
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

        bossSkillPointSource = null;
        bossHealth = null;

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

        if (bossEnemy != null && bossHealthBar != null)
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
        yield return RevealAbyssFiresAndSpawnBossRoutine();

        if (bossEnemy != null && bossHealthBar != null)
        {
            bossHealth = ResolveBossHealth(bossEnemy);
            if (bossHealth != null)
            {
                bossHealthBar.BindBossHealth(bossHealth);
            }
        }

        StartBossCameraReturnToPlayer();

        ApplyPhaseState(currentBossPhase, preview: false);
    }

    private IEnumerator RevealAbyssFiresAndSpawnBossRoutine()
    {
        if (abyssFires != null && abyssFires.Length > 0)
        {
            for (int i = 0; i < abyssFires.Length; i++)
            {
                SetAbyssFireVisible(i, true);
                if (abyssFireRevealDelay > 0f)
                {
                    yield return new WaitForSeconds(abyssFireRevealDelay);
                }
            }
        }

        ActivateBossAfterAbyssFires();
        yield break;
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
    }

    private void HandleBossDied(Enemy enemy)
    {
        encounterCompleted = true;
        SetDoorsLocked(false);
        RestoreBossCamera();
        ReleasePlayerAfterBossIntro();

        if (bossHealthBar != null)
        {
            bossHealthBar.Hide();
        }
    }

    private void HandleBossMeleeAttackCompleted()
    {
        AddSkillPoint(1);
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
    }

    private IEnumerator RevealAbyssFiresRoutine()
    {
        if (abyssFires == null || abyssFires.Length == 0)
        {
            yield break;
        }

        for (int i = 0; i < abyssFires.Length; i++)
        {
            SetAbyssFireVisible(i, true);
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

    private void SetAbyssFireVisibleCount(int visibleCount)
    {
        visibleCount = Mathf.Clamp(visibleCount, 0, abyssFires != null ? abyssFires.Length : 0);
        for (int i = 0; i < abyssFires.Length; i++)
        {
            SetAbyssFireVisible(i, i < visibleCount);
        }
    }

    private void SetAbyssFireVisible(int index, bool visible)
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

        fire.gameObject.SetActive(visible);
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
        return count;
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        if (player != null && other.GetComponentInParent<Player>() == player)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(playerTag) && other.CompareTag(playerTag))
        {
            return true;
        }

        return other.GetComponentInParent<Player>() != null;
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

    [Header("Editor")]
    [SerializeField] private bool startHidden = true;

    private Tilemap[] platformTilemaps;
    private TilemapRenderer[] platformRenderers;
    private TilemapCollider2D[] platformColliders;
    private Rigidbody2D[] platformBodies;
    private CompositeCollider2D[] platformComposites;
    private Vector3[] platformRestWorldPositions = new Vector3[PlatformCount];
    private bool[] platformRestWorldPositionCached = new bool[PlatformCount];
#if UNITY_EDITOR
    private double lastEditorTimeSample;
#endif

    public bool IsVisible { get; private set; }

    private void Awake()
    {
        ResolveReferences();

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

        CachePlatformRestWorldPositions();
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
            CachePlatformRestWorldPositions();
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
}
