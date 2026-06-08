using UnityEngine;

[DisallowMultipleComponent]
public class Entity_AnimationTriggers : MonoBehaviour
{
    private Entity entity;

    private void Awake()
    {
        entity = GetComponentInParent<Entity>();
    }

    public void AttackOver()
    {
        if (entity is Player player)
        {
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
        else if (entity is Enemy enemy && enemy.stateMachine.CurrentState is Enemy_AttackState attackState)
        {
            attackState.AttackOver();
        }
    }

    public void AttackTrigger()
    {
        if (entity is Player player)
        {
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
        else if (entity is Enemy enemy && enemy.stateMachine.CurrentState is Enemy_AttackState attackState)
        {
            attackState.AttackTrigger();
        }
    }

    public void CurrentStateTrigger()
    {
        if (entity is Enemy enemy && enemy.stateMachine.CurrentState is Enemy_AttackState attackState)
        {
            attackState.CurrentStateTrigger();
        }
    }

    public void AnimationTrigger()
    {
    }

    public void ComboTrigger()
    {
    }
}
