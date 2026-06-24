using UnityEngine;

public class Player_BasicAttackState : EntityState
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
    private bool damageTriggered;

    public Player_BasicAttackState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        comboIndex = Mathf.Clamp(queuedComboIndex, 0, player.BasicAttackCount - 1);
        attackDirection = queuedAttackDirection != 0 ? queuedAttackDirection : GetAttackDirection();
        nextAttackDirection = attackDirection;
        queuedComboIndex = 0;
        queuedAttackDirection = 0;
        defaultAnimatorSpeed = player.anim.speed;

        player.CheckForFlip(attackDirection);
        StartAttack();
    }

    public override void Update()
    {
        base.Update();

        if (IsInsideLeftTurnInputWindow())
        {
            UpdateNextAttackDirectionFromInput();
        }

        if (player.CanDash && player.DashInputPressed())
        {
            player.QueueBasicAttackComboAfterDash(GetNextComboIndex(), nextAttackDirection);
        }

        if (TryEnterDashState())
        {
            return;
        }

        if (player.CanStartJump() && player.JumpInputPressed())
        {
            player.TryConsumeJumpStamina();
            stateMachine.ChangeState(player.jumpState);
            return;
        }

        moveTimer -= Time.deltaTime;
        attackTimer += Time.deltaTime;
        player.SetYVelocity(player.rb.velocity.y);

        if (player.HasStamina
            && IsInsideLeftComboInputWindow()
            && player.AttackInputPressed())
        {
            comboInputBuffered = true;
        }

        if (!player.GroundDetected() && player.rb.velocity.y < 0)
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
        player.SetBasicAttackIndex(0);
    }

    public void AttackTrigger()
    {
        if (damageTriggered)
        {
            return;
        }

        damageTriggered = true;
        player.SetCombatDamage(player.GetBasicAttackDamage(comboIndex));
        player.GetComponent<Entity_Combat>()?.AttackTrigger(player.GetBasicAttackData(comboIndex));
    }

    public void SetAttack(int attackIndex, int direction = 0)
    {
        queuedComboIndex = attackIndex;
        queuedAttackDirection = direction;
    }

    public void AttackOver()
    {
        if (IsInsideLeftTurnInputWindow())
        {
            UpdateNextAttackDirectionFromInput();
        }

        if (IsLastComboAttack())
        {
            player.StartBasicAttackLoopCooldown(nextAttackDirection, CanContinueCombo());
            FinishAttack();
            return;
        }

        if (CanContinueCombo())
        {
            StartNextAttack();
            return;
        }

        float comboRightWindow = player.GetBasicAttackComboInputRightWindow(comboIndex);
        float turnRightWindow = player.GetBasicAttackTurnInputRightWindow(comboIndex);

        if (CanOpenRightComboInputWindow(comboRightWindow))
        {
            player.OpenBasicAttackComboWindow(GetNextComboIndex(), nextAttackDirection, comboRightWindow, turnRightWindow);
        }

        FinishAttack();
    }

    private void StartAttack()
    {
        player.TryConsumeBasicAttackStamina(comboIndex);

        attackTimer = 0f;
        attackDuration = GetCurrentAttackDuration();
        moveTimer = player.BasicAttackMoveDuration;
        comboInputBuffered = false;
        damageTriggered = false;
        nextAttackDirection = attackDirection;

        player.anim.speed = player.GetBasicAttackAnimationSpeed(comboIndex);
        player.SetAnimation(false, false);
        player.SetJumpFall(false);
        player.SetWallSlide(false);
        player.SetDash(false);
        player.SetBasicAttack(true);
        player.SetBasicAttackIndex(comboIndex + 1);
        player.anim.CrossFadeInFixedTime(player.GetBasicAttackAnimationName(comboIndex), .03f);
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
        if (player.BasicAttackCount <= 0)
        {
            return false;
        }

        if (!player.HasStamina)
        {
            return false;
        }

        return comboInputBuffered || player.AttackInputHeld();
    }

    private bool CanOpenRightComboInputWindow(float rightWindow)
    {
        return player.BasicAttackCount > 0
            && rightWindow > 0f
            && player.HasStamina;
    }

    private bool IsInsideLeftComboInputWindow()
    {
        if (player.BasicAttackCount <= 0)
        {
            return false;
        }

        float leftWindow = player.GetBasicAttackComboInputLeftWindow(comboIndex);

        if (leftWindow <= 0f)
        {
            return false;
        }

        float leftWindowStartTime = Mathf.Max(0f, attackDuration - leftWindow);

        return attackTimer >= leftWindowStartTime;
    }

    private bool IsInsideLeftTurnInputWindow()
    {
        if (player.BasicAttackCount <= 0)
        {
            return false;
        }

        float leftWindow = player.GetBasicAttackTurnInputLeftWindow(comboIndex);

        if (leftWindow <= 0f)
        {
            return false;
        }

        float leftWindowStartTime = Mathf.Max(0f, attackDuration - leftWindow);

        return attackTimer >= leftWindowStartTime;
    }

    private int GetNextComboIndex()
    {
        return (comboIndex + 1) % player.BasicAttackCount;
    }

    private bool IsLastComboAttack()
    {
        return comboIndex >= player.BasicAttackCount - 1;
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
        float animationLength = player.GetBasicAttackAnimationLength(comboIndex);

        return animationLength > 0f
            ? animationLength
            : player.BasicAttackMoveDuration;
    }

    private void FinishAttack()
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
        Vector2 velocity = new Vector2(0f, player.rb.velocity.y);

        if (moveTimer > 0f)
        {
            Vector2 moveDistance = player.GetBasicAttackMoveDistance(comboIndex);
            velocity.x += attackDirection * moveDistance.x / player.BasicAttackMoveDuration;
            velocity.y = moveDistance.y / player.BasicAttackMoveDuration;
        }

        if (player.CanMoveDuringBasicAttack)
        {
            velocity.x += xInput * player.MoveSpeed * player.BasicAttackMoveSpeedMultiplier;
        }

        return velocity;
    }
}
