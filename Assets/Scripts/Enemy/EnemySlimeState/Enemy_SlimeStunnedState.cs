using UnityEngine;

public class Enemy_SlimeStunnedState : EnemyState
{
    private readonly Enemy_Slime slime;
    private float defaultGravityScale;
    private RigidbodyConstraints2D defaultConstraints;
    private bool hasCompleted;

    public Enemy_SlimeStunnedState(Enemy_Slime slime, StateMachine stateMachine)
        : base(slime, stateMachine)
    {
        this.slime = slime;
    }

    public override void Enter()
    {
        base.Enter();

        hasCompleted = false;
        defaultGravityScale = slime != null && slime.rb != null ? slime.rb.gravityScale : 1f;
        defaultConstraints = slime != null && slime.rb != null ? slime.rb.constraints : RigidbodyConstraints2D.None;
        stateTimer = slime != null ? slime.StunnedDuration : 1f;

        if (slime != null)
        {
            slime.SetAnimation(false, false, false);
            slime.SetBattleAnimation(false, 0f);
            slime.SetStunnedAnimation(true);
            slime.ApplyStunnedColliderProfile();

            if (slime.anim != null)
            {
                if (!slime.anim.isActiveAndEnabled)
                {
                    slime.anim.enabled = true;
                }

                slime.anim.speed = 1f;

                string stunnedStateName = slime.StunnedAnimationState;
                string fullStateName = stunnedStateName.Contains(".")
                    ? stunnedStateName
                    : $"Base Layer.{stunnedStateName}";
                slime.anim.Play(fullStateName, 0, 0f);
                slime.anim.Update(0f);
            }
        }

        if (slime != null && slime.rb != null)
        {
            slime.rb.gravityScale = 0f;
            slime.rb.velocity = new Vector2(
                slime.StunnedVelocity.x * -slime.FacingDirection,
                slime.StunnedVelocity.y
            );
            slime.rb.angularVelocity = 0f;
            slime.rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

    public override void Update()
    {
        base.Update();

        if (hasCompleted || slime == null)
        {
            return;
        }

        if (stateTimer > 0f)
        {
            return;
        }

        hasCompleted = true;

        if (slime.IsAlerted && slime.battleState != null)
        {
            stateMachine.ChangeState(slime.battleState);
        }
        else if (slime.idleState != null)
        {
            stateMachine.ChangeState(slime.idleState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (slime == null || slime.rb == null || hasCompleted)
        {
            return;
        }

        slime.rb.velocity = Vector2.zero;
        slime.rb.angularVelocity = 0f;
        slime.rb.MovePosition(slime.rb.position);
    }

    public override void Exit()
    {
        base.Exit();

        if (slime != null)
        {
            slime.SetStunnedAnimation(false);
            slime.RestoreAliveColliderProfile();
        }

        if (slime != null && slime.rb != null)
        {
            slime.rb.gravityScale = defaultGravityScale;
            slime.rb.constraints = defaultConstraints;
            slime.rb.velocity = Vector2.zero;
            slime.rb.angularVelocity = 0f;
        }
    }
}
