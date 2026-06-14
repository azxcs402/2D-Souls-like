using UnityEngine;

public class Enemy_MageAttackState : Enemy_MageGroundedState
{
    private bool animationTriggered;
    private bool hasCompleted;
    private bool damageTriggered;
    private bool timedCounterWindowOpened;
    private bool timedCounterWindowClosed;
    private int attackDirection;
    private Entity_Combat combat;
    private float attackDuration;
    private float elapsedAttackTime;

    public Enemy_MageAttackState(Enemy_Mage enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        animationTriggered = false;
        hasCompleted = false;
        damageTriggered = false;
        timedCounterWindowOpened = false;
        timedCounterWindowClosed = false;
        elapsedAttackTime = 0f;
        combat = mage != null ? mage.Combat : null;
        mage?.DisableCounterWindow();
        attackDirection = mage != null && mage.PlayerTarget != null
            ? (mage.PlayerTarget.position.x >= mage.transform.position.x ? 1 : -1)
            : mage != null ? mage.FacingDirection : 1;

        if (mage != null && mage.PlayerTarget != null)
        {
            mage.FaceDirection(attackDirection);
        }

        attackDuration = GetAttackClipLength();
        stateTimer = attackDuration;

        if (mage != null)
        {
            enemy.SetAnimation(false, false, true);
            mage.SetBattleAnimation(false, 0f);
            mage.SetMoveAnimationSpeed(1f);
            mage.SetAttackAnimationSpeed(1f);
            enemy.SetVelocity(0f, rb != null ? rb.velocity.y : 0f);
            PlayAttackAnimation();
        }
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

        if (animationTriggered || stateTimer <= 0f)
        {
            hasCompleted = true;

            if (mage != null)
            {
                mage.CompleteAttackState();
            }
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (mage == null)
        {
            return;
        }

        enemy.SetVelocity(0f, rb != null ? rb.velocity.y : 0f);
    }

    public void AttackTrigger()
    {
        if (damageTriggered)
        {
            return;
        }

        bool hitPlayer = combat != null && mage != null && combat.AttackTrigger(mage.MageAttackData);
        if (hitPlayer)
        {
            damageTriggered = true;
        }
    }

    public void CurrentStateTrigger()
    {
        animationTriggered = true;
        stateTimer = 0f;
    }

    public override void Exit()
    {
        base.Exit();

        mage?.DisableCounterWindow();

        if (mage != null && mage.anim != null)
        {
            mage.anim.SetBool("attack", false);
        }
    }

    private void PlayAttackAnimation()
    {
        if (mage == null || mage.anim == null)
        {
            return;
        }

        if (!mage.anim.isActiveAndEnabled)
        {
            mage.anim.enabled = true;
        }

        mage.anim.speed = 1f;
        mage.SetAttackAnimationSpeed(1f);
        mage.anim.SetBool("attack", true);

        string fullStateName = mage.AttackAnimationState.Contains(".")
            ? mage.AttackAnimationState
            : $"Base Layer.{mage.AttackAnimationState}";
        int fullStateHash = Animator.StringToHash(fullStateName);

        if (mage.anim.HasState(0, fullStateHash))
        {
            mage.anim.CrossFadeInFixedTime(fullStateName, 0f, 0);
            mage.anim.Update(0f);
            return;
        }

        mage.PlayAnimatorState(mage.AttackAnimationState);
    }

    private void UpdateTimedCounterWindow()
    {
        if (mage == null)
        {
            return;
        }

        float duration = Mathf.Max(.01f, attackDuration);
        float openTime = duration * .35f;
        float closeTime = duration * .7f;

        if (!timedCounterWindowOpened && elapsedAttackTime >= openTime)
        {
            timedCounterWindowOpened = true;
            mage.EnableCounterWindow();
        }

        if (!timedCounterWindowClosed && elapsedAttackTime >= closeTime)
        {
            timedCounterWindowClosed = true;
            mage.DisableCounterWindow();
        }
    }

    private float GetAttackClipLength()
    {
        if (mage == null || mage.anim == null || mage.anim.runtimeAnimatorController == null)
        {
            return .5f;
        }

        string[] candidateNames =
        {
            mage.AttackAnimationState,
            "mageAttack",
            "skeletonAttack"
        };

        AnimationClip[] clips = mage.anim.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
            {
                continue;
            }

            for (int j = 0; j < candidateNames.Length; j++)
            {
                if (!string.IsNullOrWhiteSpace(candidateNames[j]) && clip.name == candidateNames[j])
                {
                    return Mathf.Max(.05f, clip.length);
                }
            }
        }

        return .5f;
    }
}
