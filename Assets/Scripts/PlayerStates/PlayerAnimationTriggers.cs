using UnityEngine;

public class PlayerAnimationTriggers : MonoBehaviour
{
    private Player player;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
    }

    public void AttackOver()
    {
        if (player == null)
        {
            return;
        }

        if (player.stateMachine.CurrentState == player.basicAttackState)
        {
            player.basicAttackState.AttackOver();
        }
        else if (player.stateMachine.CurrentState == player.airAttackState)
        {
            player.airAttackState.AttackOver();
        }
        else if (player.stateMachine.CurrentState == player.fallAttackState)
        {
            player.fallAttackState.AttackOver();
        }
    }

    public void AttackTrigger()
    {
        if (player == null)
        {
            return;
        }

        if (player.stateMachine.CurrentState == player.basicAttackState)
        {
            player.basicAttackState.AttackTrigger();
        }
        else if (player.stateMachine.CurrentState == player.airAttackState)
        {
            player.airAttackState.AttackTrigger();
        }
        else if (player.stateMachine.CurrentState == player.fallAttackState)
        {
            player.fallAttackState.AttackTrigger();
        }
    }

    public void AnimationTrigger()
    {
        AttackTrigger();
    }

    public void ComboTrigger()
    {
        AttackTrigger();
    }
}
