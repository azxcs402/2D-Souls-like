using UnityEngine;

public class Enemy_ReaperTeleportState : EnemyState
{
    private readonly Enemy_Reaper reaper;
    private bool hasTeleported;
    private bool hasCompleted;

    public Enemy_ReaperTeleportState(Enemy enemy, StateMachine stateMachine)
        : base(enemy, stateMachine, "teleport")
    {
        reaper = enemy as Enemy_Reaper;
    }

    public override void Enter()
    {
        base.Enter();

        hasTeleported = false;
        hasCompleted = false;
        stateTimer = GetTeleportClipLength() + (reaper != null ? reaper.TeleportPostDelay : 0f);

        if (reaper == null)
        {
            return;
        }

        reaper.MakeUntargetable(false);
        reaper.SetAnimation(false, false, false);
        reaper.SetBattleAnimation(false, 0f);
        reaper.SetAttackAnimationSpeed(1f);
        reaper.SetFacingLocked(true);
        PlayTeleportAnimation();
    }

    public override void Update()
    {
        base.Update();

        if (reaper == null || stateMachine.CurrentState != this || hasCompleted)
        {
            return;
        }

        if (!hasTeleported && (reaper.TeleportTriggered || stateTimer <= 0f))
        {
            hasTeleported = true;
            reaper.transform.position = reaper.FindTeleportPoint();
            reaper.SetTeleportTrigger(false);
        }

        if (hasTeleported && (triggerCalled || stateTimer <= 0f))
        {
            hasCompleted = true;

            if (reaper.CanUseSpellCast() && reaper.PlayerInSpellCastRange && reaper.spellCastState != null)
            {
                stateMachine.ChangeState(reaper.spellCastState);
            }
            else if (reaper.battleState != null)
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
            reaper.MakeUntargetable(true);
            reaper.SetTeleportTrigger(false);
            reaper.SetFacingLocked(false);
        }
    }

    public void TeleportTrigger()
    {
        reaper?.SetTeleportTrigger(true);
    }

    public void CurrentStateTrigger()
    {
        triggerCalled = true;
        stateTimer = 0f;
    }

    public void AttackOver()
    {
        triggerCalled = true;
        stateTimer = 0f;
    }

    private void PlayTeleportAnimation()
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

        string stateName = "reaperTeleport";
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

    private float GetTeleportClipLength()
    {
        if (reaper == null || reaper.anim == null || reaper.anim.runtimeAnimatorController == null)
        {
            return .5f;
        }

        AnimationClip[] clips = reaper.anim.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && (clip.name == "reaperTeleport" || clip.name == "reaperTeleport_performed"))
            {
                return Mathf.Max(.05f, clip.length);
            }
        }

        return .5f;
    }
}
