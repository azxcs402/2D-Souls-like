using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_ReaperSpell : MonoBehaviour
{
    [SerializeField] private LayerMask whatIsTarget;
    [SerializeField] private Collider2D col;
    [SerializeField, Min(0f)] private float damageActivationDelay = 0.45f;
    [SerializeField, Min(.01f)] private float lifeTime = 2f;
    [SerializeField, Min(1)] private int baseDamage = 1;

    private Entity_Combat combat;
    private DamageScaleData damageScaleData;
    private Coroutine activationCoroutine;

    private void Awake()
    {
        EnsureTargetMaskAssigned();

        if (col == null)
        {
            col = GetComponent<Collider2D>();
        }

        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Reset()
    {
        EnsureTargetMaskAssigned();
    }

    private void OnValidate()
    {
        EnsureTargetMaskAssigned();
    }

    private void EnsureTargetMaskAssigned()
    {
        if (whatIsTarget == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                whatIsTarget = 1 << playerLayer;
            }
        }
    }

    public void SetupSpell(Entity_Combat combat, DamageScaleData damageScaleData)
    {
        this.combat = combat;
        this.damageScaleData = damageScaleData;

        DisableCollider();

        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }

        activationCoroutine = StartCoroutine(EnableColliderAfterDelay());
        Destroy(gameObject, lifeTime);
    }

    public void EnableCollider()
    {
        if (col != null)
        {
            col.enabled = true;
        }
    }

    public void DisableCollider()
    {
        if (col != null)
        {
            col.enabled = false;
        }
    }

    private IEnumerator EnableColliderAfterDelay()
    {
        if (damageActivationDelay > 0f)
        {
            yield return new WaitForSeconds(damageActivationDelay);
        }

        EnableCollider();
        activationCoroutine = null;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null)
        {
            return;
        }

        if (((1 << collision.gameObject.layer) & whatIsTarget.value) == 0)
        {
            return;
        }

        Entity_Health targetHealth = collision.GetComponentInParent<Entity_Health>();
        if (targetHealth == null)
        {
            return;
        }

        int damage = combat != null ? Mathf.Max(1, combat.Damage) : Mathf.Max(1, baseDamage);
        if (damageScaleData != null)
        {
            int scaledBaseDamage = combat != null ? combat.Damage : baseDamage;
            damage = Mathf.Max(1, Mathf.RoundToInt(scaledBaseDamage * Mathf.Max(.01f, damageScaleData.phyiscal)));
        }

        targetHealth.TakeDamage(damage, combat, Vector2.zero);
        DisableCollider();
    }
}
