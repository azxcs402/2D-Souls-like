using UnityEngine;

public class Enemy_ReaperSpellCastState : EnemyState
{
    private readonly Enemy_Reaper reaper;
    private bool animationTriggered;
    private bool hasCompleted;
    private bool performedAnimationQueued;

    public Enemy_ReaperSpellCastState(Enemy enemy, StateMachine stateMachine)
        : base(enemy, stateMachine, "spellCast")
    {
        reaper = enemy as Enemy_Reaper;
    }

    public override void Enter()
    {
        base.Enter();

        animationTriggered = false;
        hasCompleted = false;
        performedAnimationQueued = false;
        stateTimer = GetSpellCastClipLength();

        if (reaper == null)
        {
            return;
        }

        reaper.SetSpellCastPerformed(false);
        reaper.SetAnimation(false, false, false);
        reaper.SetBattleAnimation(false, 0f);
        reaper.SetAttackAnimationSpeed(1f);
        reaper.SetFacingLocked(true);
        reaper.SetVelocity(0f, reaper.rb != null ? reaper.rb.velocity.y : 0f);
        PlaySpellCastAnimation();
        reaper.SetSpellCastOnCooldown();
        reaper.ForceSpecialAttack();
    }

    public override void Update()
    {
        base.Update();

        if (reaper == null || stateMachine.CurrentState != this || hasCompleted)
        {
            return;
        }

        if (!performedAnimationQueued && (reaper.SpellCastPerformed || stateTimer <= 0f))
        {
            performedAnimationQueued = true;

            if (reaper.anim != null)
            {
                reaper.anim.SetBool("spellCast_Performed", true);
            }

            PlaySpellCastPerformedAnimation();
            stateTimer = GetSpellCastPerformedClipLength();
        }
        else if (performedAnimationQueued && (animationTriggered || stateTimer <= 0f))
        {
            hasCompleted = true;

            if (reaper.IsAlerted && reaper.battleState != null)
            {
                stateMachine.ChangeState(reaper.battleState);
            }
            else if (reaper.idleState != null)
            {
                stateMachine.ChangeState(reaper.idleState);
            }
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (reaper == null)
        {
            return;
        }

        reaper.SetVelocity(0f, reaper.rb != null ? reaper.rb.velocity.y : 0f);
    }

    public override void Exit()
    {
        base.Exit();

        if (reaper != null && reaper.anim != null)
        {
            reaper.anim.SetBool("spellCast", false);
            reaper.anim.SetBool("spellCast_Performed", false);
            reaper.SetSpellCastPerformed(false);
        }

        reaper?.SetFacingLocked(false);
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

    public void AttackOver()
    {
        animationTriggered = true;
        stateTimer = 0f;
    }

    private void PlaySpellCastAnimation()
    {
        if (reaper == null || reaper.anim == null)
        {
            return;
        }

        if (!reaper.anim.isActiveAndEnabled)
        {
            reaper.anim.enabled = true;
        }

        reaper.anim.speed = 1f;
        reaper.anim.SetBool("spellCast", true);

        string stateName = "reaperSpellCast";
        string fullStateName = stateName.Contains(".") ? stateName : $"Base Layer.{stateName}";
        int fullStateHash = Animator.StringToHash(fullStateName);

        if (reaper.anim.HasState(0, fullStateHash))
        {
            reaper.anim.CrossFadeInFixedTime(fullStateName, 0f, 0);
            reaper.anim.Update(0f);
            return;
        }

        reaper.PlayAnimatorState(stateName);
    }

    private void PlaySpellCastPerformedAnimation()
    {
        if (reaper == null || reaper.anim == null)
        {
            return;
        }

        string stateName = "reaperSpellCast_performed";
        string fullStateName = stateName.Contains(".") ? stateName : $"Base Layer.{stateName}";
        int fullStateHash = Animator.StringToHash(fullStateName);

        if (reaper.anim.HasState(0, fullStateHash))
        {
            reaper.anim.CrossFadeInFixedTime(fullStateName, 0f, 0);
            reaper.anim.Update(0f);
            return;
        }

        reaper.PlayAnimatorState(stateName);
    }

    private float GetSpellCastClipLength()
    {
        return GetClipLength("reaperSpellCast", "reaperSpellCast_loop");
    }

    private float GetSpellCastPerformedClipLength()
    {
        return GetClipLength("reaperSpellCast_performed", "reaperSpellCast");
    }

    private float GetClipLength(string primaryName, string fallbackName)
    {
        if (reaper == null || reaper.anim == null || reaper.anim.runtimeAnimatorController == null)
        {
            return .5f;
        }

        AnimationClip[] clips = reaper.anim.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
            {
                continue;
            }

            if (clip.name == primaryName || clip.name == fallbackName)
            {
                return Mathf.Max(.05f, clip.length);
            }
        }

        return .5f;
    }
}
