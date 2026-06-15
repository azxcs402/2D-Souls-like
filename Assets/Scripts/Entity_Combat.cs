using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Entity_Combat : MonoBehaviour
{
    private static int nextAttackId = 1;

    [Header("Damage")]
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField] private bool showCombatDebugLogs = true;

    [Header("Target detection")]
    [SerializeField] private Transform targetCheck;
    [SerializeField, Min(.01f)] private float targetCheckRadius = .6f;
    [SerializeField] private LayerMask whatIsTarget;
    [SerializeField] private bool showTargetCheckGizmos = true;
    [SerializeField] private bool alwaysShowTargetCheckGizmos;

    [Header("Combat preview")]
    [SerializeField] private bool enableCombatPreview = true;
    [SerializeField] private bool showPreviewTargetCheckObject = true;
    [SerializeField] private bool showPreviewAttackData = true;
    [SerializeField] private AnimationClip previewAnimationClip;
    [SerializeField, Range(0f, 1f)] private float previewNormalizedTime;

    [Header("Setup")]
    [SerializeField] private bool autoEnsureHealthComponent = true;

    public int Damage => damage;
    public Transform TargetCheck => targetCheck;
    public float TargetCheckRadius => targetCheckRadius;
    public LayerMask WhatIsTarget => whatIsTarget;
    public bool ShowTargetCheckGizmos => showTargetCheckGizmos;
    public bool AlwaysShowTargetCheckGizmos => alwaysShowTargetCheckGizmos;
    public bool EnableCombatPreview => enableCombatPreview;
    public bool ShowPreviewTargetCheckObject => showPreviewTargetCheckObject;
    public bool ShowPreviewAttackData => showPreviewAttackData;
    public AnimationClip PreviewAnimationClip => previewAnimationClip;
    public float PreviewNormalizedTime => previewNormalizedTime;

    public void SetDamage(int value)
    {
        damage = Mathf.Max(1, value);
    }

    private Entity owner;
    private Entity_VFX vfx;
    private readonly Dictionary<Entity_Combat, int> receivedAttackIdsByAttacker = new Dictionary<Entity_Combat, int>();

    private void Awake()
    {
        owner = GetComponent<Entity>();
        EnsureVfxComponent();
        vfx = GetComponent<Entity_VFX>();
        if (autoEnsureHealthComponent)
        {
            EnsureHealthComponent();
        }
        EnsureTargetCheck();
        AutoAssignTargetLayerIfEmpty();
    }

    private void Reset()
    {
        EnsureVfxComponent();
        if (autoEnsureHealthComponent)
        {
            EnsureHealthComponent();
        }
        EnsureTargetCheck();
        AutoAssignTargetLayerIfEmpty();
    }

    private void OnValidate()
    {
        targetCheckRadius = Mathf.Max(.01f, targetCheckRadius);
        damage = Mathf.Max(1, damage);

        if (!Application.isPlaying)
        {
            EnsureVfxComponent();
            if (autoEnsureHealthComponent)
            {
                EnsureHealthComponent();
            }
            EnsureTargetCheck();
            AutoAssignTargetLayerIfEmpty();
        }
    }

    public bool AttackTrigger(Entity_AttackData attackData)
    {
        return AttackTrigger(attackData, GetNextAttackId());
    }

    public bool AttackTrigger(Entity_AttackData attackData, int attackId)
    {
        Vector2 attackCenter = GetAttackCenter(attackData);
        float attackRadius = attackData.TargetCheckRadius;

        if (showCombatDebugLogs)
        {
            Debug.Log(
                $"{name} AttackTrigger start. attackId={attackId}, center={attackCenter}, radius={attackRadius:0.000}, layerMask={whatIsTarget.value}",
                this);
        }

        bool hitAnyTarget = TryAttackTargets(
            Physics2D.OverlapCircleAll(attackCenter, attackRadius, whatIsTarget),
            attackData,
            attackId,
            strictLayerMatch: true
        );

        if (!hitAnyTarget)
        {
            hitAnyTarget = TryAttackTargets(
                Physics2D.OverlapCircleAll(attackCenter, attackRadius, ~0),
                attackData,
                attackId,
                strictLayerMatch: false
            );
        }

        if (showCombatDebugLogs)
        {
            Debug.Log(
                $"{name} AttackTrigger end. attackId={attackId}, hitAnyTarget={hitAnyTarget}",
                this);
        }

        return hitAnyTarget;
    }

    public bool AttackTriggerFromTargetCheck(float attackRadius, Vector2 knockbackForce)
    {
        return AttackTriggerFromTargetCheck(attackRadius, knockbackForce, GetNextAttackId());
    }

    public bool AttackTriggerFromTargetCheck(float attackRadius, Vector2 knockbackForce, int attackId)
    {
        Vector2 attackCenter = GetTargetCheckWorldPosition();

        if (showCombatDebugLogs)
        {
            Debug.Log(
                $"{name} AttackTriggerFromTargetCheck start. attackId={attackId}, center={attackCenter}, radius={attackRadius:0.000}, layerMask={whatIsTarget.value}",
                this);
        }

        bool hitAnyTarget = TryAttackTargets(
            Physics2D.OverlapCircleAll(attackCenter, attackRadius, whatIsTarget),
            new Entity_AttackData(Vector2.zero, attackRadius, knockbackForce),
            attackId,
            strictLayerMatch: true
        );

        if (!hitAnyTarget)
        {
            hitAnyTarget = TryAttackTargets(
                Physics2D.OverlapCircleAll(attackCenter, attackRadius, ~0),
                new Entity_AttackData(Vector2.zero, attackRadius, knockbackForce),
                attackId,
                strictLayerMatch: false
            );
        }

        if (showCombatDebugLogs)
        {
            Debug.Log(
                $"{name} AttackTriggerFromTargetCheck end. attackId={attackId}, hitAnyTarget={hitAnyTarget}",
                this);
        }

        return hitAnyTarget;
    }

    public bool AttackTrigger(Vector2 knockbackForce)
    {
        return AttackTrigger(new Entity_AttackData(GetLegacyTargetCheckOffset(), targetCheckRadius, knockbackForce));
    }

    public bool ReceiveHit(Entity_Combat attacker, Vector2 knockbackVelocity)
    {
        return ReceiveHit(attacker, knockbackVelocity, GetNextAttackId());
    }

    public bool ReceiveHit(Entity_Combat attacker, Vector2 knockbackVelocity, int attackId)
    {
        if (!CanReceiveAttack(attacker, attackId))
        {
            return false;
        }

        Entity_Health health = GetComponentInParent<Entity_Health>();
        return health != null
            && health.TakeDamage(attacker != null ? attacker.Damage : 0, attacker, knockbackVelocity);
    }

    public bool TryReceiveHitFromCollider(Entity_Combat attacker, Vector2 attackCenter, float attackRadius, Vector2 knockbackVelocity)
    {
        return TryReceiveHitFromCollider(attacker, attackCenter, attackRadius, knockbackVelocity, GetNextAttackId());
    }

    public bool TryReceiveHitFromCollider(Entity_Combat attacker, Vector2 attackCenter, float attackRadius, Vector2 knockbackVelocity, int attackId)
    {
        Collider2D targetCollider = GetComponentInParent<Collider2D>();
        if (targetCollider == null)
        {
            return false;
        }

        Vector2 closestPoint = targetCollider.ClosestPoint(attackCenter);
        if (Vector2.Distance(closestPoint, attackCenter) > Mathf.Max(.01f, attackRadius))
        {
            return false;
        }

        return ReceiveHit(attacker, knockbackVelocity, attackId);
    }

    private bool TryAttackTargets(Collider2D[] targets, Entity_AttackData attackData, int attackId, bool strictLayerMatch)
    {
        HashSet<Entity_Health> damagedTargets = new HashSet<Entity_Health>();
        bool hitAnyTarget = false;
        Vector2 knockbackVelocity = GetWorldKnockback(attackData.KnockbackForce);

        if (showCombatDebugLogs)
        {
            Debug.Log(
                $"{name} evaluating {targets.Length} collider(s). strictLayerMatch={strictLayerMatch}, attackId={attackId}",
                this);
        }

        foreach (Collider2D targetCollider in targets)
        {
            if (targetCollider == null || IsSelfCollider(targetCollider))
            {
                if (showCombatDebugLogs && targetCollider != null)
                {
                    Debug.Log($"{name} skipped self collider {targetCollider.name}.", targetCollider);
                }
                continue;
            }

            if (strictLayerMatch && !IsTargetLayer(targetCollider.gameObject.layer))
            {
                if (showCombatDebugLogs)
                {
                    Debug.Log(
                        $"{name} skipped {targetCollider.name} on layer {LayerMask.LayerToName(targetCollider.gameObject.layer)} because it is not in target mask.",
                        targetCollider);
                }
                continue;
            }

            Entity_Health targetHealth = targetCollider.GetComponentInParent<Entity_Health>();
            if (targetHealth == null || targetHealth == GetComponentInParent<Entity_Health>())
            {
                if (showCombatDebugLogs)
                {
                    Debug.Log(
                        $"{name} collider {targetCollider.name} has no valid Entity_Health target.",
                        targetCollider);
                }
                continue;
            }

            if (!damagedTargets.Add(targetHealth))
            {
                if (showCombatDebugLogs)
                {
                    Debug.Log($"{name} already damaged {targetHealth.name} for attackId={attackId}.", targetHealth);
                }
                continue;
            }

            bool hitTarget = targetHealth.TakeDamage(Damage, this, knockbackVelocity);
            hitAnyTarget |= hitTarget;

            if (hitTarget && vfx != null)
            {
                vfx.CreateOnHitVFX(targetHealth.transform);
            }

            if (showCombatDebugLogs)
            {
                Debug.Log(
                    $"{name} attackId={attackId} -> target={targetHealth.name}, hit={hitTarget}, damage={Damage}, knockback={knockbackVelocity}",
                    targetHealth);
            }
        }

        return hitAnyTarget;
    }

    private static int GetNextAttackId()
    {
        if (nextAttackId == int.MaxValue)
        {
            nextAttackId = 1;
        }

        return nextAttackId++;
    }

    private bool CanReceiveAttack(Entity_Combat attacker, int attackId)
    {
        if (attacker == null || attackId <= 0)
        {
            return true;
        }

        if (receivedAttackIdsByAttacker.TryGetValue(attacker, out int lastAttackId)
            && lastAttackId == attackId)
        {
            return false;
        }

        receivedAttackIdsByAttacker[attacker] = attackId;
        return true;
    }

    private Vector2 GetWorldKnockback(Vector2 knockbackForce)
    {
        if (owner == null)
        {
            owner = GetComponent<Entity>();
        }

        int direction = owner != null ? owner.FacingDirection : 1;
        return new Vector2(knockbackForce.x * direction, knockbackForce.y);
    }

    public Vector2 GetAttackCenter(Entity_AttackData attackData)
    {
        Vector2 offset = attackData.TargetCheckOffset;

        if (owner == null)
        {
            owner = GetComponent<Entity>();
        }

        int direction = owner != null ? owner.FacingDirection : 1;
        offset.x *= direction;

        if (targetCheck != null)
        {
            return (Vector2)targetCheck.position + offset;
        }

        return (Vector2)transform.position + offset;
    }

    public Vector2 GetTargetCheckWorldPosition()
    {
        EnsureTargetCheck();
        if (targetCheck != null)
        {
            return targetCheck.position;
        }

        return transform.position;
    }

    public Entity_AttackData GetLegacyAttackData(Vector2 knockbackForce)
    {
        return new Entity_AttackData(GetLegacyTargetCheckOffset(), targetCheckRadius, knockbackForce);
    }

    public bool HasTarget()
    {
        EnsureTargetCheck();

        if (targetCheck == null)
        {
            return false;
        }

        Collider2D[] targets = Physics2D.OverlapCircleAll(
            targetCheck.position,
            targetCheckRadius,
            whatIsTarget
        );

        foreach (Collider2D targetCollider in targets)
        {
            if (targetCollider != null && !IsSelfCollider(targetCollider))
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureTargetCheck()
    {
        if (targetCheck != null)
        {
            return;
        }

        Transform existingTargetCheck = transform.Find("TargetCheck");
        if (existingTargetCheck != null)
        {
            targetCheck = existingTargetCheck;
            return;
        }

        GameObject targetCheckObject = new GameObject("TargetCheck");
        targetCheckObject.transform.SetParent(transform, false);
        targetCheckObject.transform.localPosition = Vector3.zero;
        targetCheck = targetCheckObject.transform;
    }

    private Vector2 GetLegacyTargetCheckOffset()
    {
        EnsureTargetCheck();

        if (targetCheck == null)
        {
            return Vector2.zero;
        }

        Vector2 offset = targetCheck.position - transform.position;
        if (owner == null)
        {
            owner = GetComponent<Entity>();
        }

        int direction = owner != null ? owner.FacingDirection : 1;
        offset.x *= direction;
        return offset;
    }

    private void AutoAssignTargetLayerIfEmpty()
    {
        if (whatIsTarget != 0)
        {
            return;
        }

        if (owner == null)
        {
            owner = GetComponent<Entity>();
        }

        string targetLayerName = owner is Player ? "Enemy" : "Player";
        int targetLayer = LayerMask.NameToLayer(targetLayerName);
        if (targetLayer >= 0)
        {
            whatIsTarget = 1 << targetLayer;
        }
    }

    private void EnsureHealthComponent()
    {
        if (GetComponent<Entity_Health>() != null)
        {
            return;
        }

        if (GetComponent<Enemy>() != null)
        {
            gameObject.AddComponent<Enemy_Healthy>();
        }
        else
        {
            gameObject.AddComponent<Entity_Health>();
        }
    }

    private void EnsureVfxComponent()
    {
        if (GetComponent<Entity_VFX>() == null)
        {
            gameObject.AddComponent<Entity_VFX>();
        }
    }

    private bool IsSelfCollider(Collider2D targetCollider)
    {
        return targetCollider.transform == transform
            || targetCollider.transform.IsChildOf(transform);
    }

    private bool IsTargetLayer(int layer)
    {
        if (whatIsTarget == 0)
        {
            return true;
        }

        return (whatIsTarget.value & (1 << layer)) != 0;
    }

    private void OnDrawGizmos()
    {
        if (alwaysShowTargetCheckGizmos)
        {
            DrawTargetCheckGizmos();
        }
    }

    private void OnDrawGizmosSelected()
    {
        DrawTargetCheckGizmos();
    }

    private void DrawTargetCheckGizmos()
    {
        if (!showTargetCheckGizmos)
        {
            return;
        }

        EnsureTargetCheck();

        if (targetCheck == null)
        {
            return;
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(targetCheck.position, targetCheckRadius);
        Gizmos.DrawLine(transform.position, targetCheck.position);
    }
}
