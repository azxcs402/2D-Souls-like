using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public abstract class Enemy : Entity
{
    private static readonly int IdleAnimHash = Animator.StringToHash("idle");
    private static readonly int MoveAnimHash = Animator.StringToHash("move");

    [Header("Enemy Info")]
    [SerializeField] protected int maxHealth = 1;
    [SerializeField] protected bool canTakeDamage = true;

    public StateMachine stateMachine { get; protected set; }
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool CanTakeDamage => canTakeDamage && !isDead;
    public bool IsDead => isDead;

    protected int currentHealth;
    protected bool isDead;

    protected override bool UseWallChecks => true;

    protected override void Awake()
    {
        EnsureCoreComponents();
        base.Awake();

        stateMachine = new StateMachine();
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

    public void SetAnimation(bool idle, bool move)
    {
        if (anim == null)
        {
            return;
        }

        anim.SetBool(IdleAnimHash, idle);
        anim.SetBool(MoveAnimHash, move);
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
        SetVelocity(0f, rb != null ? rb.velocity.y : 0f);
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
