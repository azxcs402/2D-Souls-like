using UnityEngine;

public class Enemy_Skeleton : Enemy
{
    [Header("Patrol Info")]
    [SerializeField, Min(.1f)] private float idleDuration = 1.2f;
    [SerializeField, Min(.1f)] private float moveDuration = 2f;

    [Header("Edge Check")]
    [SerializeField] private bool useEdgeCheck = true;
    [SerializeField, Min(0f)] private float edgeCheckForwardOffset = .1f;
    [SerializeField, Min(.01f)] private float edgeCheckDistance = .08f;

    [Header("Skeleton Info")]
    [SerializeField] private float skeletonMoveSpeed = 2f;
    [SerializeField] private float skeletonAggroRange = 4f;
    [SerializeField] private float skeletonAttackRange = 1f;
    [SerializeField] private float skeletonTurnDelay = .15f;
    [SerializeField] private int skeletonContactDamage = 1;
    [SerializeField] private bool canSkeletonBeKnockedBack = true;

    [Header("Animation State Names")]
    [SerializeField] private string idleAnimationState = "skeletonIdle";
    [SerializeField] private string moveAnimationState = "skeletonMove";

    public Enemy_IdleState idleState { get; private set; }
    public Enemy_MoveState moveState { get; private set; }

    public float IdleDuration => idleDuration;
    public float MoveDuration => moveDuration;
    public bool UseEdgeCheck => useEdgeCheck;
    public float EdgeCheckForwardOffset => edgeCheckForwardOffset;
    public float EdgeCheckDistance => edgeCheckDistance;
    public float SkeletonMoveSpeed => skeletonMoveSpeed;
    public float SkeletonAggroRange => skeletonAggroRange;
    public float SkeletonAttackRange => skeletonAttackRange;
    public float SkeletonTurnDelay => skeletonTurnDelay;
    public int SkeletonContactDamage => skeletonContactDamage;
    public bool CanSkeletonBeKnockedBack => canSkeletonBeKnockedBack;
    public string IdleAnimationState => idleAnimationState;
    public string MoveAnimationState => moveAnimationState;

    protected override void Awake()
    {
        base.Awake();

        idleState = new Enemy_IdleState(this, stateMachine);
        moveState = new Enemy_MoveState(this, stateMachine);
    }

    private void Start()
    {
        if (stateMachine.CurrentState == null && idleState != null)
        {
            stateMachine.Initialize(idleState);
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        idleDuration = Mathf.Max(.1f, idleDuration);
        moveDuration = Mathf.Max(.1f, moveDuration);
        edgeCheckForwardOffset = Mathf.Max(0f, edgeCheckForwardOffset);
        edgeCheckDistance = Mathf.Max(.01f, edgeCheckDistance);
        skeletonMoveSpeed = Mathf.Max(0f, skeletonMoveSpeed);
        skeletonAggroRange = Mathf.Max(0f, skeletonAggroRange);
        skeletonAttackRange = Mathf.Max(0f, skeletonAttackRange);
        skeletonTurnDelay = Mathf.Max(0f, skeletonTurnDelay);
        skeletonContactDamage = Mathf.Max(0, skeletonContactDamage);
        if (string.IsNullOrWhiteSpace(idleAnimationState))
        {
            idleAnimationState = "skeletonIdle";
        }

        if (string.IsNullOrWhiteSpace(moveAnimationState))
        {
            moveAnimationState = "skeletonMove";
        }
    }
}
