using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class ArenaEncounterController : MonoBehaviour
{
    [Header("Wave Data")]
    [SerializeField] private ArenaWaveSet waveSet;
    [SerializeField] private Transform[] spawnPoints = new Transform[0];
    [SerializeField, Min(0f)] private float defaultDelayBeforeSpawn = 0.25f;
    [SerializeField, Min(0f)] private float defaultDelayAfterClear = 0.75f;
    [SerializeField] private bool showSpawnPointGizmos = true;

    [Header("Doors")]
    [SerializeField] private ArenaDoorController[] doors = new ArenaDoorController[0];
    [SerializeField] private bool lockDoorsWhenEncounterStarts = true;

    [Header("Floating Platform")]
    [SerializeField] private ArenaFloatingPlatformController floatingPlatform;
    [SerializeField, Min(0)] private int showPlatformAfterWaveIndex = 1;
    [SerializeField, Min(0)] private int hidePlatformAfterWaveIndex = 2;

    [Header("Trigger")]
    [SerializeField] private bool startOnlyOnce = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Min(0f)] private float clearConfirmDelay = 0.25f;

    private readonly HashSet<Enemy> activeEnemies = new HashSet<Enemy>();
    private BoxCollider2D triggerCollider;
    private Coroutine encounterRoutine;
    private bool encounterStarted;
    private bool encounterCompleted;
    private int currentWaveIndex = -1;

    public int CurrentWaveIndex => currentWaveIndex;
    public int ActiveEnemyCount => activeEnemies.Count;
    public bool HasActiveEnemies => activeEnemies.Count > 0;
    public string ActiveEnemySummary
    {
        get
        {
            if (activeEnemies.Count == 0)
            {
                return "None";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
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
    }

    private void Awake()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
        SyncSpawnPointsFromScene();

        if (spawnPoints == null)
        {
            spawnPoints = new Transform[0];
        }

        if (doors == null)
        {
            doors = new ArenaDoorController[0];
        }

        if (waveSet == null)
        {
            return;
        }

        int maxWaveIndex = Mathf.Max(0, waveSet.Waves.Count - 1);
        showPlatformAfterWaveIndex = Mathf.Clamp(showPlatformAfterWaveIndex, 0, maxWaveIndex);
        hidePlatformAfterWaveIndex = Mathf.Clamp(hidePlatformAfterWaveIndex, 0, maxWaveIndex);

        if (defaultDelayBeforeSpawn < 0f)
        {
            defaultDelayBeforeSpawn = 0f;
        }

        if (defaultDelayAfterClear < 0f)
        {
            defaultDelayAfterClear = 0f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider2D collider = triggerCollider != null ? triggerCollider : GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            return;
        }

        Transform colliderTransform = collider.transform;
        Vector3 center = colliderTransform.TransformPoint(collider.offset);
        Vector3 size = colliderTransform.TransformVector(collider.size);
        size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));

        Gizmos.color = encounterStarted
            ? new Color(1f, 0.55f, 0f, 0.9f)
            : new Color(0.1f, 0.9f, 1f, 0.8f);

        Gizmos.DrawWireCube(center, size);

        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.12f);
        Gizmos.DrawCube(center, size);
    }

    private void OnDrawGizmos()
    {
        if (!showSpawnPointGizmos)
        {
            return;
        }

        SyncSpawnPointsFromScene();
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return;
        }

        Color pointColor = new Color(1f, 0.85f, 0.2f, 1f);
        Color labelColor = new Color(1f, 0.95f, 0.6f, 1f);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform spawnPoint = spawnPoints[i];
            if (spawnPoint == null)
            {
                continue;
            }

            Vector3 position = spawnPoint.position;
            Gizmos.color = pointColor;
            Gizmos.DrawSphere(position, 0.08f);
            Gizmos.DrawLine(position + Vector3.left * 0.12f, position + Vector3.right * 0.12f);
            Gizmos.DrawLine(position + Vector3.up * 0.12f, position + Vector3.down * 0.12f);

#if UNITY_EDITOR
            Handles.color = labelColor;
            Handles.Label(position + Vector3.up * 0.18f, spawnPoint.name);
