using UnityEngine;

public class Enemy_BattleState : Enemy_GroundedState
{
    public Enemy_BattleState(Enemy enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        enemy.SetAnimation(false, false, false);

        if (skeleton != null && skeleton.PlayerTarget != null)
        {
            if (ShouldMoveTowardPlayer())
            {
                enemy.FaceDirection(skeleton.PlayerTargetDirection);
                skeleton.SetBattleAnimation(true, skeleton.PlayerTargetDirection);
            }
            else
            {
                skeleton.SetBattleAnimation(true, 0f);
            }

            PlayBattleAnimation();
        }
        else
        {
            skeleton?.SetBattleAnimation(true, 0f);
            PlayBattleAnimation();
        }
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.CurrentState != this || skeleton == null)
        {
            return;
        }

        if (skeleton.ShouldReturnToPatrol)
        {
            skeleton.ClearReturnToPatrolRequest();
            stateMachine.ChangeState(skeleton.idleState);
            return;
        }

        if (!skeleton.IsAlerted)
        {
            stateMachine.ChangeState(skeleton.idleState);
            return;
        }

        if (!skeleton.PlayerWithinChaseHeight)
        {
            skeleton.StopChasingPlayer();
            stateMachine.ChangeState(skeleton.idleState);
            return;
        }

        if (skeleton.PlayerInAttackRange && skeleton.CanAttack && skeleton.attackState != null)
        {
            stateMachine.ChangeState(skeleton.attackState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (rb == null || skeleton == null)
        {
            return;
        }

        bool shouldMove = ShouldMoveTowardPlayer();
        float xVelocity = 0f;

        enemy.SetAnimation(false, false, false);

        if (shouldMove && skeleton.PlayerTarget != null)
        {
            int moveDirection = skeleton.PlayerTargetDirection;
            float moveSpeed = skeleton.BattleMoveSpeed;

            if (enemy.CanMoveTowardDirection(moveDirection))
            {
                xVelocity = moveDirection * moveSpeed;
                enemy.FaceDirection(moveDirection);
                skeleton.SetBattleAnimation(true, moveDirection);
            }
            else
            {
                skeleton.SetBattleAnimation(true, 0f);
            }
        }
        else
        {
            skeleton.SetBattleAnimation(true, 0f);
        }

        enemy.SetVelocity(xVelocity, rb.velocity.y);
        PlayBattleAnimation();
    }

    public override void Exit()
    {
        base.Exit();

        skeleton?.SetBattleAnimation(false, 0f);
    }

    private void PlayBattleAnimation()
    {
        if (enemy.anim == null || skeleton == null || string.IsNullOrWhiteSpace(skeleton.BattleAnimationState))
        {
            return;
        }

        string fullStateName = skeleton.BattleAnimationState.Contains(".")
            ? skeleton.BattleAnimationState
            : $"Base Layer.{skeleton.BattleAnimationState}";
        int fullStateHash = Animator.StringToHash(fullStateName);

        if (!enemy.anim.HasState(0, fullStateHash))
        {
            enemy.SetAnimation(false, true, false);
            return;
        }

        if (!enemy.anim.GetCurrentAnimatorStateInfo(0).IsName(skeleton.BattleAnimationState)
            && !enemy.anim.GetCurrentAnimatorStateInfo(0).IsName(fullStateName))
        {
            enemy.anim.CrossFadeInFixedTime(fullStateName, 0f, 0);
        }
    }

    private bool ShouldMoveTowardPlayer()
    {
        if (skeleton == null || skeleton.PlayerTarget == null || enemy.cd == null)
        {
            return false;
        }

        Bounds enemyBounds = enemy.cd.bounds;
        Bounds playerBounds = skeleton.PlayerBounds;
        float verticalDistance = Mathf.Abs(playerBounds.center.y - enemyBounds.center.y);

        if (verticalDistance > skeleton.ChaseVerticalDistance)
        {
            return false;
        }

        float horizontalGap = GetHorizontalGap(enemyBounds, playerBounds);
        return horizontalGap > skeleton.BattleStopDistance;
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
