using UnityEngine;

public class Enemy_ReaperAttackState : EnemyState
{
    private readonly Enemy_Reaper reaper;
    private bool animationTriggered;
    private bool hasCompleted;
    private bool damageTriggered;
    private Entity_Combat combat;
    private int attackDirection;
    private float attackDuration;
    private float elapsedAttackTime;

    public Enemy_ReaperAttackState(Enemy enemy, StateMachine stateMachine)
        : base(enemy, stateMachine, "attack")
    {
        reaper = enemy as Enemy_Reaper;
    }

    public override void Enter()
    {
        base.Enter();

        animationTriggered = false;
        hasCompleted = false;
        damageTriggered = false;
        combat = reaper != null ? reaper.CombatComponent : null;
        elapsedAttackTime = 0f;
        attackDirection = reaper != null && reaper.PlayerTarget != null
            ? (reaper.PlayerTarget.position.x >= reaper.transform.position.x ? 1 : -1)
            : reaper != null ? reaper.FacingDirection : 1;

        if (reaper != null && reaper.PlayerTarget != null)
        {
            reaper.FaceDirection(attackDirection);
            reaper.SetFacingLocked(true);
        }

        attackDuration = GetAttackClipLength();
        stateTimer = attackDuration;

        if (reaper != null)
        {
            reaper.ForceDisableCounterWindow();
            reaper.SetAnimation(false, false, true);
            reaper.SetBattleAnimation(false, 0f);
            reaper.SetAttackAnimationSpeed(1f);
            reaper.SetVelocity(0f, reaper.rb != null ? reaper.rb.velocity.y : 0f);
            PlayAttackAnimation();
        }
    }

    public override void Update()
    {
        base.Update();
        elapsedAttackTime += Time.deltaTime;

        if (reaper == null || stateMachine.CurrentState != this || hasCompleted)
        {
            return;
        }

        if (!damageTriggered && IsInsideDamageWindow())
        {
            if (TryApplyAttackDamage())
            {
                damageTriggered = true;
            }
        }

        if (animationTriggered || stateTimer <= 0f)
        {
            hasCompleted = true;
            reaper?.CompleteAttackState();

            if (reaper != null && reaper.battleState != null)
            {
                stateMachine.ChangeState(reaper.battleState);
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

        if (reaper != null)
        {
            reaper.SetFacingLocked(false);
            reaper.ForceDisableCounterWindow();
            reaper.SetAnimation(false, false, false);
            reaper.SetAttackAnimationSpeed(1f);
            reaper.SetVelocity(0f, reaper.rb != null ? reaper.rb.velocity.y : 0f);
        }
    }

    public void AttackTrigger()
    {
        if (damageTriggered || combat == null || reaper == null)
        {
            return;
        }

        TryApplyAttackDamage();
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

    private bool IsInsideDamageWindow()
    {
        if (reaper == null || attackDuration <= 0f)
        {
            return false;
        }

        float startTime = Mathf.Clamp01(reaper.AttackDamageWindowStartNormalized) * attackDuration;
        float endTime = Mathf.Clamp01(reaper.AttackDamageWindowEndNormalized) * attackDuration;
        return elapsedAttackTime >= startTime && elapsedAttackTime <= endTime;
    }

    private bool TryApplyAttackDamage()
    {
        if (combat == null || reaper == null)
        {
            return false;
        }

        return combat.AttackTrigger(reaper.ReaperAttackData);
    }

    private void PlayAttackAnimation()
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

        string stateName = "reaperAttack";
        string fullStateName = stateName.Contains(".") ? stateName : $"Base Layer.{stateName}";
        int fullStateHash = Animator.StringToHash(fullStateName);

        if (reaper.anim.HasState(0, fullStateHash))
        {
            reaper.anim.Play(fullStateName, 0, 0f);
            reaper.anim.Update(0f);
            return;
        }

        reaper.PlayAnimatorState(stateName);
    }

    private float GetAttackClipLength()
    {
        if (reaper == null || reaper.anim == null || reaper.anim.runtimeAnimatorController == null)
        {
            return .5f;
        }

        AnimationClip[] clips = reaper.anim.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && (clip.name == "reaperAttack" || clip.name == "skeletonAttack"))
            {
                return Mathf.Max(.05f, clip.length);
            }
        }

        return .5f;
    }
}
