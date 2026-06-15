using UnityEngine;

public class Enemy_SlimeBattleState : EnemyState
{
    private readonly Enemy_Slime slime;
    private Transform player;
    private float lastTimeSeenPlayer;

    public Enemy_SlimeBattleState(Enemy_Slime slime, StateMachine stateMachine)
        : base(slime, stateMachine)
    {
        this.slime = slime;
    }

    public override void Enter()
    {
        base.Enter();

        if (slime == null)
        {
            return;
        }

        player = slime.GetPlayerReference();
        lastTimeSeenPlayer = Time.time;

        slime.SetAnimation(false, false, false);
        slime.SetBattleAnimation(true, 0f);
        slime.SetMoveAnimationSpeed(slime.MoveAnimSpeedMultiplier);

        if (player == null)
        {
            stateMachine.ChangeState(slime.idleState);
            return;
        }

        if (ShouldRetreat())
        {
            ShortRetreat();
        }
    }

    public override void Update()
    {
        base.Update();

        if (slime == null || stateMachine.CurrentState != this)
        {
            return;
        }

        if (slime.PlayerVisible && slime.PlayerTarget != null)
        {
            player = slime.PlayerTarget;
            lastTimeSeenPlayer = Time.time;
        }

        if (player == null)
        {
            stateMachine.ChangeState(slime.idleState);
            return;
        }

        if (Time.time - lastTimeSeenPlayer >= slime.BattleTimeDuration)
        {
            slime.StopChasingPlayer();
            stateMachine.ChangeState(slime.idleState);
            return;
        }

        if (WithinAttackRange() && slime.CanAttack)
        {
            stateMachine.ChangeState(slime.attackState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (slime == null || slime.rb == null || player == null)
        {
            return;
        }

        if (!ShouldMoveTowardPlayer())
        {
            slime.SetVelocity(0f, slime.rb.velocity.y);
            slime.SetBattleAnimation(true, 0f);
            return;
        }

        int direction = GetPlayerDirection();
        slime.FaceDirection(direction);
        slime.SetBattleAnimation(true, direction);
        slime.SetVelocity(direction * slime.BattleMoveSpeed, slime.rb.velocity.y);
    }

    private void ShortRetreat()
    {
        if (slime == null || slime.rb == null || player == null)
        {
            return;
        }

        int direction = GetPlayerDirection();
        Vector2 velocity = new Vector2(
            slime.RetreatVelocity.x * -direction,
            slime.RetreatVelocity.y
        );
        slime.SetVelocity(velocity.x, velocity.y);
    }

    private bool WithinAttackRange()
    {
        return slime.PlayerInAttackRange;
    }

    private bool ShouldRetreat()
    {
        return slime.PlayerWithinChaseHeight && DistanceToPlayer() < slime.MinRetreatDistance;
    }

    private float DistanceToPlayer()
    {
        if (player == null || slime == null)
        {
            return float.MaxValue;
        }

        return Mathf.Abs(player.position.x - slime.transform.position.x);
    }

    private int DirectionToPlayer()
    {
        if (player == null || slime == null)
        {
            return slime != null ? slime.FacingDirection : 1;
        }

        return player.position.x >= slime.transform.position.x ? 1 : -1;
    }

    private bool ShouldMoveTowardPlayer()
    {
        if (slime == null || slime.rb == null || player == null)
        {
            return false;
        }

        if (!slime.CanChasePlayer || !slime.PlayerWithinChaseHeight)
        {
            return false;
        }

        Bounds enemyBounds = slime.cd != null ? slime.cd.bounds : new Bounds(slime.transform.position, Vector3.one);
        Bounds playerBounds = slime.PlayerBounds;
        float horizontalGap = GetHorizontalGap(enemyBounds, playerBounds);

        return horizontalGap > slime.BattleStopDistance;
    }

    private float GetHorizontalGap(Bounds enemyBounds, Bounds playerBounds)
    {
        if (playerBounds.center.x >= enemyBounds.center.x)
        {
            return Mathf.Max(0f, playerBounds.min.x - enemyBounds.max.x);
        }

        return Mathf.Max(0f, enemyBounds.min.x - playerBounds.max.x);
    }

    private int GetPlayerDirection()
    {
        if (slime == null)
        {
            return 1;
        }

        if (slime.PlayerTargetDirection != 0)
        {
            return slime.PlayerTargetDirection;
        }

        return DirectionToPlayer();
    }
}
