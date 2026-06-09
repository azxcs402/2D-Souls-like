using UnityEngine;

public class Enemy_StunnedState : EnemyState
{
    private float defaultGravityScale;
    private RigidbodyConstraints2D defaultConstraints;
    private float stunDuration;
    private bool hasCompleted;
    private string activeStunnedStateName;
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
        defaultConstraints = enemy != null && enemy.rb != null ? enemy.rb.constraints : RigidbodyConstraints2D.None;
        stunDuration = GetStunClipLength();
        stateTimer = stunDuration;
        activeStunnedStateName = null;

        if (enemy.anim != null)
        {
            enemy.anim.speed = 1f;
            enemy.SetAnimation(false, false, false);
            skeleton?.SetStunnedAnimation(true);
            PlayStunnedAnimation();
        }

        if (enemy.rb != null)
        {
            enemy.rb.gravityScale = 0f;
            enemy.rb.velocity = Vector2.zero;
            enemy.rb.angularVelocity = 0f;
            enemy.rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

    public override void Update()
    {
        base.Update();

        if (hasCompleted)
        {
            return;
        }

        if (skeleton != null)
        {
            skeleton.SetStunnedAnimation(true);
        }

        if (stateTimer > 0f)
        {
            EnsureStunnedAnimationLoops();
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

        enemy.rb.velocity = Vector2.zero;
        enemy.rb.angularVelocity = 0f;
        enemy.rb.MovePosition(enemy.rb.position);
        EnsureStunnedAnimationLoops();
    }

    public override void Exit()
    {
        base.Exit();

        if (skeleton != null)
        {
            skeleton.SetStunnedAnimation(false);
        }

        if (enemy.rb != null)
        {
            enemy.rb.gravityScale = defaultGravityScale;
            enemy.rb.constraints = defaultConstraints;
            enemy.rb.velocity = Vector2.zero;
            enemy.rb.angularVelocity = 0f;
        }
    }

    private void PlayStunnedAnimation()
    {
        if (enemy.anim == null)
        {
            return;
        }

        string[] candidateStateNames = GetCandidateStunnedStateNames();
        for (int i = 0; i < candidateStateNames.Length; i++)
        {
            string candidateStateName = candidateStateNames[i];
            if (string.IsNullOrWhiteSpace(candidateStateName))
            {
                continue;
            }

            string fullStateName = candidateStateName.Contains(".")
                ? candidateStateName
                : $"Base Layer.{candidateStateName}";
            int fullStateHash = Animator.StringToHash(fullStateName);

            if (enemy.anim.HasState(0, fullStateHash))
            {
                activeStunnedStateName = candidateStateName;
                enemy.anim.Play(fullStateName, 0, 0f);
                enemy.anim.Update(0f);
                return;
            }
        }

        activeStunnedStateName = candidateStateNames[0];
        enemy.PlayAnimatorState(candidateStateNames[0]);
    }

    private void EnsureStunnedAnimationLoops()
    {
        if (enemy == null || enemy.anim == null || string.IsNullOrWhiteSpace(activeStunnedStateName))
        {
            return;
        }

        string fullStateName = activeStunnedStateName.Contains(".")
            ? activeStunnedStateName
            : $"Base Layer.{activeStunnedStateName}";
        int fullStateHash = Animator.StringToHash(fullStateName);

        if (!enemy.anim.HasState(0, fullStateHash))
        {
            return;
        }

        AnimatorStateInfo stateInfo = enemy.anim.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.fullPathHash != fullStateHash)
        {
            return;
        }

        if (stateInfo.loop)
        {
            return;
        }

        if (stateInfo.normalizedTime < 1f)
        {
            return;
        }

        enemy.anim.Play(fullStateName, 0, 0f);
        enemy.anim.Update(0f);
    }

    private float GetStunClipLength()
    {
        if (enemy == null || enemy.anim == null || enemy.anim.runtimeAnimatorController == null)
        {
            return .5f;
        }

        string[] candidateStateNames = GetCandidateStunnedStateNames();

        foreach (AnimationClip clip in enemy.anim.runtimeAnimatorController.animationClips)
        {
            if (clip == null)
            {
                continue;
            }

            for (int i = 0; i < candidateStateNames.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(candidateStateNames[i]) && clip.name == candidateStateNames[i])
                {
                    return Mathf.Max(.05f, clip.length);
                }
            }
        }

        return .5f;
    }

    private string[] GetCandidateStunnedStateNames()
    {
        string configuredStateName = skeleton != null && !string.IsNullOrWhiteSpace(skeleton.StunnedAnimationState)
            ? skeleton.StunnedAnimationState
            : "skeletonStunned";

        return new[]
        {
            configuredStateName,
            "skeletonStunned",
            "skeletonStunnded"
        };
    }
}
