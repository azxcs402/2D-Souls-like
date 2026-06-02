using UnityEngine;

public class Enemy_MoveState : EnemyState
{
    private readonly Enemy_Skeleton skeleton;
    private float turnTimer;

    public Enemy_MoveState(Enemy enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
        skeleton = enemy as Enemy_Skeleton;
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = skeleton != null ? skeleton.MoveDuration : .5f;
        turnTimer = 0f;

        if (skeleton != null)
        {
            enemy.SetAnimation(false, true);
        }
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.CurrentState != this)
        {
            return;
        }

        if (turnTimer > 0f)
        {
            turnTimer -= Time.deltaTime;
        }

        if (skeleton != null && turnTimer <= 0f)
        {
            bool shouldTurnAround = enemy.FacingWallContactDetected();

            if (!shouldTurnAround && skeleton.UseEdgeCheck)
            {
                shouldTurnAround = enemy.EdgeDetected(
                    skeleton.EdgeCheckForwardOffset,
                    skeleton.EdgeCheckDistance
                );
            }

            if (shouldTurnAround)
            {
                enemy.TurnAround();
                turnTimer = skeleton.SkeletonTurnDelay;
            }
        }

        if (stateTimer <= 0f && skeleton != null)
        {
            stateMachine.ChangeState(skeleton.idleState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (rb == null || skeleton == null)
        {
            return;
        }

        enemy.SetAnimation(false, true);
        enemy.SetVelocity(
            enemy.FacingDirection * skeleton.SkeletonMoveSpeed,
            rb.velocity.y
        );
    }
}
