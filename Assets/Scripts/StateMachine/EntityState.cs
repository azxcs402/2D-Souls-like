using UnityEngine;

public interface IState
{
    void Enter();
    void Update();
    void FixedUpdate();
    void Exit();
}

public class EntityState : IState
{
    protected StateMachine stateMachine;
    protected Player player;

    protected Rigidbody2D rb;
    protected float xInput;
    protected float stateTimer;

    public EntityState(Player player, StateMachine stateMachine)
    {
        this.player = player;
        this.stateMachine = stateMachine;

        rb = player.rb;
    }

    public virtual void Enter()
    {

    }

    public virtual void Update()
    {
        xInput = player.moveInput.x;
        stateTimer -= Time.deltaTime;
    }

    public virtual void FixedUpdate()
    {

    }

    public virtual void Exit()
    {

    }

    protected bool TryEnterDashState()
    {
        return TryEnterDashState(0);
    }

    protected bool TryEnterBasicAttackState()
    {
        bool canContinuePendingCombo = player.HasBasicAttackComboWindow && player.AttackInputHeld();
        bool canRestartAfterLoopCooldown = player.HasBasicAttackLoopRestartRequest;
        int nextAttackIndex = canRestartAfterLoopCooldown
            ? 0
            : (player.HasBasicAttackComboWindow ? player.PendingBasicAttackComboIndex : 0);

        if (stateMachine.CurrentState == player.basicAttackState
            || stateMachine.CurrentState == player.airAttackState
            || stateMachine.CurrentState == player.fallAttackState
            || stateMachine.CurrentState == player.dashState
            || !player.GroundDetected()
            || (player.IsBasicAttackLoopCooldownActive && !canRestartAfterLoopCooldown)
            || (!player.AttackInputPressed() && !canContinuePendingCombo && !canRestartAfterLoopCooldown))
        {
            return false;
        }

        if (!player.TryConsumeBasicAttackStamina(nextAttackIndex))
        {
            return false;
        }

        if (player.TryConsumeBasicAttackLoopRestartRequest(out int restartDirection))
        {
            player.basicAttackState.SetAttack(0, restartDirection);
        }
        else if (player.TryConsumeBasicAttackComboWindow(out int comboIndex, out int attackDirection))
        {
            player.basicAttackState.SetAttack(comboIndex, attackDirection);
        }
        else
        {
            player.basicAttackState.SetAttack(0);
        }

        stateMachine.ChangeState(player.basicAttackState);
        return true;
    }

    protected bool TryEnterAirAttackState()
    {
        bool canContinuePendingCombo = player.HasAirAttackComboWindow && player.AttackInputHeld();
        bool hasPendingCombo = player.HasAirAttackComboWindow;
        bool hasWallJumpAirAttackWindow = player.HasWallJumpAirAttackWindow;
        bool isWallAttackBlocked = (stateMachine.CurrentState == player.wallSlideState
            || stateMachine.CurrentState == player.wallHoldState
            || player.WallContactDetected()
            || player.FacingWallContactDetected()
            || (hasWallJumpAirAttackWindow && !player.WallJumpAirAttackEnabled));

        if (stateMachine.CurrentState == player.basicAttackState
            || stateMachine.CurrentState == player.airAttackState
            || stateMachine.CurrentState == player.fallAttackState
            || stateMachine.CurrentState == player.dashState
            || isWallAttackBlocked
            || player.GroundDetected()
            || (!player.CanAirAttack && !hasPendingCombo)
            || (!player.AttackInputPressed() && !canContinuePendingCombo))
        {
            if (isWallAttackBlocked)
            {
                player.SetBasicAttack(false);
                player.SetAirAttackIndex(0);
            }

            return false;
        }

        int nextAttackIndex = player.HasAirAttackComboWindow ? player.PendingAirAttackComboIndex : 0;
        if (!player.TryConsumeAirAttackStamina(nextAttackIndex))
        {
            return false;
        }

        if (hasWallJumpAirAttackWindow)
        {
            player.ClearWallJumpAirAttackWindow();
        }

        if (player.TryConsumeAirAttackComboWindow(out int comboIndex, out int attackDirection))
        {
            player.airAttackState.SetAttack(comboIndex, attackDirection);
        }
        else
        {
            player.airAttackState.SetAttack(0);
        }

        stateMachine.ChangeState(player.airAttackState);
        return true;
    }

    protected bool TryEnterFallAttackState()
    {
        bool fallAttackInputPressed = player.AttackInputPressed()
            || (player.AttackInputHeld() && player.DownInputPressed());

        if (stateMachine.CurrentState == player.fallAttackState
            || stateMachine.CurrentState == player.dashState
            || !player.CanStartFallAttack()
            || !fallAttackInputPressed)
        {
            return false;
        }

        stateMachine.ChangeState(player.fallAttackState);
        return true;
    }

    protected bool TryEnterDashState(int directionOverride)
    {
        if (stateMachine.CurrentState == player.dashState
            || !player.CanDash
            || !player.DashInputPressed()
            || !player.TryConsumeDashStamina())
        {
            return false;
        }

        if (directionOverride != 0)
        {
            player.SetDashDirectionOverride(directionOverride);
        }

        stateMachine.ChangeState(player.dashState);
        return true;
    }

    protected bool TryEnterWallDashState()
    {
        if (!player.WallDashAwayFromWallEnabled)
        {
            return TryEnterDashState();
        }

        return TryEnterDashState(-player.FacingDirection);
    }
}
