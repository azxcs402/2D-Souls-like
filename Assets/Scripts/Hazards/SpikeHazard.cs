using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(TilemapCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class SpikeHazard : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 20;
    [SerializeField, Min(0.1f)] private float retriggerDelay = 1f;

    private readonly HashSet<int> touchedPlayers = new HashSet<int>();

    private void Awake()
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Static;
            body.simulated = true;
        }

        TilemapCollider2D collider = GetComponent<TilemapCollider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        Player player = other.GetComponentInParent<Player>();
        if (player == null)
        {
            return;
        }

        touchedPlayers.Remove(player.GetInstanceID());
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        Player player = other.GetComponentInParent<Player>();
        if (player == null || player.IsDead || player.IsHazardRecoveryActive)
        {
            return;
        }

        int playerId = player.GetInstanceID();
        if (touchedPlayers.Contains(playerId))
        {
            return;
        }

        Entity_Health playerHealth = player.GetComponent<Entity_Health>();
        if (playerHealth == null)
        {
            return;
        }

        bool tookDamage = playerHealth.TakeDamage(damage, this, Vector2.zero);
        if (!tookDamage || playerHealth.IsDead)
        {
            return;
        }

        touchedPlayers.Add(playerId);
        StartCoroutine(ClearTouchedPlayerAfterDelay(playerId));

        Vector3 safePosition;
        player.TryGetLastGroundedSafePosition(out safePosition);

        if (GameManager.instance != null)
        {
            GameManager.instance.BeginHazardRecoverySequence(player, safePosition);
            return;
        }

        player.RecoverFromHazard(safePosition);
    }

    private IEnumerator ClearTouchedPlayerAfterDelay(int playerId)
    {
        yield return new WaitForSeconds(retriggerDelay);
        touchedPlayers.Remove(playerId);
    }
}
