using UnityEngine;

public class Enemy_ReaperBattleState : EnemyState
{
    private readonly Enemy_Reaper reaper;

    public Enemy_ReaperBattleState(Enemy_Reaper enemy, StateMachine stateMachine)
        : base(enemy, stateMachine, "battle")
    {
        reaper = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = reaper != null ? reaper.MaxBattleIdleTime : 5f;

        if (reaper == null)
        {
            return;
        }

        reaper.SetAnimation(false, false, false);
        reaper.SetAttackAnimationSpeed(1f);
        reaper.SetMoveAnimationSpeed(1f);
        reaper.SetBattleAnimationSpeed(Mathf.Max(.01f, reaper.BattleMoveSpeed / Mathf.Max(.01f, reaper.MoveSpeed)));
        reaper.SetBattleAnimation(true, 0f);

        if (reaper.PlayerTarget != null)
        {
            reaper.FaceDirection(reaper.PlayerTargetDirection);
        }
    }

    public override void Update()
    {
        base.Update();

        if (reaper == null || stateMachine.CurrentState != this)
        {
            return;
        }

        if (!reaper.IsAlerted)
        {
            if (reaper.idleState != null)
            {
                stateMachine.ChangeState(reaper.idleState);
            }
            return;
        }

        if (!reaper.PlayerWithinChaseHeight)
        {
            reaper.StopChasingPlayer();
            if (reaper.idleState != null)
            {
                stateMachine.ChangeState(reaper.idleState);
            }
            return;
        }

        if (reaper.PlayerInAttackRange && reaper.CanAttack && reaper.attackState != null)
        {
            stateMachine.ChangeState(reaper.attackState);
            return;
        }

        if (reaper.PlayerInSpellCastRange && reaper.CanDoSpellCast && reaper.spellCastState != null)
        {
            stateMachine.ChangeState(reaper.spellCastState);
            return;
        }

        if (stateTimer <= 0f && reaper.teleportState != null)
        {
            if (reaper.ShouldTeleport())
            {
                stateMachine.ChangeState(reaper.teleportState);
            }
            else
            {
                stateTimer = reaper.BattleTimeDuration;
            }
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (reaper == null || reaper.rb == null)
        {
            return;
        }

        float xVelocity = 0f;

        if (reaper.CanChasePlayer && reaper.PlayerTarget != null)
        {
            int directionToPlayer = reaper.PlayerTargetDirection;
            float horizontalGap = Mathf.Abs(reaper.PlayerTarget.position.x - reaper.transform.position.x);

            if (horizontalGap > reaper.BattleStopDistance)
            {
                int moveDirection = directionToPlayer;
                if (horizontalGap < reaper.MinRetreatDistance)
                {
                    moveDirection = -directionToPlayer;
                }
                else
                {
                    moveDirection = directionToPlayer;
                }

                if (reaper.CanMoveTowardDirection(moveDirection))
                {
                    xVelocity = moveDirection * reaper.BattleMoveSpeed;
                    reaper.FaceDirection(directionToPlayer);
                }
                else
                {
                    xVelocity = 0f;
                }
            }
        }

        reaper.SetBattleAnimation(true, xVelocity);
        reaper.SetVelocity(xVelocity, reaper.rb.velocity.y);
    }
}
