using UnityEngine;

public class Enemy_AbyssMageMoveState : Enemy_AbyssMageGroundedState
{
    private float turnTimer;

    public Enemy_AbyssMageMoveState(Enemy_AbyssMage enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = mage != null ? mage.GetRandomMoveDuration() : .5f;
        turnTimer = 0f;

        if (mage != null)
        {
            enemy.SetAnimation(false, true, false);
            mage.SetBattleAnimation(false, 0f);
            mage.SetMoveAnimationSpeed(1f);
        }
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.CurrentState != this)
        {
            return;
        }

        if (mage != null && mage.ShouldReturnToPatrol)
        {
            mage.ClearReturnToPatrolRequest();

            if (mage.idleState != null)
            {
                stateMachine.ChangeState(mage.idleState);
            }

            return;
        }

        if (TryEnterCombatState())
        {
            return;
        }

        if (turnTimer > 0f)
        {
            turnTimer -= Time.deltaTime;
        }

        if (mage != null && turnTimer <= 0f)
        {
            bool shouldTurnAround = enemy.FacingWallContactDetected();

            if (!shouldTurnAround)
            {
                shouldTurnAround = enemy.EdgeDetected(
                    enemy.FacingDirection,
                    enemy.WallCheckDistance,
                    enemy.GroundCheckDistance
                );
            }

            if (shouldTurnAround)
            {
                enemy.TurnAround();
                turnTimer = mage.PatrolTurnDelay;
            }
        }

        if (stateTimer <= 0f && mage != null)
        {
            stateMachine.ChangeState(mage.idleState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (rb == null || mage == null)
        {
            return;
        }

        enemy.SetAnimation(false, true, false);
        mage.SetMoveAnimationSpeed(1f);
        mage.SetBattleAnimation(false, 0f);

        int moveDirection = enemy.FacingDirection;
        float moveSpeed = mage.MoveSpeed;

        enemy.FaceDirection(moveDirection);
        enemy.SetVelocity(moveDirection * moveSpeed, rb.velocity.y);
    }
}
