using UnityEngine;

public class Enemy_SlimeAttackState : EnemyState
{
    private readonly Enemy_Slime slime;
    private bool animationTriggered;
    private bool hasCompleted;
    private int attackDirection;
    private float elapsedAttackTime;
    private float attackDuration;
    private bool damageTriggered;
    private bool timedDamageAttempted;
    private bool damageWindowActive;
    private bool timedCounterWindowOpened;
    private bool timedCounterWindowClosed;
    private Entity_Combat combat;

    public Enemy_SlimeAttackState(Enemy_Slime slime, StateMachine stateMachine)
        : base(slime, stateMachine)
    {
        this.slime = slime;
    }

    public override void Enter()
    {
        base.Enter();

        animationTriggered = false;
        hasCompleted = false;
        elapsedAttackTime = 0f;
        damageTriggered = false;
        timedDamageAttempted = false;
        damageWindowActive = false;
        timedCounterWindowOpened = false;
        timedCounterWindowClosed = false;
        combat = slime.GetComponent<Entity_Combat>();
        slime.ForceDisableCounterWindow();
        attackDirection = slime.PlayerTarget != null
            ? (slime.PlayerTarget.position.x >= slime.transform.position.x ? 1 : -1)
            : slime.FacingDirection;

        if (slime.PlayerTarget != null)
        {
            slime.FaceDirection(attackDirection);
        }

        attackDuration = GetAttackClipLength();
        stateTimer = attackDuration;

        slime.SetAnimation(false, false, true);
        slime.SetBattleAnimation(false, 0f);
        slime.SetMoveAnimationSpeed(slime.MoveAnimSpeedMultiplier);
        slime.SetAttackAnimationSpeed(1f);
        slime.SetVelocity(0f, slime.rb != null ? slime.rb.velocity.y : 0f);
        PlayAttackAnimation();
    }

    public override void Update()
    {
        base.Update();
        elapsedAttackTime += Time.deltaTime;

        if (stateMachine.CurrentState != this || hasCompleted)
        {
            return;
        }

        UpdateTimedCounterWindow();

        if (!timedDamageAttempted && elapsedAttackTime >= attackDuration * .6f)
        {
            timedDamageAttempted = true;
            AttackTrigger();
        }

        if (animationTriggered || stateTimer <= 0f)
        {
            hasCompleted = true;
            slime?.CompleteAttackState();
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (slime == null)
        {
            return;
        }

        Vector2 velocity = GetAttackVelocity();
        slime.SetVelocity(velocity.x, velocity.y);
    }

    public override void Exit()
    {
        base.Exit();

        damageWindowActive = false;
        slime?.ForceDisableCounterWindow();
        slime?.SetAnimation(false, false, false);
        slime?.SetAttackAnimationSpeed(1f);
        slime?.SetVelocity(0f, rb != null ? rb.velocity.y : 0f);
    }

    public void CurrentStateTrigger()
    {
        AttackOver();
    }

    public void AttackTrigger()
    {
        if (damageTriggered)
        {
            return;
        }

        damageWindowActive = true;
        bool hitPlayer = TryApplyAttackDamage();
        damageWindowActive = false;

        if (hitPlayer)
        {
            damageTriggered = true;
        }
    }

    public void AttackOver()
    {
        damageWindowActive = false;
        slime?.ForceDisableCounterWindow();
        animationTriggered = true;
        stateTimer = 0f;
    }

    private bool TryApplyAttackDamage()
    {
        if (!damageWindowActive || combat == null || slime == null)
        {
            return false;
        }

        return combat.AttackTrigger(slime.SlimeAttackData);
    }

    private void PlayAttackAnimation()
    {
        if (slime == null || slime.anim == null)
        {
            return;
        }

        if (!slime.anim.isActiveAndEnabled)
        {
            slime.anim.enabled = true;
        }

        slime.anim.speed = 1f;
        slime.SetAttackAnimationSpeed(1f);

        string attackStateName = slime.AttackAnimationState;
        string fullStateName = attackStateName.Contains(".")
            ? attackStateName
            : $"Base Layer.{attackStateName}";
        int fullStateHash = Animator.StringToHash(fullStateName);

        if (slime.anim.HasState(0, fullStateHash))
        {
            slime.anim.Play(fullStateName, 0, 0f);
            slime.anim.Update(0f);
            return;
        }

        slime.PlayAnimatorState(attackStateName);
    }

    private void UpdateTimedCounterWindow()
    {
        float duration = Mathf.Max(.01f, attackDuration);
        float openTime = duration * .35f;
        float closeTime = duration * .7f;

        if (!timedCounterWindowOpened && elapsedAttackTime >= openTime)
        {
            timedCounterWindowOpened = true;
            slime.EnableCounterWindow();
        }

        if (!timedCounterWindowClosed && elapsedAttackTime >= closeTime)
        {
            timedCounterWindowClosed = true;
            slime.ForceDisableCounterWindow();
        }
    }

    private float GetAttackClipLength()
    {
        if (slime == null || slime.anim == null || slime.anim.runtimeAnimatorController == null)
        {
            return .5f;
        }

        AnimationClip[] clips = slime.anim.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && (clip.name == "slimeAttack" || clip.name == "skeletonAttack"))
            {
                return Mathf.Max(.05f, clip.length);
            }
        }

        return .5f;
    }

    private Vector2 GetAttackVelocity()
    {
        Vector2 velocity = new Vector2(0f, slime.rb != null ? slime.rb.velocity.y : 0f);

        if (slime == null)
        {
            return velocity;
        }

        float duration = Mathf.Max(.01f, attackDuration);
        float elapsedRatio = elapsedAttackTime / duration;

        if (elapsedRatio < .6f)
        {
            velocity.x = attackDirection * slime.MoveSpeed * .65f;
        }

        return velocity;
    }
}
