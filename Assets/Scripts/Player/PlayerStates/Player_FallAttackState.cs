using UnityEngine;

public class Player_FallAttackState : EntityState
{
    private Entity_Combat combat;
    private float defaultGravityScale;
    private int attackDirection;
    private int attackId;
    private bool isDiving;
    private bool diveRequested;
    private bool hasLanded;
    private bool damageTriggered;
    private bool damageWindowActive;
    private float damageWindowTimer;
    private bool attackOverRequested;
    private bool finishRequested;
    private bool attackFinished;

    public Player_FallAttackState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        combat = player.GetComponent<Entity_Combat>();
        attackDirection = GetAttackDirection();
        defaultGravityScale = player.rb.gravityScale;
        attackId = 0;
        stateTimer = GetStartAnimationDuration();
        isDiving = false;
        diveRequested = false;
        hasLanded = false;
        damageTriggered = false;
        damageWindowActive = false;
        damageWindowTimer = 0f;
        attackOverRequested = false;
        finishRequested = false;
        attackFinished = false;

        player.DisableFallAttackUntilGrounded();
        player.CheckForFlip(attackDirection);
        player.rb.gravityScale = defaultGravityScale * player.FallAttackGravityMultiplier;

        player.SetAnimation(false, false);
        player.SetBasicAttack(false);
        player.SetJumpFall(false);
        player.SetWallSlide(false);
        player.SetDash(false);
        player.ResetFallAttackTrigger();
        player.SetFallAttackFinishRequested(false);
        player.SetFallAttack(true);
        player.anim.speed = player.FallAttackAnimationSpeed;
        player.anim.CrossFadeInFixedTime(player.FallAttackStartAnimationName, .03f);
    }

    public override void Update()
    {
        base.Update();

        if (TryEnterDashState())
        {
            return;
        }

        player.SetYVelocity(player.rb.velocity.y);

        if (!isDiving && (diveRequested || stateTimer <= 0f))
        {
            StartDive();
        }

        UpdateDamageWindow();

        if (player.GroundDetected() && player.rb.velocity.y <= 0f)
        {
            if (!hasLanded)
            {
                StartLandingRecovery();
            }

            RequestFinish();
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (isDiving)
        {
            TryApplyDamage();

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
        player.anim.speed = player.FallAttackAnimationSpeed;
        player.ResetFallAttackTrigger();
        player.SetFallAttackFinishRequested(false);
        player.SetFallAttack(false);
    }

    public void AttackTrigger()
    {
        diveRequested = true;
        OpenDamageWindow();
        TryApplyDamage();
    }

    public void AttackOver()
    {
        attackOverRequested = true;
        TryFinishFallAttack();
    }

    private void StartDive()
    {
        if (isDiving || attackFinished)
        {
            return;
        }

        isDiving = true;
        diveRequested = false;
        stateTimer = float.PositiveInfinity;
        player.rb.gravityScale = 0f;
        player.anim.speed = player.FallAttackAnimationSpeed;
        player.ResetFallAttackTrigger();
        player.anim.CrossFadeInFixedTime(player.FallAttackPerformed1AnimationName, .03f);
    }

    private void TryApplyDamage()
    {
        if (damageTriggered || !diveRequested || !damageWindowActive)
        {
            return;
        }

        if (combat == null)
        {
            combat = player.GetComponent<Entity_Combat>();
        }

        if (combat == null)
        {
            return;
        }

        if (attackId <= 0)
        {
            attackId = combat.CreateAttackId();
        }

        player.SetCombatDamage(player.FallAttackDamage);

        bool hitAnyTarget = combat.AttackTriggerWithId(player.FallAttackData, attackId);
        if (!hitAnyTarget)
        {
            hitAnyTarget = combat.AttackTriggerWithId(player.FallAttackExtendedData, attackId);
        }

        if (hitAnyTarget)
        {
            damageTriggered = true;
            CloseDamageWindow();
            RequestFinish();
            TryFinishFallAttack();
        }
    }

    private void UpdateDamageWindow()
    {
        if (!damageWindowActive)
        {
            return;
        }

        damageWindowTimer -= Time.deltaTime;
        if (damageWindowTimer > 0f)
        {
            return;
        }

        CloseDamageWindow();
        TryFinishFallAttack();
    }

    private void OpenDamageWindow()
    {
        float windowDuration = Mathf.Max(0f, player.FallAttackDamageWindowDuration);
        if (windowDuration <= 0f)
        {
            damageWindowActive = true;
            damageWindowTimer = 0f;
            return;
        }

        damageWindowActive = true;
        damageWindowTimer = windowDuration;
    }

    private void CloseDamageWindow()
    {
        damageWindowActive = false;
        damageWindowTimer = 0f;
    }

    private void RequestFinish()
    {
        if (finishRequested)
        {
            return;
        }

        finishRequested = true;
        player.SetFallAttackFinishRequested(true);
    }

    private void TryFinishFallAttack()
    {
        if (!attackOverRequested || damageWindowActive)
        {
            return;
        }

        FinishFallAttack();
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

    private int GetAttackDirection()
    {
        return player.FacingDirection;
    }

    private void StartLandingRecovery()
    {
        hasLanded = true;
        player.rb.gravityScale = defaultGravityScale;
        player.SetVelocity(0f, 0f);

        TryApplyLandingDamage();
        RequestFinish();
        TryFinishFallAttack();
    }

    private void TryApplyLandingDamage()
    {
        if (damageTriggered)
        {
            return;
        }

        if (combat == null)
        {
            combat = player.GetComponent<Entity_Combat>();
        }

        if (combat == null)
        {
            return;
        }

        if (attackId <= 0)
        {
            attackId = combat.CreateAttackId();
        }

        player.SetCombatDamage(player.FallAttackDamage);

        bool hitAnyTarget = combat.AttackTriggerWithId(player.FallAttackData, attackId);
        if (!hitAnyTarget)
        {
            hitAnyTarget = combat.AttackTriggerWithId(player.FallAttackExtendedData, attackId);
        }

        if (hitAnyTarget)
        {
            damageTriggered = true;
            CloseDamageWindow();
            RequestFinish();
        }
    }

    private void FinishFallAttack()
    {
        if (attackFinished)
        {
            return;
        }

        attackFinished = true;
        player.SetFallAttackFinishRequested(false);
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
}
