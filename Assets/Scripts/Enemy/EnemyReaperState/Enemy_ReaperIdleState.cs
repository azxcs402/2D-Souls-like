using UnityEngine;

public class Enemy_ReaperIdleState : EnemyState
{
    private readonly Enemy_Reaper reaper;
    private bool hasResolvedIdleEndTurn;

    public Enemy_ReaperIdleState(Enemy_Reaper enemy, StateMachine stateMachine)
        : base(enemy, stateMachine, "idle")
    {
        reaper = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = reaper != null ? reaper.GetRandomIdleDuration() : .5f;
        hasResolvedIdleEndTurn = false;

        if (reaper == null)
        {
            return;
        }

        reaper.SetBattleAnimation(false, 0f);
        reaper.SetAttackAnimationSpeed(1f);
        reaper.SetMoveAnimationSpeed(reaper.MoveAnimSpeedMultiplier);
        reaper.SetStunnedAnimation(false);
        reaper.SetAnimation(true, false, false);
        StopHorizontalMovement();
    }

    public override void Update()
    {
        base.Update();

        if (reaper == null || stateMachine.CurrentState != this)
        {
            return;
        }

        if (reaper.IsAlerted && reaper.PlayerWithinChaseHeight && reaper.battleState != null)
        {
            stateMachine.ChangeState(reaper.battleState);
            return;
        }

        if (stateTimer <= 0f)
        {
            if (!hasResolvedIdleEndTurn)
            {
                if (Random.value <= reaper.PatrolTurnChance)
                {
                    reaper.TurnAround();
                }

                hasResolvedIdleEndTurn = true;
                return;
            }

            if (reaper.moveState != null)
            {
                stateMachine.ChangeState(reaper.moveState);
            }
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        StopHorizontalMovement();
    }

    private void StopHorizontalMovement()
    {
        if (reaper != null && reaper.rb != null)
        {
            reaper.SetVelocity(0f, reaper.rb.velocity.y);
        }
    }
}
