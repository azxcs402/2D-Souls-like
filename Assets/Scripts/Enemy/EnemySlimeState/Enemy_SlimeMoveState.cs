using UnityEngine;

public class Enemy_SlimeMoveState : EnemyState
{
    private readonly Enemy_Slime slime;
    private float turnTimer;

    public Enemy_SlimeMoveState(Enemy_Slime slime, StateMachine stateMachine)
        : base(slime, stateMachine)
    {
        this.slime = slime;
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = slime != null ? slime.GetRandomMoveDuration() : .5f;
        turnTimer = 0f;

        if (slime == null)
        {
            return;
        }

        if (!slime.GroundDetected() || slime.FacingWallContactDetected())
        {
            slime.TurnAround();
            turnTimer = slime.PatrolTurnDelay;
        }

        slime.SetBattleAnimation(false, 0f);
        slime.SetMoveAnimationSpeed(slime.MoveAnimSpeedMultiplier);
        slime.SetAnimation(false, true, false);
    }

    public override void Update()
    {
        base.Update();

        if (slime == null || stateMachine.CurrentState != this)
        {
            return;
        }

        if (slime.ShouldReturnToPatrol)
        {
            slime.ClearReturnToPatrolRequest();

            if (slime.idleState != null)
            {
                stateMachine.ChangeState(slime.idleState);
            }

            return;
        }

        if (turnTimer > 0f)
        {
            turnTimer -= Time.deltaTime;
        }

        if (turnTimer <= 0f)
        {
            bool shouldTurnAround = !slime.GroundDetected()
                || slime.FacingWallContactDetected()
                || slime.EdgeDetected(.08f, .08f);

            if (shouldTurnAround)
            {
                slime.TurnAround();
                turnTimer = slime.PatrolTurnDelay;
            }
        }

        if (stateTimer <= 0f && slime != null)
        {
            stateMachine.ChangeState(slime.idleState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (slime == null || slime.rb == null)
        {
            return;
        }

        float xVelocity = slime.MoveSpeed * slime.FacingDirection;
        slime.SetVelocity(xVelocity, slime.rb.velocity.y);
    }
}