#endif
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (encounterStarted)
        {
            RegisterEnemyFromCollider(other);
            return;
        }

        if (encounterCompleted && startOnlyOnce)
        {
            return;
        }

        if (!IsPlayerCollider(other))
        {
            return;
        }

        BeginEncounter();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (encounterStarted)
        {
            RegisterEnemyFromCollider(other);
            return;
        }

        if (!encounterStarted && !encounterCompleted && IsPlayerCollider(other))
        {
            BeginEncounter();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!encounterStarted || encounterCompleted)
        {
            return;
        }

        // Player can leave, but the encounter continues until cleared.
    }

    public void BeginEncounter()
    {
        if (encounterStarted || (encounterCompleted && startOnlyOnce))
        {
            return;
        }

        if (waveSet == null || waveSet.Waves.Count == 0)
        {
            Debug.LogWarning($"{name}: ArenaEncounterController has no waves configured.", this);
            return;
        }

        encounterStarted = true;
        encounterCompleted = false;

        if (lockDoorsWhenEncounterStarts)
        {
            SetDoorsLocked(true);
        }

        if (encounterRoutine != null)
        {
            StopCoroutine(encounterRoutine);
        }

        encounterRoutine = StartCoroutine(RunEncounter());
    }

    public void ResetEncounter()
    {
        if (encounterRoutine != null)
        {
            StopCoroutine(encounterRoutine);
            encounterRoutine = null;
        }

        UnregisterAllEnemies();
        encounterStarted = false;
        encounterCompleted = false;
        currentWaveIndex = -1;
        SetDoorsLocked(false);
    }

    private IEnumerator RunEncounter()
    {
        currentWaveIndex = 0;

        while (waveSet != null && currentWaveIndex < waveSet.Waves.Count)
        {
            ArenaWaveDefinition wave = waveSet.Waves[currentWaveIndex];
            float delayBeforeSpawn = wave != null ? wave.DelayBeforeSpawn : defaultDelayBeforeSpawn;
            if (delayBeforeSpawn > 0f)
            {
                yield return new WaitForSeconds(delayBeforeSpawn);
            }

            SpawnWave(wave);

            bool waveCleared = false;
            while (!waveCleared)
            {
                yield return new WaitUntil(() =>
                {
                    CleanupInvalidEnemies();
                    return activeEnemies.Count == 0;
                });

                if (clearConfirmDelay > 0f)
                {
                    yield return new WaitForSeconds(clearConfirmDelay);
                }

                CleanupInvalidEnemies();
                waveCleared = activeEnemies.Count == 0;
            }

            float delayAfterClear = wave != null ? wave.DelayAfterClear : defaultDelayAfterClear;
            if (delayAfterClear > 0f)
            {
                yield return new WaitForSeconds(delayAfterClear);
            }

            if (floatingPlatform != null)
            {
                if (wave != null)
                {
                    switch (wave.PlatformTransition)
                    {
                        case ArenaPlatformTransition.Show:
                            yield return floatingPlatform.PlayAppearanceSequence();
                            break;
                        case ArenaPlatformTransition.Hide:
                            yield return floatingPlatform.PlayDisappearanceSequence();
                            break;
                        default:
                            if (currentWaveIndex == 1)
                            {
                                yield return floatingPlatform.PlayAppearanceSequence();
                            }
                            else
                            {
                                yield return PlayLegacyPlatformTriggerIfNeeded();
                            }
                            break;
                    }
                }
                else
                {
                    yield return PlayLegacyPlatformTriggerIfNeeded();
                }
            }

            currentWaveIndex++;
        }

        encounterCompleted = true;
        encounterStarted = false;
        SetDoorsLocked(false);
        encounterRoutine = null;
    }

    private void SpawnWave(ArenaWaveDefinition wave)
    {
        if (wave == null)
        {
            return;
        }

        IReadOnlyList<ArenaSpawnEntry> spawns = wave.Spawns;
        for (int i = 0; i < spawns.Count; i++)
        {
            ArenaSpawnEntry entry = spawns[i];
            if (entry == null || entry.EnemyPrefab == null)
            {
                continue;
            }

            int count = entry.Count;
            for (int spawnIndex = 0; spawnIndex < count; spawnIndex++)
            {
                Transform spawnPoint = GetSpawnPoint(entry.UseRandomSpawnPoint, entry.SpawnPointIndex, spawnIndex);
                Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
                Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

                UnityEngine.Object spawnedObject = Instantiate((UnityEngine.Object)entry.EnemyPrefab, position, rotation);
                GameObject enemyObject = spawnedObject as GameObject;
                if (enemyObject == null && spawnedObject is Component spawnedComponent)
                {
                    enemyObject = spawnedComponent.gameObject;
                }

                if (enemyObject == null)
                {
                    Debug.LogWarning($"{name}: Failed to instantiate enemy prefab {entry.EnemyPrefab.name}.", this);
                    continue;
                }

                ApplySpawnOverrides(enemyObject, entry);

                RegisterSpawnedEnemy(enemyObject);
            }
        }
    }

    private void ApplySpawnOverrides(GameObject enemyObject, ArenaSpawnEntry entry)
    {
        if (enemyObject == null || entry == null)
        {
            return;
        }

        if (entry.OverrideScale)
        {
            Vector3 scale = entry.SpawnScale;
            scale.x = Mathf.Max(.01f, scale.x);
            scale.y = Mathf.Max(.01f, scale.y);
            scale.z = Mathf.Max(.01f, scale.z);
            enemyObject.transform.localScale = scale;
        }

        Enemy enemy = enemyObject.GetComponentInChildren<Enemy>(true);
        if (enemy != null && entry.OverrideMaxHealth)
        {
            enemy.SetMaxHealth(entry.MaxHealth);
        }

        Entity_Combat combat = enemyObject.GetComponentInChildren<Entity_Combat>(true);
        if (combat != null && entry.OverrideCombatDamage)
        {
            combat.SetDamage(entry.CombatDamage);
        }
    }

    private Transform GetSpawnPoint(bool useRandomSpawnPoint, int spawnPointIndex, int spawnIndex)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return transform;
        }

        if (!useRandomSpawnPoint && spawnPointIndex >= 0 && spawnPointIndex < spawnPoints.Length)
        {
            return spawnPoints[spawnPointIndex];
        }

        if (useRandomSpawnPoint)
        {
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }

        return spawnPoints[Mathf.Abs(spawnIndex) % spawnPoints.Length];
    }

    private void RegisterSpawnedEnemy(GameObject enemyObject)
    {
        if (enemyObject == null)
        {
            return;
        }

        Enemy enemy = enemyObject.GetComponentInChildren<Enemy>(true);
        if (enemy == null || enemy.IsDead)
        {
            Debug.LogWarning($"{name}: Spawned object {enemyObject.name} does not contain an Enemy component.", enemyObject);
            return;
        }

        RegisterEnemy(enemy);
    }

    private void RegisterEnemyFromCollider(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null && !enemy.IsDead)
        {
            RegisterEnemy(enemy);
        }
    }

    private void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null || enemy.IsDead || activeEnemies.Contains(enemy))
        {
            return;
        }

        activeEnemies.Add(enemy);
        enemy.OnDied += HandleEnemyDied;
    }

    private void UnregisterEnemy(Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        if (activeEnemies.Remove(enemy))
        {
            enemy.OnDied -= HandleEnemyDied;
        }
    }

    private void UnregisterAllEnemies()
    {
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.OnDied -= HandleEnemyDied;
            }
        }

        activeEnemies.Clear();
    }

    private void CleanupInvalidEnemies()
    {
        if (activeEnemies.Count == 0)
        {
            return;
        }

        List<Enemy> missingEnemies = null;
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy == null || enemy.IsDead)
            {
                missingEnemies ??= new List<Enemy>();
                missingEnemies.Add(enemy);
            }
        }

        if (missingEnemies == null)
        {
            return;
        }

        for (int i = 0; i < missingEnemies.Count; i++)
        {
            activeEnemies.Remove(missingEnemies[i]);
        }
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        UnregisterEnemy(enemy);
    }

    private void SetDoorsLocked(bool locked)
    {
        if (doors == null)
        {
            return;
        }

        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i] != null)
            {
                doors[i].SetLocked(locked);
            }
        }
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(playerTag) && other.CompareTag(playerTag))
        {
            return true;
        }

        return other.GetComponentInParent<Player>() != null;
    }

    private void ResolveReferences()
    {
        if (floatingPlatform == null)
        {
            floatingPlatform = GetComponentInChildren<ArenaFloatingPlatformController>(true);
        }
    }

    private void SyncSpawnPointsFromScene()
    {
        Transform spawnRoot = transform.Find("SpawnPoints");
        if (spawnRoot == null)
        {
            return;
        }

        List<Transform> discoveredSpawnPoints = new List<Transform>();
        CollectSpawnPoints(spawnRoot, discoveredSpawnPoints);

        if (discoveredSpawnPoints.Count == 0)
        {
            return;
        }

        discoveredSpawnPoints.Sort(CompareSpawnPointsByName);

        bool needsSync = spawnPoints == null || spawnPoints.Length != discoveredSpawnPoints.Count;
        if (!needsSync)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != discoveredSpawnPoints[i])
                {
                    needsSync = true;
                    break;
                }
            }
        }

        if (!needsSync)
        {
            return;
        }

        spawnPoints = discoveredSpawnPoints.ToArray();
    }

    private static void CollectSpawnPoints(Transform parent, List<Transform> spawnPointsList)
    {
        if (parent == null || spawnPointsList == null)
        {
            return;
        }

        foreach (Transform child in parent)
        {
            if (child == null)
            {
                continue;
            }

            if (child.name.StartsWith("SpawnPoint_", System.StringComparison.Ordinal) && !spawnPointsList.Contains(child))
            {
                spawnPointsList.Add(child);
            }
        }
    }

    private static int CompareSpawnPointsByName(Transform left, Transform right)
    {
        return GetSpawnPointIndex(left != null ? left.name : null).CompareTo(GetSpawnPointIndex(right != null ? right.name : null));
    }

    private static int GetSpawnPointIndex(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return int.MaxValue;
        }

        int underscoreIndex = label.LastIndexOf('_');
        if (underscoreIndex < 0 || underscoreIndex >= label.Length - 1)
        {
            return int.MaxValue;
        }

        if (int.TryParse(label.Substring(underscoreIndex + 1), out int index))
        {
            return index;
        }

        return int.MaxValue;
    }

    private IEnumerator PlayLegacyPlatformTriggerIfNeeded()
    {
        if (floatingPlatform == null)
        {
            yield break;
        }

        if (currentWaveIndex == showPlatformAfterWaveIndex)
        {
            yield return floatingPlatform.PlayAppearanceSequence();
        }
        else if (currentWaveIndex == hidePlatformAfterWaveIndex)
        {
            yield return floatingPlatform.PlayDisappearanceSequence();
        }
    }

}

