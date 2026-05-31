using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class Player : MonoBehaviour
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
    private static readonly int FallAttackTriggerAnimHash = Animator.StringToHash("fallAttackTrigger");

    [Header("Move Info")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float airMoveSpeedMultiplier = .85f;

    [Header("Jump Info")]
    [SerializeField] private float jumpForce = 12f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = .08f;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Wall Check")]
    [SerializeField] private float wallCheckDistance = .03f;
    [SerializeField, Range(.1f, 1.5f)] private float wallCheckVerticalSpan = .65f;
    [SerializeField] private Transform primaryWallCheck;
    [SerializeField] private Transform secondaryWallCheck;

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
    [SerializeField] private float[] basicAttackComboInputLeftWindows = { 10f, 10f, 10f };
    [SerializeField] private float[] basicAttackComboInputRightWindows = { 0f, 0f, 0f };
    [SerializeField] private float[] basicAttackTurnInputLeftWindows = { 10f, 10f, 10f };
    [SerializeField] private float[] basicAttackTurnInputRightWindows = { 0f, 0f, 0f };
    [SerializeField] private float basicAttackDashComboInputWindow = .25f;
    [SerializeField] private float basicAttackDashTurnInputWindow = .25f;
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
    [SerializeField] private string fallAttackStartAnimationName = "playerFallAttack_start";
    [SerializeField] private string fallAttackEndAnimationName = "playerFallAttack_end";
    [SerializeField] private float fallAttackAnimationSpeed = 1f;
    [SerializeField] private float fallAttackWindupDuration = .35f;
    [SerializeField] private float fallAttackGravityMultiplier = 1f;
    [SerializeField] private float fallAttackDiveSpeed = 14f;
    [SerializeField] private float fallAttackDiveAngle = 25f;
    [SerializeField] private float fallAttackGroundCheckDistance = 1.5f;
    [SerializeField] private float fallAttackGroundSearchDistance = 30f;
    [SerializeField] private float fallAttackEndAnimationMinSpeed = .05f;
    [SerializeField] private float fallAttackEndAnimationMaxSpeed = 8f;
    [SerializeField] private float fallAttackEndAnimationLandingOffset = .15f;

    // Rigidbody
    public Rigidbody2D rb { get; private set; }
    public CapsuleCollider2D cd { get; private set; }

    // Animator
    public Animator anim { get; private set; }

    private Transform visualTransform;
    private Vector3 defaultVisualLocalPosition;
    private bool applyWallSlideVisualOffset;
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

    // Facing Direction
    private int facingDirection = 1;

    // Input
    public Vector2 moveInput { get; private set; }
    private bool wasDownInputHeldLastFrame;
    private bool downInputPressedThisFrame;
    public float MoveSpeed => moveSpeed;
    public float AirMoveSpeed => moveSpeed * airMoveSpeedMultiplier;
    public float WallSlideSpeed => wallSlideSpeed;
    public float WallSlideNoInputDropTime => wallSlideNoInputDropTime;
    public float GroundCheckDistance => groundCheckDistance;
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
    public bool WallDashAwayFromWallEnabled => wallDashAwayFromWallEnabled;
    public float BasicAttackMoveDuration => basicAttackMoveDuration;
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
    public int FacingDirection => facingDirection;
    public bool HasBasicAttackComboWindow => pendingBasicAttackComboIndex >= 0 && pendingBasicAttackComboTimer > 0f;
    public bool HasAirAttackComboWindow => pendingAirAttackComboIndex >= 0 && pendingAirAttackComboTimer > 0f;
    public string FallAttackStartAnimationName => fallAttackStartAnimationName;
    public string FallAttackEndAnimationName => fallAttackEndAnimationName;
    public float FallAttackAnimationSpeed => fallAttackAnimationSpeed;
    public float FallAttackWindupDuration => fallAttackWindupDuration;
    public float FallAttackGravityMultiplier => fallAttackGravityMultiplier;
    public float FallAttackDiveSpeed => fallAttackDiveSpeed;
    public float FallAttackDiveAngle => fallAttackDiveAngle;
    public float FallAttackGroundCheckDistance => fallAttackGroundCheckDistance;
    public float FallAttackGroundSearchDistance => fallAttackGroundSearchDistance;
    public float FallAttackEndAnimationMinSpeed => fallAttackEndAnimationMinSpeed;
    public float FallAttackEndAnimationMaxSpeed => fallAttackEndAnimationMaxSpeed;
    public float FallAttackEndAnimationLandingOffset => fallAttackEndAnimationLandingOffset;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cd = GetComponent<CapsuleCollider2D>();

        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (cd != null && cd.sharedMaterial == null)
        {
            cd.sharedMaterial = new PhysicsMaterial2D("Player_NoFriction")
            {
                friction = 0,
                bounciness = 0
            };
        }

        anim = GetComponentInChildren<Animator>();
        visualTransform = anim.transform;
        defaultVisualLocalPosition = visualTransform.localPosition;

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

        EnsureWallCheckTransforms();
    }

    private void Reset()
    {
        EnsureWallCheckTransforms();
    }

    private void OnEnable()
    {
        input?.Enable();
    }

    private void OnDisable()
    {
        input?.Disable();
    }

    private void Start()
    {
        stateMachine.Initialize(idleState);
    }

    private void Update()
    {
        moveInput = input.Player.Movement.ReadValue<Vector2>();
        UpdateDownInputPressedThisFrame();
        UpdateDashCooldownTimer();
        UpdateWallJumpAirAttackWindowTimer();
        UpdateBasicAttackComboTimer();
        UpdateAirAttackComboTimer();

        stateMachine.CurrentState?.Update();

        wasDownInputHeldLastFrame = DownInputHeld();
    }

    private void FixedUpdate()
    {
        stateMachine.CurrentState?.FixedUpdate();
    }

    private void LateUpdate()
    {
        UpdateVisualPosition();
    }

    // Movement
    public void SetVelocity(float xVelocity, float yVelocity)
    {
        rb.velocity = new Vector2(xVelocity, yVelocity);
    }

    // Jump
    public void Jump()
    {
        SetVelocity(rb.velocity.x, jumpForce);
    }

    // Ground Detection
    public bool GroundDetected()
    {
        Vector2 origin = GetGroundCheckRayOrigin();

        return Physics2D.Raycast(
            origin,
            Vector2.down,
            groundCheckDistance,
            whatIsGround
        );
    }

    public bool GroundContactDetected()
    {
        if (cd == null)
        {
            cd = GetComponent<CapsuleCollider2D>();
        }

        return cd != null && cd.IsTouchingLayers(whatIsGround);
    }

    public bool AirAttackComboGroundDetected()
    {
        if (airAttackComboGroundBlockDistance <= 0f)
        {
            return false;
        }

        Vector2 origin = GetGroundCheckRayOrigin();

        return Physics2D.Raycast(
            origin,
            Vector2.down,
            airAttackComboGroundBlockDistance,
            whatIsGround
        );
    }

    public bool WallDetected()
    {
        return WallDetectedAtTransform(primaryWallCheck)
            && WallDetectedAtTransform(secondaryWallCheck);
    }

    public bool WallContactDetected()
    {
        return WallDetectedAtTransform(primaryWallCheck, 1)
            && WallDetectedAtTransform(secondaryWallCheck, 1)
            || WallDetectedAtTransform(primaryWallCheck, -1)
            && WallDetectedAtTransform(secondaryWallCheck, -1);
    }

    public bool FacingWallContactDetected()
    {
        return FacingWallContactDetected(facingDirection);
    }

    public bool FacingWallContactDetected(int direction)
    {
        if (direction == 0)
        {
            return false;
        }

        Bounds bounds = GetColliderBounds();
        float checkDistance = Mathf.Max(wallCheckDistance, .08f);
        float frontX = direction > 0 ? bounds.max.x : bounds.min.x;
        float inset = Mathf.Min(.08f, bounds.extents.y * .2f);

        Vector2 upperOrigin = new Vector2(bounds.center.x, bounds.max.y - inset);
        Vector2 middleOrigin = bounds.center;
        Vector2 lowerOrigin = new Vector2(bounds.center.x, bounds.min.y + inset);
        Vector2 rayDirection = Vector2.right * (direction > 0 ? 1 : -1);

        return Physics2D.Raycast(upperOrigin, rayDirection, Mathf.Abs(frontX - upperOrigin.x) + checkDistance, whatIsGround)
            || Physics2D.Raycast(middleOrigin, rayDirection, Mathf.Abs(frontX - middleOrigin.x) + checkDistance, whatIsGround)
            || Physics2D.Raycast(lowerOrigin, rayDirection, Mathf.Abs(frontX - lowerOrigin.x) + checkDistance, whatIsGround);
    }

    public bool IsInputTowardWall(float xInput)
    {
        return Mathf.Abs(xInput) > .01f
            && Mathf.Sign(xInput) == facingDirection;
    }

    public bool IsInputAwayFromWall(float xInput)
    {
        return Mathf.Abs(xInput) > .01f
            && Mathf.Sign(xInput) == -facingDirection;
    }

    public bool IsNoHorizontalInput(float xInput)
    {
        return Mathf.Abs(xInput) <= .01f;
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

    public bool FallAttackGroundDetected()
    {
        return Physics2D.Raycast(
            transform.position,
            Vector2.down,
            fallAttackGroundCheckDistance,
            whatIsGround
        );
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
        if (comboDuration <= 0f || attackIndex < 0 || attackIndex >= BasicAttackCount)
        {
            return;
        }

        pendingBasicAttackComboIndex = attackIndex;
        pendingBasicAttackDirection = NormalizeDirection(attackDirection, facingDirection);
        pendingBasicAttackComboTimer = comboDuration;
        pendingBasicAttackTurnTimer = Mathf.Max(0f, turnDuration);

        UpdatePendingBasicAttackDirectionFromInput();
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

    public void LockWallSlideAfterNoInputDrop()
    {
        IsWallSlideDropLocked = true;
    }

    public void ResetWallSlideDropLock()
    {
        IsWallSlideDropLocked = false;
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

    // Flip
    public void CheckForFlip(float xInput)
    {
        if (xInput > 0 && facingDirection == -1)
        {
            Flip(1);
        }
        else if (xInput < 0 && facingDirection == 1)
        {
            Flip(-1);
        }
    }

    private void Flip(int direction)
    {
        facingDirection = direction;

        transform.rotation =
            facingDirection == 1
            ? Quaternion.identity
            : Quaternion.Euler(0, 180, 0);
    }

    private void OnValidate()
    {
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
        wallJumpForce.x = Mathf.Max(0f, wallJumpForce.x);
        wallJumpForce.y = Mathf.Max(0f, wallJumpForce.y);
        wallJumpDuration = Mathf.Max(0f, wallJumpDuration);
        wallJumpInputGraceTime = Mathf.Max(0f, wallJumpInputGraceTime);
        dashDistance = Mathf.Max(0f, dashDistance);
        dashDuration = Mathf.Max(.01f, dashDuration);
        dashCooldown = Mathf.Max(0f, dashCooldown);
        basicAttackDashComboInputWindow = Mathf.Max(0f, basicAttackDashComboInputWindow);
        basicAttackDashTurnInputWindow = Mathf.Max(0f, basicAttackDashTurnInputWindow);
        fallAttackAnimationSpeed = Mathf.Max(.01f, fallAttackAnimationSpeed);
        fallAttackWindupDuration = Mathf.Max(0f, fallAttackWindupDuration);
        fallAttackGravityMultiplier = Mathf.Max(0f, fallAttackGravityMultiplier);
        fallAttackDiveSpeed = Mathf.Max(0f, fallAttackDiveSpeed);
        fallAttackDiveAngle = Mathf.Clamp(fallAttackDiveAngle, 0f, 89f);
        fallAttackGroundCheckDistance = Mathf.Max(0f, fallAttackGroundCheckDistance);
        fallAttackGroundSearchDistance = Mathf.Max(fallAttackGroundCheckDistance + .01f, fallAttackGroundSearchDistance);
        fallAttackEndAnimationMinSpeed = Mathf.Max(0f, fallAttackEndAnimationMinSpeed);
        fallAttackEndAnimationMaxSpeed = Mathf.Max(fallAttackEndAnimationMinSpeed + .01f, fallAttackEndAnimationMaxSpeed);
        if (basicAttackAnimationSpeeds != null)
        {
            for (int i = 0; i < basicAttackAnimationSpeeds.Length; i++)
            {
                basicAttackAnimationSpeeds[i] = Mathf.Max(.01f, basicAttackAnimationSpeeds[i]);
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
            EnsureWallCheckTransforms();
        }
    }

    // Debug Ray
    private void OnDrawGizmosSelected()
    {
        Vector2 airComboBlockRayOrigin = GetGroundCheckRayOrigin();
        Vector2 groundRayOrigin = GetGroundCheckRayOrigin();

        Gizmos.color = Color.red;
        Gizmos.DrawLine(groundRayOrigin, groundRayOrigin + Vector2.down * groundCheckDistance);

        Gizmos.color = new Color(1f, .45f, 0f, 1f);
        Gizmos.DrawLine(airComboBlockRayOrigin, airComboBlockRayOrigin + Vector2.down * airAttackComboGroundBlockDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * fallAttackGroundCheckDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * fallAttackGroundSearchDistance);
        Gizmos.color = Color.red;
        DrawWallCheckGizmo(primaryWallCheck);
        DrawWallCheckGizmo(secondaryWallCheck);

    }

    private bool WallDetectedAtTransform(Transform wallCheckTransform)
    {
        return WallDetectedAtTransform(wallCheckTransform, facingDirection);
    }

    private bool WallDetectedAtTransform(Transform wallCheckTransform, int direction)
    {
        if (wallCheckTransform == null)
        {
            return false;
        }

        return Physics2D.Raycast(
            wallCheckTransform.position,
            Vector2.right * direction,
            wallCheckDistance,
            whatIsGround
        );
    }

    private void DrawWallCheckGizmo(Transform wallCheckTransform)
    {
        if (wallCheckTransform == null)
        {
            return;
        }

        Vector2 origin = wallCheckTransform.position;
        Gizmos.DrawLine(origin, origin + Vector2.right * facingDirection * wallCheckDistance);
    }

    private void EnsureWallCheckTransforms()
    {
        primaryWallCheck = EnsureWallCheckTransform(primaryWallCheck, "PrimaryWallCheck", new Vector2(.32f, wallCheckVerticalSpan * .5f));
        secondaryWallCheck = EnsureWallCheckTransform(secondaryWallCheck, "SecondaryWallCheck", new Vector2(.32f, -wallCheckVerticalSpan * .5f));
        ApplyWallCheckVerticalSpan();
    }

    private void ApplyWallCheckVerticalSpan()
    {
        ApplyWallCheckLocalY(primaryWallCheck, wallCheckVerticalSpan * .5f);
        ApplyWallCheckLocalY(secondaryWallCheck, -wallCheckVerticalSpan * .5f);
    }

    private void ApplyWallCheckLocalY(Transform wallCheckTransform, float localY)
    {
        if (wallCheckTransform == null || wallCheckTransform.parent != transform)
        {
            return;
        }

        Vector3 localPosition = wallCheckTransform.localPosition;
        localPosition.y = localY;
        wallCheckTransform.localPosition = localPosition;
    }

    private Transform EnsureWallCheckTransform(Transform wallCheckTransform, string wallCheckName, Vector2 localPosition)
    {
        if (wallCheckTransform != null)
        {
            return wallCheckTransform;
        }

        Transform existing = transform.Find(wallCheckName);
        if (existing != null)
        {
            return existing;
        }

        if (Application.isPlaying)
        {
            return null;
        }

        GameObject wallCheckObject = new GameObject(wallCheckName);
        wallCheckObject.transform.SetParent(transform, false);
        wallCheckObject.transform.localPosition = localPosition;
        wallCheckObject.transform.localRotation = Quaternion.identity;
        wallCheckObject.transform.localScale = Vector3.one;
        return wallCheckObject.transform;
    }

    private Bounds GetColliderBounds()
    {
        if (cd == null)
        {
            cd = GetComponent<CapsuleCollider2D>();
        }

        return cd != null
            ? cd.bounds
            : new Bounds(transform.position, Vector3.one);
    }

    private Vector2 GetGroundCheckBoxSize(Bounds bounds)
    {
        float height = Mathf.Min(.08f, bounds.size.y * .1f);
        return new Vector2(bounds.size.x * .8f, height);
    }

    private Vector2 GetGroundCheckBoxOrigin(Bounds bounds, Vector2 boxSize)
    {
        return new Vector2(bounds.center.x, bounds.min.y + boxSize.y * .5f);
    }

    private Vector2 GetGroundCheckRayOrigin()
    {
        Bounds bounds = GetColliderBounds();
        return new Vector2(bounds.center.x, bounds.min.y + .02f);
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

        visualTransform.localPosition = targetPosition;
    }

    private void UpdateDashCooldownTimer()
    {
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
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
