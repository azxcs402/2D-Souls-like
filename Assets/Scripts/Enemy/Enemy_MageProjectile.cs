using UnityEngine;

public class Enemy_MageProjectile : MonoBehaviour, IProjectileBreakable
{
    [Header("Flight")]
    [SerializeField, Min(0f)] private float arrivalDuration = .22f;
    [SerializeField, Min(0f)] private float hoverDuration = 1f;
    [SerializeField, Min(0f)] private float homingDuration = .5f;
    [SerializeField, Min(.1f)] private float flightSpeed = 10f;

    [Header("Hover Drift")]
    [SerializeField, Min(0f)] private float hoverDriftRadius = .18f;
    [SerializeField, Min(.01f)] private float hoverDriftChangeInterval = .18f;
    [SerializeField, Min(0f)] private float hoverDriftMoveSpeed = 1.6f;

    [Header("Impact")]
    [SerializeField] private LayerMask whatCanCollideWith;
    [SerializeField] private Vector2 impactKnockback = new Vector2(4f, 2f);
    [SerializeField, Min(0f)] private float destroyDelayAfterImpact = 2f;

    [Header("Player Attack Break Range")]
    [SerializeField] private CircleCollider2D breakRangeCollider;
    [SerializeField, Min(0f)] private float breakRangeRadius = .6f;
    [SerializeField] private Vector2 breakRangeOffset = Vector2.zero;

