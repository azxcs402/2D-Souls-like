using UnityEngine;

public class Enemy_SlimeIdleState : EnemyState
{
    private readonly Enemy_Slime slime;
    private bool hasResolvedIdleEndTurn;

    public Enemy_SlimeIdleState(Enemy_Slime slime, StateMachine stateMachine)
        : base(slime, stateMachine)
    {
        this.slime = slime;
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = slime != null ? slime.GetRandomIdleDuration() : .5f;
        hasResolvedIdleEndTurn = false;

        if (slime != null)
        {
            slime.SetBattleAnimation(false, 0f);
            slime.SetMoveAnimationSpeed(slime.MoveAnimSpeedMultiplier);
            slime.SetStunnedAnimation(false);
            slime.SetAnimation(true, false, false);
        }

        StopHorizontalMovement();
    }

    public override void Update()
    {
        base.Update();

        if (slime == null || stateMachine.CurrentState != this)
        {
            return;
        }

        if (stateTimer <= 0f && slime != null)
        {
            if (!hasResolvedIdleEndTurn)
            {
                slime.TryRandomPatrolTurn();
                hasResolvedIdleEndTurn = true;
                return;
            }

            stateMachine.ChangeState(slime.moveState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        StopHorizontalMovement();
    }

    private void StopHorizontalMovement()
    {
        if (slime != null && slime.rb != null)
        {
            slime.SetVelocity(0f, slime.rb.velocity.y);
        }
    }
}
