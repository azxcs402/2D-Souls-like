using UnityEngine;

public class Player_WallSlideState : EntityState
{
    private float noInputTimer;
    private float awayInputTimer;

    public Player_WallSlideState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        noInputTimer = 0f;
        awayInputTimer = 0f;

        player.SetJumpFall(false);
        player.SetWallSlide(true);
        player.SetWallSlideVisualOffset(true);
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

        if (!player.IsNoHorizontalInput(xInput))
        {
            player.ResetWallSlideDropLock();
        }

        if (CanWallJump())
        {
            stateMachine.ChangeState(player.wallJumpState);
            return;
        }

        if (player.GroundDetected() && player.rb.velocity.y <= 0)
        {
            Land();
            return;
        }

        if (!player.WallDetected())
        {
            stateMachine.ChangeState(player.fallState);
            return;
        }

        if (player.IsInputAwayFromWall(xInput))
        {
            HandleAwayInput();
            return;
        }

        awayInputTimer = 0f;

        if (player.CanWallHold && player.IsInputTowardWall(xInput))
        {
            stateMachine.ChangeState(player.wallHoldState);
            return;
        }

        HandleNoInputDropTimer();

        player.SetYVelocity(player.rb.velocity.y);
    }

    private bool CanWallJump()
    {
        return player.JumpInputPressed()
            || (player.IsInputAwayFromWall(xInput) && player.JumpInputHeld());
    }

    private void HandleAwayInput()
    {
        awayInputTimer += Time.deltaTime;

        if (awayInputTimer >= player.WallJumpInputGraceTime)
        {
            stateMachine.ChangeState(player.fallState);
        }
    }

    private void Land()
    {
        player.SetJumpFall(false);

        if (Mathf.Abs(xInput) <= .01f)
        {
            stateMachine.ChangeState(player.idleState);
        }
        else
        {
            stateMachine.ChangeState(player.moveState);
        }
    }

    private void HandleNoInputDropTimer()
    {
        if (player.WallSlideNoInputDropTime <= 0f)
        {
            return;
        }

        if (!player.IsNoHorizontalInput(xInput))
        {
            noInputTimer = 0f;
            return;
        }

        noInputTimer += Time.deltaTime;

        if (noInputTimer >= player.WallSlideNoInputDropTime)
        {
            player.LockWallSlideAfterNoInputDrop();
            stateMachine.ChangeState(player.fallState);
        }
    }

    public override void FixedUpdate()
    {
        float yVelocity = Mathf.Max(player.rb.velocity.y, -player.WallSlideSpeed);

        player.SetVelocity(player.FacingDirection * .25f, yVelocity);
    }

    public override void Exit()
    {
        base.Exit();

        player.SetWallSlide(false);
        player.SetWallSlideVisualOffset(false);
    }
}
