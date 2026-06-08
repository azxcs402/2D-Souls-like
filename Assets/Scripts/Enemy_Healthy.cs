using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_Healthy : Entity_Health
{
    private Enemy enemy;
    private Enemy_Skeleton skeleton;

    protected override void Awake()
    {
        base.Awake();

        enemy = GetComponent<Enemy>();
        skeleton = GetComponent<Enemy_Skeleton>();
        SyncEnemyHealth();
    }

    public override bool TakeDamage(int damage, Entity_Combat damageSource, Vector2 knockbackVelocity)
    {
        if (!canTakeDamage || damage <= 0 || !CanReceiveDamageFrom(damageSource))
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

        if (enemy == null)
        {
            return base.TakeDamage(damage, damageSource, knockbackVelocity);
        }

        if (!enemy.CanTakeDamage)
        {
            SyncEnemyHealth();
            return false;
        }

        enemy.TakeDamage(damage);
        SyncEnemyHealth();
        ApplyKnockback(knockbackVelocity);
        entityVFX?.PlayOnDamageVFX();
        Debug.Log($"{name} took {damage} damage from player. HP: {currentHealth}/{maxHealth}.", this);

        if (skeleton != null && !isDead)
        {
            skeleton.EnterBattleFromDamage(damageSource != null ? damageSource.transform : null);
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

    protected override bool CanBeKnockedBack()
    {
        return skeleton == null || skeleton.CanSkeletonBeKnockedBack;
    }

    protected override void OnDamageTaken(int damage, Entity_Combat damageSource)
    {
        if (skeleton != null)
        {
            skeleton.EnterBattleFromDamage(damageSource != null ? damageSource.transform : null);
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
    }

    public void RefreshFromEnemy()
    {
        SyncEnemyHealth();
    }
}
