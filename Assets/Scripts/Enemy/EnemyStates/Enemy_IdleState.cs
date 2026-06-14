using UnityEngine;

public class Enemy_IdleState : Enemy_GroundedState
{
    private bool hasResolvedIdleEndTurn;

    public Enemy_IdleState(Enemy enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = skeleton != null ? skeleton.GetRandomIdleDuration() : .5f;
        hasResolvedIdleEndTurn = false;

        if (skeleton != null)
        {
            enemy.SetAnimation(true, false);
        }

        StopHorizontalMovement();
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.CurrentState != this)
        {
            return;
        }

        if (TryEnterCombatState())
        {
            return;
        }

        if (stateTimer <= 0f && skeleton != null)
        {
            if (!hasResolvedIdleEndTurn)
            {
                skeleton.TryRandomPatrolTurn();
                hasResolvedIdleEndTurn = true;
                return;
            }

            stateMachine.ChangeState(skeleton.moveState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (skeleton != null)
        {
            enemy.SetAnimation(true, false);
        }

        StopHorizontalMovement();
    }
}