[DisallowMultipleComponent]
public class ArenaFloatingPlatformController : MonoBehaviour, IArenaFloatingPlatformPreviewable
{
    private const string GroundLayerName = "Ground";
    private const int PlatformCount = 3;

    [Header("References")]
    [SerializeField] private Transform groundReferencePoint;
    [SerializeField] private Tilemap platform1Tilemap;
    [SerializeField] private Tilemap platform2Tilemap;
    [SerializeField] private Tilemap platform3Tilemap;

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

        yield return PlayPlatformRiseSequence(0);
        if (platformRiseInterval > 0f)
        {
            yield return new WaitForSeconds(platformRiseInterval);
        }

        yield return PlayPlatformRiseSequence(1);
        if (platformRiseInterval > 0f)
        {
            yield return new WaitForSeconds(platformRiseInterval);
        }

        yield return PlayPlatformRiseSequence(2);
    }

    public IEnumerator PlayDisappearanceSequence()
    {
        SetAllVisible();
        if (disappearanceStartDelay > 0f)
        {
            yield return new WaitForSeconds(disappearanceStartDelay);
        }

        yield return PlayPlatformSinkSequence(2);
        if (platformRiseInterval > 0f)
        {
            yield return new WaitForSeconds(platformRiseInterval);
        }

        yield return PlayPlatformSinkSequence(1);
        if (platformRiseInterval > 0f)
        {
            yield return new WaitForSeconds(platformRiseInterval);
        }

        yield return PlayPlatformSinkSequence(0);
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

    public void SetPlatform1()
    {
        SetOnlyPlatformVisible(0);
    }

    public void SetPlatform2()
    {
        SetOnlyPlatformVisible(1);
    }

    public void SetPlatform3()
    {
        SetOnlyPlatformVisible(2);
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

        platformTilemaps ??= new Tilemap[PlatformCount];
        platformRenderers ??= new TilemapRenderer[PlatformCount];
        platformColliders ??= new TilemapCollider2D[PlatformCount];
        platformBodies ??= new Rigidbody2D[PlatformCount];
        platformComposites ??= new CompositeCollider2D[PlatformCount];

        platformTilemaps[0] = platform1Tilemap;
        platformTilemaps[1] = platform2Tilemap;
        platformTilemaps[2] = platform3Tilemap;

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
