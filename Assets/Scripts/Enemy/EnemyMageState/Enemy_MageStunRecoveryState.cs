using UnityEngine;

public class Enemy_MageStunRecoveryState : EnemyState
{
    private readonly Enemy_Mage mage;
    private float defaultGravityScale;
    private RigidbodyConstraints2D defaultConstraints;
    private bool hasCompleted;

    public Enemy_MageStunRecoveryState(Enemy_Mage enemy, StateMachine stateMachine)
        : base(enemy, stateMachine)
    {
        mage = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        hasCompleted = false;
        defaultGravityScale = enemy != null && enemy.rb != null ? enemy.rb.gravityScale : 1f;
        defaultConstraints = enemy != null && enemy.rb != null ? enemy.rb.constraints : RigidbodyConstraints2D.None;
        stateTimer = GetRecoveryClipLength();

        if (mage != null)
        {
            enemy.SetAnimation(false, false, false);
            mage.SetBattleAnimation(false, 0f);
            PlayRecoveryAnimation();
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

        if (hasCompleted || mage == null)
        {
            return;
        }

        if (stateTimer > 0f)
        {
            return;
        }

        hasCompleted = true;

        if (mage.IsAlerted && mage.battleState != null)
        {
            stateMachine.ChangeState(mage.battleState);
        }
        else if (mage.idleState != null)
        {
            stateMachine.ChangeState(mage.idleState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (mage == null || enemy.rb == null || hasCompleted)
        {
            return;
        }

        enemy.rb.velocity = Vector2.zero;
        enemy.rb.angularVelocity = 0f;
        enemy.rb.MovePosition(enemy.rb.position);
    }

    public override void Exit()
    {
        base.Exit();

        if (enemy.rb != null)
        {
            enemy.rb.gravityScale = defaultGravityScale;
            enemy.rb.constraints = defaultConstraints;
            enemy.rb.velocity = Vector2.zero;
            enemy.rb.angularVelocity = 0f;
        }
    }

    private void PlayRecoveryAnimation()
    {
        if (mage == null || mage.anim == null)
        {
            return;
        }

        if (!mage.anim.isActiveAndEnabled)
        {
            mage.anim.enabled = true;
        }

        mage.anim.speed = 1f;

        string stateName = mage.StunRecoveryAnimationState;
        string fullStateName = stateName.Contains(".")
            ? stateName
            : $"Base Layer.{stateName}";
        int fullStateHash = Animator.StringToHash(fullStateName);

        if (mage.anim.HasState(0, fullStateHash))
        {
            mage.anim.CrossFadeInFixedTime(fullStateName, 0f, 0);
            mage.anim.Update(0f);
            return;
        }

        mage.PlayAnimatorState(mage.StunRecoveryAnimationState);
    }

    private float GetRecoveryClipLength()
    {
        if (mage == null || mage.anim == null || mage.anim.runtimeAnimatorController == null)
        {
            return .5f;
        }

        AnimationClip[] clips = mage.anim.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
            {
                continue;
            }

            if (clip.name == mage.StunRecoveryAnimationState || clip.name == "mageStunRecovery")
            {
                return Mathf.Max(.05f, clip.length);
            }
        }

        return .5f;
    }
}
