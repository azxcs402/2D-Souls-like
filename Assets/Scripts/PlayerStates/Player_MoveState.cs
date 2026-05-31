using UnityEngine;

public class Player_MoveState : Player_GroundedState
{
    public Player_MoveState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        player.SetAnimation(false, true);
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.CurrentState != this)
        {
            return;
        }

        if (Mathf.Abs(xInput) <= .01f)
        {
            stateMachine.ChangeState(player.idleState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        player.SetVelocity(
            xInput * player.MoveSpeed,
            player.rb.velocity.y
        );

        player.CheckForFlip(xInput);
    }

    public override void Exit()
    {
        base.Exit();
    }
}
