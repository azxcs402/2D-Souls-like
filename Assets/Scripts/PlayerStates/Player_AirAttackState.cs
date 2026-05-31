using UnityEngine;

public class Player_AirAttackState : EntityState
{
    private int attackDirection;
    private int nextAttackDirection;
    private int comboIndex;
    private int queuedComboIndex;
    private int queuedAttackDirection;
    private float moveTimer;
    private float attackTimer;
    private float attackDuration;
    private float defaultAnimatorSpeed;
    private bool comboInputBuffered;
    private bool currentAttackResolved;

    public Player_AirAttackState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        comboIndex = Mathf.Clamp(queuedComboIndex, 0, player.AirAttackCount - 1);
        attackDirection = queuedAttackDirection != 0 ? queuedAttackDirection : GetAttackDirection();
        nextAttackDirection = attackDirection;
        queuedComboIndex = 0;
        queuedAttackDirection = 0;
        defaultAnimatorSpeed = player.anim.speed;
        currentAttackResolved = false;

        if (comboIndex == 0)
        {
            player.DisableAirAttackUntilGrounded();
        }

        player.CheckForFlip(attackDirection);
        StartAttack();
    }

    public override void Update()
    {
        base.Update();

        if (player.GroundContactDetected() && player.rb.velocity.y <= 0f)
        {
            FinishAttack();
            return;
        }

        if (TryEnterFallAttackState())
        {
            return;
        }

        if (IsInsideLeftTurnInputWindow())
        {
            UpdateNextAttackDirectionFromInput();
        }

        if (player.CanDash && player.DashInputPressed() && HasNextCombo())
        {
            player.QueueAirAttackComboAfterDash(GetNextComboIndex(), nextAttackDirection);
        }

        if (TryEnterDashState())
        {
            return;
        }

        moveTimer -= Time.deltaTime;
        attackTimer += Time.deltaTime;
        player.SetYVelocity(player.rb.velocity.y);

        if (!currentAttackResolved && attackTimer >= attackDuration)
        {
            AttackOver();
            return;
        }

        if (!player.AirAttackComboGroundDetected()
            && IsInsideLeftComboInputWindow()
            && player.AttackInputPressed())
        {
            comboInputBuffered = true;
        }

        if (!player.GroundDetected() && player.rb.velocity.y < 0f)
        {
            player.SetJumpFall(true);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        Vector2 velocity = GetAttackVelocity();
        player.SetVelocity(velocity.x, velocity.y);
    }

    public override void Exit()
    {
        base.Exit();

        player.anim.speed = defaultAnimatorSpeed;
        player.SetBasicAttack(false);
        player.SetAirAttackIndex(0);
    }

    public void AttackTrigger()
    {
        // Kept for existing animation events. Combo input is evaluated by the configured time window.
    }

    public void SetAttack(int attackIndex, int direction = 0)
    {
        queuedComboIndex = attackIndex;
        queuedAttackDirection = direction;
    }

    public void AttackOver()
    {
        if (currentAttackResolved)
        {
            return;
        }

        currentAttackResolved = true;

        if (IsInsideLeftTurnInputWindow())
        {
            UpdateNextAttackDirectionFromInput();
        }

        if (CanContinueCombo())
        {
            StartNextAttack();
            return;
        }

        float comboRightWindow = player.GetAirAttackComboInputRightWindow(comboIndex);
        float turnRightWindow = player.GetAirAttackTurnInputRightWindow(comboIndex);

        if (CanOpenRightComboInputWindow(comboRightWindow))
        {
            player.OpenAirAttackComboWindow(GetNextComboIndex(), nextAttackDirection, comboRightWindow, turnRightWindow);
        }

        FinishAttack();
    }

    private void StartAttack()
    {
        attackTimer = 0f;
        attackDuration = GetCurrentAttackDuration();
        moveTimer = player.AirAttackMoveDuration;
        comboInputBuffered = false;
        currentAttackResolved = false;
        nextAttackDirection = attackDirection;

        player.anim.speed = player.GetAirAttackAnimationSpeed(comboIndex);
        player.SetAnimation(false, false);
        player.SetJumpFall(false);
        player.SetWallSlide(false);
        player.SetDash(false);
        player.SetBasicAttack(true);
        player.SetAirAttackIndex(comboIndex + 1);
        player.anim.CrossFadeInFixedTime(player.GetAirAttackAnimationName(comboIndex), .03f);
    }

    private void StartNextAttack()
    {
        comboIndex = GetNextComboIndex();
        attackDirection = nextAttackDirection;
        player.CheckForFlip(attackDirection);

        StartAttack();
    }

    private bool CanContinueCombo()
    {
        if (!HasNextCombo())
        {
            return false;
        }

        if (player.AirAttackComboGroundDetected())
        {
            return false;
        }

        return comboInputBuffered || player.AttackInputHeld();
    }

    private bool CanOpenRightComboInputWindow(float rightWindow)
    {
        return HasNextCombo()
            && rightWindow > 0f
            && !player.AirAttackComboGroundDetected();
    }

    private bool IsInsideLeftComboInputWindow()
    {
        if (!HasNextCombo())
        {
            return false;
        }

        float leftWindow = player.GetAirAttackComboInputLeftWindow(comboIndex);

        if (leftWindow <= 0f)
        {
            return false;
        }

        float leftWindowStartTime = Mathf.Max(0f, attackDuration - leftWindow);

        return attackTimer >= leftWindowStartTime;
    }

    private bool IsInsideLeftTurnInputWindow()
    {
        if (!HasNextCombo())
        {
            return false;
        }

        float leftWindow = player.GetAirAttackTurnInputLeftWindow(comboIndex);

        if (leftWindow <= 0f)
        {
            return false;
        }

        float leftWindowStartTime = Mathf.Max(0f, attackDuration - leftWindow);

        return attackTimer >= leftWindowStartTime;
    }

    private bool HasNextCombo()
    {
        return comboIndex < player.AirAttackCount - 1;
    }

    private int GetNextComboIndex()
    {
        return Mathf.Min(comboIndex + 1, player.AirAttackCount - 1);
    }

    private void UpdateNextAttackDirectionFromInput()
    {
        if (player.IsNoHorizontalInput(player.moveInput.x))
        {
            return;
        }

        nextAttackDirection = player.moveInput.x > 0 ? 1 : -1;
    }

    private float GetCurrentAttackDuration()
    {
        float animationLength = player.GetAirAttackAnimationLength(comboIndex);

        return animationLength > 0f
            ? animationLength
            : player.AirAttackMoveDuration;
    }

    private void FinishAttack()
    {
        currentAttackResolved = true;

        if (player.GroundContactDetected())
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

    private int GetAttackDirection()
    {
        if (!player.IsNoHorizontalInput(player.moveInput.x))
        {
            return (int)Mathf.Sign(player.moveInput.x);
        }

        return player.FacingDirection;
    }

    private Vector2 GetAttackVelocity()
    {
        Vector2 velocity = new Vector2(0f, -player.AirAttackFallSpeed);

        if (moveTimer > 0f)
        {
            Vector2 moveDistance = player.GetAirAttackMoveDistance(comboIndex);
            velocity.x = attackDirection * moveDistance.x / player.AirAttackMoveDuration;
            velocity.y += moveDistance.y / player.AirAttackMoveDuration;
        }

        return velocity;
    }
}
