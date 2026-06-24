using UnityEngine;

[DisallowMultipleComponent]
public class EnemyProjectileHealth : Entity_Health
{
    private Enemy_MageProjectile mageProjectile;
    private Enemy_AbyssMageFireball abyssMageFireball;
    private Enemy_AbyssMageHybridOrb abyssMageHybridOrb;

    protected override void Awake()
    {
        showMiniHealthBar = false;
        autoCreateHealthBar = false;

        base.Awake();

        mageProjectile = GetComponent<Enemy_MageProjectile>();
        abyssMageFireball = GetComponent<Enemy_AbyssMageFireball>();
        abyssMageHybridOrb = GetComponent<Enemy_AbyssMageHybridOrb>();
    }

    public void SetImmuneToAttacks(bool immune)
    {
        canTakeDamage = !immune;
    }

    public override bool TakeDamage(int damage, Entity_Combat damageSource, Vector2 knockbackVelocity)
    {
        return base.TakeDamage(damage, damageSource, knockbackVelocity);
    }

    protected override bool CanReceiveDamageFrom(Entity_Combat damageSource)
    {
        if (damageSource == null)
        {
            return false;
        }

        if (abyssMageFireball != null && abyssMageFireball.IsImmuneToAttacks)
        {
            return false;
        }

        if (mageProjectile != null && !mageProjectile.CanBeBrokenByAttack(damageSource))
        {
            return false;
        }

        if (abyssMageFireball != null && !abyssMageFireball.CanBeBrokenByAttack(damageSource))
        {
            return false;
        }

        if (abyssMageHybridOrb != null && !abyssMageHybridOrb.CanBeBrokenByAttack(damageSource))
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
        return false;
    }

    protected override void Die(Component damageSource)
    {
        if (mageProjectile == null)
        {
            mageProjectile = GetComponent<Enemy_MageProjectile>();
        }

        if (abyssMageFireball == null)
        {
            abyssMageFireball = GetComponent<Enemy_AbyssMageFireball>();
        }

        if (abyssMageHybridOrb == null)
        {
            abyssMageHybridOrb = GetComponent<Enemy_AbyssMageHybridOrb>();
        }

        if (mageProjectile != null)
        {
            mageProjectile.BreakProjectile();
            return;
        }

        if (abyssMageHybridOrb != null)
        {
            abyssMageHybridOrb.BreakProjectile(damageSource);
            return;
        }

        if (abyssMageFireball != null)
        {
            abyssMageFireball.BreakProjectile(damageSource);
            return;
        }

        Destroy(gameObject);
    }
}
