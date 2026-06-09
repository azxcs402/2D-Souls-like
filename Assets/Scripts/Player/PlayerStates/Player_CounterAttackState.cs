using UnityEngine;

public class Player_CounterAttackState : EntityState
{
    private Entity_Combat combat;
    private bool counterPerformed;
    private bool performedAnimationFinished;

    public Player_CounterAttackState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {
        combat = player.GetComponent<Entity_Combat>();
    }

    public override void Enter()
    {
        base.Enter();

        combat ??= player.GetComponent<Entity_Combat>();
        counterPerformed = false;
        performedAnimationFinished = false;
        stateTimer = player.CounterDuration;

        player.SetAnimation(false, false);
        player.SetJumpFall(false);
        player.SetWallSlide(false);
        player.SetDash(false);
        player.SetBasicAttack(false);
        player.SetFallAttack(false);
        player.ResetFallAttackTrigger();
        player.SetCounterAttackPerformed(false);
        player.SetCounterAttack(true);
        player.SetVelocity(0f, player.GroundDetected() ? 0f : player.rb.velocity.y);

        PlayAnimation(player.CounterAttackAnimationState);
    }

    public override void Update()
    {
        base.Update();

        if (!counterPerformed)
        {
            if (TryPerformCounter())
            {
                return;
            }

            if (stateTimer <= 0f)
            {
                FinishCounter();
            }

            return;
        }

        if (performedAnimationFinished || stateTimer <= 0f)
        {
            FinishCounter();
        }
    }

    public override void FixedUpdate()
    {
        player.SetVelocity(0f, player.GroundDetected() ? 0f : player.rb.velocity.y);
    }

    public override void Exit()
    {
        player.SetCounterAttack(false);
        player.SetCounterAttackPerformed(false);
    }

    public void CurrentStateTrigger()
    {
        performedAnimationFinished = true;
    }

    private bool TryPerformCounter()
    {
        if (combat == null || combat.TargetCheck == null)
        {
            return false;
        }

        Collider2D[] targets = Physics2D.OverlapCircleAll(
            combat.TargetCheck.position,
            combat.TargetCheckRadius,
            combat.WhatIsTarget
        );

        foreach (Collider2D targetCollider in targets)
        {
            if (targetCollider == null || IsSelfCollider(targetCollider))
            {
                continue;
            }

            ICounterable counterable = GetCounterable(targetCollider);
            if (counterable == null || !counterable.IsCounterWindowActive)
            {
                continue;
            }

            if (counterable.TryCounter())
            {
                EnterCounterPerformed();
                return true;
            }
        }

        return false;
    }

    private void EnterCounterPerformed()
    {
        counterPerformed = true;
        performedAnimationFinished = false;
        stateTimer = Mathf.Max(.01f, player.GetAnimationLength(player.CounterAttackPerformedAnimationState));

        player.SetCounterAttack(false);
        player.SetCounterAttackPerformed(true);
        player.SetVelocity(0f, player.GroundDetected() ? 0f : player.rb.velocity.y);

        PlayAnimation(player.CounterAttackPerformedAnimationState);
    }

    private void FinishCounter()
    {
        if (!player.GroundDetected())
        {
            stateMachine.ChangeState(player.fallState);
            return;
        }

        stateMachine.ChangeState(Mathf.Abs(player.moveInput.x) > .05f ? player.moveState : player.idleState);
    }

    private void PlayAnimation(string animationState)
    {
        if (player.anim == null || string.IsNullOrWhiteSpace(animationState))
        {
            return;
        }

        player.anim.Play(animationState, 0, 0f);
        player.anim.Update(0f);
    }

    private bool IsSelfCollider(Collider2D targetCollider)
    {
        return targetCollider.transform == player.transform
            || targetCollider.transform.IsChildOf(player.transform);
    }

    private ICounterable GetCounterable(Collider2D targetCollider)
    {
        MonoBehaviour[] behaviours = targetCollider.GetComponentsInParent<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is ICounterable counterable)
            {
                return counterable;
            }
        }

        return null;
    }
}
