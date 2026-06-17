using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class EnemyProjectileBreakRange : MonoBehaviour
{
    private const string ProjectileBreakableLayerName = "ProjectileBreakable";

    [SerializeField] private CircleCollider2D breakRangeCollider;
    [SerializeField, Min(0f)] private float breakRangeRadius = .6f;
    [SerializeField] private Vector2 breakRangeLocalOffset = Vector2.zero;
    [SerializeField] private Color gizmoColor = new Color(.15f, .95f, 1f, .95f);

    public CircleCollider2D BreakRangeCollider => breakRangeCollider;
    public float BreakRangeRadius => breakRangeRadius;
    public Vector2 BreakRangeLocalOffset => breakRangeLocalOffset;

    private void Awake()
    {
        CacheCollider();
        ApplySettings();
    }

    private void OnValidate()
    {
        CacheCollider();
        ApplySettings();
    }

    public void Configure(float radius, Vector2 localOffset)
    {
        breakRangeRadius = Mathf.Max(0f, radius);
        breakRangeLocalOffset = localOffset;
        CacheCollider();
        ApplySettings();
    }

    private void CacheCollider()
    {
        if (breakRangeCollider == null)
        {
            breakRangeCollider = GetComponent<CircleCollider2D>();
        }
    }

    private void ApplySettings()
    {
        if (breakRangeCollider == null)
        {
            return;
        }

        int breakableLayer = LayerMask.NameToLayer(ProjectileBreakableLayerName);
        if (breakableLayer >= 0)
        {
            gameObject.layer = breakableLayer;
        }

        breakRangeCollider.isTrigger = true;
        breakRangeCollider.radius = Mathf.Max(0f, breakRangeRadius);
        transform.localPosition = breakRangeLocalOffset;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, breakRangeRadius));
    }
}
