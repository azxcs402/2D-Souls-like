using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class Enemy_Mage : Enemy, ICounterable, IEnemyBattleResponder
{
    private static readonly int BattleAnimHash = Animator.StringToHash("battle");
    private static readonly int XVelocityAnimHash = Animator.StringToHash("xVelocity");
    private static readonly int MoveAnimSpeedMultiplierHash = Animator.StringToHash("moveAnimSpeedMultiplier");
    private static readonly int BattleAnimSpeedMultiplierHash = Animator.StringToHash("battleAnimSpeedMultiplier");
    private static readonly int AttackSpeedMultiplierHash = Animator.StringToHash("attackSpeedMultiplier");
    private static readonly int StunnedBoolHash = Animator.StringToHash("stunned");
    private static readonly int HasStunRecoveryHash = Animator.StringToHash("hasStunRecovery");
    private static readonly int SpellCastPerformedHash = Animator.StringToHash("spellCast_performed");

    [Header("Quest Info")]
    [SerializeField] private string questTargetId = "enemy_mage";

    [Header("Battle Details")]
    [SerializeField, Min(.1f)] private float battleMoveSpeed = 4f;
    [SerializeField, Min(.1f)] private float attackDistance = 1.3f;
    [SerializeField, Min(.1f)] private float spellCastDistance = 4.5f;
    [SerializeField, Min(0f)] private float attackCooldown = .5f;
    [SerializeField, Min(0f)] private float spellAttackCooldown = 5f;
    [SerializeField] private bool showBattleRangesInScene = false;
    [SerializeField] private bool canChasePlayer = true;
    [SerializeField, Min(.1f)] private float battleTimeDuration = 5f;
    [SerializeField, Min(0f)] private float minRetreatDistance = 1f;
    [SerializeField, Min(0f)] private float battleStopDistance = .08f;
    [SerializeField] private Vector2 retreatVelocity = new Vector2(7f, 4f);
    [SerializeField, Min(0f)] private float retreatCooldown = 4f;
    [SerializeField, Min(0f)] private float retreatMaxDistance = 8f;
    [SerializeField, Min(0f)] private float retreatSpeed = 15f;

    [Header("Teleport Details")]
    [SerializeField] private Transform teleportAreaAnchor;
    [SerializeField, Min(.1f)] private Vector2 teleportAreaSize = new Vector2(7f, 3f);
    [SerializeField, Min(1)] private int teleportMaxPlacementAttempts = 18;
    [SerializeField, Min(0f)] private float teleportGroundSearchHeight = 2f;
    [SerializeField, Min(.1f)] private float teleportGroundSearchDistance = 8f;
    [SerializeField, Min(1)] private int teleportImageEchoCount = 6;
    [SerializeField, Min(.05f)] private float teleportImageEchoLifetime = .3f;
    [SerializeField, Min(0f)] private float teleportPostDelay = .06f;

    [Header("Teleport Triggers")]
    [SerializeField, Range(0f, 100f)] private float onHitTeleportInitialChance = 5f;
    [SerializeField, Range(0f, 100f)] private float onHitTeleportChanceIncrement = 5f;
    [SerializeField, Range(0f, 100f)] private float rangeTeleportInitialChance = 6f;
    [SerializeField, Range(0f, 100f)] private float rangeTeleportChanceIncrement = 2f;
    [SerializeField, Min(.1f)] private float rangeTeleportCheckInterval = 1f;

    [Header("Stunned State Details")]
    [SerializeField, Min(.1f)] private float stunnedDuration = 1f;
    [SerializeField] private Vector2 stunnedVelocity = new Vector2(7f, 7f);
    [SerializeField] private bool canBeStunned = true;

    [Header("Patrol Info")]
    [SerializeField, Min(.1f)] private float idleDurationMin = 2f;
    [SerializeField, Min(.1f)] private float idleDurationMax = 2f;
    [SerializeField, Min(.1f)] private float moveDurationMin = 2f;
    [SerializeField, Min(.1f)] private float moveDurationMax = 2f;
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

    [Header("Mage Info")]
    [SerializeField] private GameObject spellPrefab;
    [SerializeField] private Transform spellStartPosition;
    [SerializeField, Min(0)] private int amountToCast = 3;
    [SerializeField, Min(0f)] private float spellCastCooldown = .3f;
    [SerializeField, Min(0f)] private float projectileHoverArrivalDuration = .22f;
    [SerializeField] private Transform projectileHoverAreaAnchor;
    [SerializeField, Min(.1f)] private Vector2 projectileHoverAreaSize = new Vector2(4f, 1.8f);
    [SerializeField] private Vector2 projectileHoverAreaOffset = new Vector2(0f, 2.2f);
    [SerializeField, Min(0f)] private float projectileHoverMinSeparation = .72f;
    [SerializeField, Min(1)] private int projectileHoverMaxPlacementAttempts = 24;
    [SerializeField] private Transform behindCollisionCheck;
    [SerializeField] private bool hasRecoveryAnimation = true;
    [SerializeField] private bool canBeKnockedBack = true;

    [Header("Attack Info")]
    [SerializeField] private Entity_AttackData mageAttackData = new Entity_AttackData(new Vector2(.7f, 0f), .6f, new Vector2(4f, 2f));
    [SerializeField] private string attackAnimationState = "skeletonAttack";
    [SerializeField] private string idleAnimationState = "mageIdle";
    [SerializeField] private string moveAnimationState = "mageMove";
    [SerializeField] private string battleAnimationState = "mageBattle - idle/move";
    [SerializeField] private string stunnedAnimationState = "mageStunned";
    [SerializeField] private string stunRecoveryAnimationState = "mageStunRecovery";
    [SerializeField] private string spellCastAnimationState = "mageSpellCast";
    [SerializeField] private string spellCastPerformedAnimationState = "mageSpellCast_performed";

    [Header("Death Info")]
    [SerializeField, Min(0f)] private float deadFallSpeed = 2.25f;
    [SerializeField, Min(0f)] private float deadSlideSpeed = .65f;
    [SerializeField, Min(0f)] private float deadSlideAcceleration = 1.2f;
    [SerializeField, Min(0f)] private float deadDropThroughDelay = 0f;
    [SerializeField, Min(0f)] private float deadDisappearDelay = 4f;
    [SerializeField, Range(0f, 180f)] private float deadFallAngle = 90f;

    public Enemy_MageGroundedState groundedState { get; private set; }
    public Enemy_MageIdleState idleState { get; private set; }
    public Enemy_MageMoveState moveState { get; private set; }
    public Enemy_MageBattleState battleState { get; private set; }
    public Enemy_MageAttackState attackState { get; private set; }
    public Enemy_MageRetreatState retreatState { get; private set; }
    public Enemy_MageSpellCastState spellCastState { get; private set; }
    public Enemy_MageStunnedState stunnedState { get; private set; }
    public Enemy_MageStunRecoveryState stunRecoveryState { get; private set; }
    public Enemy_MageDeadState deadState { get; private set; }

    public string QuestTargetId => questTargetId;
    public float BattleMoveSpeed => battleMoveSpeed;
    public float AttackDistance => attackDistance;
    public float SpellCastDistance => spellCastDistance;
    public float AttackCooldown => attackCooldown;
    public float SpellAttackCooldown => spellAttackCooldown;
    public bool ShowBattleRangesInScene => showBattleRangesInScene;
    public bool CanChasePlayer => canChasePlayer;
    public float BattleTimeDuration => battleTimeDuration;
    public float MinRetreatDistance => minRetreatDistance;
    public float BattleStopDistance => battleStopDistance;
    public Vector2 RetreatVelocity => retreatVelocity;
    public float RetreatCooldown => retreatCooldown;
    public float RetreatMaxDistance => retreatMaxDistance;
    public float RetreatSpeed => retreatSpeed;
    public Transform TeleportAreaAnchor => teleportAreaAnchor;
    public Vector2 TeleportAreaSize => teleportAreaAnchor != null
        ? teleportAreaAnchor.GetComponent<Enemy_MageTeleportAreaAnchor>()?.AreaSize ?? teleportAreaSize
        : teleportAreaSize;
    public Vector2 TeleportAreaOffset => GetTeleportAreaLocalCenter();
    public Vector2 TeleportAreaCenter => (Vector2)transform.position + GetTeleportAreaLocalCenter();
    public int TeleportMaxPlacementAttempts => teleportMaxPlacementAttempts;
    public float TeleportGroundSearchHeight => teleportGroundSearchHeight;
    public float TeleportGroundSearchDistance => teleportGroundSearchDistance;
    public int TeleportImageEchoCount => teleportImageEchoCount;
    public float TeleportImageEchoLifetime => teleportImageEchoLifetime;
    public float TeleportPostDelay => teleportPostDelay;
    public float OnHitTeleportInitialChance => onHitTeleportInitialChance;
    public float OnHitTeleportChanceIncrement => onHitTeleportChanceIncrement;
    public float RangeTeleportInitialChance => rangeTeleportInitialChance;
    public float RangeTeleportChanceIncrement => rangeTeleportChanceIncrement;
    public float RangeTeleportCheckInterval => rangeTeleportCheckInterval;
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
    public GameObject SpellPrefab => spellPrefab;
    public Transform SpellStartPosition => spellStartPosition;
    public int AmountToCast => amountToCast;
    public float SpellCastCooldown => spellCastCooldown;
    public float ProjectileHoverArrivalDuration => projectileHoverArrivalDuration;
    public Transform ProjectileHoverAreaAnchor => projectileHoverAreaAnchor;
    public Vector2 ProjectileHoverAreaSize => projectileHoverAreaAnchor != null
        ? projectileHoverAreaAnchor.GetComponent<Enemy_MageProjectileHoverAreaAnchor>()?.HoverAreaSize ?? projectileHoverAreaSize
        : projectileHoverAreaSize;
    public Vector2 ProjectileHoverAreaOffset => GetProjectileHoverAreaLocalCenter();
    public Vector2 ProjectileHoverAreaCenter => (Vector2)transform.position + GetProjectileHoverAreaLocalCenter();
    public float ProjectileHoverMinSeparation => projectileHoverMinSeparation;
    public int ProjectileHoverMaxPlacementAttempts => projectileHoverMaxPlacementAttempts;
    public Transform BehindCollisionCheck => behindCollisionCheck;
    public bool HasRecoveryAnimation => hasRecoveryAnimation;
    public bool CanBeKnockedBack => canBeKnockedBack;
    public Entity_AttackData MageAttackData => mageAttackData;
    public string AttackAnimationState => attackAnimationState;
    public string IdleAnimationState => idleAnimationState;
    public string MoveAnimationState => moveAnimationState;
    public string BattleAnimationState => battleAnimationState;
    public string StunnedAnimationState => stunnedAnimationState;
    public string StunRecoveryAnimationState => stunRecoveryAnimationState;
    public string SpellCastAnimationState => spellCastAnimationState;
    public string SpellCastPerformedAnimationState => spellCastPerformedAnimationState;
    public float DeadFallSpeed => deadFallSpeed;
    public float DeadSlideSpeed => deadSlideSpeed;
    public float DeadSlideAcceleration => deadSlideAcceleration;
    public float DeadDropThroughDelay => deadDropThroughDelay;
    public float DeadDisappearDelay => deadDisappearDelay;
    public float DeadFallAngle => deadFallAngle;

    public bool IsAlerted => isAlerted;
    public bool ShouldReturnToPatrol => shouldReturnToPatrol;
    public bool CanAttack => attackCooldownTimer <= 0f;
    public bool CanSpellCast => spellAttackCooldownTimer <= 0f;
    public bool IsCounterWindowActive => counterWindowActive;
    public int PlayerTargetDirection => playerTargetDirection;
    public Transform PlayerTarget => playerTarget;
    public bool PlayerVisible => playerVisible;
    public bool PlayerInAttackRange => playerInMeleeRange;
    public bool PlayerInMeleeRange => playerInMeleeRange;
    public bool PlayerInSpellCastRange => playerInSpellCastRange;
    public bool PlayerWithinChaseHeight => playerWithinChaseHeight;
    public bool IsStunned => stateMachine != null && stateMachine.CurrentState == stunnedState;
    public bool IsSpellCasting => stateMachine != null && stateMachine.CurrentState == spellCastState;
    public bool SpellCastPerformed => spellCastPerformed;
    public Entity_Combat Combat { get; private set; }

    private Transform playerTarget;
    private bool isAlerted;
    private bool shouldReturnToPatrol;
    private bool playerVisible;
    private bool playerInMeleeRange;
    private bool playerInSpellCastRange;
    private bool playerWithinChaseHeight;
    private bool counterWindowActive;
    private int playerTargetDirection = 1;
    private float lastTimeSeenPlayer;
    private float attackCooldownTimer;
    private float spellAttackCooldownTimer;
    private float lastTimeUsedRetreat = float.NegativeInfinity;
    private float battleAnimSpeedMultiplier = 1f;
    private float currentOnHitTeleportChance;
    private float currentRangeTeleportChance;
    private float rangeTeleportTimer;
    private bool spellCastPerformed;
    private bool halfHealthTeleportTriggered;
    private bool hasQueuedTeleportDestination;
    private Vector2 queuedTeleportDestination;
    private Coroutine spellCastCoroutine;
    private readonly List<ProjectileHoverReservation> reservedProjectileHoverReservations = new List<ProjectileHoverReservation>();
    private int nextProjectileHoverReservationId = 1;

    protected override void Awake()
    {
        base.Awake();

        Combat = GetComponent<Entity_Combat>();
        EnsureGroundMaskAssigned();
        CacheChildReferences();
        NormalizeAnimationStateNames();
        ResetTeleportProbabilities();
        battleAnimSpeedMultiplier = CalculateBattleAnimSpeedMultiplier();

        groundedState = new Enemy_MageGroundedState(this, stateMachine);
        idleState = new Enemy_MageIdleState(this, stateMachine);
        moveState = new Enemy_MageMoveState(this, stateMachine);
        battleState = new Enemy_MageBattleState(this, stateMachine);
        attackState = new Enemy_MageAttackState(this, stateMachine);
        retreatState = new Enemy_MageRetreatState(this, stateMachine);
        spellCastState = new Enemy_MageSpellCastState(this, stateMachine);
        stunnedState = new Enemy_MageStunnedState(this, stateMachine);
        stunRecoveryState = new Enemy_MageStunRecoveryState(this, stateMachine);
        deadState = new Enemy_MageDeadState(this, stateMachine);

        if (anim != null)
        {
            anim.SetBool(HasStunRecoveryHash, hasRecoveryAnimation);
            anim.SetFloat(AttackSpeedMultiplierHash, 1f);
        }
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

        if (spellAttackCooldownTimer > 0f)
        {
            spellAttackCooldownTimer -= Time.deltaTime;
        }

        UpdateRangeTeleportTrigger();
        battleAnimSpeedMultiplier = CalculateBattleAnimSpeedMultiplier();
        base.Update();
    }

    protected override void SyncAnimationState()
    {
        if (IsDead
            || stateMachine?.CurrentState == deadState
            || stateMachine?.CurrentState == stunnedState
            || stateMachine?.CurrentState == stunRecoveryState
            || stateMachine?.CurrentState == attackState
            || stateMachine?.CurrentState == retreatState
            || stateMachine?.CurrentState == spellCastState)
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
            if (ShouldPrioritizeRetreat() && retreatState != null)
            {
                TryTriggerTeleport();
            }
            else if (PlayerInMeleeRange && CanAttack && attackState != null)
            {
                stateMachine.ChangeState(attackState);
            }
            else if (PlayerInSpellCastRange && CanSpellCast && spellCastState != null)
            {
                stateMachine.ChangeState(spellCastState);
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

    public void StartSpellAttackCooldown()
    {
        spellAttackCooldownTimer = Mathf.Max(0f, spellAttackCooldown);
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
        playerInMeleeRange = false;
        playerInSpellCastRange = false;
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
            && stateMachine.CurrentState != spellCastState
            && stateMachine.CurrentState != stunnedState
            && stateMachine.CurrentState != stunRecoveryState
            && stateMachine.CurrentState != deadState
            && stateMachine.CurrentState != retreatState)
        {
            stateMachine.ChangeState(battleState);
        }
    }

    public void HandleTeleportTriggerOnDamaged()
    {
        if (IsDead || stateMachine == null)
        {
            return;
        }

        if (!halfHealthTeleportTriggered && CurrentHealth <= Mathf.CeilToInt(MaxHealth * .5f))
        {
            if (TryTriggerTeleport(ignoreCooldown: true))
            {
                halfHealthTeleportTriggered = true;
                return;
            }
        }

        if (TryRollPercent(currentOnHitTeleportChance)
            && TryTriggerTeleport(ignoreCooldown: true))
        {
            return;
        }

        currentOnHitTeleportChance = Mathf.Clamp(
            currentOnHitTeleportChance + onHitTeleportChanceIncrement,
            0f,
            100f
        );
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
            anim.SetFloat(BattleAnimSpeedMultiplierHash, battle ? battleAnimSpeedMultiplier : 1f);
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

    public void SetAttackAnimationSpeed(float speedMultiplier)
    {
        if (anim == null)
        {
            return;
        }

        if (HasAnimatorParameter(AttackSpeedMultiplierHash))
        {
            anim.SetFloat(AttackSpeedMultiplierHash, Mathf.Max(.01f, speedMultiplier));
        }
    }

    public void SetStunnedAnimation(bool stunned)
    {
        if (anim == null)
        {
            return;
        }

        if (HasAnimatorParameter(StunnedBoolHash))
        {
            anim.SetBool(StunnedBoolHash, stunned);
        }
    }

    public void SetSpellCastPerformed(bool performed)
    {
        spellCastPerformed = performed;
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

        counterWindowActive = false;
    }

    public bool TryCounter()
    {
        if (!counterWindowActive || !CanBeStunned || stunnedState == null || stateMachine == null || IsDead)
        {
            return false;
        }

        stateMachine.ChangeState(stunnedState);
        return true;
    }

    public bool CantMoveBackwards()
    {
        if (behindCollisionCheck == null)
        {
            return false;
        }

        bool detectedWall = Physics2D.Raycast(behindCollisionCheck.position, Vector2.right * -FacingDirection, 1.5f, whatIsGround);
        bool noGround = Physics2D.Raycast(behindCollisionCheck.position, Vector2.down, 1.5f, whatIsGround) == false;

        return noGround || detectedWall;
    }

    public void SpecialAttack()
    {
        StartSpellAttackCooldown();

        if (spellCastCoroutine != null)
        {
            return;
        }

        if (spellPrefab == null || AmountToCast <= 0)
        {
            SetSpellCastPerformed(true);
            return;
        }

        Transform target = GetPlayerReference();
        if (target == null)
        {
            SetSpellCastPerformed(true);
            return;
        }

        spellCastCoroutine = StartCoroutine(CastSpellCo(target));
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        CacheChildReferences();
        EnsureGroundMaskAssigned();
        NormalizeAnimationStateNames();

        maxHealth = Mathf.Max(1, maxHealth);
        battleMoveSpeed = Mathf.Max(.1f, battleMoveSpeed);
        attackDistance = Mathf.Max(.1f, attackDistance);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        spellAttackCooldown = Mathf.Max(0f, spellAttackCooldown);
        projectileHoverArrivalDuration = Mathf.Max(0f, projectileHoverArrivalDuration);
        battleTimeDuration = Mathf.Max(.1f, battleTimeDuration);
        minRetreatDistance = Mathf.Max(0f, minRetreatDistance);
        battleStopDistance = Mathf.Max(0f, battleStopDistance);
        retreatCooldown = Mathf.Max(0f, retreatCooldown);
        retreatMaxDistance = Mathf.Max(0f, retreatMaxDistance);
        retreatSpeed = Mathf.Max(0f, retreatSpeed);
        teleportMaxPlacementAttempts = Mathf.Max(1, teleportMaxPlacementAttempts);
        teleportGroundSearchHeight = Mathf.Max(0f, teleportGroundSearchHeight);
        teleportGroundSearchDistance = Mathf.Max(.1f, teleportGroundSearchDistance);
        teleportImageEchoCount = Mathf.Max(1, teleportImageEchoCount);
        teleportImageEchoLifetime = Mathf.Max(.05f, teleportImageEchoLifetime);
        teleportPostDelay = Mathf.Max(0f, teleportPostDelay);
        onHitTeleportInitialChance = Mathf.Clamp(onHitTeleportInitialChance, 0f, 100f);
        onHitTeleportChanceIncrement = Mathf.Clamp(onHitTeleportChanceIncrement, 0f, 100f);
        rangeTeleportInitialChance = Mathf.Clamp(rangeTeleportInitialChance, 0f, 100f);
        rangeTeleportChanceIncrement = Mathf.Clamp(rangeTeleportChanceIncrement, 0f, 100f);
        rangeTeleportCheckInterval = Mathf.Max(.1f, rangeTeleportCheckInterval);
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
        amountToCast = Mathf.Max(0, amountToCast);
        spellCastCooldown = Mathf.Max(0f, spellCastCooldown);
        projectileHoverMinSeparation = Mathf.Max(0f, projectileHoverMinSeparation);
        projectileHoverMaxPlacementAttempts = Mathf.Max(1, projectileHoverMaxPlacementAttempts);
        retreatCooldown = Mathf.Max(0f, retreatCooldown);
        retreatMaxDistance = Mathf.Max(0f, retreatMaxDistance);
        retreatSpeed = Mathf.Max(0f, retreatSpeed);
        deadFallSpeed = Mathf.Max(0f, deadFallSpeed);
        deadSlideSpeed = Mathf.Max(0f, deadSlideSpeed);
        deadSlideAcceleration = Mathf.Max(0f, deadSlideAcceleration);
        deadDropThroughDelay = Mathf.Max(0f, deadDropThroughDelay);
        deadDisappearDelay = Mathf.Max(0f, deadDisappearDelay);
        deadFallAngle = Mathf.Clamp(deadFallAngle, 0f, 180f);
        projectileHoverAreaSize = new Vector2(
            Mathf.Max(.1f, projectileHoverAreaSize.x),
            Mathf.Max(.1f, projectileHoverAreaSize.y)
        );
        teleportAreaSize = new Vector2(
            Mathf.Max(.1f, teleportAreaSize.x),
            Mathf.Max(.1f, teleportAreaSize.y)
        );
        battleAnimSpeedMultiplier = CalculateBattleAnimSpeedMultiplier();

        if (whatIsPlayer == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                whatIsPlayer = 1 << playerLayer;
            }
        }
    }

    private IEnumerator CastSpellCo(Transform target)
    {
        SetSpellCastPerformed(false);

        for (int i = 0; i < amountToCast; i++)
        {
            if (spellPrefab == null || target == null)
            {
                break;
            }

            bool reservedHoverSlot = TryReserveProjectileHoverSlot(out int hoverReservationId, out Vector2 hoverOffset);
            Vector3 spawnPosition = GetSpellSpawnPosition();

            GameObject projectileObject = Instantiate(spellPrefab, spawnPosition, Quaternion.identity);
            Enemy_MageProjectile projectile = projectileObject.GetComponent<Enemy_MageProjectile>();

            if (projectile != null)
            {
                projectile.SetupProjectile(this, target, Combat, reservedHoverSlot ? hoverReservationId : -1, hoverOffset);
            }

            if (spellCastCooldown > 0f)
            {
                yield return new WaitForSeconds(spellCastCooldown);
            }
            else
            {
                yield return null;
            }
        }

        SetSpellCastPerformed(true);
        spellCastCoroutine = null;
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
            shouldReturnToPatrol = false;
        }
        else
        {
            playerVisible = false;
        }

        if (playerTarget == null)
        {
            playerTarget = FindAnyPlayerReference();
            if (playerTarget == null)
            {
                playerInMeleeRange = false;
                playerInSpellCastRange = false;
                playerWithinChaseHeight = false;
                return;
            }
        }

        float horizontalDistance = Mathf.Abs(playerTarget.position.x - transform.position.x);
        float verticalDistance = Mathf.Abs(playerTarget.position.y - transform.position.y);
        playerWithinChaseHeight = verticalDistance <= chaseVerticalDistance;
        playerInMeleeRange = playerWithinChaseHeight && horizontalDistance <= attackDistance;
        playerInSpellCastRange = horizontalDistance <= spellCastDistance;

        if (playerInSpellCastRange)
        {
            if (!isAlerted)
            {
                isAlerted = true;
                shouldReturnToPatrol = false;
            }

            lastTimeSeenPlayer = Time.time;
        }

        if (playerVisible && playerWithinChaseHeight && playerTargetDirection != FacingDirection)
        {
            FaceDirection(playerTargetDirection);
        }

        if (isAlerted
            && !playerVisible
            && !playerInSpellCastRange
            && lastTimeSeenPlayer > 0f
            && Time.time - lastTimeSeenPlayer >= loseSightDuration)
        {
            StopChasingPlayer();
        }
    }

    public Transform PlayerDetected()
    {
        if (whatIsPlayer.value == 0)
        {
            return null;
        }

        Transform origin = playerCheck != null ? playerCheck : transform;
        float distance = Mathf.Max(.01f, playerCheckDistance);
        Transform detectedPlayer = DetectPlayerAtDirection(origin.position, distance, FacingDirection);
        if (detectedPlayer != null)
        {
            return detectedPlayer;
        }

        return DetectPlayerAtDirection(origin.position, distance, -FacingDirection);
    }

    public Transform GetPlayerReference()
    {
        if (playerTarget != null)
        {
            return playerTarget;
        }

        return PlayerDetected();
    }

    private Transform FindAnyPlayerReference()
    {
        Player player = FindObjectOfType<Player>();
        return player != null ? player.transform : null;
    }

    public Bounds PlayerBounds => GetPlayerBounds();

    public Vector2 GetSpellSpawnPosition()
    {
        return spellStartPosition != null ? spellStartPosition.position : transform.position;
    }

    public bool TryReserveProjectileHoverSlot(out int reservationId, out Vector2 hoverOffset)
    {
        reservationId = -1;
        hoverOffset = Vector2.zero;

        for (int attempt = 0; attempt < projectileHoverMaxPlacementAttempts; attempt++)
        {
            Vector2 candidate = GetRandomHoverLocalOffset();
            if (!IsHoverOffsetOccupied(candidate))
            {
                reservationId = nextProjectileHoverReservationId++;
                reservedProjectileHoverReservations.Add(new ProjectileHoverReservation(reservationId, candidate));
                hoverOffset = candidate;
                return true;
            }
        }

        return false;
    }

    public void ReleaseProjectileHoverSlot(int reservationId)
    {
        if (reservationId < 0)
        {
            return;
        }

        for (int i = reservedProjectileHoverReservations.Count - 1; i >= 0; i--)
        {
            if (reservedProjectileHoverReservations[i].Id == reservationId)
            {
                reservedProjectileHoverReservations.RemoveAt(i);
                return;
            }
        }
    }

    public Vector2 GetProjectileHoverSlotWorldPosition(int reservationId)
    {
        Vector2 localOffset = GetProjectileHoverSlotLocalOffset(reservationId);
        return (Vector2)transform.position + localOffset;
    }

    private Vector2 GetProjectileHoverSlotLocalOffset(int reservationId)
    {
        for (int i = 0; i < reservedProjectileHoverReservations.Count; i++)
        {
            if (reservedProjectileHoverReservations[i].Id == reservationId)
            {
                return reservedProjectileHoverReservations[i].LocalOffset;
            }
        }

        return Vector2.zero;
    }

    private bool IsHoverOffsetOccupied(Vector2 candidate)
    {
        float minSeparationSqr = projectileHoverMinSeparation * projectileHoverMinSeparation;
        for (int i = 0; i < reservedProjectileHoverReservations.Count; i++)
        {
            if ((reservedProjectileHoverReservations[i].LocalOffset - candidate).sqrMagnitude < minSeparationSqr)
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 GetRandomHoverLocalOffset()
    {
        Vector2 hoverAreaCenter = GetProjectileHoverAreaLocalCenter();
        Vector2 halfSize = ProjectileHoverAreaSize * .5f;
        float randomX = Random.Range(-halfSize.x, halfSize.x);
        float randomY = Random.Range(-halfSize.y, halfSize.y);
        return new Vector2(randomX, randomY) + hoverAreaCenter;
    }

    private Vector2 GetProjectileHoverAreaLocalCenter()
    {
        if (projectileHoverAreaAnchor != null)
        {
            return projectileHoverAreaAnchor.localPosition;
        }

        return projectileHoverAreaOffset;
    }

    private Vector2 GetTeleportAreaLocalCenter()
    {
        if (teleportAreaAnchor != null)
        {
            return teleportAreaAnchor.localPosition;
        }

        return Vector2.zero;
    }

    public bool TryTriggerTeleport(bool ignoreCooldown = false)
    {
        if (IsDead
            || retreatState == null
            || stateMachine == null
            || stateMachine.CurrentState == retreatState)
        {
            return false;
        }

        if (!ignoreCooldown && !CanUseRetreatAbility())
        {
            return false;
        }

        if (!TryFindTeleportDestination(out Vector2 destination))
        {
            return false;
        }

        queuedTeleportDestination = destination;
        hasQueuedTeleportDestination = true;
        MarkRetreatUsed();
        ResetTeleportProbabilities();
        stateMachine.ChangeState(retreatState);
        return true;
    }

    public bool ConsumeQueuedTeleportDestination(out Vector2 destination)
    {
        if (hasQueuedTeleportDestination)
        {
            hasQueuedTeleportDestination = false;
            destination = queuedTeleportDestination;
            return true;
        }

        destination = Vector2.zero;
        return false;
    }

    public void ResetTeleportProbabilities()
    {
        currentOnHitTeleportChance = onHitTeleportInitialChance;
        currentRangeTeleportChance = rangeTeleportInitialChance;
        rangeTeleportTimer = 0f;
    }

    public bool TryTeleportToRetreatPoint(out Vector2 destination)
    {
        if (!TryFindTeleportDestination(out destination))
        {
            return false;
        }

        return TeleportToDestination(destination);
    }

    public bool TeleportToDestination(Vector2 destination)
    {
        Vector3 visualStart = GetVisualWorldPosition((Vector2)transform.position);
        Vector3 visualEnd = GetVisualWorldPosition(destination);

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.position = destination;
        }
        else
        {
            transform.position = destination;
        }

        Entity_VFX entityVFX = GetComponent<Entity_VFX>();
        if (entityVFX != null)
        {
            entityVFX.CreateImageEchoTrail(visualStart, visualEnd, teleportImageEchoCount, teleportImageEchoLifetime);
        }

        return true;
    }

    public bool TryFindTeleportDestination(out Vector2 destination)
    {
        EnsureGroundMaskAssigned();
        destination = transform.position;
        Vector2 areaCenter = TeleportAreaCenter;
        Vector2 halfSize = TeleportAreaSize * .5f;
        Transform player = GetPlayerReference();
        int playerDirection = player != null && player.position.x >= transform.position.x ? 1 : -1;

        for (int attempt = 0; attempt < teleportMaxPlacementAttempts; attempt++)
        {
            bool restrictAwaySide = attempt < teleportMaxPlacementAttempts * .65f;
            float minX = areaCenter.x - halfSize.x;
            float maxX = areaCenter.x + halfSize.x;

            if (restrictAwaySide)
            {
                if (playerDirection > 0)
                {
                    maxX = Mathf.Min(maxX, transform.position.x);
                }
                else
                {
                    minX = Mathf.Max(minX, transform.position.x);
                }
            }

            if (minX > maxX)
            {
                minX = areaCenter.x - halfSize.x;
                maxX = areaCenter.x + halfSize.x;
            }

            float candidateGroundX = Random.Range(minX, maxX);
            float rayOriginY = areaCenter.y + halfSize.y + teleportGroundSearchHeight;
            float rayDistance = teleportGroundSearchHeight + TeleportAreaSize.y + teleportGroundSearchDistance;
            RaycastHit2D hit = Physics2D.Raycast(
                new Vector2(candidateGroundX, rayOriginY),
                Vector2.down,
                rayDistance,
                whatIsGround
            );

            if (hit.collider == null)
            {
                continue;
            }

            Vector2 candidateRootPosition = BuildTeleportRootPosition(candidateGroundX, hit.point.y);
            if (!IsTeleportDestinationSafe(candidateRootPosition))
            {
                continue;
            }

            destination = candidateRootPosition;
            return true;
        }

        return false;
    }

    private Vector2 BuildTeleportRootPosition(float groundX, float groundY)
    {
        if (cd == null)
        {
            return new Vector2(groundX, groundY);
        }

        float rootToColliderCenterX = transform.position.x - cd.bounds.center.x;
        float rootToColliderBottomY = transform.position.y - cd.bounds.min.y;

        return new Vector2(
            groundX + rootToColliderCenterX,
            groundY + rootToColliderBottomY + .02f
        );
    }

    private bool IsTeleportDestinationSafe(Vector2 rootPosition)
    {
        if (cd == null)
        {
            return true;
        }

        Vector2 worldScale = new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        Vector2 capsuleSize = new Vector2(
            Mathf.Max(.05f, cd.size.x * worldScale.x * .92f),
            Mathf.Max(.05f, cd.size.y * worldScale.y * .9f)
        );

        Vector2 colliderCenter = rootPosition + Vector2.Scale(cd.offset, worldScale) + Vector2.up * .08f;

        Collider2D overlap = Physics2D.OverlapCapsule(
            colliderCenter,
            capsuleSize,
            cd.direction,
            0f,
            whatIsGround
        );

        return overlap == null;
    }

    private void UpdateRangeTeleportTrigger()
    {
        if (IsDead || retreatState == null || stateMachine == null || !CanUseRetreatAbility())
        {
            return;
        }

        Transform player = GetPlayerReference();
        if (player == null || !IsPlayerInsideAnyTeleportTriggerRange(player.position))
        {
            return;
        }

        rangeTeleportTimer += Time.deltaTime;
        while (rangeTeleportTimer >= rangeTeleportCheckInterval)
        {
            rangeTeleportTimer -= rangeTeleportCheckInterval;

            if (TryRollPercent(currentRangeTeleportChance) && TryTriggerTeleport())
            {
                return;
            }

            currentRangeTeleportChance = Mathf.Clamp(
                currentRangeTeleportChance + rangeTeleportChanceIncrement,
                0f,
                100f
            );
        }
    }

    private bool IsPlayerInsideAnyTeleportTriggerRange(Vector3 playerPosition)
    {
        Enemy_MageTeleportTriggerAnchor[] anchors = GetComponentsInChildren<Enemy_MageTeleportTriggerAnchor>(true);
        for (int i = 0; i < anchors.Length; i++)
        {
            Enemy_MageTeleportTriggerAnchor anchor = anchors[i];
            if (anchor == null)
            {
                continue;
            }

            float radius = anchor.Radius;
            if (radius <= 0f)
            {
                continue;
            }

            if (Vector2.Distance(anchor.transform.position, playerPosition) <= radius)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryRollPercent(float chancePercent)
    {
        return Random.value <= Mathf.Clamp01(chancePercent / 100f);
    }

    private Vector3 GetVisualWorldPosition(Vector2 rootPosition)
    {
        Transform visualTransform = anim != null ? anim.transform : transform;
        Vector3 visualOffset = visualTransform.position - transform.position;
        return (Vector3)rootPosition + visualOffset;
    }

    private readonly struct ProjectileHoverReservation
    {
        public ProjectileHoverReservation(int id, Vector2 localOffset)
        {
            Id = id;
            LocalOffset = localOffset;
        }

        public int Id { get; }
        public Vector2 LocalOffset { get; }
    }

    private int GetPlayerDirection()
    {
        if (playerTarget == null)
        {
            return FacingDirection;
        }

        float xDelta = playerTarget.position.x - transform.position.x;
        if (Mathf.Abs(xDelta) <= .01f)
        {
            return FacingDirection;
        }

        return xDelta > 0f ? 1 : -1;
    }

    private Transform DetectPlayerAtDirection(Vector2 origin, float distance, int direction)
    {
        if (direction != 1 && direction != -1)
        {
            return null;
        }

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * direction, distance, whatIsPlayer);
        if (hit.collider == null)
        {
            return null;
        }

        Player player = hit.collider.GetComponentInParent<Player>();
        return player != null ? player.transform : hit.collider.transform;
    }

    private float CalculateBattleAnimSpeedMultiplier()
    {
        if (moveSpeed <= 0f)
        {
            return 1f;
        }

        return Mathf.Max(.01f, battleMoveSpeed / moveSpeed);
    }

    private void CacheChildReferences()
    {
        if (playerCheck == null)
        {
            playerCheck = FindChild("TargetCheck") ?? FindChild("PlayerCheck");
        }

        if (spellStartPosition == null)
        {
            spellStartPosition = FindChild("SpellStartPoint");
        }

        if (projectileHoverAreaAnchor == null)
        {
            projectileHoverAreaAnchor = FindChild("ProjectileHoverArea")
                ?? FindChild("SpellHoverArea")
                ?? FindChild("ProjectileHoverAreaAnchor");
        }

        if (behindCollisionCheck == null)
        {
            behindCollisionCheck = FindChild("BehindCheck") ?? FindChild("BehindCollisionCheck");
        }

        if (teleportAreaAnchor == null)
        {
            teleportAreaAnchor = FindChild("TeleportArea")
                ?? FindChild("MageTeleportArea")
                ?? FindChild("TeleportRange");
        }
    }

    public bool CanUseRetreatAbility()
    {
        return Time.time > lastTimeUsedRetreat + retreatCooldown;
    }

    public void MarkRetreatUsed()
    {
        lastTimeUsedRetreat = Time.time;
    }

    private bool ShouldPrioritizeRetreat()
    {
        if (!PlayerWithinChaseHeight || !CanUseRetreatAbility())
        {
            return false;
        }

        Transform player = GetPlayerReference();
        if (player == null)
        {
            return false;
        }

        return Mathf.Abs(player.position.x - transform.position.x) < minRetreatDistance;
    }

    private void EnsureGroundMaskAssigned()
    {
        if (whatIsGround != 0)
        {
            return;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
        {
            whatIsGround = 1 << groundLayer;
        }
    }

    private Transform FindChild(string childName)
    {
        if (string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        Transform[] children = transform.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == childName)
            {
                return children[i];
            }
        }

        return null;
    }

    private void NormalizeAnimationStateNames()
    {
        if (string.IsNullOrWhiteSpace(attackAnimationState))
        {
            attackAnimationState = "skeletonAttack";
        }

        if (string.IsNullOrWhiteSpace(idleAnimationState))
        {
            idleAnimationState = "mageIdle";
        }

        if (string.IsNullOrWhiteSpace(moveAnimationState))
        {
            moveAnimationState = "mageMove";
        }

        if (spellCastDistance < attackDistance)
        {
            spellCastDistance = attackDistance;
        }

        if (string.IsNullOrWhiteSpace(battleAnimationState))
        {
            battleAnimationState = "mageBattle - idle/move";
        }

        if (string.IsNullOrWhiteSpace(stunnedAnimationState))
        {
            stunnedAnimationState = "mageStunned";
        }

        if (string.IsNullOrWhiteSpace(stunRecoveryAnimationState))
        {
            stunRecoveryAnimationState = "mageStunRecovery";
        }

        if (string.IsNullOrWhiteSpace(spellCastAnimationState))
        {
            spellCastAnimationState = "mageSpellCast";
        }

        if (string.IsNullOrWhiteSpace(spellCastPerformedAnimationState))
        {
            spellCastPerformedAnimationState = "mageSpellCast_performed";
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

    private bool HasAnimatorParameter(int parameterHash)
    {
        if (anim == null)
        {
            return false;
        }

        for (int i = 0; i < anim.parameters.Length; i++)
        {
            if (anim.parameters[i].nameHash == parameterHash)
            {
                return true;
            }
        }

        return false;
    }
}
