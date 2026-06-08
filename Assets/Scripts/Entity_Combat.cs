using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Entity_Combat : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField, Range(1, 20)] private int damage = 1;

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

    private Entity owner;
    private int lastAttackFrame = -1;

    private void Awake()
    {
        owner = GetComponent<Entity>();
        EnsureHealthComponent();
        EnsureTargetCheck();
        AutoAssignTargetLayerIfEmpty();
    }

    private void Reset()
    {
        EnsureHealthComponent();
        EnsureTargetCheck();
        AutoAssignTargetLayerIfEmpty();
    }

    private void OnValidate()
    {
        targetCheckRadius = Mathf.Max(.01f, targetCheckRadius);
        damage = Mathf.Max(1, damage);

        if (!Application.isPlaying)
        {
            EnsureTargetCheck();
            AutoAssignTargetLayerIfEmpty();
        }
    }

    public bool AttackTrigger(Entity_AttackData attackData)
    {
        if (lastAttackFrame == Time.frameCount)
        {
            return false;
        }

        lastAttackFrame = Time.frameCount;

        Vector2 attackCenter = GetAttackCenter(attackData);
        float attackRadius = attackData.TargetCheckRadius;

        Collider2D[] targets = Physics2D.OverlapCircleAll(
            attackCenter,
            attackRadius,
            whatIsTarget
        );

        HashSet<Entity_Combat> damagedTargets = new HashSet<Entity_Combat>();
        bool hitAnyTarget = false;
        foreach (Collider2D targetCollider in targets)
        {
            if (targetCollider == null || IsSelfCollider(targetCollider))
            {
                continue;
            }

            Entity_Combat targetCombat = targetCollider.GetComponentInParent<Entity_Combat>();
            if (targetCombat == null || targetCombat == this)
            {
                continue;
            }

            if (!damagedTargets.Add(targetCombat))
            {
                continue;
            }

            hitAnyTarget |= targetCombat.ReceiveHit(this, GetWorldKnockback(attackData.KnockbackForce));
        }

        return hitAnyTarget;
    }

    public bool AttackTrigger(Vector2 knockbackForce)
    {
        return AttackTrigger(new Entity_AttackData(GetLegacyTargetCheckOffset(), targetCheckRadius, knockbackForce));
    }

    public bool ReceiveHit(Entity_Combat attacker, Vector2 knockbackVelocity)
    {
        Entity_Health health = GetComponentInParent<Entity_Health>();
        return health != null
            && health.TakeDamage(attacker != null ? attacker.Damage : 0, attacker, knockbackVelocity);
    }

    public bool TryReceiveHitFromCollider(Entity_Combat attacker, Vector2 attackCenter, float attackRadius, Vector2 knockbackVelocity)
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

        return ReceiveHit(attacker, knockbackVelocity);
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
        if (owner == null)
        {
            owner = GetComponent<Entity>();
        }

        int direction = owner != null ? owner.FacingDirection : 1;
        Vector2 offset = attackData.TargetCheckOffset;
        offset.x *= direction;
        return (Vector2)transform.position + offset;
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

    private bool IsSelfCollider(Collider2D targetCollider)
    {
        return targetCollider.transform == transform
            || targetCollider.transform.IsChildOf(transform);
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
