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

    public void EnableCounterWindow()
    {
        if (entity is Enemy enemy && enemy is ICounterable counterable)
        {
            Debug.Log($"{enemy.name} animation event EnableCounterWindow fired.", enemy);
            counterable.EnableCounterWindow();
        }
    }

    public void DisableCounterWindow()
    {
        if (entity is Enemy enemy && enemy is ICounterable counterable)
        {
            Debug.Log($"{enemy.name} animation event DisableCounterWindow fired.", enemy);
            counterable.DisableCounterWindow();
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
        else if (entity is Enemy slimeEnemy && slimeEnemy.stateMachine.CurrentState is Enemy_SlimeAttackState slimeAttackState)
        {
            slimeAttackState.AttackTrigger();
        }
        else if (entity is Enemy_Mage mage)
        {
            if (mage.stateMachine.CurrentState is Enemy_MageAttackState mageAttackState)
            {
                mageAttackState.AttackTrigger();
            }
            else if (mage.stateMachine.CurrentState is Enemy_MageSpellCastState mageSpellCastState)
            {
                mageSpellCastState.AttackTrigger();
            }
        }
        else if (entity is Enemy enemy && enemy.stateMachine.CurrentState is Enemy_AttackState attackState)
        {
            attackState.AttackTrigger();
        }
    }

    public void CurrentStateTrigger()
    {
        if (entity is Player player && player.stateMachine.CurrentState == player.counterAttackState)
        {
            player.counterAttackState.CurrentStateTrigger();
        }
        else if (entity is Enemy slimeEnemy && slimeEnemy.stateMachine.CurrentState is Enemy_SlimeAttackState slimeAttackState)
        {
            slimeAttackState.CurrentStateTrigger();
        }
        else if (entity is Enemy_Mage mage)
        {
            if (mage.stateMachine.CurrentState is Enemy_MageAttackState mageAttackState)
            {
                mageAttackState.CurrentStateTrigger();
            }
            else if (mage.stateMachine.CurrentState is Enemy_MageSpellCastState mageSpellCastState)
            {
                mageSpellCastState.CurrentStateTrigger();
            }
        }
        else if (entity is Enemy enemy && enemy.stateMachine.CurrentState is Enemy_AttackState attackState)
        {
            attackState.CurrentStateTrigger();
        }
    }

    public void SpecialAttackTrigger()
    {
        if (entity is Enemy_Mage mage && mage.IsSpellCasting)
        {
            mage.SpecialAttack();
        }
    }

    public void AnimationTrigger()
    {
    }

    public void ComboTrigger()
    {
    }
}
