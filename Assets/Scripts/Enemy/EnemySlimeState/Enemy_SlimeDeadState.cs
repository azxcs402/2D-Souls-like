using UnityEngine;

public class Enemy_SlimeDeadState : EnemyState
{
    private readonly Enemy_Slime slime;
    private bool droppedThroughGround;
    private bool destroyRequested;
    private int corpseSlideDirection;

    public Enemy_SlimeDeadState(Enemy_Slime slime, StateMachine stateMachine)
        : base(slime, stateMachine)
    {
        this.slime = slime;
    }

    public override void Enter()
    {
        base.Enter();

        droppedThroughGround = false;
        destroyRequested = false;
        corpseSlideDirection = slime != null && slime.FacingDirection != 0 ? slime.FacingDirection : 1;
        stateTimer = slime != null ? slime.DeadDisappearDelay : 4f;

        slime?.CreateSlimeOnDeath();

        if (slime != null && slime.anim != null)
        {
            slime.anim.speed = 1f;
            slime.SetAnimation(false, false, false);
            slime.SetBattleAnimation(false, 0f);
            slime.SetStunnedAnimation(false);
            slime.anim.enabled = false;
            slime.anim.transform.localRotation = Quaternion.Euler(0f, 0f, slime.DeadFallAngle * (slime.FacingDirection >= 0 ? 1f : -1f));
        }

        if (slime != null && slime.rb != null)
        {
            Vector2 velocity = slime.rb.velocity;
            velocity.x = corpseSlideDirection * slime.DeadSlideSpeed;
            velocity.y = -Mathf.Abs(slime.DeadFallSpeed);
            slime.rb.velocity = velocity;
        }

        if (slime != null && slime.DeadDropThroughDelay <= 0f)
        {
            droppedThroughGround = true;
            EnableCorpseTriggerMode();
        }
    }

    public override void Update()
    {
        base.Update();

        if (destroyRequested || slime == null)
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
        Object.Destroy(slime.gameObject);
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (slime == null || slime.rb == null || destroyRequested)
        {
            return;
        }

        Vector2 velocity = slime.rb.velocity;
        float targetX = corpseSlideDirection * slime.DeadSlideSpeed;
        velocity.x = Mathf.MoveTowards(velocity.x, targetX, slime.DeadSlideAcceleration * Time.fixedDeltaTime);
        slime.rb.velocity = velocity;
    }

    public override void Exit()
    {
        base.Exit();

        if (slime != null && slime.anim != null && !slime.anim.isActiveAndEnabled)
        {
            slime.anim.enabled = true;
            slime.anim.speed = 1f;
        }
    }

    private bool HasDropThroughDelayElapsed()
    {
        if (slime == null)
        {
            return stateTimer <= 0f;
        }

        return stateTimer <= Mathf.Max(0f, slime.DeadDisappearDelay - slime.DeadDropThroughDelay);
    }

    private void EnableCorpseTriggerMode()
    {
        if (slime != null && slime.cd != null)
        {
            slime.cd.isTrigger = true;
        }
    }
}
