using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class Enemy_Slime : Enemy, ICounterable, IEnemyBattleResponder
{
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

    [Header("Player Detection")]
    [SerializeField] private LayerMask whatIsPlayer;
    [SerializeField] private Transform playerCheck;
    [SerializeField, Min(.01f)] private float playerCheckDistance = 10f;
    [SerializeField, Min(0f)] private float chaseVerticalDistance = 3f;
    [SerializeField, Min(0f)] private float loseSightDuration = 3f;

    [Header("Slime Info")]
    [SerializeField] private GameObject slimeToCreatePrefab;
    [SerializeField, Min(0)] private int amountOfSlimesToCreate = 2;
    [SerializeField] private Vector2 newSlimeVelocity = new Vector2(4f, 3f);
    [SerializeField] private bool hasRecoveryAnimation = true;
    [SerializeField] private bool canBeKnockedBack = true;
    [SerializeField] private bool slimeSpriteFacesLeftByDefault = true;

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
    public Transform PlayerCheck => playerCheck;
    public float PlayerCheckDistance => playerCheckDistance;
    public float ChaseVerticalDistance => chaseVerticalDistance;
    public float LoseSightDuration => loseSightDuration;
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

    public bool IsAlerted => isAlerted;
    public bool ShouldReturnToPatrol => shouldReturnToPatrol;
    public bool CanAttack => attackCooldownTimer <= 0f;
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
            playerTarget = PlayerDetected();
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

    public Transform PlayerDetected()
    {
        if (whatIsPlayer.value == 0)
        {
            return null;
        }

        Transform origin = playerCheck != null ? playerCheck : transform;
        float distance = Mathf.Max(.01f, playerCheckDistance);
        RaycastHit2D hit = Physics2D.Raycast(origin.position, Vector2.right * FacingDirection, distance, whatIsPlayer);

        if (hit.collider == null)
        {
            return null;
        }

        Player player = hit.collider.GetComponentInParent<Player>();
        return player != null ? player.transform : hit.collider.transform;
    }

    public Transform GetPlayerReference()
    {
        if (playerTarget != null)
        {
            return playerTarget;
        }

        return PlayerDetected();
    }

    public void ApplySpawnVelocity()
    {
        if (rb == null)
        {
            return;
        }

        Vector2 velocity = new Vector2(
            newSlimeVelocity.x * Random.Range(-1f, 1f),
            newSlimeVelocity.y * Random.Range(1f, 2f)
        );
        rb.velocity = velocity;
    }

    public void CreateSlimeOnDeath()
    {
        if (slimeToCreatePrefab == null || amountOfSlimesToCreate <= 0)
        {
            return;
        }

        for (int i = 0; i < amountOfSlimesToCreate; i++)
        {
            GameObject newSlime = Instantiate(slimeToCreatePrefab, transform.position, Quaternion.identity);
            Enemy_Slime slime = newSlime.GetComponent<Enemy_Slime>();

            if (slime != null)
            {
                slime.ApplySpawnVelocity();

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
        playerCheckDistance = Mathf.Max(.01f, playerCheckDistance);
        chaseVerticalDistance = Mathf.Max(0f, chaseVerticalDistance);
        loseSightDuration = Mathf.Max(0f, loseSightDuration);
        amountOfSlimesToCreate = Mathf.Max(0, amountOfSlimesToCreate);
        deadFallSpeed = Mathf.Max(0f, deadFallSpeed);
        deadSlideSpeed = Mathf.Max(0f, deadSlideSpeed);
        deadSlideAcceleration = Mathf.Max(0f, deadSlideAcceleration);
        deadDropThroughDelay = Mathf.Max(0f, deadDropThroughDelay);
        deadDisappearDelay = Mathf.Max(0f, deadDisappearDelay);
        deadFallAngle = Mathf.Clamp(deadFallAngle, 0f, 180f);
        battleAnimSpeedMultiplier = CalculateBattleAnimSpeedMultiplier();
    }

    private void UpdatePlayerPerception()
    {
        Transform detectedPlayer = PlayerDetected();

        if (detectedPlayer != null)
        {
            playerTarget = detectedPlayer;
            playerVisible = true;
            playerTargetDirection = GetPlayerDirection();
            lastTimeSeenPlayer = Time.time;
            isAlerted = true;
        }
        else
        {
            playerVisible = false;
        }

        if (playerTarget == null)
        {
            playerInAttackRange = false;
            playerWithinChaseHeight = false;
            return;
        }

        float horizontalDistance = Mathf.Abs(playerTarget.position.x - transform.position.x);
        float verticalDistance = Mathf.Abs(playerTarget.position.y - transform.position.y);
        playerWithinChaseHeight = verticalDistance <= chaseVerticalDistance;
        playerInAttackRange = playerWithinChaseHeight && horizontalDistance <= attackDistance;

        if (isAlerted && !playerVisible && lastTimeSeenPlayer > 0f && Time.time - lastTimeSeenPlayer >= loseSightDuration)
        {
            StopChasingPlayer();
        }
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
        if (playerTarget == null)
        {
            return FacingDirection;
        }

        return playerTarget.position.x >= transform.position.x ? 1 : -1;
    }

    private float CalculateBattleAnimSpeedMultiplier()
    {
        if (moveSpeed <= 0f)
        {
            return 1f;
        }

        return Mathf.Max(.01f, battleMoveSpeed / moveSpeed);
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
        if (playerComponent != null && playerComponent.cd != null)
        {
            return playerComponent.cd.bounds;
        }

        return new Bounds(playerTarget.position, Vector3.one);
    }
}
