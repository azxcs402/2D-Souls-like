using UnityEngine;

public class Player_DashState : EntityState
{
    private float defaultGravityScale;
    private int dashDirection;

    public Player_DashState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = player.DashDuration;
        dashDirection = GetDashDirection();
        defaultGravityScale = player.rb.gravityScale;

        player.StartDashCooldown();
        player.CheckForFlip(dashDirection);
        player.rb.gravityScale = 0f;

        player.SetAnimation(false, false);
        player.SetJumpFall(false);
        player.SetWallSlide(false);
        player.SetWallSlideVisualOffset(false);
        player.SetDash(true);
        player.SetYVelocity(0f);
        player.SetVelocity(dashDirection * player.DashSpeed, 0f);
    }

    public override void Update()
    {
        base.Update();

        player.SetYVelocity(0f);

        if (player.WallDetected() || stateTimer <= 0f)
        {
            FinishDash();
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        player.SetVelocity(dashDirection * player.DashSpeed, 0f);
    }

    public override void Exit()
    {
        base.Exit();

        player.rb.gravityScale = defaultGravityScale;
        player.SetDash(false);
        player.OpenQueuedBasicAttackComboAfterDash();
        player.OpenQueuedAirAttackComboAfterDash();
    }

    private int GetDashDirection()
    {
        if (player.TryConsumeDashDirectionOverride(out int directionOverride))
        {
            return directionOverride;
        }

        if (!player.IsNoHorizontalInput(player.moveInput.x))
        {
            return (int)Mathf.Sign(player.moveInput.x);
        }

        return player.FacingDirection;
    }

    private void FinishDash()
    {
        if (player.GroundDetected())
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

        stateMachine.ChangeState(player.fallState);
    }
}
