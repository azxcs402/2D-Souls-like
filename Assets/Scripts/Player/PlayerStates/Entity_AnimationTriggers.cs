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
        else if (entity is Enemy_Reaper reaperAttackEnemy && reaperAttackEnemy.stateMachine.CurrentState is Enemy_ReaperAttackState reaperAttackState)
        {
            reaperAttackState.AttackOver();
        }
        else if (entity is Enemy_Reaper reaperSpellEnemy && reaperSpellEnemy.stateMachine.CurrentState is Enemy_ReaperSpellCastState reaperSpellCastState)
        {
            reaperSpellCastState.AttackOver();
        }
        else if (entity is Enemy_Reaper reaperTeleportEnemy && reaperTeleportEnemy.stateMachine.CurrentState is Enemy_ReaperTeleportState reaperTeleportState)
        {
            reaperTeleportState.AttackOver();
        }
    }

    public void EnableCounterWindow()
    {
        if (entity is Enemy enemy && enemy is ICounterable counterable)
        {
            counterable.EnableCounterWindow();
        }
    }

    public void DisableCounterWindow()
    {
        if (entity is Enemy enemy && enemy is ICounterable counterable)
        {
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
        else if (entity is Enemy_AbyssMage abyssMage)
        {
            if (abyssMage.stateMachine.CurrentState is Enemy_AbyssMageAttackState abyssMageAttackState)
            {
                abyssMageAttackState.AttackTrigger();
            }
            else if (abyssMage.stateMachine.CurrentState is Enemy_AbyssMageSpellCastState abyssMageSpellCastState)
            {
                abyssMageSpellCastState.AttackTrigger();
            }
        }
        else if (entity is Enemy_Reaper reaper)
        {
            if (reaper.stateMachine.CurrentState is Enemy_ReaperAttackState reaperAttackState)
            {
                reaperAttackState.AttackTrigger();
            }
            else if (reaper.stateMachine.CurrentState is Enemy_ReaperSpellCastState reaperSpellCastState)
            {
                reaperSpellCastState.AttackTrigger();
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
        else if (entity is Enemy_AbyssMage abyssMage)
        {
            if (abyssMage.stateMachine.CurrentState is Enemy_AbyssMageAttackState abyssMageAttackState)
            {
                abyssMageAttackState.CurrentStateTrigger();
            }
            else if (abyssMage.stateMachine.CurrentState is Enemy_AbyssMageSpellCastState abyssMageSpellCastState)
            {
                abyssMageSpellCastState.CurrentStateTrigger();
            }
        }
        else if (entity is Enemy_Reaper reaper)
        {
            if (reaper.stateMachine.CurrentState is Enemy_ReaperAttackState reaperAttackState)
            {
                reaperAttackState.CurrentStateTrigger();
            }
            else if (reaper.stateMachine.CurrentState is Enemy_ReaperSpellCastState reaperSpellCastState)
            {
                reaperSpellCastState.CurrentStateTrigger();
            }
            else if (reaper.stateMachine.CurrentState is Enemy_ReaperTeleportState reaperTeleportState)
            {
                reaperTeleportState.CurrentStateTrigger();
            }
        }
        else if (entity is Enemy enemy && enemy.stateMachine.CurrentState is Enemy_AttackState attackState)
        {
            attackState.CurrentStateTrigger();
        }
    }

    public void TeleportTrigger()
    {
        if (entity is Enemy_Reaper reaper && reaper.stateMachine.CurrentState is Enemy_ReaperTeleportState reaperTeleportState)
        {
            reaperTeleportState.TeleportTrigger();
        }
    }

    public void SpecialAttackTrigger()
    {
        if (entity is Enemy_Mage mage && mage.IsSpellCasting)
        {
            mage.SpecialAttack();
        }
        else if (entity is Enemy_AbyssMage abyssMage && abyssMage.IsSpellCasting)
        {
            abyssMage.SpecialAttack();
        }
        else if (entity is Enemy_Reaper reaper && reaper.IsSpellCasting)
        {
            reaper.SpecialAttack();
        }
    }

    public void AnimationTrigger()
    {
    }

    public void ComboTrigger()
    {
    }
}
