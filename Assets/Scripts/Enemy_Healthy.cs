using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_Healthy : Entity_Health
{
    private Enemy enemy;
    private Enemy_Mage mage;
    private Enemy_AbyssMage abyssMage;
    private Enemy_Reaper reaper;
    private Enemy_Skeleton skeleton;
    private Enemy_Slime slime;

    protected override void Awake()
    {
        abyssMage = GetComponent<Enemy_AbyssMage>();

        if (abyssMage != null)
        {
            showMiniHealthBar = false;
            autoCreateHealthBar = false;
        }

        base.Awake();

        enemy = GetComponent<Enemy>();
        mage = GetComponent<Enemy_Mage>();
        reaper = GetComponent<Enemy_Reaper>();
        skeleton = GetComponent<Enemy_Skeleton>();
        slime = GetComponent<Enemy_Slime>();

        if (abyssMage != null)
        {
            if (healthBar != null)
            {
                GameObject miniHealthBarObject = healthBar.gameObject;
                healthBar = null;

                if (miniHealthBarObject != null)
                {
                    miniHealthBarObject.SetActive(false);
                    Destroy(miniHealthBarObject);
                }
            }
        }
    }

    private void Start()
    {
        SyncEnemyHealth();
    }

    public override bool TakeDamage(int damage, Entity_Combat damageSource, Vector2 knockbackVelocity)
    {
        return TakeDamageInternal(damage, damageSource, knockbackVelocity);
    }

    public override bool TakeDamage(int damage, Component damageSource, Vector2 knockbackVelocity)
    {
        return TakeDamageInternal(damage, damageSource, knockbackVelocity);
    }

    private bool TakeDamageInternal(int damage, Component damageSource, Vector2 knockbackVelocity)
    {
        if (!canTakeDamage || damage <= 0)
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

        if (enemy == null)
        {
            enemy = GetComponent<Enemy>();
        }

        if (skeleton == null)
        {
            skeleton = GetComponent<Enemy_Skeleton>();
        }

        if (mage == null)
        {
            mage = GetComponent<Enemy_Mage>();
        }

        if (abyssMage == null)
        {
            abyssMage = GetComponent<Enemy_AbyssMage>();
        }

        if (reaper == null)
        {
            reaper = GetComponent<Enemy_Reaper>();
        }

        if (slime == null)
        {
            slime = GetComponent<Enemy_Slime>();
        }

        if (enemy == null)
        {
            return base.TakeDamage(damage, damageSource, knockbackVelocity);
        }

        if (!enemy.CanTakeDamage)
        {
            SyncEnemyHealth();
            return false;
        }

        enemy.TakeDamage(damage, allowRevive: false);
        SyncEnemyHealth();
        ApplyKnockback(knockbackVelocity);
        entityVFX?.PlayOnDamageVFX();

        if (mage != null && !isDead)
        {
            mage.EnterBattleFromDamage(damageSource != null ? damageSource.transform : null);
            mage.HandleTeleportTriggerOnDamaged();
        }

        if (abyssMage != null && !isDead)
        {
            abyssMage.EnterBattleFromDamage(damageSource != null ? damageSource.transform : null);
            abyssMage.HandleTeleportTriggerOnDamaged();
        }

        if (reaper != null && !isDead)
        {
            reaper.EnterBattleFromDamage(damageSource != null ? damageSource.transform : null);
            reaper.HandleTeleportTriggerOnDamaged();
        }

        if (skeleton != null && !isDead)
        {
            skeleton.EnterBattleFromDamage(damageSource != null ? damageSource.transform : null);
        }

        if (slime != null && !isDead)
        {
            slime.EnterBattleFromDamage(damageSource != null ? damageSource.transform : null);
        }

        return true;
    }

    protected override bool CanReceiveDamageFrom(Entity_Combat damageSource)
    {
        if (damageSource == null)
        {
            return false;
        }

        Player player = damageSource.GetComponentInParent<Player>();
        if (player != null)
        {
            return true;
        }

        int playerLayer = LayerMask.NameToLayer("Player");
        return playerLayer >= 0 && damageSource.gameObject.layer == playerLayer;
    }

    protected override bool CanReceiveDamageFrom(Component damageSource)
    {
        if (damageSource == null)
        {
            return false;
        }

        if (damageSource is SpikeHazard)
        {
            return true;
        }

        Player player = damageSource.GetComponentInParent<Player>();
        if (player != null)
        {
            return true;
        }

        int playerLayer = LayerMask.NameToLayer("Player");
        return playerLayer >= 0 && damageSource.gameObject.layer == playerLayer;
    }

    protected override bool CanBeKnockedBack()
    {
        if (mage != null)
        {
            return mage.CanBeKnockedBack;
        }

        if (abyssMage != null)
        {
            return abyssMage.CanBeKnockedBack;
        }

        if (reaper != null)
        {
            return reaper.CanBeKnockedBack;
        }

        if (slime != null)
        {
            return slime.CanSlimeBeKnockedBack;
        }

        return skeleton == null || skeleton.CanSkeletonBeKnockedBack;
    }

    protected override void OnDamageTaken(int damage, Entity_Combat damageSource)
    {
        if (mage != null)
        {
            mage.EnterBattleFromDamage(damageSource != null ? damageSource.transform : null);
        }

        if (abyssMage != null)
        {
            abyssMage.EnterBattleFromDamage(damageSource != null ? damageSource.transform : null);
        }

        if (reaper != null)
        {
            reaper.EnterBattleFromDamage(damageSource != null ? damageSource.transform : null);
        }

        if (skeleton != null)
        {
            skeleton.EnterBattleFromDamage(damageSource != null ? damageSource.transform : null);
        }

        if (slime != null)
        {
            slime.EnterBattleFromDamage(damageSource != null ? damageSource.transform : null);
        }
    }

    private void SyncEnemyHealth()
    {
        if (enemy == null)
        {
            return;
        }

        maxHealth = enemy.MaxHealth;
        currentHealth = enemy.CurrentHealth;
        isDead = enemy.IsDead;
        UpdateHealthBar();
    }

    public void RefreshFromEnemy()
    {
        SyncEnemyHealth();
    }
}
