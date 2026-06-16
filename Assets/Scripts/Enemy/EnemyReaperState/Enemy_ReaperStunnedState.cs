using UnityEngine;

public class Enemy_ReaperStunnedState : EnemyState
{
    private readonly Enemy_Reaper reaper;
    private float defaultGravityScale;
    private RigidbodyConstraints2D defaultConstraints;
    private bool hasCompleted;

    public Enemy_ReaperStunnedState(Enemy enemy, StateMachine stateMachine)
        : base(enemy, stateMachine, "stunned")
    {
        reaper = enemy as Enemy_Reaper;
    }

    public override void Enter()
    {
        base.Enter();

        hasCompleted = false;
        defaultGravityScale = reaper != null && reaper.rb != null ? reaper.rb.gravityScale : 1f;
        defaultConstraints = reaper != null && reaper.rb != null ? reaper.rb.constraints : RigidbodyConstraints2D.None;
        stateTimer = reaper != null ? reaper.StunnedDuration : 1f;

        if (reaper == null)
        {
            return;
        }

        reaper.SetAnimation(false, false, false);
        reaper.SetBattleAnimation(false, 0f);
        reaper.SetStunnedAnimation(true);
        PlayStunnedAnimation();

        if (reaper.rb != null)
        {
            reaper.rb.gravityScale = 0f;
            reaper.rb.velocity = new Vector2(
                reaper.StunnedVelocity.x * -reaper.FacingDirection,
                reaper.StunnedVelocity.y
            );
            reaper.rb.angularVelocity = 0f;
            reaper.rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

    public override void Update()
    {
        base.Update();

        if (reaper == null || hasCompleted)
        {
            return;
        }

        if (stateTimer > 0f)
        {
            return;
        }

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

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (reaper == null || reaper.rb == null || hasCompleted)
        {
            return;
        }

        reaper.rb.velocity = Vector2.zero;
        reaper.rb.angularVelocity = 0f;
        reaper.rb.MovePosition(reaper.rb.position);
    }

    public override void Exit()
    {
        base.Exit();

        if (reaper != null)
        {
            reaper.SetStunnedAnimation(false);
        }

        if (reaper != null && reaper.rb != null)
        {
            reaper.rb.gravityScale = defaultGravityScale;
            reaper.rb.constraints = defaultConstraints;
            reaper.rb.velocity = Vector2.zero;
            reaper.rb.angularVelocity = 0f;
        }
    }

    private void PlayStunnedAnimation()
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

        string stateName = "reaperStunned";
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
}
