using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_AbyssMageHybridOrb : MonoBehaviour, IProjectileBreakable
{
    public enum OrbKind
    {
        Outer = 0,
        Core = 1
    }

    [SerializeField] private CircleCollider2D orbCollider;
    [SerializeField] private EnemyProjectileHealth orbHealth;
    [SerializeField] private SpriteRenderer orbRenderer;
    [SerializeField] private RuntimeAnimatorController explosionAnimatorController;
    [SerializeField, Min(0f)] private float damageCooldown = 0.18f;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField, Min(1f)] private float orbitRadiusScale = 1f;
    [SerializeField, Min(0.01f)] private float explosionVisualDuration = 0.65f;

    private Enemy_AbyssMageFireball owner;
    private LayerMask whatCanCollideWith;
    private Vector2 impactKnockback = new Vector2(4f, 2f);
    private OrbKind orbKind = OrbKind.Outer;
    private float nextDamageTime;
    private bool isBroken;

    public bool IsCore => orbKind == OrbKind.Core;

    private void Awake()
    {
        EnsureComponents();
    }

    private void OnValidate()
    {
        damageCooldown = Mathf.Max(0f, damageCooldown);
        damage = Mathf.Max(1, damage);
        orbitRadiusScale = Mathf.Max(1f, orbitRadiusScale);
        explosionVisualDuration = Mathf.Max(0.01f, explosionVisualDuration);
        EnsureComponents();
    }

    public void Configure(
        Enemy_AbyssMageFireball owner,
        OrbKind orbKind,
        int damage,
        float damageCooldown,
        float hitboxRadius,
        float orbitRadiusScale,
        LayerMask whatCanCollideWith,
        Vector2 impactKnockback,
        RuntimeAnimatorController explosionAnimatorController)
    {
        this.owner = owner;
        this.orbKind = orbKind;
        this.damage = Mathf.Max(1, damage);
        this.damageCooldown = Mathf.Max(0f, damageCooldown);
        this.orbitRadiusScale = Mathf.Max(1f, orbitRadiusScale);
        this.whatCanCollideWith = whatCanCollideWith;
        this.impactKnockback = impactKnockback;
        this.explosionAnimatorController = explosionAnimatorController;
        nextDamageTime = 0f;
        isBroken = false;

        EnsureComponents();

        if (orbCollider != null)
        {
            orbCollider.isTrigger = true;
            orbCollider.radius = Mathf.Max(0.01f, hitboxRadius * this.orbitRadiusScale);
            orbCollider.offset = Vector2.zero;
            orbCollider.enabled = true;
        }

        if (orbHealth != null)
        {
            orbHealth.enabled = true;
        }

        if (orbRenderer != null)
        {
            orbRenderer.enabled = true;
        }
    }

    private void EnsureComponents()
    {
        if (orbCollider == null)
        {
            orbCollider = GetComponent<CircleCollider2D>();
        }

        if (orbCollider == null)
        {
            orbCollider = gameObject.AddComponent<CircleCollider2D>();
        }

        if (orbHealth == null)
        {
            orbHealth = GetComponent<EnemyProjectileHealth>();
        }

        if (orbHealth == null)
        {
            orbHealth = gameObject.AddComponent<EnemyProjectileHealth>();
        }

        if (orbRenderer == null)
        {
            orbRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryResolveCollision(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryResolveCollision(collision);
    }

    private bool TryResolveCollision(Collider2D collision)
    {
        if (owner == null || owner.IsBroken || isBroken || collision == null)
        {
            return false;
        }

        if (((1 << collision.gameObject.layer) & whatCanCollideWith.value) == 0)
        {
            return false;
        }

        if (collision.transform == owner.transform || collision.transform.IsChildOf(owner.transform))
        {
            return false;
        }

        if (IsDashingPlayer(collision))
        {
            return false;
        }

        Player player = collision.GetComponentInParent<Player>();
        if (player == null)
        {
            return false;
        }

        if (player.IsCounterAttacking)
        {
            if (player.TryConsumeProjectileBlockStamina())
            {
                BreakProjectile(player);
                return true;
            }

            return false;
        }

        if (Time.time < nextDamageTime)
        {
            return false;
        }

        Entity_Health targetHealth = collision.GetComponentInParent<Entity_Health>();
        if (targetHealth == null)
        {
            return false;
        }

        if (owner != null && !owner.TryConsumeHybridPlayerDamageWindow())
        {
            return false;
        }

        Vector2 knockback = collision.transform.position.x >= transform.position.x
            ? new Vector2(impactKnockback.x, impactKnockback.y)
            : new Vector2(-impactKnockback.x, impactKnockback.y);

        int actualDamage = damage;
        bool hit = targetHealth.TakeDamage(actualDamage, owner != null ? owner.CombatComponent : null, knockback);
        if (hit)
        {
            nextDamageTime = Time.time + damageCooldown;
            BreakProjectile(player);
        }

        return hit;
    }

    public void BreakProjectile()
    {
        BreakProjectile(null);
    }

    public void BreakProjectile(Component damageSource)
    {
        if (isBroken)
        {
            return;
        }

        isBroken = true;

        if (orbCollider != null)
        {
            orbCollider.enabled = false;
        }

        if (orbHealth != null)
        {
            orbHealth.enabled = false;
        }

        PlayExplosionVisual();

        if (orbRenderer != null)
        {
            orbRenderer.enabled = false;
        }

        if (owner != null)
        {
            owner.NotifyHybridOrbBroken(this, damageSource);
        }

        Destroy(gameObject);
    }

    public void ForceBreakWithoutNotify()
    {
        if (isBroken)
        {
            return;
        }

        isBroken = true;
        if (orbCollider != null)
        {
            orbCollider.enabled = false;
        }

        if (orbHealth != null)
        {
            orbHealth.enabled = false;
        }

        PlayExplosionVisual();

        if (orbRenderer != null)
        {
            orbRenderer.enabled = false;
        }
        Destroy(gameObject);
    }

    private bool IsDashingPlayer(Collider2D collision)
    {
        Player player = collision.GetComponentInParent<Player>();
        return player != null
            && player.stateMachine != null
            && player.dashState != null
            && player.stateMachine.CurrentState == player.dashState;
    }

    private void PlayExplosionVisual()
    {
        SpriteRenderer sourceRenderer = orbRenderer != null ? orbRenderer : GetComponent<SpriteRenderer>();
        if (sourceRenderer == null || sourceRenderer.sprite == null)
        {
            return;
        }

        GameObject explosionObject = new GameObject($"{name}_Explosion");
        explosionObject.transform.position = transform.position;
        explosionObject.transform.rotation = transform.rotation;
        explosionObject.transform.localScale = transform.lossyScale;

        SpriteRenderer explosionRenderer = explosionObject.AddComponent<SpriteRenderer>();
        explosionRenderer.sprite = sourceRenderer.sprite;
        explosionRenderer.color = sourceRenderer.color;
        explosionRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        explosionRenderer.sortingOrder = sourceRenderer.sortingOrder + 2;

        Animator explosionAnimator = explosionObject.AddComponent<Animator>();
        explosionAnimator.runtimeAnimatorController = explosionAnimatorController;
        explosionAnimator.enabled = explosionAnimatorController != null;
        if (explosionAnimator.enabled)
        {
            explosionAnimator.Rebind();
            explosionAnimator.Update(0f);
        }

        HybridExplosionVisualLifetime lifetime = explosionObject.AddComponent<HybridExplosionVisualLifetime>();
        lifetime.Configure(GetExplosionVisualDuration());
    }

    private float GetExplosionVisualDuration()
    {
        float duration = Mathf.Max(0.01f, explosionVisualDuration);
        if (explosionAnimatorController == null || explosionAnimatorController.animationClips == null)
        {
            return duration;
        }

        for (int i = 0; i < explosionAnimatorController.animationClips.Length; i++)
        {
            AnimationClip clip = explosionAnimatorController.animationClips[i];
            if (clip != null)
            {
                duration = Mathf.Max(duration, clip.length);
            }
        }

        return duration;
    }
}

[DisallowMultipleComponent]
public class HybridExplosionVisualLifetime : MonoBehaviour
{
    private float duration;
    private float elapsed;

    public void Configure(float duration)
    {
        this.duration = Mathf.Max(0.01f, duration);
        elapsed = 0f;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= duration)
        {
            Destroy(gameObject);
        }
    }
}
