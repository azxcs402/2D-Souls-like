using System.Collections;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class Player : Entity
{
    private static readonly int IdleAnimHash = Animator.StringToHash("idle");
    private static readonly int MoveAnimHash = Animator.StringToHash("move");
    private static readonly int YVelocityAnimHash = Animator.StringToHash("yVelocity");
    private static readonly int JumpFallAnimHash = Animator.StringToHash("jumpFall");
    private static readonly int WallSlideAnimHash = Animator.StringToHash("wallSlide");
    private static readonly int DashAnimHash = Animator.StringToHash("dash");
    private static readonly int BasicAttackAnimHash = Animator.StringToHash("basicAttack");
    private static readonly int BasicAttackIndexAnimHash = Animator.StringToHash("basicAttackIndex");
    private static readonly int AirAttackIndexAnimHash = Animator.StringToHash("basicAttack_air_Index");
    private static readonly int FallAttackAnimHash = Animator.StringToHash("fallAttack");
    private static readonly int FallAttackFinishRequestedAnimHash = Animator.StringToHash("fallAttackFinishRequested");
    private static readonly int FallAttackTriggerAnimHash = Animator.StringToHash("fallAttackTrigger");
    private static readonly int CounterAttackAnimHash = Animator.StringToHash("counterAttack");
    private static readonly int CounterAttackPerformedAnimHash = Animator.StringToHash("counterAttackPerformed");
    private static readonly int DeadAnimHash = Animator.StringToHash("Dead");

    [Header("Move Info")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float airMoveSpeedMultiplier = .85f;

    [Header("Jump Info")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField, Min(.01f)] private float jumpHeadClearanceHeight = 2f;
    [SerializeField, Range(.1f, 1f)] private float jumpHeadClearanceWidthMultiplier = .9f;
    [SerializeField, Min(0f)] private float jumpHeadClearanceBottomOffset = .02f;

    [Header("Wall Slide Info")]
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private float wallSlideVisualOffset = .2f;
    [SerializeField] private float wallSlideNoInputDropTime = 0f;

    [Header("Wall Hold Info")]
    [SerializeField] private bool wallHoldEnabled = true;
    [SerializeField] private bool wallHoldHasDuration = true;
    [SerializeField] private float wallHoldDuration = 4f;

    [Header("Wall Jump Info")]
    [SerializeField] private Vector2 wallJumpForce = new Vector2(8f, 12f);
    [SerializeField] private float wallJumpDuration = .25f;
    [SerializeField] private float wallJumpInputGraceTime = .1f;
    [SerializeField] private bool wallJumpAirAttackEnabled = true;

    [Header("Dash Info")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = .2f;
    [SerializeField] private float dashCooldown = 3f;
    [SerializeField] private bool wallDashAwayFromWallEnabled = true;
    [SerializeField] private bool ignoreEnemyCollisionDuringDash = true;

    [Header("Basic Attack Info")]
    [SerializeField] private string[] basicAttackAnimationNames =
    {
        "playerBasicAttack_1",
        "playerBasicAttack_2",
        "playerBasicAttack_3"
    };
    [SerializeField] private string[] airAttackAnimationNames =
    {
        "playerBasicAttack_1_air",
        "playerBasicAttack_2_air",
        "playerBasicAttack_3_air"
    };
    [SerializeField] private float[] basicAttackAnimationSpeeds = { 1f, 1f, 1f };
    [SerializeField] private Vector2[] basicAttackMoveDistances =
    {
        new Vector2(.35f, 0f),
        new Vector2(.45f, 0f),
        new Vector2(.55f, 0f)
    };
    [SerializeField, HideInInspector] private Vector2[] basicAttackKnockbackForces =
    {
        new Vector2(4f, 2f),
        new Vector2(5f, 2.5f),
        new Vector2(7f, 3f)
    };
    [SerializeField] private Entity_AttackData[] basicAttackData =
    {
        new Entity_AttackData(new Vector2(.6f, 0f), .6f, new Vector2(4f, 2f)),
        new Entity_AttackData(new Vector2(.7f, 0f), .65f, new Vector2(5f, 2.5f)),
        new Entity_AttackData(new Vector2(.8f, 0f), .7f, new Vector2(7f, 3f))
    };
    [SerializeField] private int[] basicAttackDamages = { 12, 14, 18 };
    [SerializeField] private float[] basicAttackComboInputLeftWindows = { 10f, 10f, 10f };
    [SerializeField] private float[] basicAttackComboInputRightWindows = { 0f, 0f, 0f };
    [SerializeField] private float[] basicAttackTurnInputLeftWindows = { 10f, 10f, 10f };
    [SerializeField] private float[] basicAttackTurnInputRightWindows = { 0f, 0f, 0f };
    [SerializeField] private float basicAttackDashComboInputWindow = .25f;
    [SerializeField] private float basicAttackDashTurnInputWindow = .25f;
    [SerializeField] private float basicAttackLoopCooldown = .25f;
    [SerializeField] private float basicAttackMoveDuration = .12f;
    [SerializeField] private bool canMoveDuringBasicAttack = true;
    [SerializeField] private float basicAttackMoveSpeedMultiplier = .25f;

    [Header("Air Attack Info")]
    [SerializeField] private float[] airAttackAnimationSpeeds = { 1f, 1f, 1f };
    [SerializeField] private Vector2[] airAttackMoveDistances =
    {
        new Vector2(.35f, .12f),
        new Vector2(.45f, .12f),
        new Vector2(.55f, .12f)
    };
    [SerializeField, HideInInspector] private Vector2[] airAttackKnockbackForces =
    {
        new Vector2(4f, 1f),
        new Vector2(5f, 1.5f),
        new Vector2(7f, 2f)
    };
    [SerializeField] private Entity_AttackData[] airAttackData =
    {
        new Entity_AttackData(new Vector2(.6f, 0f), .6f, new Vector2(4f, 1f)),
        new Entity_AttackData(new Vector2(.7f, 0f), .65f, new Vector2(5f, 1.5f)),
        new Entity_AttackData(new Vector2(.8f, 0f), .7f, new Vector2(7f, 2f))
    };
    [SerializeField] private int[] airAttackDamages = { 10, 12, 16 };
    [SerializeField] private float[] airAttackComboInputLeftWindows = { 10f, 10f, 10f };
    [SerializeField] private float[] airAttackComboInputRightWindows = { 0f, 0f, 0f };
    [SerializeField] private float[] airAttackTurnInputLeftWindows = { 10f, 10f, 10f };
    [SerializeField] private float[] airAttackTurnInputRightWindows = { 0f, 0f, 0f };
    [SerializeField] private float airAttackDashComboInputWindow = .25f;
    [SerializeField] private float airAttackDashTurnInputWindow = .25f;
    [SerializeField] private float airAttackComboGroundBlockDistance = .2f;
    [SerializeField] private float airAttackMoveDuration = .12f;
    [SerializeField] private float airAttackFallSpeed = 1.5f;

    [Header("Fall Attack Info")]
    [SerializeField] private string fallAttackStartAnimationName = "playerFallAttack";
    [SerializeField] private string fallAttackPerformed1AnimationName = "playerFallAttack_performed1";
    [SerializeField] private string fallAttackPerformed2AnimationName = "playerFallAttack_performed2";
    [SerializeField] private float fallAttackAnimationSpeed = 1f;
    [SerializeField] private float fallAttackWindupDuration = .35f;
    [SerializeField] private float fallAttackGravityMultiplier = 1f;
    [SerializeField] private float fallAttackDiveSpeed = 14f;
    [SerializeField] private float fallAttackDiveAngle = 25f;
    [SerializeField] private float fallAttackGroundCheckDistance = 1.5f;
    [SerializeField] private float fallAttackGroundSearchDistance = 30f;
    [SerializeField, Min(0f)] private float fallAttackDamageWindowDuration = .12f;
    [SerializeField] private float fallAttackEndAnimationMinSpeed = .05f;
    [SerializeField] private float fallAttackEndAnimationMaxSpeed = 8f;
    [SerializeField] private float fallAttackEndAnimationLandingOffset = .15f;
    [SerializeField] private Entity_AttackData fallAttackData = new Entity_AttackData(new Vector2(.6f, -.2f), .7f, new Vector2(6f, 3f));
    [SerializeField] private Entity_AttackData fallAttackExtendedData = new Entity_AttackData(new Vector2(.6f, -.2f), 1.1f, new Vector2(6f, 3f));
    [SerializeField] private int fallAttackDamage = 22;

    [Header("Damage Override Info")]
    [SerializeField, Tooltip("When enabled, every player attack deals 999 damage.")]
    private bool forceAllAttackDamageTo999;
    [SerializeField, Tooltip("When enabled, every player attack deals 1 damage.")]
    private bool forceAllAttackDamageTo1;

    [Header("Counter Attack Info")]
    [SerializeField, Min(0f)] private float counterDuration = .35f;
    [SerializeField, Min(.01f)] private float counterAttackTargetCheckRadiusMultiplier = 1.35f;
    [SerializeField] private string counterAttackAnimationState = "playerCounterAttack";
    [SerializeField] private string counterAttackPerformedAnimationState = "playerCounterAttack_performed";

    [Header("Stamina Info")]
    [SerializeField, Min(1f)] private float maxStamina = 200f;
    [SerializeField, Min(0f)] private float staminaRecoveryPerSecond = 45f;
    [SerializeField, Min(0f)] private float staminaEmptyRecoveryDelay = 1f;
    [SerializeField, Min(0f)] private float jumpStaminaCost = 20f;
    [SerializeField, Min(0f)] private float jumpStaminaRecoveryDelay = .15f;
    [SerializeField, Min(0f)] private float nonCombatJumpStaminaCost = 16f;
    [SerializeField, Min(0f)] private float nonCombatJumpStaminaRecoveryDelay = 0f;
    [SerializeField, Min(0f)] private float wallJumpStaminaCost = 20f;
    [SerializeField, Min(0f)] private float wallJumpStaminaRecoveryDelay = .15f;
    [SerializeField, Min(0f)] private float nonCombatWallJumpStaminaCost = 16f;
    [SerializeField, Min(0f)] private float nonCombatWallJumpStaminaRecoveryDelay = 0f;
    [SerializeField, Min(0f)] private float dashStaminaCost = 25f;
    [SerializeField, Min(0f)] private float dashStaminaRecoveryDelay = .25f;
    [SerializeField, Min(0f)] private float nonCombatDashStaminaCost = 21f;
    [SerializeField, Min(0f)] private float nonCombatDashStaminaRecoveryDelay = 0f;
    [SerializeField, Min(0f)] private float counterAttackStaminaCost = 15f;
    [SerializeField, Min(0f)] private float counterAttackSuccessStaminaCost = 20f;
    [SerializeField, Min(0f)] private float projectileBlockStaminaCost = 40f;
    [SerializeField, Min(0f)] private float counterAttackStaminaRecoveryDelay = .6f;
    [SerializeField, Min(0f)] private float wallHoldStaminaDrainPerSecond = 14f;
    [SerializeField, Min(0f)] private float wallSlideStaminaDrainPerSecond = 8f;
    [SerializeField, Min(0f)] private float wallContactStaminaRecoveryDelay = .45f;
    [SerializeField] private float[] basicAttackStaminaCosts = { 24f, 28f, 36f };
    [SerializeField] private float[] airAttackStaminaCosts = { 28f, 32f, 40f };
    [SerializeField, Min(0f)] private float attackStaminaRecoveryDelay = .45f;
    [SerializeField, ReadOnlyField] private float currentStamina;

    [Header("Healing Potion Info")]
    [SerializeField, Min(0)] private int maxHealingPotionCount = 5;
    [SerializeField, Min(0)] private int currentHealingPotionCount = 5;
    [SerializeField, Min(0f)] private float healingPotionHealAmount = 35f;
    [SerializeField, Min(0.01f)] private float healingPotionUseDuration = 2.5f;
    [SerializeField, Range(.1f, 1f)] private float healingPotionMoveSpeedMultiplier = .45f;
    [SerializeField] private Sprite healingPotionWorldIconSprite;
    [SerializeField] private Vector3 healingPotionWorldIconOffset = new Vector3(0f, 1.85f, 0f);

    [Header("Death Info")]
    [SerializeField, Min(0f)] private float deathGroundVisualDownOffset = .08f;
    [SerializeField, Min(0f)] private float deathGroundVisualBottomPadding = .02f;
    [SerializeField] private CapsuleCollider2D deathCollider;

    private Transform visualTransform;
    private Vector3 defaultVisualLocalPosition;
    private Vector2 defaultColliderSize;
    private Vector2 defaultColliderOffset;
    private CapsuleDirection2D defaultColliderDirection;
    private RigidbodyType2D defaultBodyType;
    private RigidbodyConstraints2D defaultBodyConstraints;
    private float defaultGravityScale;
    private bool applyWallSlideVisualOffset;
    private bool applyDeathGroundVisualOffset;
    private bool canWallHold = true;
    private bool canFallAttack = true;
    private bool canAirAttack = true;
    private float wallJumpAirAttackWindowTimer;
    private float dashCooldownTimer;
    private bool useDashDirectionOverride;
    private int dashDirectionOverride;
    private int pendingBasicAttackComboIndex = -1;
    private int pendingBasicAttackDirection;
    private float pendingBasicAttackComboTimer;
    private float pendingBasicAttackTurnTimer;
    private float basicAttackLoopCooldownTimer;
    private bool hasBasicAttackLoopRestartRequest;
    private int basicAttackLoopRestartDirection;
    private int pendingAirAttackComboIndex = -1;
    private int pendingAirAttackDirection;
    private float pendingAirAttackComboTimer;
    private float pendingAirAttackTurnTimer;
    private bool hasBasicAttackComboAfterDash;
    private int basicAttackComboAfterDashIndex;
    private int basicAttackComboAfterDashDirection;
    private bool hasAirAttackComboAfterDash;
    private int airAttackComboAfterDashIndex;
    private int airAttackComboAfterDashDirection;
    private bool dashEnemyCollisionIgnoreActive;
    private bool previousPlayerEnemyLayerIgnore;
    private Entity_Health health;
    private Entity_Combat combat;
    private float staminaRecoveryTimer;
    private float healingPotionUseTimer;
    private bool healingPotionInUse;
    private Vector3 lastGroundedSafePosition;
    private bool hasLastGroundedSafePosition;
    private bool hazardRecoveryActive;
    private Vector3 pendingHazardRecoveryPosition;
    private bool hasPendingHazardRecoveryPosition;
    private GameObject healingPotionWorldIconObject;
    private PlayerHealingPotionWorldIcon healingPotionWorldIcon;
    private bool movementLocked;
    private bool bossIntroMoveActive;
    private int bossIntroMoveDirection = 1;
    private float bossIntroMoveSpeedMultiplier = 1f;
    private float bossIntroMoveTargetX;

    public event Action<Player> OnStaminaChanged;
    public event Action<Player> OnHealingPotionChanged;

    // Input
    public PlayerInputSet input { get; private set; }

    // State Machine
    public StateMachine stateMachine { get; private set; }

    // States
    public Player_IdleState idleState { get; private set; }
    public Player_MoveState moveState { get; private set; }
    public Player_JumpState jumpState { get; private set; }
    public Player_FallState fallState { get; private set; }
    public Player_WallSlideState wallSlideState { get; private set; }
    public Player_WallHoldState wallHoldState { get; private set; }
    public Player_WallJumpState wallJumpState { get; private set; }
    public Player_DashState dashState { get; private set; }
    public Player_BasicAttackState basicAttackState { get; private set; }
    public Player_AirAttackState airAttackState { get; private set; }
    public Player_FallAttackState fallAttackState { get; private set; }
    public Player_CounterAttackState counterAttackState { get; private set; }
    public Player_DeadState deadState { get; private set; }

    // Input
    public Vector2 moveInput { get; private set; }
    private bool wasDownInputHeldLastFrame;
    private bool downInputPressedThisFrame;
    private bool healingPotionPressedThisFrame;
    public float MoveSpeed => moveSpeed * HealingPotionMoveSpeedMultiplier * bossIntroMoveSpeedMultiplier;
    public float AirMoveSpeed => moveSpeed * airMoveSpeedMultiplier * HealingPotionMoveSpeedMultiplier * bossIntroMoveSpeedMultiplier;
    public float JumpHeadClearanceHeight => jumpHeadClearanceHeight;
    public float JumpHeadClearanceWidthMultiplier => jumpHeadClearanceWidthMultiplier;
    public float JumpHeadClearanceBottomOffset => jumpHeadClearanceBottomOffset;
    public float WallSlideSpeed => wallSlideSpeed;
    public float WallSlideNoInputDropTime => wallSlideNoInputDropTime;
    public bool WallHoldHasDuration => wallHoldHasDuration;
    public float WallHoldDuration => wallHoldDuration;
    public bool CanWallHold => wallHoldEnabled && canWallHold;
    public bool CanFallAttack => canFallAttack;
    public bool CanAirAttack => canAirAttack;
    public bool IsWallSlideDropLocked { get; private set; }
    public Vector2 WallJumpForce => wallJumpForce;
    public float WallJumpDuration => wallJumpDuration;
    public float WallJumpInputGraceTime => wallJumpInputGraceTime;
    public bool WallJumpAirAttackEnabled => wallJumpAirAttackEnabled;
    public bool HasWallJumpAirAttackWindow => wallJumpAirAttackWindowTimer > 0f;
    public float DashDistance => dashDistance;
    public float DashDuration => dashDuration;
    public float DashSpeed => dashDistance / dashDuration;
    public bool CanDash => dashCooldownTimer <= 0f;
    public float DashCooldownDuration => dashCooldown;
    public float DashCooldownRemaining => Mathf.Max(0f, dashCooldownTimer);
    public float DashCooldownNormalized => dashCooldown <= 0f ? 0f : Mathf.Clamp01(dashCooldownTimer / dashCooldown);
    public bool WallDashAwayFromWallEnabled => wallDashAwayFromWallEnabled;
    public bool IgnoreEnemyCollisionDuringDash => ignoreEnemyCollisionDuringDash;
    public float BasicAttackMoveDuration => basicAttackMoveDuration;
    public bool IsBasicAttackLoopCooldownActive => basicAttackLoopCooldownTimer > 0f;
    public bool HasBasicAttackLoopRestartRequest => hasBasicAttackLoopRestartRequest
        && basicAttackLoopCooldownTimer <= 0f
        && AttackInputHeld();
    public int PendingBasicAttackLoopRestartDirection => basicAttackLoopRestartDirection;
    public bool CanMoveDuringBasicAttack => canMoveDuringBasicAttack;
    public float BasicAttackMoveSpeedMultiplier => basicAttackMoveSpeedMultiplier;
    public float BasicAttackDashComboInputWindow => basicAttackDashComboInputWindow;
    public float BasicAttackDashTurnInputWindow => basicAttackDashTurnInputWindow;
    public int BasicAttackCount => Mathf.Max(1, basicAttackAnimationNames?.Length ?? 0);
    public int AirAttackCount => Mathf.Max(1, airAttackAnimationNames?.Length ?? 0);
    public float AirAttackMoveDuration => airAttackMoveDuration;
    public float AirAttackFallSpeed => airAttackFallSpeed;
    public float AirAttackDashComboInputWindow => airAttackDashComboInputWindow;
    public float AirAttackDashTurnInputWindow => airAttackDashTurnInputWindow;
    public float AirAttackComboGroundBlockDistance => airAttackComboGroundBlockDistance;
    public bool HasBasicAttackComboWindow => pendingBasicAttackComboIndex >= 0 && pendingBasicAttackComboTimer > 0f;
    public bool HasAirAttackComboWindow => pendingAirAttackComboIndex >= 0 && pendingAirAttackComboTimer > 0f;
    public int PendingBasicAttackComboIndex => pendingBasicAttackComboIndex;
    public int PendingAirAttackComboIndex => pendingAirAttackComboIndex;
    public string FallAttackStartAnimationName => fallAttackStartAnimationName;
    public string FallAttackPerformed1AnimationName => fallAttackPerformed1AnimationName;
    public string FallAttackPerformed2AnimationName => fallAttackPerformed2AnimationName;
    public float FallAttackAnimationSpeed => fallAttackAnimationSpeed;
    public float FallAttackWindupDuration => fallAttackWindupDuration;
    public float FallAttackGravityMultiplier => fallAttackGravityMultiplier;
    public float FallAttackDiveSpeed => fallAttackDiveSpeed;
    public float FallAttackDiveAngle => fallAttackDiveAngle;
    public float FallAttackGroundCheckDistance => fallAttackGroundCheckDistance;
    public float FallAttackGroundSearchDistance => fallAttackGroundSearchDistance;
    public float FallAttackDamageWindowDuration => fallAttackDamageWindowDuration;
    public float FallAttackEndAnimationMinSpeed => fallAttackEndAnimationMinSpeed;
    public float FallAttackEndAnimationMaxSpeed => fallAttackEndAnimationMaxSpeed;
    public float FallAttackEndAnimationLandingOffset => fallAttackEndAnimationLandingOffset;
    public Entity_AttackData FallAttackData => fallAttackData;
    public Entity_AttackData FallAttackExtendedData => fallAttackExtendedData;
    public int FallAttackDamage => ApplyAttackDamageOverride(Mathf.Max(1, fallAttackDamage));
    public float CounterDuration => counterDuration;
    public float CounterAttackTargetCheckRadiusMultiplier => counterAttackTargetCheckRadiusMultiplier;
    public string CounterAttackAnimationState => counterAttackAnimationState;
    public string CounterAttackPerformedAnimationState => counterAttackPerformedAnimationState;
    public int MaxHealingPotionCount => maxHealingPotionCount;
    public int CurrentHealingPotionCount => currentHealingPotionCount;
    public float HealingPotionHealAmount => healingPotionHealAmount;
    public float HealingPotionUseDuration => healingPotionUseDuration;
    public float HealingPotionUseRemaining => Mathf.Max(0f, healingPotionUseTimer);
    public float HealingPotionUseNormalized => healingPotionUseDuration <= 0f ? 0f : Mathf.Clamp01(healingPotionUseTimer / healingPotionUseDuration);
    public float HealingPotionMoveSpeedMultiplier => healingPotionInUse ? healingPotionMoveSpeedMultiplier : 1f;
    public bool IsHealingPotionInUse => healingPotionInUse;
    public bool CanUseHealingPotion => currentHealingPotionCount > 0
        && !healingPotionInUse
        && !IsDead
        && !hazardRecoveryActive;
    public Sprite HealingPotionWorldIconSprite => healingPotionWorldIconSprite;
    public Vector3 HealingPotionWorldIconOffset => healingPotionWorldIconOffset;

    public bool PlayPlayerCombatAudio(AudioKey audioKey)
    {
        if (AudioManager.instance == null)
        {
            return false;
        }

        AudioManager.instance.PlayGlobalSFX(audioKey);
        return true;
    }
    public void SetHealingPotionWorldIconSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        if (healingPotionWorldIconSprite == sprite)
        {
            return;
        }

        healingPotionWorldIconSprite = sprite;
        if (healingPotionWorldIcon != null)
        {
            healingPotionWorldIcon.SetSprite(sprite);
        }
    }

    public void SetMovementLocked(bool locked)
    {
        if (movementLocked == locked)
        {
            return;
        }

        movementLocked = locked;
        if (movementLocked)
        {
            moveInput = Vector2.zero;
            if (rb != null)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
            }
        }
    }

    public float MovementInputX => bossIntroMoveActive ? bossIntroMoveDirection : moveInput.x;

    public void BeginBossIntroMoveTowards(float targetX, float distance, float speedMultiplier = 1f)
    {
        if (rb == null || stateMachine == null || moveState == null)
        {
            return;
        }

        bossIntroMoveActive = true;
        bossIntroMoveDirection = targetX >= rb.position.x ? 1 : -1;
        bossIntroMoveSpeedMultiplier = Mathf.Max(0.01f, speedMultiplier);
        bossIntroMoveTargetX = rb.position.x + bossIntroMoveDirection * Mathf.Max(0f, distance);

        SetMovementLocked(true);
        stateMachine.ChangeState(moveState);
    }

    public void CancelBossIntroMove()
    {
        bossIntroMoveActive = false;
        bossIntroMoveDirection = 1;
        bossIntroMoveSpeedMultiplier = 1f;
        bossIntroMoveTargetX = 0f;
    }

    public float MaxStamina => maxStamina;
    public float CurrentStamina => currentStamina;
    public int CurrentStaminaRounded => Mathf.RoundToInt(currentStamina);
    public bool HasStamina => currentStamina > 0f;
    public bool IsCombatStaminaContext()
    {
        return stateMachine?.CurrentState == basicAttackState
            || stateMachine?.CurrentState == airAttackState
            || stateMachine?.CurrentState == fallAttackState
            || stateMachine?.CurrentState == counterAttackState
            || (combat != null && combat.HasTarget());
    }
    public bool IsCounterAttacking => stateMachine?.CurrentState == counterAttackState;
    public float DeathGroundVisualDownOffset => deathGroundVisualDownOffset;
    public float DeathGroundVisualBottomPadding => deathGroundVisualBottomPadding;
    public float DefaultGravityScale => defaultGravityScale;
    public CapsuleCollider2D DeathCollider => deathCollider;
    public bool IsDead => health != null && health.IsDead;
    public bool IsHazardRecoveryActive => hazardRecoveryActive;
    public bool IsMovementLocked => movementLocked;

    protected override void Awake()
    {
        base.Awake();

        visualTransform = anim.transform;
        defaultVisualLocalPosition = visualTransform.localPosition;
        if (cd != null)
        {
            defaultColliderSize = cd.size;
            defaultColliderOffset = cd.offset;
            defaultColliderDirection = cd.direction;
        }
        CacheDeathColliderReference();
        RestoreAliveColliderProfile();
        defaultBodyType = rb != null ? rb.bodyType : RigidbodyType2D.Dynamic;
        defaultBodyConstraints = rb != null ? rb.constraints : RigidbodyConstraints2D.FreezeRotation;
        defaultGravityScale = rb != null ? rb.gravityScale : 1f;
        currentStamina = maxStamina;
        currentHealingPotionCount = Mathf.Clamp(currentHealingPotionCount, 0, Mathf.Max(0, maxHealingPotionCount));
        healingPotionUseTimer = 0f;
        healingPotionInUse = false;
        combat = GetComponent<Entity_Combat>();
        combat?.SetDamage(GetBasicAttackDamage(0));

        input = new PlayerInputSet();

        stateMachine = new StateMachine();

        idleState = new Player_IdleState(this, stateMachine);
        moveState = new Player_MoveState(this, stateMachine);
        jumpState = new Player_JumpState(this, stateMachine);
        fallState = new Player_FallState(this, stateMachine);
        wallSlideState = new Player_WallSlideState(this, stateMachine);
        wallHoldState = new Player_WallHoldState(this, stateMachine);
        wallJumpState = new Player_WallJumpState(this, stateMachine);
        dashState = new Player_DashState(this, stateMachine);
        basicAttackState = new Player_BasicAttackState(this, stateMachine);
        airAttackState = new Player_AirAttackState(this, stateMachine);
        fallAttackState = new Player_FallAttackState(this, stateMachine);
        counterAttackState = new Player_CounterAttackState(this, stateMachine);
        deadState = new Player_DeadState(this, stateMachine);
    }

    protected override void Reset()
    {
        base.Reset();
    }

    private void OnEnable()
    {
        input?.Enable();
        if (input != null)
        {
            input.Player.UsePotion.started += HandleUsePotionPerformed;
            input.Player.UsePotion.performed += HandleUsePotionPerformed;
        }
    }

    private void OnDisable()
    {
        EndDashEnemyCollisionIgnore();
        CancelBossIntroMove();
        if (input != null)
        {
            input.Player.UsePotion.started -= HandleUsePotionPerformed;
            input.Player.UsePotion.performed -= HandleUsePotionPerformed;
        }
        input?.Disable();
    }

    private void Start()
    {
        health = GetComponent<Entity_Health>();
        if (health != null)
        {
            health.OnDamaged += HandleHealthDamaged;
        }

        RestoreStaminaToFull();
        combat?.SetDamage(GetBasicAttackDamage(0));

        if (IsDead)
        {
            SetDeadPlayerBodyLayer();
        }

        if (stateMachine.CurrentState == null)
        {
            stateMachine.Initialize(IsDead && deadState != null ? deadState : idleState);
        }

        UpdateLastGroundedSafePosition();
        EnsureHealingPotionWorldIcon();
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleHealthDamaged;
        }

        if (healingPotionWorldIconObject != null)
        {
            Destroy(healingPotionWorldIconObject);
            healingPotionWorldIconObject = null;
            healingPotionWorldIcon = null;
        }
    }

    private void Update()
    {
        if (IsDead && stateMachine.CurrentState != deadState)
        {
            EnterDeadState();
        }

        if (IsDead)
        {
            stateMachine.CurrentState?.Update();
            wasDownInputHeldLastFrame = DownInputHeld();
            return;
        }

        if (hazardRecoveryActive)
        {
            wasDownInputHeldLastFrame = DownInputHeld();
            return;
        }

        moveInput = movementLocked ? Vector2.zero : input.Player.Movement.ReadValue<Vector2>();
        UpdateDownInputPressedThisFrame();
        if (HealingPotionInputPressed())
        {
            TryUseHealingPotion();
        }
        UpdateHealingPotionUseState();
        UpdateDashCooldownTimer();
        UpdateWallJumpAirAttackWindowTimer();
        UpdateBasicAttackLoopCooldownTimer();
        UpdateBasicAttackComboTimer();
        UpdateAirAttackComboTimer();
        UpdateStaminaRecoveryTimer();
        UpdateStaminaRecovery();
        UpdateWallContactStaminaDrain();

        if (bossIntroMoveActive && stateMachine.CurrentState != moveState)
        {
            stateMachine.ChangeState(moveState);
        }

        TryEnterCounterAttackState();

        stateMachine.CurrentState?.Update();

        wasDownInputHeldLastFrame = DownInputHeld();
    }

    private void FixedUpdate()
    {
        if (IsDead)
        {
            stateMachine.CurrentState?.FixedUpdate();
            return;
        }

        if (hazardRecoveryActive)
        {
            return;
        }

        stateMachine.CurrentState?.FixedUpdate();

        if (bossIntroMoveActive && rb != null)
        {
            bool reachedTarget = bossIntroMoveDirection > 0
                ? rb.position.x >= bossIntroMoveTargetX
                : rb.position.x <= bossIntroMoveTargetX;

            if (reachedTarget)
            {
                rb.position = new Vector2(bossIntroMoveTargetX, rb.position.y);
                CancelBossIntroMove();
                if (stateMachine != null && idleState != null)
                {
                    stateMachine.ChangeState(idleState);
                }
            }
        }

        if (movementLocked && !bossIntroMoveActive && rb != null)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        UpdateLastGroundedSafePosition();
    }

    private void LateUpdate()
    {
        UpdateVisualPosition();
    }

    private bool TryEnterCounterAttackState()
    {
        if (counterAttackState == null
            || movementLocked
            || !CounterInputPressed()
            || stateMachine.CurrentState == counterAttackState
            || stateMachine.CurrentState == deadState
            || stateMachine.CurrentState == dashState)
        {
            return false;
        }

        TryConsumeCounterAttackStamina();
        stateMachine.ChangeState(counterAttackState);
        return true;
    }

    // Jump
    public void Jump()
    {
        SetVelocity(rb.velocity.x, jumpForce);
    }

    public bool TryConsumeJumpStamina()
    {
        if (IsCombatStaminaContext())
        {
            ConsumeStamina(jumpStaminaCost, jumpStaminaRecoveryDelay);
        }
        else
        {
            ConsumeStamina(nonCombatJumpStaminaCost, nonCombatJumpStaminaRecoveryDelay);
        }

        return true;
    }

    public bool TryConsumeWallJumpStamina()
    {
        if (IsCombatStaminaContext())
        {
            ConsumeStamina(wallJumpStaminaCost, wallJumpStaminaRecoveryDelay);
        }
        else
        {
            ConsumeStamina(nonCombatWallJumpStaminaCost, nonCombatWallJumpStaminaRecoveryDelay);
        }

        return true;
    }

    public bool TryConsumeDashStamina()
    {
        if (IsCombatStaminaContext())
        {
            ConsumeStamina(dashStaminaCost, dashStaminaRecoveryDelay);
        }
        else
        {
            ConsumeStamina(nonCombatDashStaminaCost, nonCombatDashStaminaRecoveryDelay);
        }

        return true;
    }

    public bool TryConsumeCounterAttackStamina()
    {
        ConsumeStamina(counterAttackStaminaCost, counterAttackStaminaRecoveryDelay);
        return true;
    }

    public bool TryConsumeCounterAttackSuccessStamina()
    {
        ConsumeStamina(counterAttackSuccessStaminaCost, counterAttackStaminaRecoveryDelay);
        PlayCounterSuccessSfx();
        return true;
    }

    public bool TryConsumeProjectileBlockStamina()
    {
        if (projectileBlockStaminaCost <= 0f)
        {
            return true;
        }

        if (currentStamina + Mathf.Epsilon < projectileBlockStaminaCost)
        {
            return false;
        }

        ConsumeStamina(projectileBlockStaminaCost, counterAttackStaminaRecoveryDelay);
        PlayCounterSuccessSfx();
        return true;
    }

    private void PlayCounterSuccessSfx()
    {
        PlayPlayerCombatAudio(AudioKey.PlayerCounterSuccess);
    }

    public bool TryConsumeBasicAttackStamina(int attackIndex)
    {
        ConsumeStamina(GetBasicAttackStaminaCost(attackIndex), attackStaminaRecoveryDelay);
        return true;
    }

    public bool TryConsumeAirAttackStamina(int attackIndex)
    {
        ConsumeStamina(GetAirAttackStaminaCost(attackIndex), attackStaminaRecoveryDelay);
        return true;
    }

    public bool TryGetGroundDistanceFromColliderCenter(float checkDistance, out float groundDistance)
    {
        CapsuleCollider2D activeCollider = GetActiveCollider();
        Vector2 origin = activeCollider != null ? (Vector2)activeCollider.bounds.center : (Vector2)transform.position;

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            Vector2.down,
            checkDistance,
            whatIsGround
        );

        groundDistance = hit.collider != null ? hit.distance : 0f;
        return hit.collider != null;
    }

    public bool JumpInputPressed()
    {
        return input.Player.Jump.triggered
            || input.Player.Jump.WasPressedThisFrame();
    }

    public bool JumpInputHeld()
    {
        return input.Player.Jump.IsPressed();
    }

    public bool DashInputPressed()
    {
        return input.Player.Dash.triggered
            || input.Player.Dash.WasPressedThisFrame();
    }

    public bool AttackInputPressed()
    {
        return input.Player.Attack.triggered
            || input.Player.Attack.WasPressedThisFrame();
    }

    public bool AttackInputHeld()
    {
        return input.Player.Attack.IsPressed();
    }

    public bool CounterInputPressed()
    {
        return Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
    }

    public bool HealingPotionInputPressed()
    {
        bool pressed = healingPotionPressedThisFrame
            || (input != null && (input.Player.UsePotion.triggered || input.Player.UsePotion.WasPressedThisFrame()));

        if (!pressed)
        {
            return false;
        }

        healingPotionPressedThisFrame = false;
        return true;
    }

    public bool DownInputHeld()
    {
        return moveInput.y < -.5f;
    }

    public bool DownInputPressed()
    {
        return downInputPressedThisFrame;
    }

    public string GetBasicAttackAnimationName(int attackIndex)
    {
        if (basicAttackAnimationNames == null
            || attackIndex < 0
            || attackIndex >= basicAttackAnimationNames.Length
            || string.IsNullOrWhiteSpace(basicAttackAnimationNames[attackIndex]))
        {
            return $"playerBasicAttack_{attackIndex + 1}";
        }

        return basicAttackAnimationNames[attackIndex];
    }

    public string GetAirAttackAnimationName(int attackIndex)
    {
        if (airAttackAnimationNames == null
            || attackIndex < 0
            || attackIndex >= airAttackAnimationNames.Length
            || string.IsNullOrWhiteSpace(airAttackAnimationNames[attackIndex]))
        {
            return $"{GetBasicAttackAnimationName(attackIndex)}_air";
        }

        return airAttackAnimationNames[attackIndex];
    }

    public float GetBasicAttackAnimationSpeed(int attackIndex)
    {
        if (basicAttackAnimationSpeeds == null
            || attackIndex < 0
            || attackIndex >= basicAttackAnimationSpeeds.Length)
        {
            return 1f;
        }

        return Mathf.Max(.01f, basicAttackAnimationSpeeds[attackIndex]);
    }

    public float GetBasicAttackStaminaCost(int attackIndex)
    {
        if (basicAttackStaminaCosts == null
            || attackIndex < 0
            || attackIndex >= basicAttackStaminaCosts.Length)
        {
            return 0f;
        }

        return Mathf.Max(0f, basicAttackStaminaCosts[attackIndex]);
    }

    public float GetAirAttackAnimationSpeed(int attackIndex)
    {
        if (airAttackAnimationSpeeds == null
            || attackIndex < 0
            || attackIndex >= airAttackAnimationSpeeds.Length)
        {
            return GetBasicAttackAnimationSpeed(attackIndex);
        }

        return Mathf.Max(.01f, airAttackAnimationSpeeds[attackIndex]);
    }

    public float GetAirAttackStaminaCost(int attackIndex)
    {
        if (airAttackStaminaCosts == null
            || attackIndex < 0
            || attackIndex >= airAttackStaminaCosts.Length)
        {
            return 0f;
        }

        return Mathf.Max(0f, airAttackStaminaCosts[attackIndex]);
    }

    public Vector2 GetBasicAttackMoveDistance(int attackIndex)
    {
        if (basicAttackMoveDistances == null
            || attackIndex < 0
            || attackIndex >= basicAttackMoveDistances.Length)
        {
            return Vector2.zero;
        }

        return basicAttackMoveDistances[attackIndex];
    }

    public Vector2 GetAirAttackMoveDistance(int attackIndex)
    {
        if (airAttackMoveDistances == null
            || attackIndex < 0
            || attackIndex >= airAttackMoveDistances.Length)
        {
            return GetBasicAttackMoveDistance(attackIndex);
        }

        return airAttackMoveDistances[attackIndex];
    }

    public Vector2 GetBasicAttackKnockbackForce(int attackIndex)
    {
        if (basicAttackKnockbackForces == null
            || attackIndex < 0
            || attackIndex >= basicAttackKnockbackForces.Length)
        {
            return Vector2.zero;
        }

        return basicAttackKnockbackForces[attackIndex];
    }

    public Vector2 GetAirAttackKnockbackForce(int attackIndex)
    {
        if (airAttackKnockbackForces == null
            || attackIndex < 0
            || attackIndex >= airAttackKnockbackForces.Length)
        {
            return GetBasicAttackKnockbackForce(attackIndex);
        }

        return airAttackKnockbackForces[attackIndex];
    }

    public Entity_AttackData GetBasicAttackData(int attackIndex)
    {
        if (basicAttackData == null
            || attackIndex < 0
            || attackIndex >= basicAttackData.Length)
        {
            return new Entity_AttackData(Vector2.zero, .6f, GetBasicAttackKnockbackForce(attackIndex));
        }

        return basicAttackData[attackIndex];
    }

    public int GetBasicAttackDamage(int attackIndex)
    {
        if (basicAttackDamages == null
            || attackIndex < 0
            || attackIndex >= basicAttackDamages.Length)
        {
            return ApplyAttackDamageOverride(12);
        }

        return ApplyAttackDamageOverride(Mathf.Max(1, basicAttackDamages[attackIndex]));
    }

    public Entity_AttackData GetAirAttackData(int attackIndex)
    {
        if (airAttackData == null
            || attackIndex < 0
            || attackIndex >= airAttackData.Length)
        {
            return new Entity_AttackData(Vector2.zero, .6f, GetAirAttackKnockbackForce(attackIndex));
        }

        return airAttackData[attackIndex];
    }

    public int GetAirAttackDamage(int attackIndex)
    {
        if (airAttackDamages == null
            || attackIndex < 0
            || attackIndex >= airAttackDamages.Length)
        {
            return ApplyAttackDamageOverride(10);
        }

        return ApplyAttackDamageOverride(Mathf.Max(1, airAttackDamages[attackIndex]));
    }

    public void SetCombatDamage(int damage)
    {
        combat ??= GetComponent<Entity_Combat>();
        combat?.SetDamage(ApplyAttackDamageOverride(damage));
    }

    private int ApplyAttackDamageOverride(int damage)
    {
        if (forceAllAttackDamageTo999)
        {
            return 999;
        }

        if (forceAllAttackDamageTo1)
        {
            return 1;
        }

        return Mathf.Max(1, damage);
    }

    public float GetBasicAttackComboInputLeftWindow(int attackIndex)
    {
        if (basicAttackComboInputLeftWindows == null
            || attackIndex < 0
            || attackIndex >= basicAttackComboInputLeftWindows.Length)
        {
            return 10f;
        }

        return Mathf.Max(0f, basicAttackComboInputLeftWindows[attackIndex]);
    }

    public float GetAirAttackComboInputLeftWindow(int attackIndex)
    {
        if (airAttackComboInputLeftWindows == null
            || attackIndex < 0
            || attackIndex >= airAttackComboInputLeftWindows.Length)
        {
            return GetBasicAttackComboInputLeftWindow(attackIndex);
        }

        return Mathf.Max(0f, airAttackComboInputLeftWindows[attackIndex]);
    }

    public float GetBasicAttackComboInputRightWindow(int attackIndex)
    {
        if (basicAttackComboInputRightWindows == null
            || attackIndex < 0
            || attackIndex >= basicAttackComboInputRightWindows.Length)
        {
            return 0f;
        }

        return Mathf.Max(0f, basicAttackComboInputRightWindows[attackIndex]);
    }

    public float GetAirAttackComboInputRightWindow(int attackIndex)
    {
        if (airAttackComboInputRightWindows == null
            || attackIndex < 0
            || attackIndex >= airAttackComboInputRightWindows.Length)
        {
            return GetBasicAttackComboInputRightWindow(attackIndex);
        }

        return Mathf.Max(0f, airAttackComboInputRightWindows[attackIndex]);
    }

    public float GetBasicAttackTurnInputLeftWindow(int attackIndex)
    {
        if (basicAttackTurnInputLeftWindows == null
            || attackIndex < 0
            || attackIndex >= basicAttackTurnInputLeftWindows.Length)
        {
            return GetBasicAttackComboInputLeftWindow(attackIndex);
        }

        return Mathf.Max(0f, basicAttackTurnInputLeftWindows[attackIndex]);
    }

    public float GetAirAttackTurnInputLeftWindow(int attackIndex)
    {
        if (airAttackTurnInputLeftWindows == null
            || attackIndex < 0
            || attackIndex >= airAttackTurnInputLeftWindows.Length)
        {
            return GetAirAttackComboInputLeftWindow(attackIndex);
        }

        return Mathf.Max(0f, airAttackTurnInputLeftWindows[attackIndex]);
    }

    public float GetBasicAttackTurnInputRightWindow(int attackIndex)
    {
        if (basicAttackTurnInputRightWindows == null
            || attackIndex < 0
            || attackIndex >= basicAttackTurnInputRightWindows.Length)
        {
            return GetBasicAttackComboInputRightWindow(attackIndex);
        }

        return Mathf.Max(0f, basicAttackTurnInputRightWindows[attackIndex]);
    }

    public float GetAirAttackTurnInputRightWindow(int attackIndex)
    {
        if (airAttackTurnInputRightWindows == null
            || attackIndex < 0
            || attackIndex >= airAttackTurnInputRightWindows.Length)
        {
            return GetAirAttackComboInputRightWindow(attackIndex);
        }

        return Mathf.Max(0f, airAttackTurnInputRightWindows[attackIndex]);
    }

    public float GetBasicAttackAnimationLength(int attackIndex)
    {
        RuntimeAnimatorController controller = anim.runtimeAnimatorController;

        if (controller == null)
        {
            return 0f;
        }

        string animationName = GetBasicAttackAnimationName(attackIndex);

        foreach (AnimationClip clip in controller.animationClips)
        {
            if (clip.name == animationName)
            {
                return clip.length / GetBasicAttackAnimationSpeed(attackIndex);
            }
        }

        return 0f;
    }

    public float GetAirAttackAnimationLength(int attackIndex)
    {
        RuntimeAnimatorController controller = anim.runtimeAnimatorController;

        if (controller == null)
        {
            return 0f;
        }

        string animationName = GetAirAttackAnimationName(attackIndex);

        foreach (AnimationClip clip in controller.animationClips)
        {
            if (clip.name == animationName)
            {
                return clip.length / GetAirAttackAnimationSpeed(attackIndex);
            }
        }

        return 0f;
    }

    public float GetAnimationLength(string animationName, float animationSpeed = 1f)
    {
        RuntimeAnimatorController controller = anim.runtimeAnimatorController;

        if (controller == null || string.IsNullOrWhiteSpace(animationName))
        {
            return 0f;
        }

        foreach (AnimationClip clip in controller.animationClips)
        {
            if (clip.name == animationName)
            {
                return clip.length / Mathf.Max(.01f, animationSpeed);
            }
        }

        return 0f;
    }

    public bool CanStartFallAttack()
    {
        return canFallAttack
            && !GroundDetected()
            && DownInputHeld()
            && !FallAttackGroundDetected();
    }

    public bool CanStartJump()
    {
        return GroundDetected()
            && HasJumpHeadClearance();
    }

    public bool HasJumpHeadClearance()
    {
        Bounds bounds = GetColliderBounds();
        Vector2 boxSize = GetJumpHeadClearanceBoxSize(bounds);
        Vector2 boxOrigin = GetJumpHeadClearanceBoxOrigin(bounds, boxSize);

        return Physics2D.OverlapBox(boxOrigin, boxSize, 0f, whatIsGround) == null;
    }

    public bool AirAttackComboGroundDetected()
    {
        return Physics2D.Raycast(
            GetGroundCheckRayOrigin(),
            Vector2.down,
            airAttackComboGroundBlockDistance,
            whatIsGround
        );
    }

    public bool FallAttackGroundDetected()
    {
        return Physics2D.Raycast(
            transform.position,
            Vector2.down,
            fallAttackGroundCheckDistance,
            whatIsGround
        );
    }

    private Vector2 GetJumpHeadClearanceBoxSize(Bounds bounds)
    {
        float width = Mathf.Max(.1f, bounds.size.x * jumpHeadClearanceWidthMultiplier);
        float height = Mathf.Max(.01f, jumpHeadClearanceHeight);

        return new Vector2(width, height);
    }

    private Vector2 GetJumpHeadClearanceBoxOrigin(Bounds bounds, Vector2 boxSize)
    {
        float bottom = bounds.min.y + jumpHeadClearanceBottomOffset;
        return new Vector2(bounds.center.x, bottom + boxSize.y * .5f);
    }

    public bool TryGetGroundDistance(float checkDistance, out float groundDistance)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            checkDistance,
            whatIsGround
        );

        groundDistance = hit.collider != null ? hit.distance : 0f;
        return hit.collider != null;
    }

    public void OpenBasicAttackComboWindow(int attackIndex, int attackDirection, float comboDuration, float turnDuration)
    {
        if (basicAttackLoopCooldownTimer > 0f
            || comboDuration <= 0f
            || attackIndex < 0
            || attackIndex >= BasicAttackCount)
        {
            return;
        }

        pendingBasicAttackComboIndex = attackIndex;
        pendingBasicAttackDirection = NormalizeDirection(attackDirection, facingDirection);
        pendingBasicAttackComboTimer = comboDuration;
        pendingBasicAttackTurnTimer = Mathf.Max(0f, turnDuration);

        UpdatePendingBasicAttackDirectionFromInput();
    }

    public void StartBasicAttackLoopCooldown(int attackDirection, bool queueRestart)
    {
        basicAttackLoopCooldownTimer = basicAttackLoopCooldown;
        hasBasicAttackLoopRestartRequest = queueRestart || AttackInputHeld();
        basicAttackLoopRestartDirection = NormalizeDirection(attackDirection, facingDirection);
        ClearBasicAttackComboWindow();
        ClearBasicAttackComboAfterDash();
    }

    public void OpenAirAttackComboWindow(int attackIndex, int attackDirection, float comboDuration, float turnDuration)
    {
        if (comboDuration <= 0f || attackIndex < 0 || attackIndex >= AirAttackCount)
        {
            return;
        }

        pendingAirAttackComboIndex = attackIndex;
        pendingAirAttackDirection = NormalizeDirection(attackDirection, facingDirection);
        pendingAirAttackComboTimer = comboDuration;
        pendingAirAttackTurnTimer = Mathf.Max(0f, turnDuration);

        UpdatePendingAirAttackDirectionFromInput();
    }

    public void QueueBasicAttackComboAfterDash(int attackIndex, int attackDirection)
    {
        if (attackIndex < 0 || attackIndex >= BasicAttackCount)
        {
            return;
        }

        hasBasicAttackComboAfterDash = true;
        basicAttackComboAfterDashIndex = attackIndex;
        basicAttackComboAfterDashDirection = NormalizeDirection(attackDirection, facingDirection);
    }

    public void OpenQueuedBasicAttackComboAfterDash()
    {
        if (!hasBasicAttackComboAfterDash)
        {
            return;
        }

        OpenBasicAttackComboWindow(
            basicAttackComboAfterDashIndex,
            basicAttackComboAfterDashDirection,
            basicAttackDashComboInputWindow,
            basicAttackDashTurnInputWindow
        );

        ClearBasicAttackComboAfterDash();
    }

    public void ClearBasicAttackComboAfterDash()
    {
        hasBasicAttackComboAfterDash = false;
        basicAttackComboAfterDashIndex = 0;
        basicAttackComboAfterDashDirection = 0;
    }

    public void QueueAirAttackComboAfterDash(int attackIndex, int attackDirection)
    {
        if (attackIndex < 0 || attackIndex >= AirAttackCount)
        {
            return;
        }

        hasAirAttackComboAfterDash = true;
        airAttackComboAfterDashIndex = attackIndex;
        airAttackComboAfterDashDirection = NormalizeDirection(attackDirection, facingDirection);
    }

    public void OpenQueuedAirAttackComboAfterDash()
    {
        if (!hasAirAttackComboAfterDash)
        {
            return;
        }

        OpenAirAttackComboWindow(
            airAttackComboAfterDashIndex,
            airAttackComboAfterDashDirection,
            airAttackDashComboInputWindow,
            airAttackDashTurnInputWindow
        );

        ClearAirAttackComboAfterDash();
    }

    public void ClearAirAttackComboAfterDash()
    {
        hasAirAttackComboAfterDash = false;
        airAttackComboAfterDashIndex = 0;
        airAttackComboAfterDashDirection = 0;
    }

    public bool TryConsumeBasicAttackComboWindow(out int attackIndex, out int attackDirection)
    {
        attackIndex = pendingBasicAttackComboIndex;
        attackDirection = pendingBasicAttackDirection;

        if (pendingBasicAttackComboIndex < 0 || pendingBasicAttackComboTimer <= 0f)
        {
            ClearBasicAttackComboWindow();
            return false;
        }

        ClearBasicAttackComboWindow();
        return true;
    }

    public bool TryConsumeBasicAttackLoopRestartRequest(out int attackDirection)
    {
        attackDirection = basicAttackLoopRestartDirection;

        if (!HasBasicAttackLoopRestartRequest)
        {
            return false;
        }

        hasBasicAttackLoopRestartRequest = false;
        basicAttackLoopRestartDirection = 0;
        return true;
    }

    public bool TryConsumeAirAttackComboWindow(out int attackIndex, out int attackDirection)
    {
        attackIndex = pendingAirAttackComboIndex;
        attackDirection = pendingAirAttackDirection;

        if (pendingAirAttackComboIndex < 0 || pendingAirAttackComboTimer <= 0f)
        {
            ClearAirAttackComboWindow();
            return false;
        }

        ClearAirAttackComboWindow();
        return true;
    }

    public void ClearBasicAttackComboWindow()
    {
        pendingBasicAttackComboIndex = -1;
        pendingBasicAttackDirection = 0;
        pendingBasicAttackComboTimer = 0f;
        pendingBasicAttackTurnTimer = 0f;
    }

    public void ClearAirAttackComboWindow()
    {
        pendingAirAttackComboIndex = -1;
        pendingAirAttackDirection = 0;
        pendingAirAttackComboTimer = 0f;
        pendingAirAttackTurnTimer = 0f;
    }

    public void StartDashCooldown()
    {
        dashCooldownTimer = dashCooldown;
    }

    public void BeginDashEnemyCollisionIgnore()
    {
        if (!ignoreEnemyCollisionDuringDash || dashEnemyCollisionIgnoreActive)
        {
            return;
        }

        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        if (playerLayer < 0 || enemyLayer < 0)
        {
            return;
        }

        previousPlayerEnemyLayerIgnore = Physics2D.GetIgnoreLayerCollision(playerLayer, enemyLayer);
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        dashEnemyCollisionIgnoreActive = true;
    }

    public void EndDashEnemyCollisionIgnore()
    {
        if (!dashEnemyCollisionIgnoreActive)
        {
            return;
        }

        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        if (playerLayer >= 0 && enemyLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, previousPlayerEnemyLayerIgnore);
        }

        dashEnemyCollisionIgnoreActive = false;
    }

    public void SetDashDirectionOverride(int direction)
    {
        if (direction == 0)
        {
            return;
        }

        useDashDirectionOverride = true;
        dashDirectionOverride = direction > 0 ? 1 : -1;
    }

    public bool TryConsumeDashDirectionOverride(out int direction)
    {
        direction = dashDirectionOverride;

        if (!useDashDirectionOverride)
        {
            return false;
        }

        useDashDirectionOverride = false;
        dashDirectionOverride = 0;
        return true;
    }

    public void ResetWallHoldAvailability()
    {
        canWallHold = true;
    }

    public void ResetFallAttackAvailability()
    {
        canFallAttack = true;
    }

    public void ResetAirAttackAvailability()
    {
        canAirAttack = true;
    }

    public void DisableFallAttackUntilGrounded()
    {
        canFallAttack = false;
    }

    public void DisableAirAttackUntilGrounded()
    {
        canAirAttack = false;
    }

    public void OpenWallJumpAirAttackWindow()
    {
        if (!wallJumpAirAttackEnabled)
        {
            ClearWallJumpAirAttackWindow();
            DisableAirAttackUntilGrounded();
            return;
        }

        wallJumpAirAttackWindowTimer = wallJumpDuration;
    }

    public void ClearWallJumpAirAttackWindow()
    {
        wallJumpAirAttackWindowTimer = 0f;
    }

    public void DisableWallHoldUntilGrounded()
    {
        canWallHold = false;
    }

    public void RestoreStaminaToFull()
    {
        currentStamina = maxStamina;
        staminaRecoveryTimer = 0f;
        NotifyStaminaChanged();
    }

    public void RestoreHealthToFull()
    {
        health ??= GetComponent<Entity_Health>();
        health?.Revive();
    }

    public void RestoreHealingPotionsToFull()
    {
        int clampedMax = Mathf.Max(0, maxHealingPotionCount);
        currentHealingPotionCount = clampedMax;
        healingPotionUseTimer = 0f;
        healingPotionInUse = false;
        NotifyHealingPotionChanged();
    }

    public void ResetForBonfire()
    {
        EndHazardRecovery();
        canWallHold = true;
        canFallAttack = true;
        canAirAttack = true;
        IsWallSlideDropLocked = false;
        dashCooldownTimer = 0f;
        staminaRecoveryTimer = 0f;
        wallJumpAirAttackWindowTimer = 0f;
        basicAttackLoopCooldownTimer = 0f;
        hasBasicAttackLoopRestartRequest = false;
        pendingBasicAttackComboIndex = -1;
        pendingAirAttackComboIndex = -1;
        pendingBasicAttackComboTimer = 0f;
        pendingAirAttackComboTimer = 0f;
        pendingBasicAttackTurnTimer = 0f;
        pendingAirAttackTurnTimer = 0f;

        ClearBasicAttackComboWindow();
        ClearAirAttackComboWindow();
        ClearWallJumpAirAttackWindow();
        ClearBasicAttackComboAfterDash();
        ClearAirAttackComboAfterDash();
        EndDashEnemyCollisionIgnore();
        RestoreStaminaToFull();
        RestoreHealingPotionsToFull();
    }

    public bool IsInWallContactState()
    {
        return stateMachine?.CurrentState == wallSlideState
            || stateMachine?.CurrentState == wallHoldState;
    }

    private bool IsInCombatActionState()
    {
        return stateMachine?.CurrentState == basicAttackState
            || stateMachine?.CurrentState == airAttackState
            || stateMachine?.CurrentState == fallAttackState
            || stateMachine?.CurrentState == dashState
            || stateMachine?.CurrentState == counterAttackState;
    }

    public bool TryGetLastGroundedSafePosition(out Vector3 safePosition)
    {
        if (hasLastGroundedSafePosition)
        {
            safePosition = lastGroundedSafePosition;
            return true;
        }

        safePosition = transform.position;
        return false;
    }

    public void BeginHazardRecovery()
    {
        hazardRecoveryActive = true;
        CancelHealingPotion();

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }
    }

    public void EndHazardRecovery()
    {
        if (rb != null)
        {
            rb.simulated = true;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (hasPendingHazardRecoveryPosition)
        {
            if (rb != null)
            {
                rb.position = pendingHazardRecoveryPosition;
            }
            else
            {
                transform.position = pendingHazardRecoveryPosition;
            }

            Physics2D.SyncTransforms();
            SnapActiveColliderBottomToGround(.5f);
            ResolveHazardOverlap();
            Physics2D.SyncTransforms();
        }

        hasPendingHazardRecoveryPosition = false;
        hazardRecoveryActive = false;
        Physics2D.SyncTransforms();
    }

    public void RecoverFromHazard(Vector3 worldPosition)
    {
        pendingHazardRecoveryPosition = worldPosition;
        hasPendingHazardRecoveryPosition = true;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        transform.position = worldPosition;

        Physics2D.SyncTransforms();

        SetAnimation(false, false);
        SetJumpFall(false);
        SetWallSlide(false);
        SetDash(false);
        SetBasicAttack(false);
        SetBasicAttackIndex(0);
        SetAirAttackIndex(0);
        SetFallAttack(false);
        SetFallAttackFinishRequested(false);
        SetCounterAttack(false);
        SetCounterAttackPerformed(false);
        ResetFallAttackTrigger();
        SetYVelocity(0f);

        if (stateMachine != null)
        {
            if (GroundDetected())
            {
                if (idleState != null)
                {
                    stateMachine.ChangeState(idleState);
                }
            }
            else if (fallState != null)
            {
                stateMachine.ChangeState(fallState);
            }
        }
    }

    public bool TryUseHealingPotion()
    {
        if (!CanUseHealingPotion)
        {
            return false;
        }

        healingPotionInUse = true;
        healingPotionUseTimer = Mathf.Max(.01f, healingPotionUseDuration);
        PlayHealingPotionAudio(AudioKey.PlayerPotionUse);
        NotifyHealingPotionChanged();
        EnsureHealingPotionWorldIcon();
        if (healingPotionWorldIcon != null)
        {
            healingPotionWorldIcon.SetVisible(true);
        }
        return true;
    }

    public void CancelHealingPotion()
    {
        if (!healingPotionInUse && healingPotionUseTimer <= 0f)
        {
            return;
        }

        healingPotionInUse = false;
        healingPotionUseTimer = 0f;
        NotifyHealingPotionChanged();

        if (healingPotionWorldIcon != null)
        {
            healingPotionWorldIcon.SetVisible(false);
        }
    }

    private void CompleteHealingPotion()
    {
        if (!healingPotionInUse)
        {
            return;
        }

        healingPotionInUse = false;
        healingPotionUseTimer = 0f;
        currentHealingPotionCount = Mathf.Clamp(currentHealingPotionCount - 1, 0, Mathf.Max(0, maxHealingPotionCount));

        Entity_Health playerHealth = health ?? GetComponent<Entity_Health>();
        health = playerHealth;
        if (playerHealth != null)
        {
            playerHealth.Heal(Mathf.RoundToInt(healingPotionHealAmount));
        }

        PlayHealingPotionAudio(AudioKey.PlayerPotionComplete);

        if (healingPotionWorldIcon != null)
        {
            healingPotionWorldIcon.SetVisible(false);
        }

        NotifyHealingPotionChanged();
    }

    private void UpdateHealingPotionUseState()
    {
        if (!healingPotionInUse)
        {
            if (healingPotionWorldIcon != null)
            {
                healingPotionWorldIcon.SetVisible(false);
            }

            return;
        }

        if (ShouldInterruptHealingPotion())
        {
            CancelHealingPotion();
            return;
        }

        healingPotionUseTimer -= Time.deltaTime;
        if (healingPotionUseTimer <= 0f)
        {
            CompleteHealingPotion();
            return;
        }

        if (healingPotionWorldIcon != null)
        {
            healingPotionWorldIcon.SetVisible(true);
        }
    }

    private bool ShouldInterruptHealingPotion()
    {
        return AttackInputPressed()
            || AttackInputHeld()
            || JumpInputPressed()
            || DashInputPressed()
            || CounterInputPressed();
    }

    private void HandleHealthDamaged(Entity_Health damagedHealth, int damage, Component damageSource)
    {
        if (damagedHealth != health || !healingPotionInUse)
        {
            return;
        }

        CancelHealingPotion();
    }

    private void HandleUsePotionPerformed(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Started && context.phase != InputActionPhase.Performed)
        {
            return;
        }

        healingPotionPressedThisFrame = true;
        TryUseHealingPotion();
    }

    private void NotifyHealingPotionChanged()
    {
        OnHealingPotionChanged?.Invoke(this);
    }

    private void PlayHealingPotionAudio(AudioKey audioKey)
    {
        if (AudioManager.instance == null)
        {
            return;
        }

        AudioManager.instance.PlayGlobalSFX(audioKey);
    }

    private void EnsureHealingPotionWorldIcon()
    {
        if (healingPotionWorldIconObject != null)
        {
            return;
        }

        Transform existingIcon = transform.Find("HealingPotionWorldIcon");
        if (existingIcon != null)
        {
            healingPotionWorldIconObject = existingIcon.gameObject;
            healingPotionWorldIcon = healingPotionWorldIconObject.GetComponent<PlayerHealingPotionWorldIcon>();
            if (healingPotionWorldIcon == null)
            {
                healingPotionWorldIcon = healingPotionWorldIconObject.AddComponent<PlayerHealingPotionWorldIcon>();
            }

            SpriteRenderer existingRenderer = healingPotionWorldIconObject.GetComponent<SpriteRenderer>();
            if (existingRenderer == null)
            {
                existingRenderer = healingPotionWorldIconObject.AddComponent<SpriteRenderer>();
            }

            if (healingPotionWorldIconSprite != null)
            {
                existingRenderer.sprite = healingPotionWorldIconSprite;
            }

            healingPotionWorldIcon.Configure(this, existingRenderer);
            healingPotionWorldIconObject.SetActive(true);
            healingPotionWorldIcon.SetVisible(false);
            return;
        }

        if (healingPotionWorldIconSprite == null)
        {
            return;
        }

        healingPotionWorldIconObject = new GameObject("HealingPotionWorldIcon");
        healingPotionWorldIconObject.transform.SetParent(transform, false);
        healingPotionWorldIcon = healingPotionWorldIconObject.AddComponent<PlayerHealingPotionWorldIcon>();
        SpriteRenderer spriteRenderer = healingPotionWorldIconObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = healingPotionWorldIconSprite;
        spriteRenderer.sortingLayerName = "Player";
        spriteRenderer.sortingOrder = 250;
        healingPotionWorldIconObject.transform.localPosition = healingPotionWorldIconOffset;

        healingPotionWorldIcon.Configure(this, spriteRenderer);
        healingPotionWorldIconObject.SetActive(true);
        healingPotionWorldIcon.SetVisible(false);
    }

    private void UpdateLastGroundedSafePosition()
    {
        if (IsDead || hazardRecoveryActive || !GroundDetected())
        {
            return;
        }

        lastGroundedSafePosition = rb != null ? (Vector3)rb.position : transform.position;
        hasLastGroundedSafePosition = true;
    }

    private void ResolveHazardOverlap()
    {
        if (!TryGetActiveColliderBounds(out Bounds bounds))
        {
            return;
        }

        const int maxAttempts = 20;
        const float step = 0.1f;

        for (int i = 0; i < maxAttempts; i++)
        {
            if (!IsOverlappingSpikeHazard(bounds))
            {
                return;
            }

            Vector2 nextPosition = rb != null
                ? rb.position + Vector2.up * step
                : (Vector2)transform.position + Vector2.up * step;

            if (rb != null)
            {
                rb.position = nextPosition;
            }
            else
            {
                transform.position = nextPosition;
            }

            Physics2D.SyncTransforms();

            if (!TryGetActiveColliderBounds(out bounds))
            {
                return;
            }
        }
    }

    private bool IsOverlappingSpikeHazard(Bounds bounds)
    {
        Vector2 size = new Vector2(
            Mathf.Max(.05f, bounds.size.x * .9f),
            Mathf.Max(.05f, bounds.size.y * .9f)
        );

        Collider2D[] overlaps = Physics2D.OverlapBoxAll(bounds.center, size, 0f);
        foreach (Collider2D overlap in overlaps)
        {
            if (overlap == null)
            {
                continue;
            }

            if (overlap.transform.root == transform.root)
            {
                continue;
            }

            if (overlap.GetComponentInParent<SpikeHazard>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void ConsumeStamina(float amount, float recoveryDelay)
    {
        amount = Mathf.Max(0f, amount);
        recoveryDelay = Mathf.Max(0f, recoveryDelay);

        if (amount <= 0f)
        {
            return;
        }

        float remainingStamina = Mathf.Max(0f, currentStamina - amount);
        SetCurrentStamina(remainingStamina);
        if (remainingStamina <= 0f)
        {
            staminaRecoveryTimer = staminaEmptyRecoveryDelay;
            return;
        }

        if (recoveryDelay > 0f)
        {
            staminaRecoveryTimer = recoveryDelay;
        }
    }

    private void DrainStamina(float amount, float recoveryDelay)
    {
        amount = Mathf.Max(0f, amount);
        recoveryDelay = Mathf.Max(0f, recoveryDelay);
        if (amount <= 0f || currentStamina <= 0f)
        {
            return;
        }

        float remainingStamina = Mathf.Max(0f, currentStamina - amount);
        SetCurrentStamina(remainingStamina);
        if (remainingStamina <= 0f)
        {
            staminaRecoveryTimer = staminaEmptyRecoveryDelay;
            return;
        }

        if (recoveryDelay > 0f)
        {
            staminaRecoveryTimer = recoveryDelay;
        }
    }

    private void SetCurrentStamina(float value)
    {
        float clampedValue = Mathf.Clamp(value, 0f, maxStamina);
        if (Mathf.Approximately(clampedValue, currentStamina))
        {
            return;
        }

        currentStamina = clampedValue;
        NotifyStaminaChanged();
    }

    private void NotifyStaminaChanged()
    {
        OnStaminaChanged?.Invoke(this);
    }

    public void LockWallSlideAfterNoInputDrop()
    {
        IsWallSlideDropLocked = true;
    }

    public void ResetWallSlideDropLock()
    {
        IsWallSlideDropLocked = false;
    }

    public void EnterDeadState()
    {
        if (stateMachine == null || deadState == null)
        {
            return;
        }

        CancelHealingPotion();
        SetDeadPlayerBodyLayer();

        if (stateMachine.CurrentState == deadState)
        {
            return;
        }

        stateMachine.ChangeState(deadState);
    }

    private void SetDeadPlayerBodyLayer()
    {
        int deadLayer = LayerMask.NameToLayer("DeadPlayerBody");

        if (deadLayer < 0)
        {
            return;
        }

        SetLayerRecursively(gameObject, deadLayer);
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
        {
            return;
        }

        target.layer = layer;

        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    // Animation
    public void SetAnimation(bool idle, bool move)
    {
        anim.SetBool(IdleAnimHash, idle);
        anim.SetBool(MoveAnimHash, move);
    }

    public void SetYVelocity(float yVelocity)
    {
        anim.SetFloat(YVelocityAnimHash, yVelocity);
    }

    public void SetJumpFall(bool jumpFall)
    {
        anim.SetBool(JumpFallAnimHash, jumpFall);
    }

    public void SetWallSlide(bool wallSlide)
    {
        anim.SetBool(WallSlideAnimHash, wallSlide);
    }

    public void SetDash(bool dash)
    {
        anim.SetBool(DashAnimHash, dash);
    }

    public void SetBasicAttack(bool basicAttack)
    {
        anim.SetBool(BasicAttackAnimHash, basicAttack);
    }

    public void SetBasicAttackIndex(int attackIndex)
    {
        anim.SetInteger(BasicAttackIndexAnimHash, Mathf.Max(0, attackIndex));
    }

    public void SetAirAttackIndex(int attackIndex)
    {
        anim.SetInteger(AirAttackIndexAnimHash, Mathf.Max(0, attackIndex));
    }

    public void SetFallAttack(bool fallAttack)
    {
        anim.SetBool(FallAttackAnimHash, fallAttack);
    }

    public void SetFallAttackFinishRequested(bool finishRequested)
    {
        anim.SetBool(FallAttackFinishRequestedAnimHash, finishRequested);
    }

    public void SetCounterAttack(bool counterAttack)
    {
        anim.SetBool(CounterAttackAnimHash, counterAttack);
    }

    public void SetCounterAttackPerformed(bool counterAttackPerformed)
    {
        anim.SetBool(CounterAttackPerformedAnimHash, counterAttackPerformed);
    }

    public void SetDead(bool dead)
    {
        anim.SetBool(DeadAnimHash, dead);
    }

    public void SetDeathGroundVisualOffset(bool active)
    {
        applyDeathGroundVisualOffset = active;
        UpdateVisualPosition();

        if (active && deathCollider == null)
        {
            AlignDeathVisualToGround();
        }
    }

    public void ApplyDeathColliderProfile()
    {
        if (cd == null)
        {
            return;
        }

        CacheDeathColliderReference();

        if (deathCollider != null)
        {
            cd.enabled = false;
            deathCollider.enabled = true;
            return;
        }

        cd.enabled = true;
        cd.direction = CapsuleDirection2D.Horizontal;
        cd.size = new Vector2(defaultColliderSize.y, defaultColliderSize.x);
        cd.offset = defaultColliderOffset;
    }

    public bool TryGetActiveColliderBounds(out Bounds bounds)
    {
        CapsuleCollider2D activeCollider = GetActiveCollider();
        if (activeCollider == null)
        {
            bounds = default;
            return false;
        }

        bounds = activeCollider.bounds;
        return true;
    }

    public bool SnapActiveColliderBottomToGround(float checkDistance)
    {
        if (rb == null)
        {
            return false;
        }

        CapsuleCollider2D activeCollider = GetActiveCollider();
        if (activeCollider == null)
        {
            return false;
        }

        Bounds bounds = activeCollider.bounds;
        float rayDistance = Mathf.Max(.01f, checkDistance + bounds.extents.y);
        float inset = Mathf.Min(bounds.extents.x * .35f, .2f);
        float leftX = bounds.min.x + inset;
        float rightX = bounds.max.x - inset;
        float originY = bounds.center.y;
        float bestGroundY = float.NegativeInfinity;
        bool foundGround = false;

        TryUpdateBestGroundY(new Vector2(leftX, originY), rayDistance, ref bestGroundY, ref foundGround);
        TryUpdateBestGroundY(new Vector2(bounds.center.x, originY), rayDistance, ref bestGroundY, ref foundGround);
        TryUpdateBestGroundY(new Vector2(rightX, originY), rayDistance, ref bestGroundY, ref foundGround);

        if (!foundGround)
        {
            return false;
        }

        float moveY = bestGroundY - bounds.min.y;
        if (Mathf.Abs(moveY) <= 0.0001f)
        {
            return true;
        }

        rb.position += Vector2.up * moveY;
        Physics2D.SyncTransforms();
        return true;
    }

    private void TryUpdateBestGroundY(Vector2 origin, float distance, ref float bestGroundY, ref bool foundGround)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            Vector2.down,
            distance,
            whatIsGround
        );

        if (hit.collider == null)
        {
            return;
        }

        if (!foundGround || hit.point.y > bestGroundY)
        {
            bestGroundY = hit.point.y;
            foundGround = true;
        }
    }

    public void RestoreAliveColliderProfile()
    {
        if (cd == null)
        {
            return;
        }

        if (deathCollider != null)
        {
            deathCollider.enabled = false;
        }

        cd.enabled = true;
        cd.direction = defaultColliderDirection;
        cd.size = defaultColliderSize;
        cd.offset = defaultColliderOffset;
    }

    public void RestoreAlivePhysicsProfile()
    {
        if (rb == null)
        {
            return;
        }

        rb.bodyType = defaultBodyType;
        rb.constraints = defaultBodyConstraints;
        rb.gravityScale = defaultGravityScale;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    public void LockCorpsePhysics()
    {
        if (rb == null)
        {
            return;
        }

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Static;
    }

    public void TriggerFallAttack()
    {
        anim.SetTrigger(FallAttackTriggerAnimHash);
    }

    public void ResetFallAttackTrigger()
    {
        anim.ResetTrigger(FallAttackTriggerAnimHash);
    }

    public void SetWallSlideVisualOffset(bool active)
    {
        applyWallSlideVisualOffset = active;
        UpdateVisualPosition();
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        CacheDeathColliderReference();
        moveSpeed = Mathf.Max(0f, moveSpeed);
        airMoveSpeedMultiplier = Mathf.Max(0f, airMoveSpeedMultiplier);
        jumpForce = Mathf.Max(0f, jumpForce);
        groundCheckDistance = Mathf.Max(.01f, groundCheckDistance);
        wallCheckDistance = Mathf.Max(.005f, wallCheckDistance);
        wallCheckVerticalSpan = Mathf.Max(.1f, wallCheckVerticalSpan);
        wallSlideSpeed = Mathf.Max(0f, wallSlideSpeed);
        wallSlideVisualOffset = Mathf.Max(0f, wallSlideVisualOffset);
        wallSlideNoInputDropTime = Mathf.Max(0f, wallSlideNoInputDropTime);
        wallHoldDuration = Mathf.Max(0f, wallHoldDuration);
        jumpHeadClearanceHeight = Mathf.Max(.01f, jumpHeadClearanceHeight);
        jumpHeadClearanceWidthMultiplier = Mathf.Clamp(jumpHeadClearanceWidthMultiplier, .1f, 1f);
        jumpHeadClearanceBottomOffset = Mathf.Max(0f, jumpHeadClearanceBottomOffset);
        wallJumpForce.x = Mathf.Max(0f, wallJumpForce.x);
        wallJumpForce.y = Mathf.Max(0f, wallJumpForce.y);
        wallJumpDuration = Mathf.Max(0f, wallJumpDuration);
        wallJumpInputGraceTime = Mathf.Max(0f, wallJumpInputGraceTime);
        dashDistance = Mathf.Max(0f, dashDistance);
        dashDuration = Mathf.Max(.01f, dashDuration);
        dashCooldown = Mathf.Max(0f, dashCooldown);
        maxStamina = Mathf.Max(1f, maxStamina);
        staminaRecoveryPerSecond = Mathf.Max(0f, staminaRecoveryPerSecond);
        staminaEmptyRecoveryDelay = Mathf.Max(0f, staminaEmptyRecoveryDelay);
        jumpStaminaCost = Mathf.Max(0f, jumpStaminaCost);
        jumpStaminaRecoveryDelay = Mathf.Max(0f, jumpStaminaRecoveryDelay);
        nonCombatJumpStaminaCost = Mathf.Max(0f, nonCombatJumpStaminaCost);
        nonCombatJumpStaminaRecoveryDelay = Mathf.Max(0f, nonCombatJumpStaminaRecoveryDelay);
        wallJumpStaminaCost = Mathf.Max(0f, wallJumpStaminaCost);
        wallJumpStaminaRecoveryDelay = Mathf.Max(0f, wallJumpStaminaRecoveryDelay);
        nonCombatWallJumpStaminaCost = Mathf.Max(0f, nonCombatWallJumpStaminaCost);
        nonCombatWallJumpStaminaRecoveryDelay = Mathf.Max(0f, nonCombatWallJumpStaminaRecoveryDelay);
        dashStaminaCost = Mathf.Max(0f, dashStaminaCost);
        dashStaminaRecoveryDelay = Mathf.Max(0f, dashStaminaRecoveryDelay);
        nonCombatDashStaminaCost = Mathf.Max(0f, nonCombatDashStaminaCost);
        nonCombatDashStaminaRecoveryDelay = Mathf.Max(0f, nonCombatDashStaminaRecoveryDelay);
        counterAttackStaminaCost = Mathf.Max(0f, counterAttackStaminaCost);
        counterAttackSuccessStaminaCost = Mathf.Max(0f, counterAttackSuccessStaminaCost);
        projectileBlockStaminaCost = Mathf.Max(0f, projectileBlockStaminaCost);
        counterAttackStaminaRecoveryDelay = Mathf.Max(0f, counterAttackStaminaRecoveryDelay);
        wallHoldStaminaDrainPerSecond = Mathf.Max(0f, wallHoldStaminaDrainPerSecond);
        wallSlideStaminaDrainPerSecond = Mathf.Max(0f, wallSlideStaminaDrainPerSecond);
        wallContactStaminaRecoveryDelay = Mathf.Max(0f, wallContactStaminaRecoveryDelay);
        maxHealingPotionCount = Mathf.Max(0, maxHealingPotionCount);
        currentHealingPotionCount = Mathf.Clamp(currentHealingPotionCount, 0, maxHealingPotionCount);
        healingPotionHealAmount = Mathf.Max(0f, healingPotionHealAmount);
        healingPotionUseDuration = Mathf.Max(0.01f, healingPotionUseDuration);
        healingPotionMoveSpeedMultiplier = Mathf.Clamp(healingPotionMoveSpeedMultiplier, .1f, 1f);
        attackStaminaRecoveryDelay = Mathf.Max(0f, attackStaminaRecoveryDelay);
        deathGroundVisualDownOffset = Mathf.Max(0f, deathGroundVisualDownOffset);
        deathGroundVisualBottomPadding = Mathf.Max(0f, deathGroundVisualBottomPadding);
        currentStamina = Mathf.Clamp(currentStamina <= 0f ? maxStamina : currentStamina, 0f, maxStamina);
        basicAttackDashComboInputWindow = Mathf.Max(0f, basicAttackDashComboInputWindow);
        basicAttackDashTurnInputWindow = Mathf.Max(0f, basicAttackDashTurnInputWindow);
        basicAttackLoopCooldown = Mathf.Max(0f, basicAttackLoopCooldown);
        fallAttackAnimationSpeed = Mathf.Max(.01f, fallAttackAnimationSpeed);
        fallAttackWindupDuration = Mathf.Max(0f, fallAttackWindupDuration);
        fallAttackGravityMultiplier = Mathf.Max(0f, fallAttackGravityMultiplier);
        fallAttackDiveSpeed = Mathf.Max(0f, fallAttackDiveSpeed);
        fallAttackDiveAngle = Mathf.Clamp(fallAttackDiveAngle, 0f, 89f);
        fallAttackGroundCheckDistance = Mathf.Max(0f, fallAttackGroundCheckDistance);
        fallAttackGroundSearchDistance = Mathf.Max(fallAttackGroundCheckDistance + .01f, fallAttackGroundSearchDistance);
        fallAttackEndAnimationMinSpeed = Mathf.Max(0f, fallAttackEndAnimationMinSpeed);
        fallAttackEndAnimationMaxSpeed = Mathf.Max(fallAttackEndAnimationMinSpeed + .01f, fallAttackEndAnimationMaxSpeed);
        counterDuration = Mathf.Max(0f, counterDuration);
        counterAttackTargetCheckRadiusMultiplier = Mathf.Max(.01f, counterAttackTargetCheckRadiusMultiplier);
        if (string.IsNullOrWhiteSpace(counterAttackAnimationState))
        {
            counterAttackAnimationState = "playerCounterAttack";
        }

        if (string.IsNullOrWhiteSpace(counterAttackPerformedAnimationState))
        {
            counterAttackPerformedAnimationState = "playerCounterAttack_performed";
        }

        if (basicAttackAnimationSpeeds != null)
        {
            for (int i = 0; i < basicAttackAnimationSpeeds.Length; i++)
            {
                basicAttackAnimationSpeeds[i] = Mathf.Max(.01f, basicAttackAnimationSpeeds[i]);
            }
        }

        if (basicAttackStaminaCosts != null)
        {
            for (int i = 0; i < basicAttackStaminaCosts.Length; i++)
            {
                basicAttackStaminaCosts[i] = Mathf.Max(0f, basicAttackStaminaCosts[i]);
            }
        }

        if (basicAttackComboInputLeftWindows != null)
        {
            for (int i = 0; i < basicAttackComboInputLeftWindows.Length; i++)
            {
                basicAttackComboInputLeftWindows[i] = Mathf.Max(0f, basicAttackComboInputLeftWindows[i]);
            }
        }

        if (basicAttackComboInputRightWindows != null)
        {
            for (int i = 0; i < basicAttackComboInputRightWindows.Length; i++)
            {
                basicAttackComboInputRightWindows[i] = Mathf.Max(0f, basicAttackComboInputRightWindows[i]);
            }
        }

        if (basicAttackTurnInputLeftWindows != null)
        {
            for (int i = 0; i < basicAttackTurnInputLeftWindows.Length; i++)
            {
                basicAttackTurnInputLeftWindows[i] = Mathf.Max(0f, basicAttackTurnInputLeftWindows[i]);
            }
        }

        if (basicAttackTurnInputRightWindows != null)
        {
            for (int i = 0; i < basicAttackTurnInputRightWindows.Length; i++)
            {
                basicAttackTurnInputRightWindows[i] = Mathf.Max(0f, basicAttackTurnInputRightWindows[i]);
            }
        }

        basicAttackMoveDuration = Mathf.Max(.01f, basicAttackMoveDuration);
        basicAttackMoveSpeedMultiplier = Mathf.Max(0f, basicAttackMoveSpeedMultiplier);
        airAttackDashComboInputWindow = Mathf.Max(0f, airAttackDashComboInputWindow);
        airAttackDashTurnInputWindow = Mathf.Max(0f, airAttackDashTurnInputWindow);
        airAttackMoveDuration = Mathf.Max(.01f, airAttackMoveDuration);
        airAttackFallSpeed = Mathf.Max(0f, airAttackFallSpeed);

        if (airAttackAnimationSpeeds != null)
        {
            for (int i = 0; i < airAttackAnimationSpeeds.Length; i++)
            {
                airAttackAnimationSpeeds[i] = Mathf.Max(.01f, airAttackAnimationSpeeds[i]);
            }
        }

        if (airAttackStaminaCosts != null)
        {
            for (int i = 0; i < airAttackStaminaCosts.Length; i++)
            {
                airAttackStaminaCosts[i] = Mathf.Max(0f, airAttackStaminaCosts[i]);
            }
        }

        if (airAttackComboInputLeftWindows != null)
        {
            for (int i = 0; i < airAttackComboInputLeftWindows.Length; i++)
            {
                airAttackComboInputLeftWindows[i] = Mathf.Max(0f, airAttackComboInputLeftWindows[i]);
            }
        }

        if (airAttackComboInputRightWindows != null)
        {
            for (int i = 0; i < airAttackComboInputRightWindows.Length; i++)
            {
                airAttackComboInputRightWindows[i] = Mathf.Max(0f, airAttackComboInputRightWindows[i]);
            }
        }

        if (airAttackTurnInputLeftWindows != null)
        {
            for (int i = 0; i < airAttackTurnInputLeftWindows.Length; i++)
            {
                airAttackTurnInputLeftWindows[i] = Mathf.Max(0f, airAttackTurnInputLeftWindows[i]);
            }
        }

        if (airAttackTurnInputRightWindows != null)
        {
            for (int i = 0; i < airAttackTurnInputRightWindows.Length; i++)
            {
                airAttackTurnInputRightWindows[i] = Mathf.Max(0f, airAttackTurnInputRightWindows[i]);
            }
        }

        if (!Application.isPlaying)
        {
            ApplyWallCheckVerticalSpan();
        }
    }

    // Debug Ray
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Vector2 airComboBlockRayOrigin = GetGroundCheckRayOrigin();

        Gizmos.color = new Color(1f, .45f, 0f, 1f);
        Gizmos.DrawLine(airComboBlockRayOrigin, airComboBlockRayOrigin + Vector2.down * airAttackComboGroundBlockDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * fallAttackGroundCheckDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * fallAttackGroundSearchDistance);

        Bounds bounds = GetColliderBounds();
        Vector2 jumpClearanceBoxSize = GetJumpHeadClearanceBoxSize(bounds);
        Vector2 jumpClearanceBoxOrigin = GetJumpHeadClearanceBoxOrigin(bounds, jumpClearanceBoxSize);

        Gizmos.color = new Color(1f, .6f, 0f, .35f);
        Gizmos.DrawWireCube(jumpClearanceBoxOrigin, jumpClearanceBoxSize);

        DrawFallAttackGizmo(fallAttackData.TargetCheckOffset, fallAttackData.TargetCheckRadius, new Color(1f, .2f, .2f, 1f), "Fall Attack");
        DrawFallAttackGizmo(fallAttackExtendedData.TargetCheckOffset, fallAttackExtendedData.TargetCheckRadius, new Color(1f, .5f, 0f, 1f), "Fall Attack Extended");
    }

    private void DrawFallAttackGizmo(Vector2 offset, float radius, Color color, string label)
    {
        Vector2 center = GetFallAttackGizmoCenter(offset);

        Gizmos.color = color;
        Gizmos.DrawWireSphere(center, radius);
        Gizmos.DrawLine(transform.position, center);

#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.Label(center + Vector2.up * (radius + .1f), label);
#endif
    }

    private Vector2 GetFallAttackGizmoCenter(Vector2 offset)
    {
        offset.x *= FacingDirection;
        return (Vector2)transform.position + offset;
    }

    private void UpdateVisualPosition()
    {
        if (visualTransform == null)
        {
            return;
        }

        Vector3 targetPosition = defaultVisualLocalPosition;

        if (applyWallSlideVisualOffset)
        {
            targetPosition += Vector3.right * wallSlideVisualOffset;
        }

        if (applyDeathGroundVisualOffset && deathCollider == null)
        {
            targetPosition += Vector3.down * deathGroundVisualDownOffset;
        }

        visualTransform.localPosition = targetPosition;
    }

    private void AlignDeathVisualToGround()
    {
        CapsuleCollider2D activeCollider = GetActiveCollider();
        if (visualTransform == null || activeCollider == null)
        {
            return;
        }

        if (!TryGetVisualBounds(out Bounds visualBounds))
        {
            return;
        }

        float targetBottomY = activeCollider.bounds.min.y + deathGroundVisualBottomPadding;
        float offsetY = targetBottomY - visualBounds.min.y;

        if (Mathf.Abs(offsetY) <= 0.0001f)
        {
            return;
        }

        visualTransform.position += Vector3.up * offsetY;
    }

    private bool TryGetVisualBounds(out Bounds bounds)
    {
        bounds = default;

        if (visualTransform == null)
        {
            return false;
        }

        Renderer[] renderers = visualTransform.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void CacheDeathColliderReference()
    {
        if (deathCollider != null)
        {
            return;
        }

        if (transform == null)
        {
            return;
        }

        Transform child = transform.Find("DeathCollider");
        if (child == null)
        {
            return;
        }

        deathCollider = child.GetComponent<CapsuleCollider2D>();
    }

    private CapsuleCollider2D GetActiveCollider()
    {
        if (deathCollider != null && deathCollider.enabled)
        {
            return deathCollider;
        }

        return cd;
    }

    private void UpdateDashCooldownTimer()
    {
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
    }

    private void UpdateStaminaRecoveryTimer()
    {
        if (staminaRecoveryTimer > 0f)
        {
            staminaRecoveryTimer -= Time.deltaTime;
        }
    }

    private void UpdateStaminaRecovery()
    {
        if (staminaRecoveryPerSecond <= 0f
            || staminaRecoveryTimer > 0f
            || currentStamina >= maxStamina)
        {
            return;
        }

        SetCurrentStamina(currentStamina + staminaRecoveryPerSecond * Time.deltaTime);
    }

    private void UpdateWallContactStaminaDrain()
    {
        if (!IsInWallContactState())
        {
            return;
        }

        float drainPerSecond = stateMachine?.CurrentState == wallHoldState
            ? wallHoldStaminaDrainPerSecond
            : wallSlideStaminaDrainPerSecond;

        if (drainPerSecond <= 0f)
        {
            return;
        }

        DrainStamina(drainPerSecond * Time.deltaTime, wallContactStaminaRecoveryDelay);

        if (currentStamina > 0f || fallState == null || stateMachine?.CurrentState == fallState)
        {
            return;
        }

        DisableWallHoldUntilGrounded();
        stateMachine.ChangeState(fallState);
    }

    private void UpdateWallJumpAirAttackWindowTimer()
    {
        if (wallJumpAirAttackWindowTimer <= 0f)
        {
            return;
        }

        wallJumpAirAttackWindowTimer -= Time.deltaTime;

        if (wallJumpAirAttackWindowTimer <= 0f)
        {
            ClearWallJumpAirAttackWindow();
        }
    }

    private void UpdateBasicAttackLoopCooldownTimer()
    {
        if (basicAttackLoopCooldownTimer <= 0f)
        {
            return;
        }

        if (AttackInputHeld())
        {
            hasBasicAttackLoopRestartRequest = true;
        }
        else
        {
            hasBasicAttackLoopRestartRequest = false;
        }

        basicAttackLoopCooldownTimer -= Time.deltaTime;

        if (basicAttackLoopCooldownTimer <= 0f)
        {
            basicAttackLoopCooldownTimer = 0f;
        }
    }

    private void UpdateDownInputPressedThisFrame()
    {
        bool downInputHeld = DownInputHeld();
        downInputPressedThisFrame = downInputHeld && !wasDownInputHeldLastFrame;
    }

    private void UpdateBasicAttackComboTimer()
    {
        if (pendingBasicAttackComboTimer <= 0f)
        {
            return;
        }

        UpdatePendingBasicAttackDirectionFromInput();

        pendingBasicAttackComboTimer -= Time.deltaTime;
        pendingBasicAttackTurnTimer -= Time.deltaTime;

        if (pendingBasicAttackComboTimer <= 0f)
        {
            ClearBasicAttackComboWindow();
        }
    }

    private void UpdateAirAttackComboTimer()
    {
        if (pendingAirAttackComboTimer <= 0f)
        {
            return;
        }

        if (AirAttackComboGroundDetected())
        {
            ClearAirAttackComboWindow();
            return;
        }

        UpdatePendingAirAttackDirectionFromInput();

        pendingAirAttackComboTimer -= Time.deltaTime;
        pendingAirAttackTurnTimer -= Time.deltaTime;

        if (pendingAirAttackComboTimer <= 0f)
        {
            ClearAirAttackComboWindow();
        }
    }

    private void UpdatePendingBasicAttackDirectionFromInput()
    {
        if (pendingBasicAttackTurnTimer <= 0f || IsNoHorizontalInput(moveInput.x))
        {
            return;
        }

        pendingBasicAttackDirection = NormalizeDirection((int)Mathf.Sign(moveInput.x), pendingBasicAttackDirection);
    }

    private void UpdatePendingAirAttackDirectionFromInput()
    {
        if (pendingAirAttackTurnTimer <= 0f || IsNoHorizontalInput(moveInput.x))
        {
            return;
        }

        pendingAirAttackDirection = NormalizeDirection((int)Mathf.Sign(moveInput.x), pendingAirAttackDirection);
    }

    private int NormalizeDirection(int direction, int fallbackDirection)
    {
        if (direction > 0)
        {
            return 1;
        }

        if (direction < 0)
        {
            return -1;
        }

        return fallbackDirection >= 0 ? 1 : -1;
    }
}

[DisallowMultipleComponent]
public class UI_PlayerHealingPotion : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Entity_Health playerHealth;
    [SerializeField] private Image potionImage;
    [SerializeField] private Image cooldownImage;
    [SerializeField] private Text countText;
    [SerializeField] private Vector2 countTextOffset = new Vector2(0f, -10f);

    public void Configure(Image iconImage, Image cooldown, Text amountText)
    {
        potionImage = iconImage;
        cooldownImage = cooldown;
        countText = amountText;
        EnsureVisualSetup();
        BindToPlayer();
        Refresh();
    }

    private void Awake()
    {
        if (potionImage == null)
        {
            potionImage = GetComponent<Image>();
        }

        if (cooldownImage == null)
        {
            Transform cooldownTransform = transform.Find("CooldownImage");
            cooldownImage = cooldownTransform != null ? cooldownTransform.GetComponent<Image>() : null;
        }

        if (countText == null)
        {
            Transform countTransform = transform.Find("CountText");
            countText = countTransform != null ? countTransform.GetComponent<Text>() : null;
        }

        EnsureVisualSetup();
    }

    private void OnValidate()
    {
        if (countText != null)
        {
            countTextOffset = countText.rectTransform.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        EnsureVisualSetup();
        BindToPlayer();
        Refresh();
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.OnHealingPotionChanged -= HandleHealingPotionChanged;
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void EnsureVisualSetup()
    {
        if (potionImage == null)
        {
            potionImage = GetComponent<Image>();
        }

        if (potionImage != null)
        {
            potionImage.enabled = true;
        }

        if (cooldownImage == null)
        {
            Transform cooldownTransform = transform.Find("CooldownImage");
            cooldownImage = cooldownTransform != null ? cooldownTransform.GetComponent<Image>() : null;
        }

        if (cooldownImage != null)
        {
            cooldownImage.type = Image.Type.Filled;
            cooldownImage.fillMethod = Image.FillMethod.Radial360;
            cooldownImage.fillClockwise = false;
            cooldownImage.fillOrigin = 2;
            cooldownImage.raycastTarget = false;
            cooldownImage.color = new Color(0f, 0f, 0f, 0.627451f);
            if (cooldownImage.sprite == null && potionImage != null)
            {
                cooldownImage.sprite = potionImage.sprite;
            }
        }

        if (countText == null)
        {
            Transform countTransform = transform.Find("CountText");
            countText = countTransform != null ? countTransform.GetComponent<Text>() : null;
        }

        if (countText != null)
        {
            countText.enabled = true;
            countText.raycastTarget = false;

            if (countText.canvasRenderer != null)
            {
                countText.canvasRenderer.SetAlpha(1f);
            }

            if (countTextOffset != countText.rectTransform.anchoredPosition)
            {
                countTextOffset = countText.rectTransform.anchoredPosition;
            }
        }
    }

    private void BindToPlayer()
    {
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }

        if (player == null)
        {
            return;
        }

        playerHealth ??= player.GetComponent<Entity_Health>();

        if (player != null && potionImage != null && potionImage.sprite != null)
        {
            player.SetHealingPotionWorldIconSprite(potionImage.sprite);
        }
        else if (player != null && player.HealingPotionWorldIconSprite != null && potionImage != null && potionImage.sprite == null)
        {
            potionImage.sprite = player.HealingPotionWorldIconSprite;
        }

        if (cooldownImage != null && potionImage != null)
        {
            cooldownImage.sprite = potionImage.sprite;
        }

        player.OnHealingPotionChanged -= HandleHealingPotionChanged;
        player.OnHealingPotionChanged += HandleHealingPotionChanged;

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
            playerHealth.OnHealthChanged += HandleHealthChanged;
        }
    }

    private void HandleHealingPotionChanged(Player changedPlayer)
    {
        if (changedPlayer == player)
        {
            Refresh();
        }
    }

    private void HandleHealthChanged(Entity_Health changedHealth)
    {
        if (changedHealth == playerHealth)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        EnsureVisualSetup();
        if (player == null)
        {
            BindToPlayer();
        }

        if (player == null)
        {
            potionImage.enabled = false;
            if (cooldownImage != null)
            {
                cooldownImage.enabled = false;
                cooldownImage.fillAmount = 0f;
            }

            if (countText != null)
            {
                countText.text = "--";
            }

            return;
        }

        potionImage.enabled = true;
        potionImage.color = player.CurrentHealingPotionCount > 0
            ? Color.white
            : new Color(1f, 1f, 1f, .35f);
        if (potionImage.sprite == null && player.HealingPotionWorldIconSprite != null)
        {
            potionImage.sprite = player.HealingPotionWorldIconSprite;
        }

        if (cooldownImage != null)
        {
            if (cooldownImage.sprite == null && potionImage != null)
            {
                cooldownImage.sprite = potionImage.sprite;
            }

            float fillAmount = player.IsHealingPotionInUse
                ? Mathf.Clamp01(player.HealingPotionUseRemaining / Mathf.Max(.01f, player.HealingPotionUseDuration))
                : 0f;

            cooldownImage.fillAmount = fillAmount;
            cooldownImage.enabled = fillAmount > 0f;
        }

        if (countText != null)
        {
            countText.text = $"{player.CurrentHealingPotionCount}/{player.MaxHealingPotionCount}";
        }
    }
}

[DisallowMultipleComponent]
public class PlayerHealingPotionWorldIcon : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public void Configure(Player targetPlayer, SpriteRenderer renderer)
    {
        player = targetPlayer;
        spriteRenderer = renderer;
        RefreshSprite();
    }

    public void SetVisible(bool visible)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (visible)
        {
            gameObject.SetActive(true);
        }

        spriteRenderer.enabled = visible;
    }

    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            Destroy(gameObject);
            return;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        RefreshSprite();

        bool visible = player.IsHealingPotionInUse;
        spriteRenderer.enabled = visible;
        if (!visible)
        {
            return;
        }

        if (player.TryGetActiveColliderBounds(out Bounds bounds))
        {
            transform.position = new Vector3(
                bounds.center.x,
                bounds.max.y,
                bounds.center.z
            ) + player.HealingPotionWorldIconOffset;
        }
        else
        {
            transform.position = player.transform.position + player.HealingPotionWorldIconOffset;
        }

        transform.rotation = Quaternion.identity;
    }

    private void RefreshSprite()
    {
        if (spriteRenderer == null || player == null)
        {
            return;
        }

        if (spriteRenderer.sprite != player.HealingPotionWorldIconSprite)
        {
            spriteRenderer.sprite = player.HealingPotionWorldIconSprite;
        }
    }
}
