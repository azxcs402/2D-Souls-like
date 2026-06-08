using UnityEngine;

public class Enemy_DeadState : Enemy_GroundedState
{
    private bool droppedThroughGround;
    private bool destroyRequested;
    private int corpseSlideDirection;

    public Enemy_DeadState(Enemy enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        droppedThroughGround = false;
        destroyRequested = false;
        corpseSlideDirection = enemy.FacingDirection != 0 ? enemy.FacingDirection : 1;
        stateTimer = skeleton != null ? skeleton.DeadDisappearDelay : 4f;

        if (enemy.anim != null)
        {
            enemy.anim.speed = 1f;
            enemy.SetAnimation(false, false, false);
            enemy.anim.enabled = false;

            float fallAngle = skeleton != null ? skeleton.DeadFallAngle : 90f;
            float fallDirection = enemy.FacingDirection >= 0 ? 1f : -1f;
            enemy.anim.transform.localRotation = Quaternion.Euler(0f, 0f, fallAngle * fallDirection);
        }

        if (rb != null)
        {
            Vector2 velocity = rb.velocity;
            float slideSpeed = skeleton != null ? skeleton.DeadSlideSpeed : 0.65f;
            float fallSpeed = skeleton != null ? skeleton.DeadFallSpeed : 2.25f;

            velocity.x = corpseSlideDirection * slideSpeed;
            velocity.y = -Mathf.Abs(fallSpeed);
            rb.velocity = velocity;
        }

        if (skeleton != null && skeleton.DeadDropThroughDelay <= 0f)
        {
            droppedThroughGround = true;
            EnableCorpseTriggerMode();
        }
    }

    public override void Update()
    {
        base.Update();

        if (destroyRequested)
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
        Object.Destroy(enemy.gameObject);
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (rb == null || destroyRequested)
        {
            return;
        }

        Vector2 velocity = rb.velocity;
        float slideSpeed = skeleton != null ? skeleton.DeadSlideSpeed : 0.65f;
        float slideAcceleration = skeleton != null ? skeleton.DeadSlideAcceleration : 1.2f;
        float targetX = corpseSlideDirection * slideSpeed;
        velocity.x = Mathf.MoveTowards(velocity.x, targetX, slideAcceleration * Time.fixedDeltaTime);
        rb.velocity = velocity;
    }

    public override void Exit()
    {
        base.Exit();

        if (enemy.anim != null && !enemy.anim.isActiveAndEnabled)
        {
            enemy.anim.enabled = true;
            enemy.anim.speed = 1f;
        }
    }

    private bool HasDropThroughDelayElapsed()
    {
        if (skeleton == null)
        {
            return stateTimer <= 0f;
        }

        return stateTimer <= Mathf.Max(0f, skeleton.DeadDisappearDelay - skeleton.DeadDropThroughDelay);
    }

    private void EnableCorpseTriggerMode()
    {
        if (enemy.cd != null)
        {
            enemy.cd.isTrigger = true;
        }
    }
}
