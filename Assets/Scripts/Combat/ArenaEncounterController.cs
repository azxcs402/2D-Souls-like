using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class ArenaEncounterController : MonoBehaviour
{
    [Header("Wave Data")]
    [SerializeField] private ArenaWaveSet waveSet;
    [SerializeField] private Transform[] spawnPoints = new Transform[0];
    [SerializeField, Min(0f)] private float defaultDelayBeforeSpawn = 0.25f;
    [SerializeField, Min(0f)] private float defaultDelayAfterClear = 0.75f;

    [Header("Doors")]
    [SerializeField] private ArenaDoorController[] doors = new ArenaDoorController[0];
    [SerializeField] private bool lockDoorsWhenEncounterStarts = true;

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

    private void Awake()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
    }

    private void OnValidate()
    {
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
                Transform spawnPoint = GetSpawnPoint(entry.UseRandomSpawnPoint, spawnIndex);
                Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
                Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

                GameObject enemyObject = Instantiate(entry.EnemyPrefab, position, rotation);
                RegisterSpawnedEnemy(enemyObject);
            }
        }
    }

    private Transform GetSpawnPoint(bool useRandomSpawnPoint, int spawnIndex)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return transform;
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
        if (enemy == null)
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
        if (enemy != null)
        {
            RegisterEnemy(enemy);
        }
    }

    private void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null || activeEnemies.Contains(enemy))
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
            if (enemy == null)
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
}
