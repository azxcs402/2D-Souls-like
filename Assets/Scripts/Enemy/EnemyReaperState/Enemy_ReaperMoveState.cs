using UnityEngine;

public class Enemy_ReaperMoveState : EnemyState
{
    private readonly Enemy_Reaper reaper;
    private float turnTimer;

    public Enemy_ReaperMoveState(Enemy_Reaper enemy, StateMachine stateMachine)
        : base(enemy, stateMachine, "move")
    {
        reaper = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = reaper != null ? reaper.GetRandomMoveDuration() : .5f;
        turnTimer = 0f;

        if (reaper == null)
        {
            return;
        }

        reaper.SetBattleAnimation(false, 0f);
        reaper.SetAttackAnimationSpeed(1f);
        reaper.SetMoveAnimationSpeed(1f);
        reaper.SetStunnedAnimation(false);
        reaper.SetAnimation(false, true, false);

        if (!reaper.GroundDetected() || reaper.FacingWallContactDetected() || !reaper.CanMoveTowardDirection(reaper.FacingDirection))
        {
            reaper.TurnAround();
            turnTimer = reaper.PatrolTurnDelay;
        }
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

        if (turnTimer > 0f)
        {
            turnTimer -= Time.deltaTime;
        }

        if (turnTimer <= 0f && (!reaper.GroundDetected() || reaper.FacingWallContactDetected() || reaper.EdgeDetected(.1f, .08f) || !reaper.CanMoveTowardDirection(reaper.FacingDirection)))
        {
            reaper.TurnAround();
            turnTimer = reaper.PatrolTurnDelay;
        }

        if (stateTimer <= 0f && reaper.idleState != null)
        {
            stateMachine.ChangeState(reaper.idleState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (reaper == null || reaper.rb == null)
        {
            return;
        }

        reaper.SetAnimation(false, true, false);
        reaper.SetMoveAnimationSpeed(1f);
        if (!reaper.CanMoveTowardDirection(reaper.FacingDirection))
        {
            reaper.SetVelocity(0f, reaper.rb.velocity.y);
            return;
        }

        reaper.SetVelocity(reaper.MoveSpeed * reaper.FacingDirection, reaper.rb.velocity.y);
    }
}
