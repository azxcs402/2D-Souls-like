using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Entity_Health : MonoBehaviour, IDamagable
{
    [Header("Health")]
    [SerializeField, Range(1, 20)] protected int maxHealth = 3;
    [SerializeField] protected bool canTakeDamage = true;

    [Header("Knockback")]
    [SerializeField, Min(.01f)] protected float knockbackDuration = .15f;

    [Header("Health Bar")]
    [SerializeField] protected Slider healthBar;
    [SerializeField] protected GameObject healthBarPrefab;
    [SerializeField] protected Vector3 healthBarLocalOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] protected bool autoCreateHealthBar = true;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool CanTakeDamage => canTakeDamage && !isDead;
    public bool IsDead => isDead;

    protected int currentHealth;
    protected bool isDead;
    protected Entity_VFX entityVFX;

    protected virtual void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        isDead = false;
        entityVFX = GetComponent<Entity_VFX>();

        if (entityVFX == null)
        {
            entityVFX = gameObject.AddComponent<Entity_VFX>();
        }

        EnsureHealthBar();
        UpdateHealthBar();
    }

    protected virtual void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        knockbackDuration = Mathf.Max(.01f, knockbackDuration);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        EnsureHealthBar();
        UpdateHealthBar();
    }

    public virtual bool TakeDamage(int damage, Entity_Combat damageSource, Vector2 knockbackVelocity)
    {
        if (!CanTakeDamage || damage <= 0 || !CanReceiveDamageFrom(damageSource))
        {
            return false;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        ApplyKnockback(knockbackVelocity);
        OnDamageTaken(damage, damageSource);

        if (currentHealth <= 0)
        {
            Die(damageSource);
        }

        return true;
    }

    public virtual void Heal(int amount)
    {
        if (amount <= 0 || isDead)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthBar();
    }

    protected virtual bool CanReceiveDamageFrom(Entity_Combat damageSource)
    {
        return damageSource != null;
    }

    protected virtual bool CanBeKnockedBack()
    {
        return true;
    }

    protected void ApplyKnockback(Vector2 knockbackVelocity)
    {
        if (!CanBeKnockedBack() || knockbackVelocity == Vector2.zero)
        {
            return;
        }

        GetComponent<Entity>()?.ApplyKnockback(knockbackVelocity, knockbackDuration);
    }

    protected virtual void OnDamageTaken(int damage, Entity_Combat damageSource)
    {
        entityVFX?.PlayOnDamageVFX();
        Debug.Log($"{name} took {damage} damage. HP: {currentHealth}/{maxHealth}.", this);
    }

    protected virtual void Die(Entity_Combat damageSource)
    {
        isDead = true;
        Debug.Log($"{name} died.", this);

        if (TryGetComponent<Player>(out Player player))
        {
            player.EnterDeadState();
            return;
        }

        Revive();
    }

    public virtual void Revive()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        isDead = false;
        Debug.Log($"{name} revived with {currentHealth}/{maxHealth} HP.", this);
        UpdateHealthBar();
    }

    protected virtual void UpdateHealthBar()
    {
        if (healthBar == null)
        {
            return;
        }

        healthBar.maxValue = maxHealth;
        healthBar.value = currentHealth;
    }

    private void EnsureHealthBar()
    {
        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<Slider>(true);
        }

        if (healthBar != null || !autoCreateHealthBar)
        {
            return;
        }

#if UNITY_EDITOR
        if (healthBarPrefab == null)
        {
            healthBarPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/MiniHealthBar.prefab"
            );
        }
#endif

        if (healthBarPrefab == null || !Application.isPlaying)
        {
            return;
        }

        GameObject healthBarInstance = Instantiate(healthBarPrefab, transform);
        healthBarInstance.transform.localPosition = healthBarLocalOffset;
        healthBarInstance.transform.localRotation = Quaternion.identity;
        healthBarInstance.transform.localScale = Vector3.one;
        healthBar = healthBarInstance.GetComponentInChildren<Slider>(true);
        if (healthBar != null)
        {
            UpdateHealthBar();
        }
    }
}
