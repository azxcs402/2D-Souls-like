using UnityEngine;

public class Enemy_MageGroundedState : EnemyState
{
    protected readonly Enemy_Mage mage;

    public Enemy_MageGroundedState(Enemy_Mage enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
        mage = enemy;
    }

    protected bool IsGrounded()
    {
        return enemy.GroundDetected() || enemy.GroundContactDetected();
    }

    protected void StopHorizontalMovement()
    {
        if (rb != null)
        {
            enemy.SetVelocity(0f, rb.velocity.y);
        }
    }

    protected bool TryEnterCombatState()
    {
        if (mage == null || !mage.IsAlerted)
        {
            return false;
        }

        if (mage.PlayerInMeleeRange && mage.CanAttack && mage.attackState != null)
        {
            stateMachine.ChangeState(mage.attackState);
        }
        else if (mage.PlayerInSpellCastRange && mage.CanSpellCast && mage.spellCastState != null)
        {
            stateMachine.ChangeState(mage.spellCastState);
        }
        else if (mage.battleState != null)
        {
            stateMachine.ChangeState(mage.battleState);
        }

        return true;
    }
}