    private Enemy_Mage owner;
    private Entity_Combat combat;
    private Rigidbody2D rb;
    private Collider2D col;
    private Animator anim;
    private Transform target;
    private bool hasImpacted;
    private bool colliderEnabled;
    private bool hoverSlotReleased;
    private float elapsedTime;
    private int hoverReservationId = -1;
    private Vector2 hoverLocalOffset;
    private Vector2 spawnPosition;
    private Vector2 lockedFlightDirection = Vector2.right;
    private Vector2 hoverDriftOffset;
    private Vector2 hoverDriftTargetOffset;
    private float nextHoverDriftChangeTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponentInChildren<Animator>(true);
        combat = GetComponent<Entity_Combat>();
        EnsureBreakRange();
    }

    private void OnValidate()
    {
        arrivalDuration = Mathf.Max(0f, arrivalDuration);
        hoverDuration = Mathf.Max(0f, hoverDuration);
        homingDuration = Mathf.Max(0f, homingDuration);
        flightSpeed = Mathf.Max(.1f, flightSpeed);
        hoverDriftRadius = Mathf.Max(0f, hoverDriftRadius);
        hoverDriftChangeInterval = Mathf.Max(.01f, hoverDriftChangeInterval);
        hoverDriftMoveSpeed = Mathf.Max(0f, hoverDriftMoveSpeed);
        destroyDelayAfterImpact = Mathf.Max(0f, destroyDelayAfterImpact);
        breakRangeRadius = Mathf.Max(0f, breakRangeRadius);
        EnsureBreakRange();

        if (whatCanCollideWith == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            int groundLayer = LayerMask.NameToLayer("Ground");
            int mask = 0;

            if (playerLayer >= 0)
            {
                mask |= 1 << playerLayer;
            }

            if (groundLayer >= 0)
            {
                mask |= 1 << groundLayer;
            }

            whatCanCollideWith = mask;
        }
    }

    public void SetupProjectile(Enemy_Mage owner, Transform target, Entity_Combat combat, int hoverReservationId, Vector2 hoverLocalOffset)
    {
        this.owner = owner;
        this.target = target;
        this.combat = GetComponent<Entity_Combat>() ?? combat;
        this.hoverReservationId = hoverReservationId;
        this.hoverLocalOffset = hoverLocalOffset;
        hasImpacted = false;
        colliderEnabled = false;
        hoverSlotReleased = false;
        elapsedTime = 0f;
        spawnPosition = transform.position;
        lockedFlightDirection = GetInitialFlightDirection();
        hoverDriftOffset = Vector2.zero;
        hoverDriftTargetOffset = Vector2.zero;
        nextHoverDriftChangeTime = arrivalDuration;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (col == null)
        {
            col = GetComponent<Collider2D>();
        }

        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>(true);
        }

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = spawnPosition;
        }

        if (col != null)
        {
            col.enabled = false;
        }

        if (anim != null)
        {
            anim.enabled = false;
        }

        ApplyBreakRangeSettings();
    }

    private void FixedUpdate()
    {
        if (hasImpacted || rb == null)
        {
            return;
        }

        elapsedTime += Time.fixedDeltaTime;

        if (elapsedTime < arrivalDuration)
        {
            rb.velocity = Vector2.zero;
            rb.position = Vector2.Lerp(spawnPosition, GetHoverWorldPosition(), arrivalDuration <= 0f ? 1f : Mathf.Clamp01(elapsedTime / arrivalDuration));
            return;
        }

        if (elapsedTime < arrivalDuration + hoverDuration)
        {
            UpdateHoverDrift();
            rb.velocity = Vector2.zero;
            rb.position = GetHoverWorldPosition() + hoverDriftOffset;
            return;
        }

        if (!colliderEnabled && col != null)
        {
            col.enabled = true;
            colliderEnabled = true;
        }

        Vector2 direction = GetFlightDirection();
        if (direction.sqrMagnitude > .0001f)
        {
            lockedFlightDirection = direction.normalized;
        }

        rb.velocity = lockedFlightDirection * flightSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Impact(collision);
    }

    public void BreakProjectile()
    {
        Impact(null);
    }

    private void OnDestroy()
    {
        ReleaseHoverReservation();
    }

    private void Impact(Collider2D collision)
    {
        if (hasImpacted)
        {
            return;
        }

        if (collision != null)
        {
            if (((1 << collision.gameObject.layer) & whatCanCollideWith) == 0)
            {
                return;
            }

            if (IsDashingPlayer(collision))
            {
                return;
            }

            if (TryBlockByCounterAttack(collision))
            {
                return;
            }
        }

        ResolveImpact(collision, true);
    }

    private bool TryBlockByCounterAttack(Collider2D collision)
    {
        Player player = collision != null ? collision.GetComponentInParent<Player>() : null;
        if (player == null || !player.IsCounterAttacking)
        {
            return false;
        }

        if (!player.TryConsumeProjectileBlockStamina())
        {
            return false;
        }

        ResolveImpact(collision, false);
        return true;
    }

    private void ResolveImpact(Collider2D collision, bool dealDamage)
    {
        if (hasImpacted)
        {
            return;
        }

        hasImpacted = true;
        ReleaseHoverReservation();

        if (dealDamage && collision != null)
        {
            Entity_Combat targetCombat = collision.GetComponentInParent<Entity_Combat>();
            if (targetCombat != null && combat != null)
            {
                Vector2 knockback = collision.transform.position.x >= transform.position.x
                    ? new Vector2(impactKnockback.x, impactKnockback.y)
                    : new Vector2(-impactKnockback.x, impactKnockback.y);

                targetCombat.ReceiveHit(combat, knockback);
            }
        }

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        if (anim != null)
        {
            anim.enabled = true;
        }

        if (col != null)
        {
            col.enabled = false;
        }

        Destroy(gameObject, destroyDelayAfterImpact);
    }

    private Vector2 GetFlightDirection()
    {
        if (elapsedTime < arrivalDuration + hoverDuration)
        {
            return Vector2.zero;
        }

        float homingTime = elapsedTime - arrivalDuration - hoverDuration;
        if (homingTime <= homingDuration)
        {
            Vector2 towardTarget = GetDirectionTowardTarget();
            if (towardTarget.sqrMagnitude > .0001f)
            {
                return towardTarget.normalized;
            }
        }

        return lockedFlightDirection;
    }

    private Vector2 GetDirectionTowardTarget()
    {
        if (target == null)
        {
            return lockedFlightDirection;
        }

        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        if (toTarget.sqrMagnitude <= .0001f)
        {
            return lockedFlightDirection;
        }

        return toTarget.normalized;
    }

    private Vector2 GetInitialFlightDirection()
    {
        if (target != null)
        {
            Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
            if (toTarget.sqrMagnitude > .0001f)
            {
                return toTarget.normalized;
            }

            if (Mathf.Abs(target.position.x - transform.position.x) > .0001f)
            {
                return target.position.x >= transform.position.x ? Vector2.right : Vector2.left;
            }
        }

        return transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
    }

    private Vector2 GetHoverWorldPosition()
    {
        if (owner == null)
        {
            return rb != null ? rb.position : (Vector2)transform.position;
        }

        return (Vector2)owner.transform.position + hoverLocalOffset;
    }

    private void UpdateHoverDrift()
    {
        if (hoverDriftRadius <= 0f)
        {
            hoverDriftOffset = Vector2.zero;
            return;
        }

        if (elapsedTime >= nextHoverDriftChangeTime)
        {
            hoverDriftTargetOffset = GetRandomHoverDriftOffset();
            nextHoverDriftChangeTime = elapsedTime + hoverDriftChangeInterval;
        }

        hoverDriftOffset = Vector2.MoveTowards(
            hoverDriftOffset,
            hoverDriftTargetOffset,
            hoverDriftMoveSpeed * Time.fixedDeltaTime
        );
    }

    private Vector2 GetRandomHoverDriftOffset()
    {
        if (hoverDriftRadius <= 0f)
        {
            return Vector2.zero;
        }

        float randomX = Random.Range(-hoverDriftRadius, hoverDriftRadius);
        float randomY = Random.Range(-hoverDriftRadius, hoverDriftRadius);
        return new Vector2(randomX, randomY);
    }

    private bool IsDashingPlayer(Collider2D collision)
    {
        Player player = collision.GetComponentInParent<Player>();
        return player != null
            && player.stateMachine != null
            && player.dashState != null
            && player.stateMachine.CurrentState == player.dashState;
    }

    private void ReleaseHoverReservation()
    {
        if (hoverSlotReleased)
        {
            return;
        }

        hoverSlotReleased = true;

        if (owner != null && hoverReservationId >= 0)
        {
            owner.ReleaseProjectileHoverSlot(hoverReservationId);
        }
    }

    private void EnsureBreakRange()
    {
        if (breakRangeCollider == null)
        {
            Transform existing = transform.Find("ProjectileBreakRange");
            if (existing != null)
            {
                breakRangeCollider = existing.GetComponent<CircleCollider2D>();
            }
        }

        if (breakRangeCollider == null)
        {
            GameObject breakRangeObject = new GameObject("ProjectileBreakRange");
            breakRangeObject.transform.SetParent(transform, false);
            breakRangeCollider = breakRangeObject.AddComponent<CircleCollider2D>();
        }

        ApplyBreakRangeSettings();
    }

    private void ApplyBreakRangeSettings()
    {
        if (breakRangeCollider == null)
        {
            return;
        }

        int projectileBreakableLayer = LayerMask.NameToLayer("ProjectileBreakable");
        if (projectileBreakableLayer >= 0)
        {
            breakRangeCollider.gameObject.layer = projectileBreakableLayer;
        }

        breakRangeCollider.isTrigger = true;
        breakRangeCollider.radius = breakRangeRadius;
        breakRangeCollider.offset = Vector2.zero;
        breakRangeCollider.transform.localPosition = breakRangeOffset;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(.15f, .95f, 1f, .95f);
        Gizmos.DrawWireSphere(GetBreakRangeWorldPosition(), Mathf.Max(0f, breakRangeRadius));
    }

    private Vector3 GetBreakRangeWorldPosition()
    {
        Transform breakRangeTransform = breakRangeCollider != null ? breakRangeCollider.transform : transform.Find("ProjectileBreakRange");
        if (breakRangeTransform != null)
        {
            return breakRangeTransform.position;
        }

        return transform.position + (Vector3)breakRangeOffset;
    }
}
