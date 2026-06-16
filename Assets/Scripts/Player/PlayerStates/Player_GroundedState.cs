using UnityEngine;

public class Player_GroundedState : EntityState
{
    public Player_GroundedState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        player.SetJumpFall(false);
        player.SetWallSlide(false);
        player.SetWallSlideVisualOffset(false);
        player.ResetWallHoldAvailability();
        player.ResetFallAttackAvailability();
        player.ResetAirAttackAvailability();
        player.ClearAirAttackComboAfterDash();
        player.ClearAirAttackComboWindow();
        player.ClearWallJumpAirAttackWindow();
        player.ResetWallSlideDropLock();
    }

    public override void Update()
    {
        base.Update();

        if (TryEnterDashState())
        {
            return;
        }

        if (TryEnterBasicAttackState())
        {
            return;
        }

        if (!player.GroundDetected())
        {
            stateMachine.ChangeState(player.fallState);
            return;
        }

        // Jump
        if (player.input.Player.Jump.triggered && player.CanStartJump())
        {
            player.TryConsumeJumpStamina();
            stateMachine.ChangeState(player.jumpState);
            return;
        }
    }
}
