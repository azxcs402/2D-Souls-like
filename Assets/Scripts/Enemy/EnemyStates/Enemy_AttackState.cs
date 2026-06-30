using UnityEngine;

public class Enemy_AttackState : Enemy_GroundedState
{
    private const string AttackAnimationStateName = "skeletonAttack";
    private bool animationTriggered;
    private bool hasCompleted;
    private int attackDirection;
    private float elapsedAttackTime;
    private bool damageTriggered;
    private bool damageWindowActive;
    private Entity_Combat combat;

    public Enemy_AttackState(Enemy enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        animationTriggered = false;
        hasCompleted = false;
        attackDirection = enemy.FacingDirection;
        elapsedAttackTime = 0f;
        damageTriggered = false;
        damageWindowActive = false;
        combat = enemy.GetComponent<Entity_Combat>();
        stateTimer = GetAttackClipLength();
        skeleton?.DisableCounterWindow();

        if (skeleton != null)
        {
            if (skeleton.PlayerTarget != null)
            {
                enemy.FaceDirection(skeleton.PlayerTargetDirection);
                attackDirection = skeleton.PlayerTargetDirection;
            }

            enemy.SetAnimation(false, false, true);
            PlayAttackAnimation();

            enemy.SetVelocity(0f, rb != null ? rb.velocity.y : 0f);
        }
    }

    public override void Update()
    {
        base.Update();
        elapsedAttackTime += Time.deltaTime;

        if (stateMachine.CurrentState != this || hasCompleted)
        {
            return;
        }

        if (animationTriggered || stateTimer <= 0f)
        {
            hasCompleted = true;

            if (skeleton != null)
            {
                skeleton.CompleteAttackState();
            }
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (skeleton == null)
        {
            return;
        }

        Vector2 velocity = GetAttackVelocity();
        enemy.SetVelocity(velocity.x, velocity.y);
    }

    public override void Exit()
    {
        base.Exit();

        skeleton?.DisableCounterWindow();
        enemy.SetAnimation(false, false, false);
        enemy.SetVelocity(0f, rb != null ? rb.velocity.y : 0f);
    }

    public void CurrentStateTrigger()
    {
        AttackOver();
    }

    public void AttackTrigger()
    {
        if (damageTriggered)
        {
            return;
        }

        damageTriggered = true;
        damageWindowActive = true;
        TryApplyAttackDamage();
        damageWindowActive = false;
    }

    public void AttackOver()
    {
        damageWindowActive = false;
        animationTriggered = true;
        stateTimer = 0f;
    }

    private bool TryApplyAttackDamage()
    {
        if (!damageWindowActive || combat == null)
        {
            return false;
        }

        Entity_AttackData attackData = skeleton != null
            ? skeleton.SkeletonAttackData
            : new Entity_AttackData(Vector2.zero, .6f, Vector2.zero);

        bool hitPlayer = skeleton == null || skeleton.EnableFallbackFullLayerDetection
            ? combat.AttackTrigger(attackData)
            : combat.AttackTriggerWithoutFallback(attackData);

        return hitPlayer;
    }

    private void PlayAttackAnimation()
    {
        if (enemy.anim == null)
        {
            return;
        }

        if (!enemy.anim.isActiveAndEnabled)
        {
            enemy.anim.enabled = true;
        }

        enemy.anim.speed = 1f;

        string fullStateName = AttackAnimationStateName.Contains(".")
            ? AttackAnimationStateName
            : $"Base Layer.{AttackAnimationStateName}";
        int fullStateHash = Animator.StringToHash(fullStateName);

        if (enemy.anim.HasState(0, fullStateHash))
        {
            enemy.anim.Play(fullStateName, 0, 0f);
            enemy.anim.Update(0f);
            return;
        }

        enemy.PlayAnimatorState(AttackAnimationStateName);
    }

    private float GetAttackClipLength()
    {
        if (enemy == null || enemy.anim == null || enemy.anim.runtimeAnimatorController == null)
        {
            return .5f;
        }

        AnimationClip[] clips = enemy.anim.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && clip.name == "skeletonAttack")
            {
                return Mathf.Max(.05f, clip.length);
            }
        }

        return .5f;
    }

    private Vector2 GetAttackVelocity()
    {
        Vector2 velocity = new Vector2(0f, rb != null ? rb.velocity.y : 0f);

        if (skeleton == null)
        {
            return velocity;
        }

        Vector2 moveDistance = skeleton.SkeletonAttackMoveDistance;
        float moveDuration = Mathf.Max(.01f, skeleton.SkeletonAttackMoveDuration);

        if (IsMoveAxisActive(skeleton.SkeletonAttackMoveXDelay, moveDuration))
        {
            int moveDirection = attackDirection;
            if (moveDirection != 0 && !enemy.CanMoveTowardDirection(moveDirection))
            {
                velocity.x = 0f;
            }
            else
            {
                velocity.x = moveDirection * moveDistance.x / moveDuration;
            }
        }

        if (IsMoveAxisActive(skeleton.SkeletonAttackMoveYDelay, moveDuration))
        {
            velocity.y = moveDistance.y / moveDuration;
        }

        return velocity;
    }

    private bool IsMoveAxisActive(float delay, float duration)
    {
        return elapsedAttackTime >= delay
            && elapsedAttackTime < delay + duration;
    }
}
