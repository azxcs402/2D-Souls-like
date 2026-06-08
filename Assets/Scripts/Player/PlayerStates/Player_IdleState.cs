using UnityEngine;

public class Player_IdleState : Player_GroundedState
{
    public Player_IdleState(Player player, StateMachine stateMachine)
        : base(player, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        player.SetAnimation(true, false);
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.CurrentState != this)
        {
            return;
        }

        if (Mathf.Abs(xInput) > .01f)
        {
            stateMachine.ChangeState(player.moveState);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        player.SetVelocity(0, player.rb.velocity.y);
    }

    public override void Exit()
    {
        base.Exit();
    }
}
