using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(TilemapCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class SpikeHazard : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 20;
    [SerializeField, Min(0.1f)] private float retriggerDelay = 0.4f;

    private readonly HashSet<int> touchedTargets = new HashSet<int>();
    private readonly Dictionary<int, float> enemyLastDamageTimes = new Dictionary<int, float>();
    private readonly List<Collider2D> overlapResults = new List<Collider2D>(16);
    private ContactFilter2D overlapFilter;
    private Collider2D spikeCollider;

    private void Awake()
    {
        spikeCollider = GetComponent<Collider2D>();
        overlapFilter.NoFilter();

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Static;
            body.simulated = true;
        }

        TilemapCollider2D collider = GetComponent<TilemapCollider2D>();
        if (collider != null)
        {
            collider.isTrigger = false;
        }
    }

    private void FixedUpdate()
    {
        ScanHazardContacts();
    }

    private void ScanHazardContacts()
    {
        if (spikeCollider == null)
        {
            return;
        }

        overlapResults.Clear();
        spikeCollider.OverlapCollider(overlapFilter, overlapResults);

        float currentTime = Time.time;
        HashSet<int> overlappedEnemyIds = new HashSet<int>();

        for (int i = 0; i < overlapResults.Count; i++)
        {
            Collider2D overlap = overlapResults[i];
            if (overlap == null)
            {
                continue;
            }

            Entity_Health health = overlap.GetComponentInParent<Entity_Health>();
            if (health == null || health.IsDead)
            {
                continue;
            }

            Player player = health.GetComponent<Player>();
            if (player != null)
            {
                TryDamagePlayer(player, health);
                continue;
            }

            int targetId = health.GetInstanceID();
            overlappedEnemyIds.Add(targetId);

            if (enemyLastDamageTimes.TryGetValue(targetId, out float lastDamageTime)
                && currentTime - lastDamageTime < retriggerDelay)
            {
                continue;
            }

            if (health.TakeDamage(damage, this, Vector2.zero))
            {
                enemyLastDamageTimes[targetId] = currentTime;
            }
        }

        RemoveMissingEnemyDamageEntries(overlappedEnemyIds);
    }

    private void TryDamagePlayer(Player player, Entity_Health health)
    {
        if (player == null || health == null)
        {
            return;
        }

        if (player.IsHazardRecoveryActive)
        {
            return;
        }

        int targetId = health.GetInstanceID();
        if (touchedTargets.Contains(targetId))
        {
            return;
        }

        bool tookDamage = health.TakeDamage(damage, this, Vector2.zero);
        if (!tookDamage || health.IsDead)
        {
            return;
        }

        touchedTargets.Add(targetId);
        StartCoroutine(ClearTouchedTargetAfterDelay(targetId));

        Vector3 safePosition;
        player.TryGetLastGroundedSafePosition(out safePosition);

        if (GameManager.instance != null)
        {
            GameManager.instance.BeginHazardRecoverySequence(player, safePosition);
            return;
        }

        player.RecoverFromHazard(safePosition);
    }

    private IEnumerator ClearTouchedTargetAfterDelay(int targetId)
    {
        yield return new WaitForSeconds(retriggerDelay);
        touchedTargets.Remove(targetId);
    }

    private void RemoveMissingEnemyDamageEntries(HashSet<int> overlappedEnemyIds)
    {
        if (enemyLastDamageTimes.Count == 0)
        {
            return;
        }

        List<int> keysToRemove = null;
        foreach (KeyValuePair<int, float> entry in enemyLastDamageTimes)
        {
            if (overlappedEnemyIds.Contains(entry.Key))
            {
                continue;
            }

            keysToRemove ??= new List<int>();
            keysToRemove.Add(entry.Key);
        }

        if (keysToRemove == null)
        {
            return;
        }

        for (int i = 0; i < keysToRemove.Count; i++)
        {
            enemyLastDamageTimes.Remove(keysToRemove[i]);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        touchedTargets.Clear();
        enemyLastDamageTimes.Clear();
    }
}
