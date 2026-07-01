using UnityEngine;
using UnityEngine.Serialization;

public class Enemy_Skeleton : Enemy, ICounterable, IEnemyBattleResponder
{
    [Header("Patrol Info")]
    [SerializeField, Min(.1f)] private float idleDurationMin = 2f;
    [SerializeField, Min(.1f)] private float idleDurationMax = 4f;
    [SerializeField, Min(.1f)] private float moveDurationMin = 4f;
    [SerializeField, Min(.1f)] private float moveDurationMax = 8f;
    [SerializeField, Range(0f, 1f)] private float patrolTurnChance = .5f;

    [Header("Edge Check")]
    [SerializeField] private bool useEdgeCheck = true;
    [SerializeField, Min(0f)] private float edgeCheckForwardOffset = .1f;
    [SerializeField, Min(.01f)] private float edgeCheckDistance = .08f;

    [Header("Vision Info")]
    [SerializeField] private LayerMask whatIsPlayer;
    [SerializeField, Min(.01f)] private float frontSightDistance = 4f;
    [SerializeField, Min(.01f)] private float backSightDistance = 2f;
    [SerializeField, Min(.01f)] private float chaseVerticalDistance = 3f;
    [SerializeField, Min(0f)] private float maxSeeThroughWallDistance = .5f;
    [SerializeField, Min(.01f)] private float wallThicknessSampleDistance = .05f;
    [SerializeField, Min(0f)] private float battleStopDistance = .08f;
    [SerializeField, Min(.1f)] private float loseSightDuration = 3f;
    [SerializeField, Min(.1f)] private float attackCooldown = .75f;
    [SerializeField] private bool showDetectionGizmos = true;

    [Header("Skeleton Info")]
    [SerializeField] private float skeletonMoveSpeed = 2f;
    [SerializeField, Min(0f)] private float battleMoveSpeed = 3f;
    [FormerlySerializedAs("chaseSpeedMultiplier")]
    [SerializeField, HideInInspector] private float battleMoveSpeedMultiplier = 1.5f;
    [SerializeField] private float skeletonAttackRange = 1f;
    [SerializeField] private Vector2 skeletonAttackMoveDistance = Vector2.zero;
    [SerializeField, HideInInspector] private Vector2 skeletonAttackKnockbackForce = new Vector2(4f, 2f);
    [SerializeField] private Entity_AttackData skeletonAttackData = new Entity_AttackData(new Vector2(.7f, 0f), .6f, new Vector2(4f, 2f));
    [SerializeField] private bool enableFallbackFullLayerDetection = false;
    [SerializeField, Min(.01f)] private float skeletonAttackMoveDuration = .12f;
    [SerializeField, Min(0f)] private float skeletonAttackMoveXDelay = 0f;
    [SerializeField, Min(0f)] private float skeletonAttackMoveYDelay = 0f;
    [SerializeField] private float skeletonTurnDelay = .15f;
    [SerializeField] private int skeletonContactDamage = 1;
    [SerializeField] private bool canSkeletonBeKnockedBack = true;
    [SerializeField] private bool canSkeletonBeKnockedBackDuringAttack;

    [Header("Stun Info")]
    [SerializeField] private Vector2 stunnedMoveDistance = new Vector2(0f, .5f);
    [SerializeField] private string stunnedBoolParameter = "stunned";
    [SerializeField] private string stunnedAnimationState = "skeletonStunned";

    [Header("Death Info")]
    [FormerlySerializedAs("deadBounceVelocity")]
    [SerializeField, Min(0f)] private float deadFallSpeed = 2.25f;
    [SerializeField, Min(0f)] private float deadSlideSpeed = .65f;
    [SerializeField, Min(0f)] private float deadSlideAcceleration = 1.2f;
    [SerializeField, Min(0f)] private float deadDropThroughDelay = 0f;
    [SerializeField, Min(0f)] private float deadDisappearDelay = 4f;
    [SerializeField, Range(0f, 180f)] private float deadFallAngle = 90f;

