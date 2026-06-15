using UnityEngine;

public class Player_FallAttackState : EntityState
{
    private float defaultGravityScale;
    private float defaultAnimatorSpeed;
    private int attackDirection;
    private float endAnimationLength;
    private float endAnimationTimer;
    private float landingTimer;
    private bool isDiving;
    private bool diveRequested;
    private bool isAttackFinished;
    private bool hasLanded;
    private bool isEndAnimationFinished;
    private bool damageTriggered;

    public Player_FallAttackState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        attackDirection = GetAttackDirection();
        defaultGravityScale = player.rb.gravityScale;
        defaultAnimatorSpeed = player.anim.speed;
        stateTimer = GetStartAnimationDuration();
        isDiving = false;
        diveRequested = false;
        isAttackFinished = false;
        hasLanded = false;
        isEndAnimationFinished = false;
        damageTriggered = false;
        endAnimationLength = 0f;
        endAnimationTimer = 0f;
        landingTimer = 0f;

        player.DisableFallAttackUntilGrounded();
        player.CheckForFlip(attackDirection);
        player.rb.gravityScale = defaultGravityScale * player.FallAttackGravityMultiplier;

        player.SetAnimation(false, false);
        player.SetBasicAttack(false);
        player.SetJumpFall(false);
        player.SetWallSlide(false);
        player.SetDash(false);
        player.ResetFallAttackTrigger();
        player.SetFallAttack(true);
        player.anim.speed = GetStartAnimationSpeed();
        player.anim.CrossFadeInFixedTime(player.FallAttackStartAnimationName, .03f);
    }

    public override void Update()
    {
        base.Update();
        player.SetYVelocity(player.rb.velocity.y);

        if (!isDiving && (diveRequested || stateTimer <= 0f) && stateTimer <= 0f)
        {
            StartDive();
        }

        if (isDiving)
        {
            if (hasLanded)
            {
                landingTimer += Time.deltaTime;
                UpdateLandingRecoveryAnimationSpeed();
            }
            else
            {
                UpdateDiveAnimationSpeed();
            }

            endAnimationTimer += Time.deltaTime * player.anim.speed;
        }

        if (player.GroundDetected() && player.rb.velocity.y <= 0f)
        {
            if (!hasLanded)
            {
                StartLandingRecovery();
            }

            TryFinishFallAttack();
        }

        if (isDiving && endAnimationTimer >= endAnimationLength)
        {
            isEndAnimationFinished = true;
            TryFinishFallAttack();
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (isDiving)
        {
            if (hasLanded)
            {
                player.SetVelocity(0f, 0f);
                return;
            }

            Vector2 diveVelocity = GetDiveVelocity();
            player.SetVelocity(diveVelocity.x, diveVelocity.y);
            return;
        }

        player.SetVelocity(0f, player.rb.velocity.y);
    }

    public override void Exit()
    {
        base.Exit();

        player.rb.gravityScale = defaultGravityScale;
        player.anim.speed = defaultAnimatorSpeed;
        player.ResetFallAttackTrigger();
        player.SetFallAttack(false);
    }

    public void AttackTrigger()
    {
        diveRequested = true;

        if (damageTriggered)
        {
            return;
        }

        damageTriggered = true;
        player.SetCombatDamage(player.FallAttackDamage);
        player.GetComponent<Entity_Combat>()?.AttackTrigger(player.FallAttackData);
    }

    public void AttackOver()
    {
        isEndAnimationFinished = true;
        TryFinishFallAttack();
    }

    private void FinishFallAttack()
    {
        if (isAttackFinished)
        {
            return;
        }

        isAttackFinished = true;
        player.SetFallAttack(false);
        player.ResetFallAttackTrigger();

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
        return player.FacingDirection;
    }

    private void StartDive()
    {
        if (isDiving || isAttackFinished)
        {
            return;
        }

        isDiving = true;
        diveRequested = false;
        stateTimer = float.PositiveInfinity;
        endAnimationLength = GetEndAnimationLength();
        endAnimationTimer = 0f;
        landingTimer = 0f;
        hasLanded = false;
        isEndAnimationFinished = false;
        player.rb.gravityScale = 0f;
        UpdateDiveAnimationSpeed();
        player.ResetFallAttackTrigger();
        player.anim.CrossFadeInFixedTime(player.FallAttackEndAnimationName, .03f);
    }

    private Vector2 GetDiveVelocity()
    {
        float angle = player.FallAttackDiveAngle * Mathf.Deg2Rad;
        float xVelocity = Mathf.Sin(angle) * attackDirection * player.FallAttackDiveSpeed;
        float yVelocity = -Mathf.Cos(angle) * player.FallAttackDiveSpeed;

        return new Vector2(xVelocity, yVelocity);
    }

    private float GetStartAnimationDuration()
    {
        if (player.FallAttackWindupDuration > 0f)
        {
            return player.FallAttackWindupDuration;
        }

        float duration = player.GetAnimationLength(
            player.FallAttackStartAnimationName,
            player.FallAttackAnimationSpeed
        );

        return duration > 0f ? duration : .1f;
    }

    private float GetStartAnimationSpeed()
    {
        float clipLength = player.GetAnimationLength(player.FallAttackStartAnimationName);

        if (player.FallAttackWindupDuration <= 0f || clipLength <= 0f)
        {
            return player.FallAttackAnimationSpeed;
        }

        return clipLength / player.FallAttackWindupDuration;
    }

    private float GetEndAnimationLength()
    {
        float duration = player.GetAnimationLength(player.FallAttackEndAnimationName);

        return duration > 0f ? duration : .1f;
    }

    private void UpdateDiveAnimationSpeed()
    {
        if (endAnimationLength <= 0f)
        {
            player.anim.speed = player.FallAttackAnimationSpeed;
            return;
        }

        if (endAnimationTimer >= endAnimationLength)
        {
            player.anim.speed = 0f;
            return;
        }

        if (!player.TryGetGroundDistance(player.FallAttackGroundSearchDistance, out float groundDistance))
        {
            player.anim.speed = player.FallAttackAnimationSpeed;
            return;
        }

        float verticalSpeed = Mathf.Abs(GetDiveVelocity().y);

        if (verticalSpeed <= .01f)
        {
            player.anim.speed = player.FallAttackAnimationSpeed;
            return;
        }

        float distanceBeforeGrounded = Mathf.Max(0f, groundDistance - player.GroundCheckDistance);
        float timeToGround = Mathf.Max(Time.fixedDeltaTime, distanceBeforeGrounded / verticalSpeed);
        float targetEndTime = Mathf.Max(Time.fixedDeltaTime, timeToGround + player.FallAttackEndAnimationLandingOffset);
        float animationTimeLeft = Mathf.Max(.01f, endAnimationLength - endAnimationTimer);
        float targetSpeed = animationTimeLeft / targetEndTime;

        player.anim.speed = Mathf.Clamp(
            targetSpeed,
            player.FallAttackEndAnimationMinSpeed,
            player.FallAttackEndAnimationMaxSpeed
        );
    }

    private void StartLandingRecovery()
    {
        hasLanded = true;
        landingTimer = 0f;
        player.rb.gravityScale = defaultGravityScale;
        player.SetVelocity(0f, 0f);

        if (player.FallAttackEndAnimationLandingOffset <= 0f)
        {
            TryFinishFallAttack();
        }
        else
        {
            UpdateLandingRecoveryAnimationSpeed();
        }
    }

    private void UpdateLandingRecoveryAnimationSpeed()
    {
        if (endAnimationLength <= 0f)
        {
            player.anim.speed = player.FallAttackAnimationSpeed;
            return;
        }

        if (endAnimationTimer >= endAnimationLength)
        {
            player.anim.speed = 0f;
            return;
        }

        float timeLeft = Mathf.Max(0f, player.FallAttackEndAnimationLandingOffset - landingTimer);
        float animationTimeLeft = Mathf.Max(.01f, endAnimationLength - endAnimationTimer);

        if (timeLeft <= 0f)
        {
            player.anim.speed = player.FallAttackEndAnimationMaxSpeed;
            return;
        }

        float targetSpeed = animationTimeLeft / timeLeft;

        player.anim.speed = Mathf.Clamp(
            targetSpeed,
            player.FallAttackEndAnimationMinSpeed,
            player.FallAttackEndAnimationMaxSpeed
        );
    }

    private void TryFinishFallAttack()
    {
        if (isAttackFinished)
        {
            return;
        }

        if (!hasLanded)
        {
            if (player.FallAttackEndAnimationLandingOffset < 0f && isEndAnimationFinished)
            {
                FinishFallAttack();
            }

            return;
        }

        if (landingTimer >= Mathf.Max(0f, player.FallAttackEndAnimationLandingOffset)
            && (isEndAnimationFinished || endAnimationTimer >= endAnimationLength))
        {
            FinishFallAttack();
        }
    }
}
