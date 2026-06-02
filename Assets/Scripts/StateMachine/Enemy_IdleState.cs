using UnityEngine;

public class Enemy_IdleState : EnemyState
{
    private readonly Enemy_Skeleton skeleton;

    public Enemy_IdleState(Enemy enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
        skeleton = enemy as Enemy_Skeleton;
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = skeleton != null ? skeleton.IdleDuration : .5f;

        if (skeleton != null)
        {
            enemy.SetAnimation(true, false);
        }

        if (rb != null)
        {
            enemy.SetVelocity(0f, rb.velocity.y);
        }
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.CurrentState != this)
        {
            return;
        }

        if (stateTimer <= 0f && skeleton != null)
        {
            stateMachine.ChangeState(skeleton.moveState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (skeleton != null)
        {
            enemy.SetAnimation(true, false);
        }

        if (rb != null)
        {
            enemy.SetVelocity(0f, rb.velocity.y);
        }
    }
}