    [Header("Animation State Names")]
    [SerializeField] private string idleAnimationState = "skeletonIdle";
    [SerializeField] private string moveAnimationState = "skeletonMove";
    [SerializeField] private string battleAnimationState = "skeletonBattle - idle/move";

    public Enemy_IdleState idleState { get; private set; }
    public Enemy_MoveState moveState { get; private set; }
    public Enemy_BattleState battleState { get; private set; }
    public Enemy_AttackState attackState { get; private set; }
    public Enemy_StunnedState stunnedState { get; private set; }
    public Enemy_DeadState deadState { get; private set; }

    public float IdleDurationMin => idleDurationMin;
    public float IdleDurationMax => idleDurationMax;
    public float MoveDurationMin => moveDurationMin;
    public float MoveDurationMax => moveDurationMax;
    public float PatrolTurnChance => patrolTurnChance;
    public bool UseEdgeCheck => useEdgeCheck;
    public float EdgeCheckForwardOffset => edgeCheckForwardOffset;
    public float EdgeCheckDistance => edgeCheckDistance;
    public float FrontSightDistance => frontSightDistance;
    public float BackSightDistance => backSightDistance;
    public float ChaseVerticalDistance => chaseVerticalDistance;
    public float MaxSeeThroughWallDistance => maxSeeThroughWallDistance;
    public float WallThicknessSampleDistance => wallThicknessSampleDistance;
    public float BattleStopDistance => battleStopDistance;
    public float LoseSightDuration => loseSightDuration;
    public float AttackCooldown => attackCooldown;
    public LayerMask WhatIsPlayer => whatIsPlayer;
    public float SkeletonMoveSpeed => skeletonMoveSpeed;
    public float BattleMoveSpeed => battleMoveSpeed;
    public float BattleMoveSpeedMultiplier => battleMoveSpeedMultiplier;
    public float SkeletonAttackRange => skeletonAttackRange;
    public Vector2 SkeletonAttackMoveDistance => skeletonAttackMoveDistance;
    public Vector2 SkeletonAttackKnockbackForce => skeletonAttackKnockbackForce;
    public Entity_AttackData SkeletonAttackData => skeletonAttackData;
    public bool EnableFallbackFullLayerDetection => enableFallbackFullLayerDetection;
    public float SkeletonAttackMoveDuration => skeletonAttackMoveDuration;
    public float SkeletonAttackMoveXDelay => skeletonAttackMoveXDelay;
    public float SkeletonAttackMoveYDelay => skeletonAttackMoveYDelay;
    public float SkeletonTurnDelay => skeletonTurnDelay;
    public int SkeletonContactDamage => skeletonContactDamage;
    public Vector2 StunnedMoveDistance => stunnedMoveDistance;
    public string StunnedBoolParameter => stunnedBoolParameter;
    public string StunnedAnimationState => stunnedAnimationState;
    public bool CanSkeletonBeKnockedBack => canSkeletonBeKnockedBack
        && (canSkeletonBeKnockedBackDuringAttack || stateMachine == null || stateMachine.CurrentState != attackState);
    public float DeadFallSpeed => deadFallSpeed;
    public float DeadSlideSpeed => deadSlideSpeed;
    public float DeadSlideAcceleration => deadSlideAcceleration;
    public float DeadDropThroughDelay => deadDropThroughDelay;
    public float DeadDisappearDelay => deadDisappearDelay;
    public float DeadFallAngle => deadFallAngle;
    public string IdleAnimationState => idleAnimationState;
    public string MoveAnimationState => moveAnimationState;
    public string BattleAnimationState => battleAnimationState;

