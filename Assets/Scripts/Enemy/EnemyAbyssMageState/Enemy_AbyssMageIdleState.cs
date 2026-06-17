using UnityEngine;

public class Enemy_AbyssMageIdleState : Enemy_AbyssMageGroundedState
{
    private bool hasResolvedIdleEndTurn;

    public Enemy_AbyssMageIdleState(Enemy_AbyssMage enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = mage != null ? mage.GetRandomIdleDuration() : .5f;
        hasResolvedIdleEndTurn = false;

        if (mage != null)
        {
            enemy.SetAnimation(true, false, false);
            mage.SetBattleAnimation(false, 0f);
            mage.SetMoveAnimationSpeed(1f);
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

        if (stateTimer <= 0f && mage != null)
        {
            if (!hasResolvedIdleEndTurn)
            {
                mage.TryRandomPatrolTurn();
                hasResolvedIdleEndTurn = true;
                return;
            }

            stateMachine.ChangeState(mage.moveState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (mage != null)
        {
            enemy.SetAnimation(true, false, false);
            mage.SetBattleAnimation(false, 0f);
        }

        StopHorizontalMovement();
    }
}
