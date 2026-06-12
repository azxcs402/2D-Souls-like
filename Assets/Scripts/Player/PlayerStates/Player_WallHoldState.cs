using UnityEngine;

public class Player_WallHoldState : EntityState
{
    private float defaultGravityScale;

    public Player_WallHoldState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = player.WallHoldDuration;
        defaultGravityScale = player.rb.gravityScale;
        player.rb.gravityScale = 0f;

        player.SetAnimation(false, false);
        player.SetJumpFall(false);
        player.SetWallSlide(true);
        player.SetWallSlideVisualOffset(true);
        player.SetVelocity(player.FacingDirection * .25f, 0f);
        player.SetYVelocity(0f);
    }

    public override void Update()
    {
        base.Update();

        if (TryEnterWallDashState())
        {
            return;
        }

        if (TryEnterFallAttackState())
        {
            return;
        }

        if (player.JumpInputPressed() && player.TryConsumeJumpStamina())
        {
            stateMachine.ChangeState(player.wallJumpState);
            return;
        }

        player.SetYVelocity(0f);

        if (player.GroundDetected() && player.rb.velocity.y <= 0)
        {
            if (Mathf.Abs(xInput) <= .01f)
            {
                stateMachine.ChangeState(player.idleState);
            }
            else
            {
                stateMachine.ChangeState(player.moveState);
            }

            return;
        }

        if (!player.WallDetected())
        {
            stateMachine.ChangeState(player.fallState);
            return;
        }

        if (player.IsInputAwayFromWall(xInput))
        {
            stateMachine.ChangeState(player.fallState);
            return;
        }

        if (!player.IsInputTowardWall(xInput))
        {
            stateMachine.ChangeState(player.wallSlideState);
            return;
        }

        if (player.WallHoldHasDuration && stateTimer <= 0)
        {
            player.DisableWallHoldUntilGrounded();
            stateMachine.ChangeState(player.wallSlideState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        player.SetVelocity(player.FacingDirection * .25f, 0f);
    }

    public override void Exit()
    {
        base.Exit();

        player.rb.gravityScale = defaultGravityScale;
        player.SetWallSlide(false);
        player.SetWallSlideVisualOffset(false);
    }
}