    public bool IsAlerted => isAlerted;
    public bool ShouldReturnToPatrol => shouldReturnToPatrol;
    public bool CanAttack => attackCooldownTimer <= 0f && !IsStunAttackRecoveryActive;
    public bool PlayerVisible => playerVisible;
    public bool PlayerInAttackRange => playerInAttackRange;
    public bool PlayerWithinChaseHeight => playerWithinChaseHeight;
    public int PlayerTargetDirection => playerTargetDirection;
    public Transform PlayerTarget => playerTarget;
    public Bounds PlayerBounds => GetPlayerBounds();
    public bool IsStunned => stateMachine != null && stateMachine.CurrentState == stunnedState;
    public bool IsCounterWindowActive => counterWindowActive;

    private Transform playerTarget;
    private Player playerComponent;
    private static readonly int BattleAnimHash = Animator.StringToHash("battle");
    private static readonly int XVelocityAnimHash = Animator.StringToHash("xVelocity");
    private static readonly int MoveAnimSpeedMultiplierHash = Animator.StringToHash("moveAnimSpeedMultiplier");
    private static readonly int BattleAnimSpeedMultiplierHash = Animator.StringToHash("battleAnimSpeedMultiplier");
    private bool isAlerted;
    private bool shouldReturnToPatrol;
    private bool playerVisible;
    private bool playerInAttackRange;
    private bool playerWithinChaseHeight;
    private bool counterWindowActive;
    private int playerTargetDirection = 1;
    private float playerLostTimer;
    private float attackCooldownTimer;

    protected override void Awake()
    {
        base.Awake();

        RefreshBattleMoveSpeedMultiplier();

        idleState = new Enemy_IdleState(this, stateMachine);
        moveState = new Enemy_MoveState(this, stateMachine);
        battleState = new Enemy_BattleState(this, stateMachine);
        attackState = new Enemy_AttackState(this, stateMachine);
        stunnedState = new Enemy_StunnedState(this, stateMachine);
        deadState = new Enemy_DeadState(this, stateMachine);
    }

    private void Start()
    {
        if (stateMachine.CurrentState == null && idleState != null)
        {
            stateMachine.Initialize(idleState);
        }
    }

    protected override void Update()
    {
        RefreshBattleMoveSpeedMultiplier();

        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        base.Update();
    }

    protected override void SyncAnimationState()
    {
        if (IsDead || stateMachine?.CurrentState == deadState || stateMachine?.CurrentState == stunnedState)
        {
            return;
        }

        CachePlayerTarget();
        UpdatePlayerPerception();

        bool canEnterCombatState =
            stateMachine.CurrentState == idleState
            || stateMachine.CurrentState == moveState
            || stateMachine.CurrentState == battleState;

        if (canEnterCombatState && isAlerted)
        {
            if (playerInAttackRange && CanAttack && attackState != null)
            {
                stateMachine.ChangeState(attackState);
            }
            else if (battleState != null)
            {
                stateMachine.ChangeState(battleState);
            }
        }
    }

    protected override IState GetDeadState()
    {
        return deadState;
    }

    public void CompleteAttackState()
    {
        StartAttackCooldown();

        if (stateMachine == null)
        {
            return;
        }

        if (isAlerted && battleState != null)
        {
            stateMachine.ChangeState(battleState);
        }
        else if (idleState != null)
        {
            stateMachine.ChangeState(idleState);
        }
    }

    public void StartAttackCooldown()
    {
        attackCooldownTimer = Mathf.Max(0f, attackCooldown);
    }

    public void ClearReturnToPatrolRequest()
    {
        shouldReturnToPatrol = false;
    }

    public void TryRandomPatrolTurn()
    {
        if (Random.value <= patrolTurnChance)
        {
            TurnAround();
        }
    }

    public float GetRandomIdleDuration()
    {
        return Random.Range(idleDurationMin, idleDurationMax);
    }

    public float GetRandomMoveDuration()
    {
        return Random.Range(moveDurationMin, moveDurationMax);
    }

