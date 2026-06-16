using UnityEngine;

public class Enemy_ReaperDeadState : EnemyState
{
    private readonly Enemy_Reaper reaper;
    private bool droppedThroughGround;
    private bool destroyRequested;
    private int corpseSlideDirection;

    public Enemy_ReaperDeadState(Enemy enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
        reaper = enemy as Enemy_Reaper;
    }

    public override void Enter()
    {
        base.Enter();

        droppedThroughGround = false;
        destroyRequested = false;
        corpseSlideDirection = reaper != null && reaper.FacingDirection != 0 ? reaper.FacingDirection : 1;
        stateTimer = reaper != null ? reaper.DeadDisappearDelay : 4f;

        if (reaper == null)
        {
            return;
        }

        reaper.ForceDisableCounterWindow();
        reaper.SetAnimation(false, false, false);
        reaper.SetBattleAnimation(false, 0f);
        reaper.SetStunnedAnimation(false);
        reaper.SetSpellCastPerformed(false);

        if (reaper.anim != null)
        {
            reaper.anim.speed = 1f;
            reaper.anim.enabled = false;
            reaper.anim.transform.localRotation = Quaternion.Euler(0f, 0f, reaper.DeadFallAngle * (reaper.FacingDirection >= 0 ? 1f : -1f));
        }

        if (reaper.rb != null)
        {
            Vector2 velocity = reaper.rb.velocity;
            velocity.x = corpseSlideDirection * reaper.DeadSlideSpeed;
            velocity.y = -Mathf.Abs(reaper.DeadFallSpeed);
            reaper.rb.velocity = velocity;
        }

        if (reaper.DeadDropThroughDelay <= 0f)
        {
            droppedThroughGround = true;
            EnableCorpseTriggerMode();
        }
    }

    public override void Update()
    {
        base.Update();

        if (destroyRequested || reaper == null)
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
        Object.Destroy(reaper.gameObject);
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (reaper == null || reaper.rb == null || destroyRequested)
        {
            return;
        }

        Vector2 velocity = reaper.rb.velocity;
        float targetX = corpseSlideDirection * reaper.DeadSlideSpeed;
        velocity.x = Mathf.MoveTowards(velocity.x, targetX, reaper.DeadSlideAcceleration * Time.fixedDeltaTime);
        reaper.rb.velocity = velocity;
    }

    public override void Exit()
    {
        base.Exit();

        if (reaper != null && reaper.anim != null && !reaper.anim.isActiveAndEnabled)
        {
            reaper.anim.enabled = true;
            reaper.anim.speed = 1f;
        }
    }

    private bool HasDropThroughDelayElapsed()
    {
        if (reaper == null)
        {
            return stateTimer <= 0f;
        }

        return stateTimer <= Mathf.Max(0f, reaper.DeadDisappearDelay - reaper.DeadDropThroughDelay);
    }

    private void EnableCorpseTriggerMode()
    {
        if (reaper != null && reaper.cd != null)
        {
            reaper.cd.isTrigger = true;
        }
    }
}
