using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Entity_Health : MonoBehaviour, IDamagable
{
    public event Action<Entity_Health> OnHealthChanged;
    public event Action<Entity_Health, int, Component> OnDamaged;
    public event Action<Entity_Health> OnDied;

    [Header("Health")]
    [SerializeField, Min(1)] protected int maxHealth = 10;
    [SerializeField] protected bool canTakeDamage = true;

    [Header("Knockback")]
    [SerializeField, Min(.01f)] protected float knockbackDuration = .15f;

    [Header("Health Bar")]
    [SerializeField] protected Slider healthBar;
    [SerializeField] protected GameObject healthBarPrefab;
    [SerializeField] protected Vector3 healthBarLocalOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] protected bool autoCreateHealthBar = true;
    [SerializeField] protected bool showMiniHealthBar = true;

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

        if (TryGetComponent<Player>(out _))
        {
            showMiniHealthBar = false;
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
        return TakeDamageInternal(damage, damageSource, knockbackVelocity);
    }

    public virtual bool TakeDamage(int damage, Component damageSource, Vector2 knockbackVelocity)
    {
        return TakeDamageInternal(damage, damageSource, knockbackVelocity);
    }

    private bool TakeDamageInternal(int damage, Component damageSource, Vector2 knockbackVelocity)
    {
        if (!CanTakeDamage || damage <= 0)
        {
            return false;
        }

        if (damageSource is Entity_Combat combatSource)
        {
            if (!CanReceiveDamageFrom(combatSource))
            {
                return false;
            }
        }
        else if (!CanReceiveDamageFrom(damageSource))
        {
            return false;
        }

        damage = ApplyDifficultyDamageModifier(damage);
        currentHealth = Mathf.Max(0, currentHealth - damage);
        ApplyKnockback(knockbackVelocity);
        OnDamageTaken(damage, damageSource as Entity_Combat);
        OnDamaged?.Invoke(this, damage, damageSource);
        UpdateHealthBar();

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

    public void SetMaxHealth(int value, bool restoreCurrentHealth = true)
    {
        maxHealth = Mathf.Max(1, value);
        if (restoreCurrentHealth)
        {
            currentHealth = maxHealth;
        }
        else
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        UpdateHealthBar();
    }

    public void SetCurrentHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        UpdateHealthBar();
    }

    protected virtual bool CanReceiveDamageFrom(Entity_Combat damageSource)
    {
        return damageSource != null;
    }

    protected virtual bool CanReceiveDamageFrom(Component damageSource)
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

        if (TryGetComponent<Player>(out _))
        {
            PlayCombatAudio(AudioKey.PlayerHurt);
        }
    }

    protected virtual void Die(Component damageSource)
    {
        isDead = true;
        OnDied?.Invoke(this);

        if (TryGetComponent<Player>(out Player player))
        {
            PlayCombatAudio(AudioKey.PlayerDeath);
            player.EnterDeadState();
            bool freezeTime = damageSource is SpikeHazard;
            GameManager.instance?.BeginPlayerDeathSequence(freezeTime);
            return;
        }

        Revive();
    }

    public virtual void Revive()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        isDead = false;
        UpdateHealthBar();

        if (TryGetComponent<Player>(out Player player))
        {
            player.EndHazardRecovery();
            player.SetDead(false);
            player.SetDeathGroundVisualOffset(false);
            player.RestoreAliveColliderProfile();
            player.RestoreAlivePhysicsProfile();
        }
    }

    private int ApplyDifficultyDamageModifier(int damage)
    {
        if (!TryGetComponent<Player>(out _))
        {
            return damage;
        }

        float damageMultiplier = GameDifficultySettings.IncomingDamageMultiplier;
        return Mathf.Max(1, Mathf.CeilToInt(damage * damageMultiplier));
    }

    public void SetMiniHealthBarVisible(bool visible)
    {
        showMiniHealthBar = visible;
        EnsureHealthBar();
        UpdateHealthBar();
    }

    public bool IsMiniHealthBarVisible => showMiniHealthBar;

    protected virtual void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        OnHealthChanged?.Invoke(this);
    }

    private void EnsureHealthBar()
    {
        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<Slider>(true);
        }

        if (healthBar != null)
        {
            ApplyMiniHealthBarVisibility();
            return;
        }

        if (!autoCreateHealthBar || !showMiniHealthBar)
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

    private void ApplyMiniHealthBarVisibility()
    {
        if (healthBar == null)
        {
            return;
        }

        GameObject healthBarObject = healthBar.gameObject;
        if (healthBarObject.activeSelf != showMiniHealthBar)
        {
            healthBarObject.SetActive(showMiniHealthBar);
        }
    }

    private void PlayCombatAudio(AudioKey audioKey)
    {
        if (TryGetComponent<Player>(out Player player))
        {
            player.PlayPlayerCombatAudio(audioKey);
            return;
        }

        AudioManager.instance?.PlayGlobalSFX(audioKey);
    }
}
