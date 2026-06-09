using UnityEngine;

public class Player_DeadState : EntityState
{
    private const string DeathAnimationName = "playerDead";
    private const float DeathFreezeNormalizedTime = .999f;

    private bool waitingForGround;
    private bool hasPlayedDeathAnimation;
    private bool hasFrozenDeathAnimation;
    private float deathAnimationLength;
    private float deathAnimationTimer;

    public Player_DeadState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        waitingForGround = !player.GroundContactDetected();
        hasPlayedDeathAnimation = false;
        hasFrozenDeathAnimation = false;
        deathAnimationTimer = 0f;
        deathAnimationLength = Mathf.Max(.01f, player.GetAnimationLength(DeathAnimationName));

        player.rb.gravityScale = player.DefaultGravityScale;
        player.SetAnimation(false, false);
        player.SetJumpFall(waitingForGround);
        player.SetWallSlide(false);
        player.SetDash(false);
        player.SetBasicAttack(false);
        player.SetBasicAttackIndex(0);
        player.SetAirAttackIndex(0);
        player.SetFallAttack(false);
        player.SetCounterAttack(false);
        player.SetCounterAttackPerformed(false);
        player.ResetFallAttackTrigger();
        player.SetDead(false);
        player.SetDeathGroundVisualOffset(false);

        if (!waitingForGround)
        {
            PlayDeathAnimation();
        }
    }

    public override void Update()
    {
        base.Update();

        if (hasPlayedDeathAnimation)
        {
            if (!hasFrozenDeathAnimation)
            {
                deathAnimationTimer += Time.deltaTime;

                if (deathAnimationTimer >= Mathf.Max(0f, deathAnimationLength - Time.deltaTime)
                    || (player.anim != null
                        && player.anim.GetCurrentAnimatorStateInfo(0).IsName(DeathAnimationName)
                        && player.anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f))
                {
                    if (player.anim != null)
                    {
                        player.anim.Play($"Base Layer.{DeathAnimationName}", 0, DeathFreezeNormalizedTime);
                        player.anim.Update(0f);
                    }

                    hasFrozenDeathAnimation = true;
                    player.anim.speed = 0f;
                }
            }

            return;
        }

        if (waitingForGround)
        {
            if (!player.GroundContactDetected())
            {
                player.SetJumpFall(true);
                player.SetYVelocity(player.rb.velocity.y);
                return;
            }

            waitingForGround = false;
            PlayDeathAnimation();
            return;
        }

        PlayDeathAnimation();
    }

    public override void FixedUpdate()
    {
        if (hasPlayedDeathAnimation)
        {
            player.SetVelocity(0f, 0f);
        }
    }

    private void PlayDeathAnimation()
    {
        if (hasPlayedDeathAnimation)
        {
            return;
        }

        hasPlayedDeathAnimation = true;
        deathAnimationTimer = 0f;

        player.SetJumpFall(false);
        player.SetWallSlide(false);
        player.SetDash(false);
        player.SetBasicAttack(false);
        player.SetBasicAttackIndex(0);
        player.SetAirAttackIndex(0);
        player.SetFallAttack(false);
        player.SetCounterAttack(false);
        player.SetCounterAttackPerformed(false);
        player.ResetFallAttackTrigger();
        player.SetYVelocity(0f);
        player.SetVelocity(0f, 0f);
        player.rb.gravityScale = 0f;
        player.SetDead(true);
        player.anim.speed = 1f;
        player.anim.Play($"Base Layer.{DeathAnimationName}", 0, 0f);
        player.anim.Update(0f);
    }
}
