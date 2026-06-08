using UnityEngine;

public class Enemy_GroundedState : EnemyState
{
    protected readonly Enemy_Skeleton skeleton;

    public Enemy_GroundedState(Enemy enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
        skeleton = enemy as Enemy_Skeleton;
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
        if (skeleton == null || !skeleton.IsAlerted)
        {
            return false;
        }

        if (skeleton.PlayerInAttackRange && skeleton.CanAttack && skeleton.attackState != null)
        {
            stateMachine.ChangeState(skeleton.attackState);
        }
        else if (skeleton.battleState != null)
        {
            stateMachine.ChangeState(skeleton.battleState);
        }

        return true;
    }
}
