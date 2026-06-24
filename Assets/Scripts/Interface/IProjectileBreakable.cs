public interface IProjectileBreakable
{
    void BreakProjectile();

    bool CanBeBrokenByAttack(Entity_Combat damageSource);
}
