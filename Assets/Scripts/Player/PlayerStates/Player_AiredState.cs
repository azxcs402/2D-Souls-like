using UnityEngine;

public class Player_AirState : EntityState
{
    public Player_AirState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        player.SetAnimation(false, false);
        player.SetJumpFall(true);
    }

    public override void Update()
    {
        base.Update();

        if (TryEnterDashState())
        {
            return;
        }

        if (TryEnterFallAttackState())
        {
            return;
        }

        if (TryEnterAirAttackState())
        {
            return;
        }

        if (player.GroundDetected() && player.rb.velocity.y <= 0)
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

            return;
        }

        if (!player.IsNoHorizontalInput(xInput))
        {
            player.ResetWallSlideDropLock();
        }

        if (CanEnterWallHold())
        {
            stateMachine.ChangeState(player.wallHoldState);
            return;
        }

        if (CanEnterWallSlide())
        {
            stateMachine.ChangeState(player.wallSlideState);
            return;
        }

        // Blend Tree
        player.SetYVelocity(player.rb.velocity.y);
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        player.SetVelocity(
            xInput * player.AirMoveSpeed,
            player.rb.velocity.y
        );

        player.CheckForFlip(xInput);
    }

    protected bool CanEnterWallSlide()
    {
        if (stateMachine.CurrentState == player.wallSlideState
            || stateMachine.CurrentState == player.wallHoldState)
        {
            return false;
        }

        return !player.GroundDetected()
            && player.HasStamina
            && player.WallDetected()
            && player.rb.velocity.y < 0
            && (!player.IsWallSlideDropLocked || !player.IsNoHorizontalInput(xInput))
            && !player.IsInputAwayFromWall(xInput);
    }

    protected bool CanEnterWallHold()
    {
        if (stateMachine.CurrentState == player.wallSlideState
            || stateMachine.CurrentState == player.wallHoldState)
        {
            return false;
        }

        return player.CanWallHold
            && player.HasStamina
            && !player.GroundDetected()
            && player.WallDetected()
            && player.rb.velocity.y < 0
            && player.IsInputTowardWall(xInput);
    }

    protected bool MovingTowardWall()
    {
        return player.IsInputTowardWall(xInput);
    }
}

public class Player_FallState : Player_AirState
{
    public Player_FallState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {

    }
}
