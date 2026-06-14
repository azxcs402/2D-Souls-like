using UnityEngine;

public class Enemy_MoveState : Enemy_GroundedState
{
    private float turnTimer;

    public Enemy_MoveState(Enemy enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = skeleton != null ? skeleton.GetRandomMoveDuration() : .5f;
        turnTimer = 0f;

        if (skeleton != null)
        {
            skeleton.SetMoveAnimationSpeed(1f);
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

        if (skeleton != null && skeleton.ShouldReturnToPatrol)
        {
            skeleton.ClearReturnToPatrolRequest();

            if (skeleton.idleState != null)
            {
                stateMachine.ChangeState(skeleton.idleState);
            }

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
        skeleton.SetMoveAnimationSpeed(1f);

        int moveDirection = enemy.FacingDirection;
        float moveSpeed = skeleton.SkeletonMoveSpeed;

        enemy.FaceDirection(moveDirection);
        enemy.SetVelocity(moveDirection * moveSpeed, rb.velocity.y);
    }
}
