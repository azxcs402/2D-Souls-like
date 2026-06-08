using UnityEngine;

[System.Serializable]
public struct Entity_AttackData
{
    [SerializeField] private Vector2 targetCheckOffset;
    [SerializeField, Min(.01f)] private float targetCheckRadius;
    [SerializeField] private Vector2 knockbackForce;

    public Vector2 TargetCheckOffset => targetCheckOffset;
    public float TargetCheckRadius => Mathf.Max(.01f, targetCheckRadius);
    public Vector2 KnockbackForce => knockbackForce;

    public Entity_AttackData(Vector2 targetCheckOffset, float targetCheckRadius, Vector2 knockbackForce)
    {
        this.targetCheckOffset = targetCheckOffset;
        this.targetCheckRadius = Mathf.Max(.01f, targetCheckRadius);
        this.knockbackForce = knockbackForce;
    }
}
