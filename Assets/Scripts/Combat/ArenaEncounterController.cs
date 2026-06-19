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
                            yield return PlayLegacyPlatformTriggerIfNeeded();
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
public class ArenaFloatingPlatformController : MonoBehaviour
{
    private const string GroundLayerName = "Ground";

    [Header("Tilemaps")]
    [SerializeField] private Tilemap background1Tilemap;
    [SerializeField] private Tilemap background2Tilemap;
    [SerializeField] private Tilemap solidTilemap;

    [Header("Appearance Sequence")]
    [SerializeField, Min(0f)] private float hiddenToBackground1Delay = 1f;
    [SerializeField, Min(0f)] private float background1ToBackground2Delay = 1f;
    [SerializeField, Min(0f)] private float background2ToSolidDelay = 1f;

    [Header("Disappearance Sequence")]
    [SerializeField, Min(0f)] private float solidToBackground2Delay = 1f;
    [SerializeField, Min(0f)] private float background2ToBackground1Delay = 1f;
    [SerializeField, Min(0f)] private float background1ToHiddenDelay = 1f;

    [Header("Editor")]
    [SerializeField] private bool startHidden = true;

    private TilemapRenderer background1Renderer;
    private TilemapRenderer background2Renderer;
    private TilemapRenderer solidRenderer;
    private TilemapCollider2D solidCollider;
    private Rigidbody2D solidBody;
    private CompositeCollider2D solidComposite;

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
            SetSolid();
        }
    }

    private void OnValidate()
    {
        ResolveReferences();

        hiddenToBackground1Delay = Mathf.Max(0f, hiddenToBackground1Delay);
        background1ToBackground2Delay = Mathf.Max(0f, background1ToBackground2Delay);
        background2ToSolidDelay = Mathf.Max(0f, background2ToSolidDelay);
        solidToBackground2Delay = Mathf.Max(0f, solidToBackground2Delay);
        background2ToBackground1Delay = Mathf.Max(0f, background2ToBackground1Delay);
        background1ToHiddenDelay = Mathf.Max(0f, background1ToHiddenDelay);
    }

    public IEnumerator PlayAppearanceSequence()
    {
        SetHidden();
        if (hiddenToBackground1Delay > 0f)
        {
            yield return new WaitForSeconds(hiddenToBackground1Delay);
        }

        SetBackground1();
        if (background1ToBackground2Delay > 0f)
        {
            yield return new WaitForSeconds(background1ToBackground2Delay);
        }

        SetBackground2();
        if (background2ToSolidDelay > 0f)
        {
            yield return new WaitForSeconds(background2ToSolidDelay);
        }

        SetSolid();
    }

    public IEnumerator PlayDisappearanceSequence()
    {
        SetSolid();
        if (solidToBackground2Delay > 0f)
        {
            yield return new WaitForSeconds(solidToBackground2Delay);
        }

        SetBackground2();
        if (background2ToBackground1Delay > 0f)
        {
            yield return new WaitForSeconds(background2ToBackground1Delay);
        }

        SetBackground1();
        if (background1ToHiddenDelay > 0f)
        {
            yield return new WaitForSeconds(background1ToHiddenDelay);
        }

        SetHidden();
    }

    public void SetHidden()
    {
        IsVisible = false;
        SetRendererAndCollider(background1Renderer, null, false);
        SetRendererAndCollider(background2Renderer, null, false);
        SetRendererAndCollider(solidRenderer, solidCollider, false);
        SetSolidPhysics(false);
    }

    public void SetBackground1()
    {
        IsVisible = true;
        SetRendererAndCollider(background1Renderer, null, true);
        SetRendererAndCollider(background2Renderer, null, false);
        SetRendererAndCollider(solidRenderer, solidCollider, false);
        SetSolidPhysics(false);
    }

    public void SetBackground2()
    {
        IsVisible = true;
        SetRendererAndCollider(background1Renderer, null, false);
        SetRendererAndCollider(background2Renderer, null, true);
        SetRendererAndCollider(solidRenderer, solidCollider, false);
        SetSolidPhysics(false);
    }

    public void SetSolid()
    {
        IsVisible = true;
        SetRendererAndCollider(background1Renderer, null, false);
        SetRendererAndCollider(background2Renderer, null, false);
        SetRendererAndCollider(solidRenderer, solidCollider, true);
        SetSolidPhysics(true);
    }

    private void ResolveReferences()
    {
        if (background1Tilemap == null)
        {
            background1Tilemap = FindTilemap("Background1");
        }

        if (background2Tilemap == null)
        {
            background2Tilemap = FindTilemap("Background2");
        }

        if (solidTilemap == null)
        {
            solidTilemap = FindTilemap("Solid");
        }

        background1Renderer = background1Tilemap != null ? background1Tilemap.GetComponent<TilemapRenderer>() : null;
        background2Renderer = background2Tilemap != null ? background2Tilemap.GetComponent<TilemapRenderer>() : null;
        solidRenderer = solidTilemap != null ? solidTilemap.GetComponent<TilemapRenderer>() : null;
        solidCollider = solidTilemap != null ? solidTilemap.GetComponent<TilemapCollider2D>() : null;
        solidBody = solidTilemap != null ? solidTilemap.GetComponent<Rigidbody2D>() : null;
        solidComposite = solidTilemap != null ? solidTilemap.GetComponent<CompositeCollider2D>() : null;

        EnsureSolidUsesGroundLayer();
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

    private void SetSolidPhysics(bool enabled)
    {
        if (solidCollider != null)
        {
            solidCollider.enabled = enabled;
        }

        if (solidComposite != null)
        {
            solidComposite.enabled = enabled;
        }

        if (solidBody != null)
        {
            solidBody.simulated = enabled;
        }
    }

    private void EnsureSolidUsesGroundLayer()
    {
        if (solidTilemap == null)
        {
            return;
        }

        int groundLayer = LayerMask.NameToLayer(GroundLayerName);
        if (groundLayer >= 0 && solidTilemap.gameObject.layer != groundLayer)
        {
            solidTilemap.gameObject.layer = groundLayer;
        }
    }
}
