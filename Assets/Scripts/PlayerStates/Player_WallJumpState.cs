using UnityEngine;

public class Player_WallJumpState : EntityState
{
    private int wallJumpDirection;

    public Player_WallJumpState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = player.WallJumpDuration;
        wallJumpDirection = -player.FacingDirection;

        player.SetAnimation(false, false);
        player.SetJumpFall(true);
        player.SetWallSlide(false);
        player.SetWallSlideVisualOffset(false);
        player.OpenWallJumpAirAttackWindow();
        player.CheckForFlip(wallJumpDirection);
        player.SetVelocity(
            wallJumpDirection * player.WallJumpForce.x,
            player.WallJumpForce.y
        );
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

        // Blend Tree
        player.SetYVelocity(player.rb.velocity.y);

        // Land
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

        if (stateTimer <= 0)
        {
            stateMachine.ChangeState(player.fallState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (stateTimer > 0)
        {
            player.SetVelocity(
                wallJumpDirection * player.WallJumpForce.x,
                player.rb.velocity.y
            );

            return;
        }

        player.SetVelocity(
            xInput * player.AirMoveSpeed,
            player.rb.velocity.y
        );

        player.CheckForFlip(xInput);
    }
}