    public void StopChasingPlayer()
    {
        isAlerted = false;
        shouldReturnToPatrol = true;
        playerVisible = false;
        playerInAttackRange = false;
        playerLostTimer = 0f;
    }

    public void EnterBattleFromDamage(Transform damageSource)
    {
        if (damageSource != null)
        {
            Player player = damageSource.GetComponentInParent<Player>();
            if (player != null)
            {
                playerTarget = player.transform;
                playerComponent = player;
            }
        }

        if (playerTarget != null)
        {
            playerTargetDirection = GetPlayerDirection();
            FaceDirection(playerTargetDirection);
        }

        isAlerted = true;
        shouldReturnToPatrol = false;
        playerVisible = true;
        playerLostTimer = 0f;

        if (stateMachine != null
            && battleState != null
            && stateMachine.CurrentState != attackState)
        {
            stateMachine.ChangeState(battleState);
        }
    }

    public void SetBattleAnimation(bool battle, float xVelocity)
    {
        if (anim == null)
        {
            return;
        }

        if (HasAnimatorParameter(BattleAnimHash))
        {
            anim.SetBool(BattleAnimHash, battle);
        }

        if (HasAnimatorParameter(XVelocityAnimHash))
        {
            anim.SetFloat(XVelocityAnimHash, xVelocity);
        }

        if (HasAnimatorParameter(BattleAnimSpeedMultiplierHash))
        {
            anim.SetFloat(BattleAnimSpeedMultiplierHash, battle ? battleMoveSpeedMultiplier : 1f);
        }

        if (!battle && HasAnimatorParameter(MoveAnimSpeedMultiplierHash))
        {
            anim.SetFloat(MoveAnimSpeedMultiplierHash, 1f);
        }
    }

    public void SetMoveAnimationSpeed(float speedMultiplier)
    {
        if (anim == null)
        {
            return;
        }

        if (HasAnimatorParameter(MoveAnimSpeedMultiplierHash))
        {
            anim.SetFloat(MoveAnimSpeedMultiplierHash, Mathf.Max(.01f, speedMultiplier));
        }
    }

    public void SetStunnedAnimation(bool stunned)
    {
        if (anim == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(stunnedBoolParameter))
        {
            int stunnedHash = Animator.StringToHash(stunnedBoolParameter);
            if (HasAnimatorParameter(stunnedHash))
            {
                anim.SetBool(stunnedHash, stunned);
            }
        }
    }

    public void EnableCounterWindow()
    {
        counterWindowActive = true;
    }

    public void DisableCounterWindow()
    {
        counterWindowActive = false;
    }

