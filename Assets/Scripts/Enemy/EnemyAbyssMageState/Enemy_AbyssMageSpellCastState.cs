using UnityEngine;

public class Enemy_AbyssMageSpellCastState : EnemyState
{
    private readonly Enemy_AbyssMage mage;
    private bool animationTriggered;
    private bool hasCompleted;
    private bool performedAnimationQueued;

    public Enemy_AbyssMageSpellCastState(Enemy_AbyssMage enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
        mage = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        animationTriggered = false;
        hasCompleted = false;
        performedAnimationQueued = false;
        stateTimer = GetSpellCastClipLength();

        if (mage != null)
        {
            mage.SetSpellCastPerformed(false);

            enemy.SetAnimation(false, false, false);
            mage.SetBattleAnimation(false, 0f);
            mage.SetAttackAnimationSpeed(1f);
            enemy.SetVelocity(0f, rb != null ? rb.velocity.y : 0f);
            mage.SetFacingLocked(true);
            PlaySpellCastAnimation();
        }
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.CurrentState != this || mage == null || hasCompleted)
        {
            return;
        }

        if (!performedAnimationQueued && (mage.SpellCastPerformed || stateTimer <= 0f))
        {
            performedAnimationQueued = true;
            if (mage.anim != null)
            {
                mage.anim.SetBool("spellCast_performed", true);

                string fullStateName = mage.SpellCastPerformedAnimationState.Contains(".")
                    ? mage.SpellCastPerformedAnimationState
                    : $"Base Layer.{mage.SpellCastPerformedAnimationState}";
                int fullStateHash = Animator.StringToHash(fullStateName);

                if (mage.anim.HasState(0, fullStateHash))
                {
                    mage.anim.CrossFadeInFixedTime(fullStateName, 0f, 0);
                    mage.anim.Update(0f);
                }
                else
                {
                    mage.PlayAnimatorState(mage.SpellCastPerformedAnimationState);
                }
            }

            stateTimer = GetSpellCastPerformedClipLength();
        }
        else if (performedAnimationQueued && (animationTriggered || stateTimer <= 0f))
        {
            hasCompleted = true;

            if (mage.IsAlerted && mage.battleState != null)
            {
                stateMachine.ChangeState(mage.battleState);
            }
            else if (mage.idleState != null)
            {
                stateMachine.ChangeState(mage.idleState);
            }
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (mage == null || rb == null || hasCompleted)
        {
            return;
        }

        enemy.SetVelocity(0f, rb.velocity.y);
    }

    public override void Exit()
    {
        base.Exit();

        if (mage != null && mage.anim != null)
        {
            mage.anim.SetBool("spellCast", false);
            mage.anim.SetBool("spellCast_performed", false);
            mage.SetSpellCastPerformed(false);
        }

        mage?.SetFacingLocked(false);
    }

    public void AttackTrigger()
    {
        animationTriggered = true;
        stateTimer = 0f;
    }

    public void CurrentStateTrigger()
    {
        animationTriggered = true;
        stateTimer = 0f;
    }

    private void PlaySpellCastAnimation()
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
        mage.anim.SetBool("spellCast", true);

        string fullStateName = mage.SpellCastAnimationState.Contains(".")
            ? mage.SpellCastAnimationState
            : $"Base Layer.{mage.SpellCastAnimationState}";
        int fullStateHash = Animator.StringToHash(fullStateName);

        if (mage.anim.HasState(0, fullStateHash))
        {
            mage.anim.CrossFadeInFixedTime(fullStateName, 0f, 0);
            mage.anim.Update(0f);
            return;
        }

        mage.PlayAnimatorState(mage.SpellCastAnimationState);
    }

    private float GetSpellCastClipLength()
    {
        return GetClipLength(
            mage != null ? mage.SpellCastAnimationState : null,
            "abyssMageSpellCast"
        );
    }

    private float GetSpellCastPerformedClipLength()
    {
        return GetClipLength(
            mage != null ? mage.SpellCastPerformedAnimationState : null,
            "abyssMageSpellCast_performed"
        );
    }

    private float GetClipLength(string primaryName, string fallbackName)
    {
        if (mage == null || mage.anim == null || mage.anim.runtimeAnimatorController == null)
        {
            return .5f;
        }

        AnimationClip[] clips = mage.anim.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
            {
                continue;
            }

            if ((!string.IsNullOrWhiteSpace(primaryName) && clip.name == primaryName)
                || (!string.IsNullOrWhiteSpace(fallbackName) && clip.name == fallbackName))
            {
                return Mathf.Max(.05f, clip.length);
            }
        }

        return .5f;
    }
}
