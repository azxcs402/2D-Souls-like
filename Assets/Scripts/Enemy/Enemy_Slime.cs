using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class Enemy_Slime : Enemy, ICounterable, IEnemyBattleResponder
{
    private static readonly float[] SplitSpawnHorizontalScales = { 1f, .8f, .6f, .4f, .2f, 0f };
    private static readonly float[] SplitSpawnVerticalOffsets = { 0f, .15f, .3f, .45f, .6f, .8f, 1f };

    private static readonly int BattleAnimHash = Animator.StringToHash("battle");
    private static readonly int XVelocityAnimHash = Animator.StringToHash("xVelocity");
    private static readonly int MoveAnimSpeedMultiplierHash = Animator.StringToHash("moveAnimSpeedMultiplier");
    private static readonly int BattleAnimSpeedMultiplierHash = Animator.StringToHash("battleAnimSpeedMultiplier");
    private static readonly int AttackSpeedMultiplierHash = Animator.StringToHash("attackSpeedMultiplier");
    private static readonly int StunnedBoolHash = Animator.StringToHash("stunned");
    private static readonly int HasStunRecoveryHash = Animator.StringToHash("hasStunRecovery");

    [Header("Quest Info")]
    [SerializeField] private string questTargetId = "enemy_slime";

    [Header("Battle Details")]
    [SerializeField, Min(.1f)] private float battleMoveSpeed = 3f;
    [SerializeField, Min(.1f)] private float attackDistance = 1.5f;
    [SerializeField, Min(0f)] private float attackCooldown = .5f;
    [SerializeField] private bool canChasePlayer = true;
    [SerializeField, Min(.1f)] private float battleTimeDuration = 5f;
    [SerializeField, Min(0f)] private float minRetreatDistance = 1f;
    [SerializeField, Min(0f)] private float battleStopDistance = .08f;
    [SerializeField] private Vector2 retreatVelocity = new Vector2(4f, 2f);

    [Header("Stunned State Details")]
    [SerializeField, Min(.1f)] private float stunnedDuration = 1f;
    [SerializeField] private Vector2 stunnedVelocity = new Vector2(7f, 2f);
    [SerializeField] private bool canBeStunned = true;

    [Header("Patrol Info")]
    [SerializeField, Min(.1f)] private float idleDurationMin = 3f;
    [SerializeField, Min(.1f)] private float idleDurationMax = 5f;
    [SerializeField, Min(.1f)] private float moveDurationMin = 5f;
    [SerializeField, Min(.1f)] private float moveDurationMax = 9f;
    [SerializeField, Range(0f, 1f)] private float patrolTurnChance = .5f;
    [SerializeField, Min(0f)] private float patrolTurnDelay = .15f;

    [Header("Movement Details")]
    [SerializeField, Min(.01f)] private float moveSpeed = 1.4f;
    [SerializeField, Range(0f, 2f)] private float moveAnimSpeedMultiplier = 1f;

    [Header("Vision Info")]
    [SerializeField] private LayerMask whatIsPlayer;
    [SerializeField, Min(.01f)] private float frontSightDistance = 4f;
    [SerializeField, Min(.01f)] private float backSightDistance = 2f;
    [SerializeField, Min(0f)] private float chaseVerticalDistance = 3f;
    [SerializeField, Min(0f)] private float maxSeeThroughWallDistance = .5f;
    [SerializeField, Min(.01f)] private float wallThicknessSampleDistance = .05f;
    [SerializeField, Min(0f)] private float loseSightDuration = 3f;
    [SerializeField] private bool showDetectionGizmos = true;
    [SerializeField] private bool showAttackGizmos = true;

    [Header("Hearing Info")]
    [SerializeField, Min(0f)] private float hearingDistance = 2f;

    [Header("Legacy Target Anchor")]
    [SerializeField] private Transform playerCheck;
    [SerializeField, Min(.01f)] private float playerCheckDistance = 10f;

    [Header("Slime Info")]
    [SerializeField] private GameObject slimeToCreatePrefab;
    [SerializeField, Min(0)] private int amountOfSlimesToCreate = 2;
    [SerializeField] private Vector2 newSlimeVelocity = new Vector2(4f, 3f);
    [SerializeField] private bool hasRecoveryAnimation = true;
    [SerializeField] private bool canBeKnockedBack = true;
    [SerializeField] private bool slimeSpriteFacesLeftByDefault = true;

    [Header("Split Info")]
    [SerializeField] private bool splitOnDeath = true;
    [SerializeField, Min(0)] private int splitGeneration = 0;
    [SerializeField, Range(1, 5)] private int maxSplitGenerations = 2;
    [SerializeField, Min(.1f)] private float splitChildScaleMultiplier = .7f;
    [SerializeField, Min(.01f)] private float splitChildHealthMultiplier = .7f;
    [SerializeField, Min(.01f)] private float splitChildDamageMultiplier = .7f;
    [SerializeField, Min(0f)] private float splitSpawnHorizontalOffset = .55f;
    [SerializeField, Min(0f)] private float splitSpawnVerticalOffset = .2f;
    [SerializeField, Min(0f), Tooltip("New split child slimes cannot attack for this many seconds after spawning.")]
    private float splitChildAttackLockDuration = 0.8f;

    [Header("Stunned Collider")]
    [SerializeField] private CapsuleCollider2D stunnedCollider;

    [Header("Attack Info")]
    [SerializeField] private Entity_AttackData slimeAttackData = new Entity_AttackData(new Vector2(.7f, 0f), .6f, new Vector2(4f, 2f));
    [SerializeField] private string attackAnimationState = "slimeAttack";
    [SerializeField] private string idleAnimationState = "slimeIdle";
    [SerializeField] private string moveAnimationState = "slimeMove";
    [SerializeField] private string battleAnimationState = "slimeBattle - idle/move";
    [SerializeField] private string stunnedAnimationState = "slimeStunned";
    [SerializeField] private string stunRecoveryAnimationState = "slimeStunRecovery";

    [Header("Death Info")]
    [SerializeField, Min(0f)] private float deadFallSpeed = 2.25f;
    [SerializeField, Min(0f)] private float deadSlideSpeed = .65f;
    [SerializeField, Min(0f)] private float deadSlideAcceleration = 1.2f;
    [SerializeField, Min(0f)] private float deadDropThroughDelay = 0f;
    [SerializeField, Min(0f)] private float deadDisappearDelay = 4f;
    [SerializeField, Range(0f, 180f)] private float deadFallAngle = 90f;

    public Enemy_SlimeIdleState idleState { get; private set; }
    public Enemy_SlimeMoveState moveState { get; private set; }
    public Enemy_SlimeBattleState battleState { get; private set; }
    public Enemy_SlimeAttackState attackState { get; private set; }
    public Enemy_SlimeStunnedState stunnedState { get; private set; }
    public Enemy_SlimeDeadState deadState { get; private set; }

    public string QuestTargetId => questTargetId;
    public float BattleMoveSpeed => battleMoveSpeed;
    public float AttackDistance => attackDistance;
    public float AttackCooldown => attackCooldown;
    public bool CanChasePlayer => canChasePlayer;
    public float BattleTimeDuration => battleTimeDuration;
    public float MinRetreatDistance => minRetreatDistance;
    public float BattleStopDistance => battleStopDistance;
    public Vector2 RetreatVelocity => retreatVelocity;
    public float StunnedDuration => stunnedDuration;
    public Vector2 StunnedVelocity => stunnedVelocity;
    public bool CanBeStunned => canBeStunned;
    public float IdleDurationMin => idleDurationMin;
    public float IdleDurationMax => idleDurationMax;
    public float MoveDurationMin => moveDurationMin;
    public float MoveDurationMax => moveDurationMax;
    public float PatrolTurnChance => patrolTurnChance;
    public float PatrolTurnDelay => patrolTurnDelay;
    public float MoveSpeed => moveSpeed;
    public float MoveAnimSpeedMultiplier => moveAnimSpeedMultiplier;
    public LayerMask WhatIsPlayer => whatIsPlayer;
    public float FrontSightDistance => frontSightDistance;
    public float BackSightDistance => backSightDistance;
    public float ChaseVerticalDistance => chaseVerticalDistance;
    public float MaxSeeThroughWallDistance => maxSeeThroughWallDistance;
    public float WallThicknessSampleDistance => wallThicknessSampleDistance;
    public float LoseSightDuration => loseSightDuration;
    public bool ShowDetectionGizmos => showDetectionGizmos;
    public Transform PlayerCheck => playerCheck;
    public float PlayerCheckDistance => playerCheckDistance;
    public GameObject SlimeToCreatePrefab => slimeToCreatePrefab;
    public int AmountOfSlimesToCreate => amountOfSlimesToCreate;
    public Vector2 NewSlimeVelocity => newSlimeVelocity;
    public bool HasRecoveryAnimation => hasRecoveryAnimation;
    public Entity_AttackData SlimeAttackData => slimeAttackData;
    public string AttackAnimationState => attackAnimationState;
    public string IdleAnimationState => idleAnimationState;
    public string MoveAnimationState => moveAnimationState;
    public string BattleAnimationState => battleAnimationState;
    public string StunnedAnimationState => stunnedAnimationState;
    public string StunRecoveryAnimationState => stunRecoveryAnimationState;
    public float DeadFallSpeed => deadFallSpeed;
    public float DeadSlideSpeed => deadSlideSpeed;
    public float DeadSlideAcceleration => deadSlideAcceleration;
    public float DeadDropThroughDelay => deadDropThroughDelay;
    public float DeadDisappearDelay => deadDisappearDelay;
    public float DeadFallAngle => deadFallAngle;
    public bool SplitOnDeath => splitOnDeath;
    public int SplitGeneration => splitGeneration;
    public int MaxSplitGenerations => maxSplitGenerations;
    public bool CanSplitOnDeath => splitOnDeath && splitGeneration + 1 < maxSplitGenerations;
    public float SplitChildAttackLockDuration => splitChildAttackLockDuration;

    public bool IsAlerted => isAlerted;
    public bool ShouldReturnToPatrol => shouldReturnToPatrol;
    public bool CanAttack => attackCooldownTimer <= 0f && spawnAttackLockTimer <= 0f && !IsStunAttackRecoveryActive;
    public bool IsCounterWindowActive => counterWindowActive;
    public int PlayerTargetDirection => playerTargetDirection;
    public Transform PlayerTarget => playerTarget;
    public bool PlayerVisible => playerVisible;
    public bool PlayerInAttackRange => playerInAttackRange;
    public bool PlayerWithinChaseHeight => playerWithinChaseHeight;
    public bool IsStunned => stateMachine != null && stateMachine.CurrentState == stunnedState;
    public bool CanSlimeBeKnockedBack => canBeKnockedBack;

    private Transform playerTarget;
    private bool isAlerted;
    private bool shouldReturnToPatrol;
    private bool playerVisible;
    private bool playerInAttackRange;
    private bool playerWithinChaseHeight;
    private bool counterWindowActive;
    private int playerTargetDirection = 1;
    private float lastTimeSeenPlayer;
    private float attackCooldownTimer;
    private float spawnAttackLockTimer;
    private float battleAnimSpeedMultiplier = 1f;
    private float defaultAnimatorSpeed = 1f;
    private CapsuleCollider2D aliveCollider;
    private Vector2 aliveColliderSize;
    private Vector2 aliveColliderOffset;
    private CapsuleDirection2D aliveColliderDirection;

    protected override void Awake()
    {
        base.Awake();

        CacheColliderReferences();
        NormalizeAnimationStateNames();
        defaultAnimatorSpeed = anim != null ? anim.speed : 1f;
        battleAnimSpeedMultiplier = CalculateBattleAnimSpeedMultiplier();
        spawnAttackLockTimer = 0f;

        idleState = new Enemy_SlimeIdleState(this, stateMachine);
        moveState = new Enemy_SlimeMoveState(this, stateMachine);
        battleState = new Enemy_SlimeBattleState(this, stateMachine);
        attackState = new Enemy_SlimeAttackState(this, stateMachine);
        stunnedState = new Enemy_SlimeStunnedState(this, stateMachine);
        deadState = new Enemy_SlimeDeadState(this, stateMachine);

        if (anim != null)
        {
            anim.SetBool(HasStunRecoveryHash, hasRecoveryAnimation);
            anim.SetFloat(AttackSpeedMultiplierHash, 1f);
            ApplySlimeVisualFacingCorrection();
        }

        RestoreAliveColliderProfile();
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
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (spawnAttackLockTimer > 0f)
        {
            spawnAttackLockTimer -= Time.deltaTime;
        }

        battleAnimSpeedMultiplier = CalculateBattleAnimSpeedMultiplier();
        base.Update();
    }

    protected override void SyncAnimationState()
    {
        if (IsDead || stateMachine?.CurrentState == deadState || stateMachine?.CurrentState == stunnedState)
        {
            return;
        }

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
        playerWithinChaseHeight = false;
        lastTimeSeenPlayer = 0f;
        playerTarget = null;
    }

    public void EnterBattleFromDamage(Transform damageSource)
    {
        if (damageSource != null)
        {
            Player player = damageSource.GetComponentInParent<Player>();
            if (player != null)
            {
                playerTarget = player.transform;
            }
            else
            {
                playerTarget = damageSource;
            }
        }

        if (playerTarget == null)
        {
            playerTarget = GetPlayerReference();
        }

        if (playerTarget != null)
        {
            playerTargetDirection = GetPlayerDirection();
            FaceDirection(playerTargetDirection);
        }

        isAlerted = true;
        shouldReturnToPatrol = false;
        playerVisible = true;
        lastTimeSeenPlayer = Time.time;

        if (stateMachine != null
            && battleState != null
            && stateMachine.CurrentState != attackState
            && stateMachine.CurrentState != stunnedState
            && stateMachine.CurrentState != deadState)
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

        anim.SetBool(BattleAnimHash, battle);
        anim.SetFloat(XVelocityAnimHash, xVelocity);
        anim.SetFloat(BattleAnimSpeedMultiplierHash, battle ? battleAnimSpeedMultiplier : 1f);

        if (!battle)
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

        anim.SetFloat(MoveAnimSpeedMultiplierHash, Mathf.Max(.01f, speedMultiplier));
    }

    public void SetAttackAnimationSpeed(float speedMultiplier)
    {
        if (anim == null)
        {
            return;
        }

        anim.SetFloat(AttackSpeedMultiplierHash, Mathf.Max(.01f, speedMultiplier));
    }

    public void SetStunnedAnimation(bool stunned)
    {
        if (anim == null)
        {
            return;
        }

        anim.SetBool(StunnedBoolHash, stunned);
    }

    public void EnableCounterWindow()
    {
        counterWindowActive = true;
    }

    public void DisableCounterWindow()
    {
        if (stateMachine != null && stateMachine.CurrentState == attackState)
        {
            return;
        }

        ForceDisableCounterWindow();
    }

    public void ForceDisableCounterWindow()
    {
        counterWindowActive = false;
    }

    public void ApplyStunnedColliderProfile()
    {
        CacheColliderReferences();

        if (aliveCollider == null)
        {
            return;
        }

        if (stunnedCollider != null)
        {
            aliveCollider.enabled = false;
            stunnedCollider.enabled = true;
            cd = stunnedCollider;
            return;
        }

        aliveCollider.enabled = true;
        aliveCollider.direction = CapsuleDirection2D.Horizontal;
        aliveCollider.size = new Vector2(aliveColliderSize.y, aliveColliderSize.x);
        aliveCollider.offset = aliveColliderOffset;
        cd = aliveCollider;
    }

    public void RestoreAliveColliderProfile()
    {
        CacheColliderReferences();

        if (aliveCollider == null)
        {
            return;
        }

        if (stunnedCollider != null)
        {
            stunnedCollider.enabled = false;
        }

        aliveCollider.enabled = true;
        aliveCollider.direction = aliveColliderDirection;
        aliveCollider.size = aliveColliderSize;
        aliveCollider.offset = aliveColliderOffset;
        cd = aliveCollider;
    }

    public Bounds PlayerBounds => GetPlayerBounds();

    public bool TryCounter()
    {
        if (!counterWindowActive || !CanBeStunned || stunnedState == null || stateMachine == null || IsDead)
        {
            return false;
        }

        stateMachine.ChangeState(stunnedState);
        return true;
    }

    public Transform GetPlayerReference()
    {
        if (playerTarget != null)
        {
            return playerTarget;
        }

        Player player = FindAnyPlayerReference();
        return player != null ? player.transform : null;
    }

    public void ApplySpawnVelocity(int horizontalDirectionSign = 0)
    {
        if (rb == null)
        {
            return;
        }

        float xDirection = horizontalDirectionSign == 0
            ? Random.Range(-1f, 1f)
            : Mathf.Sign(horizontalDirectionSign);

        if (Mathf.Approximately(xDirection, 0f))
        {
            xDirection = horizontalDirectionSign < 0 ? -1f : 1f;
        }

        Vector2 velocity = new Vector2(
            newSlimeVelocity.x * xDirection * Random.Range(.85f, 1.1f),
            newSlimeVelocity.y * Random.Range(1f, 2f)
        );
        rb.velocity = velocity;
    }

    public void CreateSlimeOnDeath()
    {
        if (!CanSplitOnDeath || amountOfSlimesToCreate <= 0)
        {
            return;
        }

        GameObject prefabToSpawn = slimeToCreatePrefab != null ? slimeToCreatePrefab : gameObject;
        int childGeneration = splitGeneration + 1;
        bool childCanSplitFurther = childGeneration + 1 < maxSplitGenerations;
        float splitSeparation = GetSplitSpawnSeparation();

        for (int i = 0; i < amountOfSlimesToCreate; i++)
        {
            int horizontalDirection = i % 2 == 0 ? -1 : 1;
            Vector3 spawnPosition = GetValidSplitSpawnPosition(horizontalDirection, splitSeparation);
            GameObject newSlime = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            Enemy_Slime slime = newSlime.GetComponent<Enemy_Slime>();

            if (slime != null)
            {
                slime.ConfigureSplitChild(childGeneration, childCanSplitFurther);
                slime.ApplySpawnVelocity(horizontalDirection);
                slime.FaceDirection(horizontalDirection);

                if (playerTarget != null)
                {
                    slime.EnterBattleFromDamage(playerTarget);
                }
            }
        }
    }

    public float GetAttackDirectionSign()
    {
        return playerTargetDirection >= 0 ? 1f : -1f;
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        CacheColliderReferences();
        NormalizeAnimationStateNames();
        battleMoveSpeed = Mathf.Max(0f, battleMoveSpeed);
        attackDistance = Mathf.Max(0f, attackDistance);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        battleTimeDuration = Mathf.Max(.1f, battleTimeDuration);
        minRetreatDistance = Mathf.Max(0f, minRetreatDistance);
        battleStopDistance = Mathf.Max(0f, battleStopDistance);
        stunnedDuration = Mathf.Max(.1f, stunnedDuration);
        idleDurationMin = Mathf.Max(.1f, idleDurationMin);
        idleDurationMax = Mathf.Max(idleDurationMin, idleDurationMax);
        moveDurationMin = Mathf.Max(.1f, moveDurationMin);
        moveDurationMax = Mathf.Max(moveDurationMin, moveDurationMax);
        patrolTurnChance = Mathf.Clamp01(patrolTurnChance);
        patrolTurnDelay = Mathf.Max(0f, patrolTurnDelay);
        moveSpeed = Mathf.Max(.01f, moveSpeed);
        moveAnimSpeedMultiplier = Mathf.Clamp(moveAnimSpeedMultiplier, 0f, 2f);
        frontSightDistance = Mathf.Max(.01f, frontSightDistance);
        backSightDistance = Mathf.Max(.01f, backSightDistance);
        chaseVerticalDistance = Mathf.Max(0f, chaseVerticalDistance);
        maxSeeThroughWallDistance = Mathf.Max(0f, maxSeeThroughWallDistance);
        wallThicknessSampleDistance = Mathf.Max(.01f, wallThicknessSampleDistance);
        loseSightDuration = Mathf.Max(0f, loseSightDuration);
        hearingDistance = Mathf.Max(0f, hearingDistance);
        amountOfSlimesToCreate = Mathf.Max(0, amountOfSlimesToCreate);
        splitGeneration = Mathf.Max(0, splitGeneration);
        maxSplitGenerations = Mathf.Clamp(maxSplitGenerations, 1, 5);
        splitChildScaleMultiplier = Mathf.Max(.1f, splitChildScaleMultiplier);
        splitChildHealthMultiplier = Mathf.Max(.01f, splitChildHealthMultiplier);
        splitChildDamageMultiplier = Mathf.Max(.01f, splitChildDamageMultiplier);
        splitSpawnHorizontalOffset = Mathf.Max(0f, splitSpawnHorizontalOffset);
        splitSpawnVerticalOffset = Mathf.Max(0f, splitSpawnVerticalOffset);
        splitChildAttackLockDuration = Mathf.Max(0f, splitChildAttackLockDuration);
        deadFallSpeed = Mathf.Max(0f, deadFallSpeed);
        deadSlideSpeed = Mathf.Max(0f, deadSlideSpeed);
        deadSlideAcceleration = Mathf.Max(0f, deadSlideAcceleration);
        deadDropThroughDelay = Mathf.Max(0f, deadDropThroughDelay);
        deadDisappearDelay = Mathf.Max(0f, deadDisappearDelay);
        deadFallAngle = Mathf.Clamp(deadFallAngle, 0f, 180f);
        battleAnimSpeedMultiplier = CalculateBattleAnimSpeedMultiplier();

        if (whatIsPlayer.value == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                whatIsPlayer = 1 << playerLayer;
            }
        }
    }

    private void UpdatePlayerPerception()
    {
        Transform detectedPlayer = TryDetectPlayer(out int targetDirection);
        Transform perceivedPlayer = detectedPlayer;

        if (detectedPlayer != null)
        {
            playerTarget = detectedPlayer;
            playerTargetDirection = targetDirection != 0 ? targetDirection : GetPlayerDirection();
            playerVisible = true;
            lastTimeSeenPlayer = Time.time;
            isAlerted = true;
            shouldReturnToPatrol = false;
        }
        else
        {
            playerVisible = false;
        }

        if (perceivedPlayer == null)
        {
            Player fallbackPlayer = FindAnyPlayerReference();
            perceivedPlayer = fallbackPlayer != null ? fallbackPlayer.transform : null;
        }

        if (perceivedPlayer == null)
        {
            playerTarget = null;
            playerInAttackRange = false;
            playerWithinChaseHeight = false;
            return;
        }

        playerTarget = perceivedPlayer;

        Bounds enemyBounds = GetColliderBounds();
        Bounds playerBounds = GetPlayerBounds();
        float horizontalGap = GetHorizontalGap(enemyBounds, playerBounds);
        float verticalDistance = Mathf.Abs(playerBounds.center.y - enemyBounds.center.y);
        playerWithinChaseHeight = verticalDistance <= chaseVerticalDistance;
        playerInAttackRange = playerWithinChaseHeight && horizontalGap <= attackDistance;

        bool playerWithinHearingRange = playerWithinChaseHeight && horizontalGap <= hearingDistance;

        if (playerWithinHearingRange)
        {
            isAlerted = true;
            shouldReturnToPatrol = false;
            playerVisible = true;
            lastTimeSeenPlayer = Time.time;
            playerTargetDirection = GetPlayerDirection(playerBounds.center);

            if (stateMachine != null
                && battleState != null
                && stateMachine.CurrentState != attackState
                && stateMachine.CurrentState != stunnedState
                && stateMachine.CurrentState != deadState
                && stateMachine.CurrentState != battleState)
            {
                stateMachine.ChangeState(battleState);
            }
        }

        if (isAlerted && !playerVisible && lastTimeSeenPlayer > 0f && Time.time - lastTimeSeenPlayer >= loseSightDuration)
        {
            StopChasingPlayer();
        }
    }

    private Transform TryDetectPlayer(out int targetDirection)
    {
        targetDirection = 0;

        if (whatIsPlayer.value == 0)
        {
            return null;
        }

        Player player = playerTarget != null
            ? playerTarget.GetComponentInParent<Player>()
            : FindAnyPlayerReference();

        if (player == null)
        {
            return null;
        }

        Bounds enemyBounds = GetColliderBounds();
        Bounds playerBounds = GetPlayerBounds(player);
        Vector2 enemyCenter = enemyBounds.center;
        Vector2 playerCenter = playerBounds.center;
        Vector2 enemyToPlayer = playerCenter - enemyCenter;
        float horizontalDistance = Mathf.Abs(enemyToPlayer.x);
        float verticalDistance = Mathf.Abs(enemyToPlayer.y);
        int playerDirection = GetPlayerDirection(player.transform.position);
        bool playerInFront = playerDirection == facingDirection;
        float sightDistance = playerInFront
            ? frontSightDistance
            : Mathf.Max(frontSightDistance, backSightDistance);

        if (horizontalDistance > sightDistance)
        {
            return null;
        }

        if (verticalDistance > chaseVerticalDistance)
        {
            return null;
        }

        if (WallBlocksHorizontalSight(playerBounds))
        {
            return null;
        }

        targetDirection = playerDirection;
        return player.transform;
    }

    private Player FindAnyPlayerReference()
    {
        Player[] players = FindObjectsOfType<Player>(true);
        for (int i = 0; i < players.Length; i++)
        {
            Player candidate = players[i];
            if (candidate == null)
            {
                continue;
            }

            if (IsInPlayerLayer(candidate.gameObject))
            {
                return candidate;
            }
        }

        return players.Length > 0 ? players[0] : null;
    }

    private bool IsInPlayerLayer(GameObject candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        if (whatIsPlayer.value == 0)
        {
            return true;
        }

        int layerMask = 1 << candidate.layer;
        return (whatIsPlayer.value & layerMask) != 0;
    }

    private bool WallBlocksHorizontalSight(Bounds playerBounds)
    {
        Bounds enemyBounds = GetColliderBounds();
        int direction = playerBounds.center.x >= enemyBounds.center.x ? 1 : -1;
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
            return hit.collider != null && !hit.collider.transform.IsChildOf(transform);
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
            Collider2D hit = hits[i];
            if (hit != null && !hit.transform.IsChildOf(transform))
            {
                return true;
            }
        }

        return false;
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

        if (!showDetectionGizmos)
        {
            return;
        }

        DrawDetectionGizmos();
    }

    private void DrawDetectionGizmos()
    {
        Bounds enemyBounds = GetColliderBounds();
        float totalWidth = Mathf.Max(.1f, frontSightDistance + backSightDistance);
        float totalHeight = Mathf.Max(.1f, chaseVerticalDistance * 2f);
        float centerOffset = (frontSightDistance - backSightDistance) * .5f * facingDirection;
        Vector3 center = enemyBounds.center + new Vector3(centerOffset, 0f, 0f);
        Vector3 size = new Vector3(totalWidth, totalHeight, 0f);

        Gizmos.color = new Color(1f, .85f, .1f, .12f);
        Gizmos.DrawCube(center, size);

        Gizmos.color = new Color(1f, .85f, .1f, 1f);
        Gizmos.DrawWireCube(center, size);

        if (showAttackGizmos)
        {
            DrawAttackGizmos(enemyBounds);
        }
    }

    private void DrawAttackGizmos(Bounds enemyBounds)
    {
        float totalWidth = Mathf.Max(.1f, enemyBounds.size.x + attackDistance * 2f);
        float totalHeight = Mathf.Max(.1f, Mathf.Max(enemyBounds.size.y, chaseVerticalDistance * 2f));
        Vector3 center = enemyBounds.center;
        Vector3 size = new Vector3(totalWidth, totalHeight, 0f);

        Gizmos.color = new Color(.2f, .9f, 1f, .14f);
        Gizmos.DrawCube(center, size);

        Gizmos.color = new Color(.2f, .9f, 1f, 1f);
        Gizmos.DrawWireCube(center, size);
    }

    private void NormalizeAnimationStateNames()
    {
        if (string.IsNullOrWhiteSpace(attackAnimationState) || attackAnimationState == "skeletonAttack")
        {
            attackAnimationState = "slimeAttack";
        }

        if (string.IsNullOrWhiteSpace(idleAnimationState) || idleAnimationState == "skeletonIdle")
        {
            idleAnimationState = "slimeIdle";
        }

        if (string.IsNullOrWhiteSpace(moveAnimationState) || moveAnimationState == "skeletonMove")
        {
            moveAnimationState = "slimeMove";
        }

        if (string.IsNullOrWhiteSpace(battleAnimationState) || battleAnimationState == "skeletonBattle - idle/move")
        {
            battleAnimationState = "slimeBattle - idle/move";
        }

        if (string.IsNullOrWhiteSpace(stunnedAnimationState) || stunnedAnimationState == "skeletonStunned")
        {
            stunnedAnimationState = "slimeStunned";
        }

        if (string.IsNullOrWhiteSpace(stunRecoveryAnimationState) || stunRecoveryAnimationState == "skeletonStunRecovery")
        {
            stunRecoveryAnimationState = "slimeStunRecovery";
        }
    }

    private int GetPlayerDirection()
    {
        return playerTarget != null
            ? GetPlayerDirection(playerTarget.position)
            : FacingDirection;
    }

    private int GetPlayerDirection(Vector2 playerPosition)
    {
        float xDelta = playerPosition.x - transform.position.x;
        if (Mathf.Abs(xDelta) <= .01f)
        {
            return FacingDirection;
        }

        return xDelta >= 0f ? 1 : -1;
    }

    private float CalculateBattleAnimSpeedMultiplier()
    {
        if (moveSpeed <= 0f)
        {
            return 1f;
        }

        return Mathf.Max(.01f, battleMoveSpeed / moveSpeed);
    }

    private float GetSplitSpawnSeparation()
    {
        Bounds colliderBounds = GetColliderBounds();
        float parentHalfWidth = Mathf.Max(.1f, colliderBounds.extents.x);
        float childHalfWidth = parentHalfWidth * Mathf.Max(.1f, splitChildScaleMultiplier);
        float extraOffset = Mathf.Max(0f, splitSpawnHorizontalOffset);

        return parentHalfWidth + childHalfWidth + extraOffset;
    }

    private Vector3 GetValidSplitSpawnPosition(int horizontalDirection, float splitSeparation)
    {
        Vector3 origin = transform.position;
        int[] directionOrder = { horizontalDirection, -horizontalDirection, 0 };

        foreach (int direction in directionOrder)
        {
            foreach (float horizontalScale in SplitSpawnHorizontalScales)
            {
                float spawnXOffset = direction * splitSeparation * horizontalScale;

                foreach (float verticalOffset in SplitSpawnVerticalOffsets)
                {
                    Vector3 candidate = origin + new Vector3(
                        spawnXOffset,
                        splitSpawnVerticalOffset + verticalOffset,
                        0f
                    );

                    if (IsValidSplitSpawnPosition(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        return origin + new Vector3(horizontalDirection * splitSeparation, splitSpawnVerticalOffset, 0f);
    }

    private bool IsValidSplitSpawnPosition(Vector3 spawnPosition)
    {
        Bounds parentBounds = GetColliderBounds();
        float scaleMultiplier = Mathf.Max(.1f, splitChildScaleMultiplier);

        Vector2 centerOffset = (Vector2)(parentBounds.center - transform.position) * scaleMultiplier;
        Vector2 predictedCenter = (Vector2)spawnPosition + centerOffset;
        Vector2 predictedSize = Vector2.Scale(parentBounds.size, new Vector2(scaleMultiplier, scaleMultiplier));
        Vector2 validationSize = new Vector2(
            Mathf.Max(.05f, predictedSize.x * .9f),
            Mathf.Max(.05f, predictedSize.y * .9f)
        );

        Collider2D[] overlaps = Physics2D.OverlapBoxAll(predictedCenter, validationSize, 0f);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D overlap = overlaps[i];
            if (overlap == null || IsSelfCollider(overlap))
            {
                continue;
            }

            if (overlap.GetComponentInParent<SpikeHazard>() != null)
            {
                return false;
            }

            if (!overlap.isTrigger)
            {
                return false;
            }
        }

        return true;
    }

    public void ConfigureSplitChild(int generation, bool canSplitFurther)
    {
        splitGeneration = Mathf.Max(0, generation);
        splitOnDeath = canSplitFurther;

        float scaleMultiplier = Mathf.Max(.1f, splitChildScaleMultiplier);
        transform.localScale = Vector3.Scale(transform.localScale, new Vector3(scaleMultiplier, scaleMultiplier, 1f));

        maxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * Mathf.Max(.01f, splitChildHealthMultiplier)));
        currentHealth = maxHealth;
        isDead = false;

        Entity_Combat combat = GetComponent<Entity_Combat>();
        if (combat != null)
        {
            combat.SetDamage(Mathf.Max(1, Mathf.RoundToInt(combat.Damage * Mathf.Max(.01f, splitChildDamageMultiplier))));
        }

        Enemy_Healthy healthy = GetComponent<Enemy_Healthy>();
        if (healthy != null)
        {
            healthy.RefreshFromEnemy();
        }

        if (anim != null)
        {
            anim.enabled = true;
            anim.speed = 1f;
        }

        StartSpawnAttackLock(splitChildAttackLockDuration);

        if (stateMachine != null && stateMachine.CurrentState == null && idleState != null)
        {
            stateMachine.Initialize(idleState);
        }
    }

    public void StartSpawnAttackLock(float duration)
    {
        spawnAttackLockTimer = Mathf.Max(0f, duration);
    }

    private void ApplySlimeVisualFacingCorrection()
    {
        if (anim == null)
        {
            return;
        }

        anim.transform.localRotation = slimeSpriteFacesLeftByDefault
            ? Quaternion.Euler(0f, 180f, 0f)
            : Quaternion.identity;
    }

    private void CacheColliderReferences()
    {
        if (aliveCollider == null)
        {
            aliveCollider = GetComponent<CapsuleCollider2D>();
        }

        if (aliveCollider != null && aliveColliderSize == Vector2.zero)
        {
            aliveColliderSize = aliveCollider.size;
            aliveColliderOffset = aliveCollider.offset;
            aliveColliderDirection = aliveCollider.direction;
        }

        if (stunnedCollider != null)
        {
            return;
        }

        Transform child = transform.Find("StunnedCollider");
        if (child != null)
        {
            stunnedCollider = child.GetComponent<CapsuleCollider2D>();
        }
    }

    private Bounds GetPlayerBounds()
    {
        if (playerTarget == null)
        {
            return new Bounds(transform.position, Vector3.one);
        }

        Player playerComponent = playerTarget.GetComponentInParent<Player>();
        return GetPlayerBounds(playerComponent);
    }

    private Bounds GetPlayerBounds(Player player)
    {
        if (player != null && player.cd != null)
        {
            return player.cd.bounds;
        }

        if (player != null)
        {
            return new Bounds(player.transform.position, Vector3.one);
        }

        return new Bounds(transform.position, Vector3.one);
    }

    private float GetHorizontalGap(Bounds enemyBounds, Bounds playerBounds)
    {
        if (playerBounds.center.x >= enemyBounds.center.x)
        {
            return Mathf.Max(0f, playerBounds.min.x - enemyBounds.max.x);
        }

        return Mathf.Max(0f, enemyBounds.min.x - playerBounds.max.x);
    }
}
