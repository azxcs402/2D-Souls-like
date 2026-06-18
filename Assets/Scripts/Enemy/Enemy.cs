using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public abstract class Enemy : Entity
{
    private static readonly int IdleAnimHash = Animator.StringToHash("idle");
    private static readonly int MoveAnimHash = Animator.StringToHash("move");
    private static readonly int AttackAnimHash = Animator.StringToHash("attack");

    public event Action<Enemy> OnDied;

    [Header("Enemy Info")]
    [SerializeField, Min(1)] protected int maxHealth = 3;
    [SerializeField] protected bool canTakeDamage = true;
    [Header("Stun Recovery")]
    [SerializeField, Min(0f)] private float stunAttackRecoveryDelay = .6f;
    [Header("Hazard Avoidance")]
    [SerializeField] private bool avoidPitAndSpikeHazards = true;
    [SerializeField, Min(0f)] private float hazardLookAheadOffset = .12f;
    [SerializeField, Min(.01f)] private float hazardProbeRadius = .12f;
    [SerializeField, Min(0f)] private float hazardProbeVerticalOffset = .05f;

    public StateMachine stateMachine { get; protected set; }
    public Entity_Combat Combat { get; protected set; }
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool CanTakeDamage => canTakeDamage && !isDead;
    public bool IsDead => isDead;
    public bool IsStunAttackRecoveryActive => stunAttackRecoveryTimer > 0f;
    public float StunAttackRecoveryDelay => stunAttackRecoveryDelay;
    public bool AvoidPitAndSpikeHazards => avoidPitAndSpikeHazards;
    public float HazardLookAheadOffset => hazardLookAheadOffset;
    public float HazardProbeRadius => hazardProbeRadius;
    public float HazardProbeVerticalOffset => hazardProbeVerticalOffset;
    public IState DeadState => GetDeadState();

    protected int currentHealth;
    protected bool isDead;
    protected int defaultLayer;
    protected float stunAttackRecoveryTimer;

    protected override bool UseWallChecks => true;

    protected override void Awake()
    {
        EnsureCoreComponents();
        base.Awake();

        stateMachine = new StateMachine();
        Combat = GetComponent<Entity_Combat>();
        defaultLayer = gameObject.layer;
        currentHealth = Mathf.Max(1, maxHealth);
        isDead = false;
        stunAttackRecoveryTimer = 0f;
    }

    protected virtual void Update()
    {
        if (stunAttackRecoveryTimer > 0f)
        {
            stunAttackRecoveryTimer = Mathf.Max(0f, stunAttackRecoveryTimer - Time.deltaTime);
        }

        SyncAnimationState();
        stateMachine.CurrentState?.Update();
    }

    protected virtual void FixedUpdate()
    {
        stateMachine.CurrentState?.FixedUpdate();
    }

    public void SetAnimation(bool idle, bool move, bool attack = false)
    {
        if (anim == null)
        {
            return;
        }

        anim.SetBool(IdleAnimHash, idle);
        anim.SetBool(MoveAnimHash, move);
        anim.SetBool(AttackAnimHash, attack);
    }

    protected virtual void SyncAnimationState()
    {
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        EnsureCoreComponents();
    }

    public virtual void TakeDamage(int damage)
    {
        TakeDamage(damage, allowRevive: true);
    }

    public virtual void TakeDamage(int damage, bool allowRevive)
    {
        if (!CanTakeDamage || damage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (currentHealth <= 0)
        {
            Die(allowRevive);
        }
    }

    public virtual void Heal(int amount)
    {
        if (amount <= 0 || isDead)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    protected virtual void Die()
    {
        Die(allowRevive: true);
    }

    protected virtual void Die(bool allowRevive)
    {
        isDead = true;

        if (stateMachine != null && DeadState != null)
        {
            stateMachine.ChangeState(DeadState);
        }

        OnDied?.Invoke(this);
        SetVelocity(0f, rb != null ? rb.velocity.y : 0f);

        if (allowRevive)
        {
            Revive();
        }
    }

    public virtual void Revive()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        isDead = false;
        stunAttackRecoveryTimer = 0f;

        if (anim != null && !anim.isActiveAndEnabled)
        {
            anim.enabled = true;
        }
    }

    public void SetMaxHealth(int value, bool restoreCurrentHealth = true)
    {
        maxHealth = Mathf.Max(1, value);
        currentHealth = restoreCurrentHealth
            ? maxHealth
            : Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    protected virtual IState GetDeadState()
    {
        return null;
    }

    public virtual void SpecialAttack()
    {
    }

    public void StartStunAttackRecovery()
    {
        stunAttackRecoveryTimer = Mathf.Max(0f, stunAttackRecoveryDelay);
    }

    public void MakeUntargetable(bool canBeTargeted)
    {
        int untargetableLayer = LayerMask.NameToLayer("Untargetable");
        if (untargetableLayer >= 0)
        {
            gameObject.layer = canBeTargeted ? defaultLayer : untargetableLayer;
        }

        canTakeDamage = canBeTargeted;
    }

    public bool CanMoveTowardDirection(int direction)
    {
        if (!avoidPitAndSpikeHazards || direction == 0)
        {
            return true;
        }

        return !IsHazardAhead(direction);
    }

    private bool IsHazardAhead(int direction)
    {
        Bounds bounds = GetColliderBounds();
        float facingSign = direction > 0 ? 1f : -1f;
        float originX = (direction > 0 ? bounds.max.x : bounds.min.x) + facingSign * hazardLookAheadOffset;
        Vector2 groundProbeOrigin = new Vector2(originX, bounds.min.y + .02f);
        float groundProbeDistance = Mathf.Max(groundCheckDistance, bounds.size.y * .5f);

        RaycastHit2D groundHit = Physics2D.Raycast(
            groundProbeOrigin,
            Vector2.down,
            groundProbeDistance,
            whatIsGround
        );

        if (groundHit.collider == null || IsSelfCollider(groundHit.collider))
        {
            return true;
        }

        Vector2 hazardProbeCenter = new Vector2(
            originX,
            groundHit.point.y + hazardProbeVerticalOffset
        );

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(hazardProbeCenter, hazardProbeRadius);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D hit = overlaps[i];
            if (hit == null || IsSelfCollider(hit))
            {
                continue;
            }

            if (hit.GetComponentInParent<SpikeHazard>() != null)
            {
                return true;
            }
        }

        return false;
    }

    protected bool IsSelfCollider(Collider2D collider)
    {
        return collider != null
            && (collider.transform == transform || collider.transform.IsChildOf(transform));
    }

    private void EnsureCoreComponents()
    {
        if (GetComponent<Rigidbody2D>() == null)
        {
            gameObject.AddComponent<Rigidbody2D>();
        }

        if (GetComponent<CapsuleCollider2D>() == null)
        {
            gameObject.AddComponent<CapsuleCollider2D>();
        }
    }
}
