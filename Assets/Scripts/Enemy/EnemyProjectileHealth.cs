using UnityEngine;

[DisallowMultipleComponent]
public class EnemyProjectileHealth : Entity_Health
{
    private Enemy_MageProjectile mageProjectile;
    private Enemy_AbyssMageFireball abyssMageFireball;

    protected override void Awake()
    {
        base.Awake();

        mageProjectile = GetComponent<Enemy_MageProjectile>();
        abyssMageFireball = GetComponent<Enemy_AbyssMageFireball>();
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

        if (mageProjectile != null)
        {
            mageProjectile.BreakProjectile();
            return;
        }

        if (abyssMageFireball != null)
        {
            abyssMageFireball.BreakProjectile();
            return;
        }

        Destroy(gameObject);
    }
}
