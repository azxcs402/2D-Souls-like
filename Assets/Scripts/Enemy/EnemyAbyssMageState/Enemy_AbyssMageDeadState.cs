using UnityEngine;

public class Enemy_AbyssMageDeadState : EnemyState
{
    private readonly Enemy_AbyssMage mage;
    private bool droppedThroughGround;
    private bool destroyRequested;
    private int corpseSlideDirection;

    public Enemy_AbyssMageDeadState(Enemy_AbyssMage enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
        mage = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        droppedThroughGround = false;
        destroyRequested = false;
        corpseSlideDirection = mage != null && mage.FacingDirection != 0 ? mage.FacingDirection : 1;
        stateTimer = mage != null ? mage.DeadDisappearDelay : 4f;

        if (mage != null)
        {
            mage.SetAnimation(false, false, false);
            mage.SetBattleAnimation(false, 0f);
            mage.SetStunnedAnimation(false);
            mage.anim.speed = 1f;
            mage.anim.enabled = false;
            mage.anim.transform.localRotation = Quaternion.Euler(0f, 0f, mage.DeadFallAngle * (mage.FacingDirection >= 0 ? 1f : -1f));
        }

        if (enemy.rb != null)
        {
            Vector2 velocity = enemy.rb.velocity;
            velocity.x = corpseSlideDirection * (mage != null ? mage.DeadSlideSpeed : .65f);
            velocity.y = -Mathf.Abs(mage != null ? mage.DeadFallSpeed : 2.25f);
            enemy.rb.velocity = velocity;
        }

        if (mage != null && mage.DeadDropThroughDelay <= 0f)
        {
            droppedThroughGround = true;
            EnableCorpseTriggerMode();
        }
    }

    public override void Update()
    {
        base.Update();

        if (destroyRequested || mage == null)
        {
            return;
        }

        if (!droppedThroughGround && HasDropThroughDelayElapsed())
        {
            droppedThroughGround = true;
            EnableCorpseTriggerMode();
        }

        if (stateTimer > 0f)
        {
            return;
        }

        destroyRequested = true;
        Object.Destroy(mage.gameObject);
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (mage == null || enemy.rb == null || destroyRequested)
        {
            return;
        }

        Vector2 velocity = enemy.rb.velocity;
        float slideSpeed = mage.DeadSlideSpeed;
        float slideAcceleration = mage.DeadSlideAcceleration;
        float targetX = corpseSlideDirection * slideSpeed;
        velocity.x = Mathf.MoveTowards(velocity.x, targetX, slideAcceleration * Time.fixedDeltaTime);
        enemy.rb.velocity = velocity;
    }

    public override void Exit()
    {
        base.Exit();

        if (mage != null && mage.anim != null && !mage.anim.isActiveAndEnabled)
        {
            mage.anim.enabled = true;
            mage.anim.speed = 1f;
        }
    }

    private bool HasDropThroughDelayElapsed()
    {
        if (mage == null)
        {
            return stateTimer <= 0f;
        }

        return stateTimer <= Mathf.Max(0f, mage.DeadDisappearDelay - mage.DeadDropThroughDelay);
    }

    private void EnableCorpseTriggerMode()
    {
        if (mage != null && mage.cd != null)
        {
            mage.cd.isTrigger = true;
        }
    }
}
