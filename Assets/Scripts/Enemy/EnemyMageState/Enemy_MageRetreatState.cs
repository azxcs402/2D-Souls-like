using UnityEngine;

public class Enemy_MageRetreatState : EnemyState
{
    private readonly Enemy_Mage mage;
    private bool teleported;

    public Enemy_MageRetreatState(Enemy_Mage enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
        mage = enemy;
    }

    public override void Enter()
    {
        base.Enter();
        teleported = false;

        if (mage != null)
        {
            enemy.SetAnimation(false, false, false);
            mage.SetBattleAnimation(true, 0f);
            mage.SetMoveAnimationSpeed(1f);
            enemy.SetVelocity(0f, 0f);
        }

        if (mage == null)
        {
            return;
        }

        if (mage.ConsumeQueuedTeleportDestination(out Vector2 queuedDestination))
        {
            teleported = mage.TeleportToDestination(queuedDestination);
        }
        else
        {
            teleported = mage.TryTeleportToRetreatPoint(out _);
        }

        stateTimer = teleported ? mage.TeleportPostDelay : 0f;
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.CurrentState != this || mage == null)
        {
            return;
        }

        if (!teleported)
        {
            if (mage.spellCastState != null)
            {
                stateMachine.ChangeState(mage.spellCastState);
            }

            return;
        }

        if (stateTimer <= 0f)
        {
            if (mage.spellCastState != null)
            {
                stateMachine.ChangeState(mage.spellCastState);
            }
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if (rb != null)
        {
            enemy.SetVelocity(0f, rb.velocity.y);
        }
    }
}
