using UnityEngine;

public interface IDamagable
{
    bool CanTakeDamage { get; }
    bool IsDead { get; }
    bool TakeDamage(int damage, Entity_Combat damageSource, Vector2 knockbackVelocity);
    void Heal(int amount);
}