    public bool TryCounter()
    {
        if (!counterWindowActive || IsDead || stunnedState == null || stateMachine == null)
        {
            return false;
        }

        stateMachine.ChangeState(stunnedState);
        return true;
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        idleDurationMin = Mathf.Max(.1f, idleDurationMin);
        idleDurationMax = Mathf.Max(idleDurationMin, idleDurationMax);
        moveDurationMin = Mathf.Max(.1f, moveDurationMin);
        moveDurationMax = Mathf.Max(moveDurationMin, moveDurationMax);
        patrolTurnChance = Mathf.Clamp01(patrolTurnChance);
        edgeCheckForwardOffset = Mathf.Max(0f, edgeCheckForwardOffset);
        edgeCheckDistance = Mathf.Max(.01f, edgeCheckDistance);
        frontSightDistance = Mathf.Max(.01f, frontSightDistance);
        backSightDistance = Mathf.Max(.01f, backSightDistance);
        chaseVerticalDistance = Mathf.Max(.01f, chaseVerticalDistance);
        maxSeeThroughWallDistance = Mathf.Max(0f, maxSeeThroughWallDistance);
        wallThicknessSampleDistance = Mathf.Max(.01f, wallThicknessSampleDistance);
        battleStopDistance = Mathf.Max(0f, battleStopDistance);
        loseSightDuration = Mathf.Max(.1f, loseSightDuration);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        skeletonMoveSpeed = Mathf.Max(0f, skeletonMoveSpeed);
        battleMoveSpeed = Mathf.Max(0f, battleMoveSpeed);
        battleMoveSpeedMultiplier = CalculateBattleMoveSpeedMultiplier();
        skeletonAttackRange = Mathf.Max(0f, skeletonAttackRange);
        skeletonAttackMoveDuration = Mathf.Max(.01f, skeletonAttackMoveDuration);
        skeletonAttackMoveXDelay = Mathf.Max(0f, skeletonAttackMoveXDelay);
        skeletonAttackMoveYDelay = Mathf.Max(0f, skeletonAttackMoveYDelay);
        skeletonTurnDelay = Mathf.Max(0f, skeletonTurnDelay);
        skeletonContactDamage = Mathf.Max(0, skeletonContactDamage);
        stunnedMoveDistance.y = Mathf.Max(0f, stunnedMoveDistance.y);
        deadFallSpeed = Mathf.Max(0f, deadFallSpeed);
        deadSlideSpeed = Mathf.Max(0f, deadSlideSpeed);
        deadSlideAcceleration = Mathf.Max(0f, deadSlideAcceleration);
        deadDropThroughDelay = Mathf.Max(0f, deadDropThroughDelay);
        deadDisappearDelay = Mathf.Max(0f, deadDisappearDelay);
        deadFallAngle = Mathf.Clamp(deadFallAngle, 0f, 180f);

        if (string.IsNullOrWhiteSpace(idleAnimationState))
        {
            idleAnimationState = "skeletonIdle";
        }

        if (string.IsNullOrWhiteSpace(moveAnimationState))
        {
            moveAnimationState = "skeletonMove";
        }

        if (string.IsNullOrWhiteSpace(battleAnimationState))
        {
            battleAnimationState = "skeletonBattle - idle/move";
        }

        if (string.IsNullOrWhiteSpace(stunnedAnimationState))
        {
            stunnedAnimationState = "skeletonStunned";
        }

        if (string.IsNullOrWhiteSpace(stunnedBoolParameter))
        {
            stunnedBoolParameter = "stunned";
        }

        if (whatIsPlayer == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                whatIsPlayer = 1 << playerLayer;
            }
        }
    }

    private float CalculateBattleMoveSpeedMultiplier()
    {
        if (skeletonMoveSpeed <= .01f)
        {
            return 1f;
        }

        return Mathf.Max(.01f, battleMoveSpeed / skeletonMoveSpeed);
    }

    private void RefreshBattleMoveSpeedMultiplier()
    {
        battleMoveSpeedMultiplier = CalculateBattleMoveSpeedMultiplier();
    }

    private void OnDrawGizmos()
    {
        if (!showDetectionGizmos)
        {
            return;
        }

        DrawDetectionGizmos();
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        DrawDetectionGizmos();
    }

    private void CachePlayerTarget()
    {
        if (playerTarget != null)
        {
            return;
        }

        Player[] players = FindObjectsOfType<Player>();
        Player player = null;

        foreach (Player candidate in players)
        {
            if (candidate == null || !IsInPlayerLayer(candidate.gameObject))
            {
                continue;
            }

            player = candidate;
            break;
        }

        if (player == null && whatIsPlayer == 0)
        {
            player = FindObjectOfType<Player>();
        }

        if (player != null)
        {
            playerTarget = player.transform;
            playerComponent = player;
        }
    }

    private void UpdatePlayerPerception()
    {
        if (playerTarget != null && !IsInPlayerLayer(playerTarget.gameObject))
        {
            playerTarget = null;
            playerComponent = null;
            StopChasingPlayer();
        }

        if (playerTarget == null)
        {
            playerVisible = false;
            playerInAttackRange = false;
            playerWithinChaseHeight = false;
            shouldReturnToPatrol = false;
            isAlerted = false;
            playerLostTimer = 0f;
            return;
        }

        playerVisible = TryDetectPlayer(out int targetDirection);
        if (playerComponent == null && playerTarget != null)
        {
            playerComponent = playerTarget.GetComponent<Player>();
        }

        if (playerVisible)
        {
            playerTargetDirection = targetDirection != 0 ? targetDirection : playerTargetDirection;
            FaceDirection(playerTargetDirection);
            playerLostTimer = 0f;
            isAlerted = true;
            shouldReturnToPatrol = false;
        }
        else if (isAlerted)
        {
            playerLostTimer += Time.deltaTime;

            if (playerLostTimer >= loseSightDuration)
            {
                isAlerted = false;
                shouldReturnToPatrol = true;
            }
        }

        Vector2 enemyCenter = GetColliderBounds().center;
        Vector2 playerCenter = GetPlayerCenter();
        playerWithinChaseHeight = IsPlayerWithinChaseHeight(enemyCenter, playerCenter);

        playerInAttackRange = IsPlayerInAttackRange(enemyCenter, playerCenter);
        if (playerInAttackRange)
        {
            playerTargetDirection = GetPlayerDirection();
            FaceDirection(playerTargetDirection);
        }
    }

    private bool TryDetectPlayer(out int targetDirection)
    {
        targetDirection = 0;

        if (playerTarget == null)
        {
            return false;
        }

        if (!IsInPlayerLayer(playerTarget.gameObject))
        {
            return false;
        }

        Vector2 enemyCenter = GetColliderBounds().center;
        Vector2 playerCenter = GetPlayerCenter();
        Vector2 enemyToPlayer = playerCenter - enemyCenter;
        float horizontalDistance = Mathf.Abs(enemyToPlayer.x);
        float verticalDistance = Mathf.Abs(enemyToPlayer.y);

        int playerDirection = GetPlayerDirection();
        bool playerInFront = playerDirection == facingDirection;
        float sightDistance = playerInFront ? frontSightDistance : backSightDistance;

        if (horizontalDistance > sightDistance)
        {
            return false;
        }

        if (verticalDistance > chaseVerticalDistance)
        {
            return false;
        }

        if (WallBlocksHorizontalSight(playerCenter))
        {
            return false;
        }

        targetDirection = playerDirection;
        return true;
    }

    private bool WallBlocksHorizontalSight(Vector2 target)
    {
        Bounds enemyBounds = GetColliderBounds();
        Bounds playerBounds = playerComponent != null && playerComponent.cd != null
            ? playerComponent.cd.bounds
            : new Bounds(target, Vector3.one);

        int direction = target.x >= enemyBounds.center.x ? 1 : -1;
        float startX = direction > 0 ? enemyBounds.max.x + .02f : enemyBounds.min.x - .02f;
        float targetX = direction > 0 ? playerBounds.min.x : playerBounds.max.x;
        float distance = Mathf.Abs(targetX - startX);

        if (distance <= .01f)
        {
            return false;
        }

        Vector2 rayDirection = Vector2.right * direction;
        float upperY = enemyBounds.max.y - Mathf.Min(.12f, enemyBounds.extents.y * .25f);
        Vector2 middleOrigin = new Vector2(startX, enemyBounds.center.y);
        Vector2 upperOrigin = new Vector2(startX, upperY);

        return GroundSegmentExceedsSeeThroughLimit(middleOrigin, rayDirection, distance)
            || GroundSegmentExceedsSeeThroughLimit(upperOrigin, rayDirection, distance);
    }

    private bool GroundSegmentExceedsSeeThroughLimit(Vector2 origin, Vector2 direction, float distance)
    {
        if (maxSeeThroughWallDistance <= 0f)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, whatIsGround);
            return hit.collider != null && !IsSelfCollider(hit.collider);
        }

        float currentGroundDistance = 0f;
        float sampleStep = Mathf.Max(.01f, wallThicknessSampleDistance);
        int segmentCount = Mathf.CeilToInt(distance / sampleStep);

        for (int i = 0; i < segmentCount; i++)
        {
            float segmentStart = i * sampleStep;
            float segmentEnd = Mathf.Min(distance, segmentStart + sampleStep);
            float segmentLength = segmentEnd - segmentStart;
            float sampleDistance = segmentStart + segmentLength * .5f;
            Vector2 samplePoint = origin + direction.normalized * sampleDistance;

            if (GroundOccupiesSightSample(samplePoint))
            {
                currentGroundDistance += segmentLength;

                if (currentGroundDistance > maxSeeThroughWallDistance)
                {
                    return true;
                }
            }
            else
            {
                currentGroundDistance = 0f;
            }
        }

        return false;
    }

    private bool GroundOccupiesSightSample(Vector2 samplePoint)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(samplePoint, whatIsGround);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && !IsSelfCollider(hits[i]))
            {
                return true;
            }
        }

        return false;
    }

    private int GetPlayerDirection()
    {
        if (playerTarget == null)
        {
            return facingDirection;
        }

        float xDelta = playerTarget.position.x - transform.position.x;
        if (Mathf.Abs(xDelta) <= .01f)
        {
            return facingDirection;
        }

        return xDelta > 0f ? 1 : -1;
    }

    private Vector2 GetPlayerCenter()
    {
        if (playerTarget == null)
        {
            return transform.position;
        }

        if (playerComponent == null && playerTarget != null)
        {
            playerComponent = playerTarget.GetComponent<Player>();
        }

        return playerComponent != null && playerComponent.cd != null
            ? (Vector2)playerComponent.cd.bounds.center
            : (Vector2)playerTarget.position;
    }

    private Bounds GetPlayerBounds()
    {
        if (playerTarget == null)
        {
            return new Bounds(transform.position, Vector3.one);
        }

        if (playerComponent == null && playerTarget != null)
        {
            playerComponent = playerTarget.GetComponent<Player>();
        }

        return playerComponent != null && playerComponent.cd != null
            ? playerComponent.cd.bounds
            : new Bounds(GetPlayerCenter(), Vector3.one);
    }

    private bool IsPlayerWithinChaseHeight(Vector2 enemyCenter, Vector2 playerCenter)
    {
        return Mathf.Abs(playerCenter.y - enemyCenter.y) <= chaseVerticalDistance;
    }

    private bool IsPlayerInAttackRange(Vector2 enemyCenter, Vector2 playerCenter)
    {
        Bounds enemyBounds = GetColliderBounds();
        Bounds playerBounds = playerComponent != null && playerComponent.cd != null
            ? playerComponent.cd.bounds
            : new Bounds(playerCenter, Vector3.one);

        float horizontalDistance = GetHorizontalGap(enemyBounds, playerBounds);
        float verticalDistance = Mathf.Abs(playerCenter.y - enemyCenter.y);
        float maxVerticalDistance = enemyBounds.extents.y + playerBounds.extents.y;

        return horizontalDistance <= skeletonAttackRange
            && verticalDistance <= maxVerticalDistance;
    }

    private float GetHorizontalGap(Bounds enemyBounds, Bounds playerBounds)
    {
        if (playerBounds.center.x >= enemyBounds.center.x)
        {
            return Mathf.Max(0f, playerBounds.min.x - enemyBounds.max.x);
        }

        return Mathf.Max(0f, enemyBounds.min.x - playerBounds.max.x);
    }

    private new bool IsSelfCollider(Collider2D collider)
    {
        if (collider == null)
        {
            return false;
        }

        return collider.transform == transform
            || collider.transform.IsChildOf(transform);
    }

    private bool IsInPlayerLayer(GameObject target)
    {
        if (target == null || whatIsPlayer == 0)
        {
            return true;
        }

        return (whatIsPlayer.value & (1 << target.layer)) != 0;
    }

    private bool HasAnimatorParameter(int parameterHash)
    {
        if (anim == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in anim.parameters)
        {
            if (parameter.nameHash == parameterHash)
            {
                return true;
            }
        }

        return false;
    }

    private void DrawDetectionGizmos()
    {
        Bounds bounds = GetColliderBounds();
        Vector2 center = bounds.center;
        Vector2 frontOrigin = GetFacingEdgePoint(facingDirection);
        Vector2 backOrigin = GetFacingEdgePoint(-facingDirection);
        Vector2 attackEnd = frontOrigin + Vector2.right * facingDirection * skeletonAttackRange;
        Vector2 frontSightEnd = frontOrigin + Vector2.right * facingDirection * frontSightDistance;
        Vector2 backSightEnd = backOrigin + Vector2.right * -facingDirection * backSightDistance;

        Gizmos.color = playerVisible ? Color.green : Color.red;
        Gizmos.DrawLine(frontOrigin, frontSightEnd);
        DrawVerticalChaseLimit(center);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(backOrigin, backSightEnd);

        Gizmos.color = playerInAttackRange ? Color.red : new Color(1f, .55f, 0f, 1f);
        Gizmos.DrawLine(frontOrigin, attackEnd);
        Gizmos.DrawWireSphere(attackEnd, .06f);

        if (playerTarget != null)
        {
            Gizmos.color = WallBlocksHorizontalSight(GetPlayerCenter()) ? Color.gray : Color.green;
            Gizmos.DrawLine(center, GetPlayerCenter());
            DrawSightBlockGizmos();
        }
    }

    private void DrawSightBlockGizmos()
    {
        Bounds enemyBounds = GetColliderBounds();
        Vector2 playerCenter = GetPlayerCenter();
        int direction = playerCenter.x >= enemyBounds.center.x ? 1 : -1;
        float startX = direction > 0 ? enemyBounds.max.x + .02f : enemyBounds.min.x - .02f;
        float distance = Mathf.Abs(playerCenter.x - startX);
        float upperY = enemyBounds.max.y - Mathf.Min(.12f, enemyBounds.extents.y * .25f);

        Vector3 middleOrigin = new Vector3(startX, enemyBounds.center.y, 0f);
        Vector3 upperOrigin = new Vector3(startX, upperY, 0f);
        Vector3 rayEndOffset = Vector3.right * direction * distance;

        Gizmos.color = WallBlocksHorizontalSight(playerCenter) ? Color.gray : Color.yellow;
        Gizmos.DrawLine(middleOrigin, middleOrigin + rayEndOffset);
        Gizmos.DrawLine(upperOrigin, upperOrigin + rayEndOffset);
    }

    private void DrawVerticalChaseLimit(Vector2 center)
    {
        Vector2 left = center + Vector2.left * backSightDistance;
        Vector2 right = center + Vector2.right * frontSightDistance;

        Gizmos.color = new Color(1f, .85f, 0f, .75f);
        Gizmos.DrawLine(left + Vector2.up * chaseVerticalDistance, right + Vector2.up * chaseVerticalDistance);
        Gizmos.DrawLine(left + Vector2.down * chaseVerticalDistance, right + Vector2.down * chaseVerticalDistance);
        Gizmos.DrawLine(left + Vector2.up * chaseVerticalDistance, left + Vector2.down * chaseVerticalDistance);
        Gizmos.DrawLine(right + Vector2.up * chaseVerticalDistance, right + Vector2.down * chaseVerticalDistance);
    }

    private Vector2 GetFacingEdgePoint(int direction)
    {
        Bounds bounds = GetColliderBounds();
        float x = direction > 0 ? bounds.max.x : bounds.min.x;
        return new Vector2(x, bounds.center.y);
    }
}
