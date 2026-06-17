using UnityEngine;

public class Enemy_MageBattleState : Enemy_MageGroundedState
{
    private Transform player;
    private float lastTimeSeenPlayer;

    public Enemy_MageBattleState(Enemy_Mage enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player = mage != null ? mage.GetPlayerReference() : null;
        lastTimeSeenPlayer = Time.time;

        if (mage != null)
        {
            enemy.SetAnimation(false, false, false);
            mage.SetBattleAnimation(true, 0f);
            mage.SetMoveAnimationSpeed(1f);
        }

        if (player == null && mage != null && mage.idleState != null)
        {
            stateMachine.ChangeState(mage.idleState);
            return;
        }

        if (ShouldRetreat())
        {
            if (CanUseRetreatAbility())
            {
                Retreat();
            }
            else
            {
                ShortRetreat();
            }
        }
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.CurrentState != this || mage == null)
        {
            return;
        }

        Transform detectedPlayer = mage.PlayerDetected();
        if (detectedPlayer != null)
        {
            player = detectedPlayer;
            lastTimeSeenPlayer = Time.time;
        }

        if (mage.ShouldReturnToPatrol)
        {
            mage.ClearReturnToPatrolRequest();

            if (mage.idleState != null)
            {
                stateMachine.ChangeState(mage.idleState);
            }

            return;
        }

        if (player == null)
        {
            mage.StopChasingPlayer();

            if (mage.idleState != null)
            {
                stateMachine.ChangeState(mage.idleState);
            }

            return;
        }

        if (Time.time - lastTimeSeenPlayer >= mage.BattleTimeDuration && !mage.PlayerInSpellCastRange)
        {
            mage.StopChasingPlayer();

            if (mage.idleState != null)
            {
                stateMachine.ChangeState(mage.idleState);
            }

            return;
        }

        if (ShouldRetreat())
        {
            if (CanUseRetreatAbility())
            {
                Retreat();
            }
            else
            {
                ShortRetreat();
            }

            return;
        }

        if (mage.PlayerInMeleeRange && mage.CanAttack && mage.attackState != null)
        {
            stateMachine.ChangeState(mage.attackState);
        }
        else if (mage.PlayerInSpellCastRange && mage.CanSpellCast && mage.spellCastState != null)
        {
            stateMachine.ChangeState(mage.spellCastState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (rb == null || mage == null)
        {
            return;
        }

        if (player == null || !ShouldMoveTowardPlayer() || !mage.PlayerWithinChaseHeight)
        {
            enemy.SetVelocity(0f, rb.velocity.y);
            mage.SetBattleAnimation(true, 0f);
            return;
        }

        int direction = GetPlayerDirection();
        enemy.FaceDirection(direction);
        mage.SetBattleAnimation(true, direction);
        enemy.SetVelocity(direction * mage.BattleMoveSpeed, rb.velocity.y);
    }

    private void Retreat()
    {
        if (mage != null && mage.retreatState != null)
        {
            mage.MarkRetreatUsed();
            stateMachine.ChangeState(mage.retreatState);
        }
    }

    private void ShortRetreat()
    {
        if (mage == null || rb == null || player == null)
        {
            return;
        }

        int direction = GetPlayerDirection();
        Vector2 velocity = new Vector2(
            mage.RetreatVelocity.x * -direction,
            mage.RetreatVelocity.y
        );

        enemy.FaceDirection(direction);
        enemy.SetVelocity(velocity.x, velocity.y);
    }

    private bool CanUseRetreatAbility()
    {
        return mage != null && mage.CanUseRetreatAbility();
    }

    private bool ShouldRetreat()
    {
        return mage != null && mage.PlayerWithinChaseHeight && DistanceToPlayer() < mage.MinRetreatDistance;
    }

    private float DistanceToPlayer()
    {
        if (player == null || mage == null)
        {
            return float.MaxValue;
        }

        return Mathf.Abs(player.position.x - mage.transform.position.x);
    }

    private int GetPlayerDirection()
    {
        if (player == null || mage == null)
        {
            return mage != null ? mage.FacingDirection : 1;
        }

        return player.position.x >= mage.transform.position.x ? 1 : -1;
    }

    private bool ShouldMoveTowardPlayer()
    {
        if (mage == null || rb == null || player == null || !mage.CanChasePlayer)
        {
            return false;
        }

        Bounds enemyBounds = mage.cd != null ? mage.cd.bounds : new Bounds(mage.transform.position, Vector3.one);
        Bounds playerBounds = mage.PlayerBounds;
        float horizontalGap = GetHorizontalGap(enemyBounds, playerBounds);

        return horizontalGap > mage.BattleStopDistance;
    }

    private float GetHorizontalGap(Bounds enemyBounds, Bounds playerBounds)
    {
        if (playerBounds.center.x >= enemyBounds.center.x)
        {
            return Mathf.Max(0f, playerBounds.min.x - enemyBounds.max.x);
        }

        return Mathf.Max(0f, enemyBounds.min.x - playerBounds.max.x);
    }
}
