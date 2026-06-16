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

    public StateMachine stateMachine { get; protected set; }
    public Entity_Combat Combat { get; protected set; }
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool CanTakeDamage => canTakeDamage && !isDead;
    public bool IsDead => isDead;
    public IState DeadState => GetDeadState();

    protected int currentHealth;
    protected bool isDead;
    protected int defaultLayer;

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
    }

    protected virtual void Update()
    {
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
        if (!CanTakeDamage || damage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (currentHealth <= 0)
        {
            Die();
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
        isDead = true;

        if (stateMachine != null && DeadState != null)
        {
            stateMachine.ChangeState(DeadState);
        }

        OnDied?.Invoke(this);
        SetVelocity(0f, rb != null ? rb.velocity.y : 0f);
    }

    public virtual void Revive()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        isDead = false;

        if (anim != null && !anim.isActiveAndEnabled)
        {
            anim.enabled = true;
        }
    }

    protected virtual IState GetDeadState()
    {
        return null;
    }

    public virtual void SpecialAttack()
    {
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
