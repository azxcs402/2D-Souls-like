using UnityEngine;

public class Enemy_StunnedState : EnemyState
{
    private float defaultGravityScale;
    private float stunDuration;
    private bool hasCompleted;
    private Enemy_Skeleton skeleton => enemy as Enemy_Skeleton;

    public Enemy_StunnedState(Enemy enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        hasCompleted = false;
        defaultGravityScale = enemy != null && enemy.rb != null ? enemy.rb.gravityScale : 1f;
        stunDuration = GetStunClipLength();
        stateTimer = stunDuration;

        if (enemy.anim != null)
        {
            enemy.anim.speed = 1f;
            enemy.SetAnimation(false, false, false);
            PlayStunnedAnimation();
        }

        if (enemy.rb != null)
        {
            enemy.rb.gravityScale = 0f;
            enemy.SetVelocity(GetStunVelocity().x, GetStunVelocity().y);
        }
    }

    public override void Update()
    {
        base.Update();

        if (hasCompleted)
        {
            return;
        }

        if (stateTimer > 0f)
        {
            return;
        }

        hasCompleted = true;

        if (skeleton != null && skeleton.IsAlerted && skeleton.battleState != null)
        {
            stateMachine.ChangeState(skeleton.battleState);
        }
        else if (skeleton != null && skeleton.idleState != null)
        {
            stateMachine.ChangeState(skeleton.idleState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (enemy.rb == null || hasCompleted)
        {
            return;
        }

        Vector2 stunVelocity = GetStunVelocity();
        enemy.SetVelocity(stunVelocity.x, stunVelocity.y);
    }

    public override void Exit()
    {
        base.Exit();

        if (enemy.rb != null)
        {
            enemy.rb.gravityScale = defaultGravityScale;
        }
    }

    private void PlayStunnedAnimation()
    {
        if (enemy.anim == null)
        {
            return;
        }

        string stunnedStateName = skeleton != null && !string.IsNullOrWhiteSpace(skeleton.StunnedAnimationState)
            ? skeleton.StunnedAnimationState
            : "skeletonStunned";

        string fullStateName = stunnedStateName.Contains(".")
            ? stunnedStateName
            : $"Base Layer.{stunnedStateName}";
        int fullStateHash = Animator.StringToHash(fullStateName);

        if (enemy.anim.HasState(0, fullStateHash))
        {
            enemy.anim.Play(fullStateName, 0, 0f);
            enemy.anim.Update(0f);
            return;
        }

        enemy.PlayAnimatorState(stunnedStateName);
    }

    private Vector2 GetStunVelocity()
    {
        Vector2 moveDistance = skeleton != null ? skeleton.StunnedMoveDistance : new Vector2(0f, .5f);
        float duration = Mathf.Max(.01f, stunDuration);

        return new Vector2(
            moveDistance.x * (enemy != null ? enemy.FacingDirection : 1f),
            moveDistance.y
        ) / duration;
    }

    private float GetStunClipLength()
    {
        if (enemy == null || enemy.anim == null || enemy.anim.runtimeAnimatorController == null)
        {
            return .5f;
        }

        string stunnedStateName = skeleton != null && !string.IsNullOrWhiteSpace(skeleton.StunnedAnimationState)
            ? skeleton.StunnedAnimationState
            : "skeletonStunned";

        foreach (AnimationClip clip in enemy.anim.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name == stunnedStateName)
            {
                return Mathf.Max(.05f, clip.length);
            }
        }

        return .5f;
    }
}
