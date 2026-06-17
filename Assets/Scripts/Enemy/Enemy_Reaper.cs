using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class Enemy_Reaper : Enemy, ICounterable, IEnemyBattleResponder
{
    private static readonly int XVelocityAnimHash = Animator.StringToHash("xVelocity");
    private static readonly int BattleAnimHash = Animator.StringToHash("battle");
    private static readonly int MoveAnimSpeedMultiplierHash = Animator.StringToHash("moveAnimSpeedMultiplier");
    private static readonly int BattleAnimSpeedMultiplierHash = Animator.StringToHash("battleAnimSpeedMultiplier");
    private static readonly int AttackSpeedMultiplierHash = Animator.StringToHash("attackSpeedMultiplier");
    private static readonly int StunnedBoolHash = Animator.StringToHash("stunned");
    private static readonly int SpellCastPerformedHash = Animator.StringToHash("spellCast_Performed");

    [Header("Quest Info")]
    [SerializeField] private string questTargetId = "enemy_reaper";

    [Header("Gizmo Display")]
    [SerializeField] private bool showDetectionGizmos = true;

    [Header("Battle Details")]
    [SerializeField, Min(.1f)] private float battleMoveSpeed = 1.6f;
    [SerializeField, Min(.1f)] private float attackDistance = 1.35f;
    [SerializeField, Min(.1f)] private float spellCastDistance = 4f;
    [SerializeField] private Transform attackRangeAnchor;
    [SerializeField] private Transform spellCastRangeAnchor;
    [SerializeField, Min(0f)] private float attackCooldown = .8f;
    [SerializeField] private bool canChasePlayer = true;
    [SerializeField, Min(.1f)] private float battleTimeDuration = 4f;
    [SerializeField, Min(0f)] private float minRetreatDistance = 1f;
    [SerializeField, Min(0f)] private float battleStopDistance = .08f;
    [SerializeField] private Vector2 retreatVelocity = new Vector2(5f, 3f);

    [Header("Teleport Details")]
    [SerializeField] private Transform teleportAreaAnchor;
    [SerializeField, Min(.1f)] private Vector2 teleportAreaSize = new Vector2(7f, 3f);
    [SerializeField, Min(1)] private int teleportMaxPlacementAttempts = 10;
    [SerializeField, Min(0f)] private float teleportGroundSearchHeight = 2f;
    [SerializeField, Min(.1f)] private float teleportGroundSearchDistance = 8f;
    [SerializeField, Min(1)] private int teleportImageEchoCount = 6;
    [SerializeField, Min(.05f)] private float teleportImageEchoLifetime = .3f;
    [SerializeField, Min(0f)] private float teleportPostDelay = .06f;
    [SerializeField, Range(0f, 1f)] private float chanceToTeleport = .18f;

    [Header("Stunned State Details")]
    [SerializeField, Min(.1f)] private float stunnedDuration = 1f;
    [SerializeField] private Vector2 stunnedVelocity = new Vector2(7f, 7f);
    [SerializeField] private bool canBeStunned = true;
    [SerializeField] private bool canBeKnockedBack = true;

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
    [SerializeField] private Transform visionAreaAnchor;
    [SerializeField, Min(.01f)] private float playerCheckDistance = 10f;
    [SerializeField, Min(0f)] private float frontSightDistance = 4f;
    [SerializeField, Min(0f)] private float backSightDistance = 2f;
    [SerializeField, Min(0f)] private float chaseVerticalDistance = 3f;
    [SerializeField, Min(0f)] private float loseSightDuration = 3f;

    [Header("Spell Cast")]
    [SerializeField] private DamageScaleData spellDamageScale;
    [SerializeField] private GameObject spellCastPrefab;
    [SerializeField, Min(0)] private int amountToCast = 4;
    [SerializeField, Min(0f)] private float spellCastRate = .9f;
    [SerializeField, Min(0f)] private float spellCastStateCooldown = 12f;
    [SerializeField] private Vector2 playerOffsetPrediction = new Vector2(1.2f, .55f);

    [Header("Attack Info")]
    [SerializeField] private Entity_AttackData reaperAttackData = new Entity_AttackData(new Vector2(.7f, 0f), .65f, new Vector2(3f, 1.5f));

    [Header("Death Info")]
    [SerializeField, Min(0f)] private float deadFallSpeed = 2.25f;
    [SerializeField, Min(0f)] private float deadSlideSpeed = .65f;
    [SerializeField, Min(0f)] private float deadSlideAcceleration = 1.2f;
    [SerializeField, Min(0f)] private float deadDropThroughDelay = 0f;
    [SerializeField, Min(0f)] private float deadDisappearDelay = 4f;
    [SerializeField, Range(0f, 180f)] private float deadFallAngle = 90f;

    public Enemy_ReaperIdleState idleState { get; private set; }
    public Enemy_ReaperMoveState moveState { get; private set; }
    public Enemy_ReaperBattleState battleState { get; private set; }
    public Enemy_ReaperAttackState attackState { get; private set; }
    public Enemy_ReaperTeleportState teleportState { get; private set; }
    public Enemy_ReaperSpellCastState spellCastState { get; private set; }
    public Enemy_ReaperStunnedState stunnedState { get; private set; }
    public Enemy_ReaperDeadState deadState { get; private set; }

    public string QuestTargetId => questTargetId;
    public bool ShowDetectionGizmos => showDetectionGizmos;
    public float BattleMoveSpeed => battleMoveSpeed;
    public Transform AttackRangeAnchor => attackRangeAnchor;
    public Transform SpellCastRangeAnchor => spellCastRangeAnchor;
    public float AttackDistance => GetAttackDistance();
    public float SpellCastDistance => GetSpellCastDistance();
    public float AttackCooldown => attackCooldown;
    public bool CanChasePlayer => canChasePlayer;
    public float BattleTimeDuration => battleTimeDuration;
    public float MaxBattleIdleTime => battleTimeDuration;
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
    public Transform VisionAreaAnchor => visionAreaAnchor;
    public float PlayerCheckDistance => playerCheckDistance;
    public float FrontSightDistance => GetVisionFrontSightDistance();
    public float BackSightDistance => GetVisionBackSightDistance();
    public float ChaseVerticalDistance => GetVisionChaseVerticalDistance();
    public float LoseSightDuration => loseSightDuration;
    public Entity_AttackData ReaperAttackData => reaperAttackData;
    public DamageScaleData SpellDamageScale => spellDamageScale;
    public GameObject SpellCastPrefab => spellCastPrefab;
    public Vector2 PlayerOffsetPrediction => playerOffsetPrediction;
    public int AmountToCast => amountToCast;
    public float SpellCastRate => spellCastRate;
    public float SpellCastStateCooldown => spellCastStateCooldown;
    public float DeadFallSpeed => deadFallSpeed;
    public float DeadSlideSpeed => deadSlideSpeed;
    public float DeadSlideAcceleration => deadSlideAcceleration;
    public float DeadDropThroughDelay => deadDropThroughDelay;
    public float DeadDisappearDelay => deadDisappearDelay;
    public float DeadFallAngle => deadFallAngle;
    public Transform TeleportAreaAnchor => teleportAreaAnchor;
    public Vector2 TeleportAreaSize => teleportAreaSize;
    public float TeleportGroundSearchHeight => teleportGroundSearchHeight;
    public float TeleportGroundSearchDistance => teleportGroundSearchDistance;
    public int TeleportMaxPlacementAttempts => teleportMaxPlacementAttempts;
    public int TeleportImageEchoCount => teleportImageEchoCount;
    public float TeleportImageEchoLifetime => teleportImageEchoLifetime;
    public float TeleportPostDelay => teleportPostDelay;
    public float ChanceToTeleport => chanceToTeleport;
    public bool CanBeKnockedBack => canBeKnockedBack;
    public bool IsAlerted => isAlerted;
    public bool PlayerVisible => playerVisible;
    public bool PlayerInAttackRange => playerInAttackRange;
    public bool PlayerInSpellCastRange => playerInSpellCastRange;
    public bool PlayerWithinChaseHeight => playerWithinChaseHeight;
    public int PlayerTargetDirection => playerTargetDirection;
    public Transform PlayerTarget => playerTarget;
    public Bounds PlayerBounds => GetPlayerBounds();
    public bool CanAttack => attackCooldownTimer <= 0f && !IsStunAttackRecoveryActive;
    public bool CanDoSpellCast => Time.time > lastTimeCastedSpells + spellCastStateCooldown;
    public bool IsCounterWindowActive => counterWindowActive;
    public bool IsSpellCasting => stateMachine != null && stateMachine.CurrentState == spellCastState;
    public bool SpellCastPerformed => spellCastPreformed;
    public bool TeleportTriggered => teleporTrigger;
    public bool CanBeCountered => canBeStunned;
    public Entity_Combat CombatComponent { get; private set; }

    private Transform playerTarget;
    private bool isAlerted;
    private bool playerVisible;
    private bool playerInAttackRange;
    private bool playerInSpellCastRange;
    private bool playerWithinChaseHeight;
    private bool counterWindowActive;
    private int playerTargetDirection = 1;
    private float lastTimeSeenPlayer;
    private float attackCooldownTimer;
    private float lastTimeCastedSpells = float.NegativeInfinity;
    private bool spellCastPreformed;
    private bool teleporTrigger;
    private float defaultTeleportChance;
    private Coroutine spellCastCoroutine;
    private Player playerScript;
    private bool shouldReturnToPatrol;
    private SpriteRenderer visualSpriteRenderer;
    private bool defaultVisualFlipX;
    private Enemy_ReaperVisionAnchor visionAnchorComponent;

    protected override void Awake()
    {
        base.Awake();

        CombatComponent = Combat;
        EnsureGroundMaskAssigned();

        idleState = new Enemy_ReaperIdleState(this, stateMachine);
        moveState = new Enemy_ReaperMoveState(this, stateMachine);
        battleState = new Enemy_ReaperBattleState(this, stateMachine);
        attackState = new Enemy_ReaperAttackState(this, stateMachine);
        teleportState = new Enemy_ReaperTeleportState(this, stateMachine);
        spellCastState = new Enemy_ReaperSpellCastState(this, stateMachine);
        stunnedState = new Enemy_ReaperStunnedState(this, stateMachine);
        deadState = new Enemy_ReaperDeadState(this, stateMachine);

        defaultTeleportChance = chanceToTeleport;
        visualSpriteRenderer = anim != null ? anim.GetComponent<SpriteRenderer>() : null;
        defaultVisualFlipX = visualSpriteRenderer != null && visualSpriteRenderer.flipX;
        CacheVisionAnchorReference();
        CacheBattleRangeAnchorReferences();
    }

    public void EnsureBattleRangeAnchors()
    {
        CacheBattleRangeAnchorReferences();
    }

    private void Start()
    {
        if (stateMachine.CurrentState == null)
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

        UpdatePlayerPerception();
        base.Update();
    }

    protected override IState GetDeadState()
    {
        return deadState;
    }

    public override void SpecialAttack()
    {
        if (spellCastCoroutine != null)
        {
            return;
        }

        spellCastCoroutine = StartCoroutine(CastSpellCo());
    }

    public void EnableCounterWindow()
    {
        counterWindowActive = true;
    }

    public void DisableCounterWindow()
    {
        ForceDisableCounterWindow();
    }

    public void ForceDisableCounterWindow()
    {
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

    public void EnterBattleFromDamage(Transform damageSource)
    {
        if (damageSource != null)
        {
            playerTarget = damageSource;
            playerTargetDirection = GetPlayerDirection(damageSource.position);
            lastTimeSeenPlayer = Time.time;
        }

        isAlerted = true;
        shouldReturnToPatrol = false;

        if (stateMachine != null && stateMachine.CurrentState != teleportState && stateMachine.CurrentState != spellCastState)
        {
            stateMachine.ChangeState(battleState);
        }
    }

    public void HandleTeleportTriggerOnDamaged()
    {
        if (stateMachine != null && !IsDead && stateMachine.CurrentState != teleportState && stateMachine.CurrentState != spellCastState)
        {
            stateMachine.ChangeState(teleportState);
        }
    }

    public void SetSpellCastPreformed(bool spellCastStatus)
    {
        spellCastPreformed = spellCastStatus;
    }

    public void SetSpellCastOnCooldown()
    {
        lastTimeCastedSpells = Time.time;
    }

    public bool ShouldTeleport()
    {
        if (Random.value < chanceToTeleport)
        {
            chanceToTeleport = defaultTeleportChance;
            return true;
        }

        chanceToTeleport = Mathf.Clamp01(chanceToTeleport + .05f);
        return false;
    }

    public void SetTeleportTrigger(bool triggerStatus)
    {
        teleporTrigger = triggerStatus;
    }

    public Vector3 FindTeleportPoint()
    {
        int maxAttempts = Mathf.Max(1, teleportMaxPlacementAttempts);
        float halfWidth = cd != null ? cd.bounds.size.x * .5f : .5f;
        Vector2 center = GetTeleportAreaCenter();
        Vector2 halfSize = teleportAreaSize * .5f;
        float topY = center.y + halfSize.y + teleportGroundSearchHeight;

        for (int i = 0; i < maxAttempts; i++)
        {
            float randomX = Random.Range(center.x - halfSize.x + halfWidth, center.x + halfSize.x - halfWidth);
            Vector2 raycastPoint = new Vector2(randomX, topY);
            RaycastHit2D hit = Physics2D.Raycast(raycastPoint, Vector2.down, teleportGroundSearchDistance + teleportGroundSearchHeight, whatIsGround);

            if (hit.collider != null)
            {
                return hit.point + new Vector2(0f, .8f);
            }
        }

        return transform.position;
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

    public void StopChasingPlayer()
    {
        isAlerted = false;
        playerVisible = false;
        playerInAttackRange = false;
        playerWithinChaseHeight = false;
        shouldReturnToPatrol = true;
        lastTimeSeenPlayer = 0f;
        playerTarget = null;
    }

    public float GetRandomIdleDuration()
    {
        return Random.Range(idleDurationMin, idleDurationMax);
    }

    public float GetRandomMoveDuration()
    {
        return Random.Range(moveDurationMin, moveDurationMax);
    }

    public void SetMoveAnimationSpeed(float speedMultiplier)
    {
        if (anim == null)
        {
            return;
        }

        anim.SetFloat(MoveAnimSpeedMultiplierHash, Mathf.Max(.01f, speedMultiplier));
    }

    public void SetBattleAnimation(bool battle, float xVelocity)
    {
        if (anim == null)
        {
            return;
        }

        anim.SetBool(BattleAnimHash, battle);
        anim.SetFloat(XVelocityAnimHash, xVelocity);

        if (!battle)
        {
            anim.SetFloat(BattleAnimSpeedMultiplierHash, 1f);
        }
    }

    public void SetBattleAnimationSpeed(float speedMultiplier)
    {
        if (anim == null)
        {
            return;
        }

        anim.SetFloat(BattleAnimSpeedMultiplierHash, Mathf.Max(.01f, speedMultiplier));
    }

    public void SetAttackAnimationSpeed(float speedMultiplier)
    {
        if (anim == null)
        {
            return;
        }

        anim.SetFloat(AttackSpeedMultiplierHash, Mathf.Max(.01f, speedMultiplier));
    }

    public void SetAttackVisualFlip(bool flipped)
    {
        if (visualSpriteRenderer == null)
        {
            return;
        }

        visualSpriteRenderer.flipX = flipped ? !defaultVisualFlipX : defaultVisualFlipX;
    }

    public void SetStunnedAnimation(bool stunned)
    {
        if (anim == null)
        {
            return;
        }

        anim.SetBool(StunnedBoolHash, stunned);
    }

    public void SetSpellCastPerformed(bool performed)
    {
        if (anim != null)
        {
            anim.SetBool(SpellCastPerformedHash, performed);
        }
    }

#if UNITY_EDITOR
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
        Bounds bounds = GetColliderBounds();
        Vector3 bodyCenter = bounds.center;
        int facing = FacingDirection == 0 ? 1 : FacingDirection;

        DrawVisionGizmo();
        DrawDetectionBox(bodyCenter, AttackDistance * 2f, chaseVerticalDistance * 2f, new Color(1f, .45f, .15f, .12f), new Color(1f, .45f, .15f, 1f), $"Attack Range / {AttackDistance:0.##}");
        DrawDetectionBox(bodyCenter, SpellCastDistance * 2f, chaseVerticalDistance * 2f, new Color(.35f, .8f, 1f, .10f), new Color(.35f, .8f, 1f, 1f), $"Spell Cast Range / {SpellCastDistance:0.##}");

        if (teleportAreaAnchor != null)
        {
            DrawTeleportAreaGizmo();
        }

        if (playerCheck != null)
        {
            Handles.color = new Color(1f, 1f, 1f, .9f);
            Handles.DrawSolidDisc(playerCheck.position, Vector3.forward, .035f);
        }

        Vector3 facingArrowStart = bodyCenter;
        Vector3 facingArrowEnd = bodyCenter + Vector3.right * facing * 0.75f;
        Handles.color = new Color(.2f, 1f, .55f, 1f);
        Handles.DrawAAPolyLine(3f, facingArrowStart, facingArrowEnd);
        Handles.ArrowHandleCap(0, facingArrowEnd, Quaternion.LookRotation(Vector3.forward, Vector3.right * facing), .15f, EventType.Repaint);
    }

    private void DrawVisionGizmo()
    {
        Vector3 center = visionAreaAnchor != null ? visionAreaAnchor.position : transform.position;
        Enemy_ReaperVisionAnchor.DrawOutline(
            center,
            FrontSightDistance,
            BackSightDistance,
            ChaseVerticalDistance,
            "Vision"
        );
    }

    private void DrawDetectionBox(Vector3 center, float width, float height, Color fillColor, Color wireColor, string label)
    {
        Vector3 half = new Vector3(width * .5f, height * .5f, 0f);
        Vector3[] corners =
        {
            center + new Vector3(-half.x, -half.y, 0f),
            center + new Vector3(-half.x, half.y, 0f),
            center + new Vector3(half.x, half.y, 0f),
            center + new Vector3(half.x, -half.y, 0f)
        };

        Handles.DrawSolidRectangleWithOutline(corners, fillColor, wireColor);
        Handles.Label(center + Vector3.up * (half.y + .12f), label, GetLabelStyle(wireColor));
    }

    private void DrawTeleportAreaGizmo()
    {
        Vector3 center = teleportAreaAnchor.position;
        Vector2 size = teleportAreaSize;
        Vector3 half = new Vector3(size.x * .5f, size.y * .5f, 0f);
        Vector3[] corners =
        {
            center + new Vector3(-half.x, -half.y, 0f),
            center + new Vector3(-half.x, half.y, 0f),
            center + new Vector3(half.x, half.y, 0f),
            center + new Vector3(half.x, -half.y, 0f)
        };

        Color fillColor = new Color(.75f, .35f, 1f, .08f);
        Color wireColor = new Color(.75f, .35f, 1f, 1f);
        Handles.DrawSolidRectangleWithOutline(corners, fillColor, wireColor);
        Handles.Label(center + Vector3.up * (half.y + .12f), $"Teleport Area / {size.x:0.##} x {size.y:0.##}", GetLabelStyle(wireColor));
    }

    private static GUIStyle GetLabelStyle(Color color)
    {
        return new GUIStyle(EditorStyles.boldLabel)
        {
            normal =
            {
                textColor = color
            }
        };
    }
#endif

    private IEnumerator CastSpellCo()
    {
        if (playerScript == null)
        {
            Player player = FindAnyPlayerReference();
            playerScript = player;
        }

        Transform target = GetPlayerReference();
        if (target == null || spellCastPrefab == null || amountToCast <= 0)
        {
            SetSpellCastPreformed(true);
            spellCastCoroutine = null;
            yield break;
        }

        for (int i = 0; i < amountToCast; i++)
        {
            float xOffset = 0f;
            if (playerScript != null && playerScript.rb != null && playerScript.rb.velocity.sqrMagnitude > 0.01f)
            {
                xOffset = playerOffsetPrediction.x * Mathf.Sign(playerScript.rb.velocity.x == 0f ? playerScript.FacingDirection : playerScript.rb.velocity.x);
            }
            else if (playerScript != null)
            {
                xOffset = playerOffsetPrediction.x * playerScript.FacingDirection;
            }

            Vector3 spellPosition = target.position + new Vector3(xOffset, playerOffsetPrediction.y, 0f);
            GameObject spellObject = Instantiate(spellCastPrefab, spellPosition, Quaternion.identity);
            Enemy_ReaperSpell spell = spellObject.GetComponent<Enemy_ReaperSpell>();
            if (spell != null)
            {
                spell.SetupSpell(CombatComponent, spellDamageScale);
            }

            yield return new WaitForSeconds(spellCastRate);
        }

        SetSpellCastPreformed(true);
        spellCastCoroutine = null;
    }

    private void UpdatePlayerPerception()
    {
        Transform detectedPlayer = PlayerDetected();

        if (detectedPlayer != null)
        {
            playerTarget = detectedPlayer;
            playerVisible = true;
            playerTargetDirection = GetPlayerDirection(detectedPlayer.position);
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
            Player fallback = FindAnyPlayerReference();
            playerTarget = fallback != null ? fallback.transform : null;
        }

        if (playerTarget == null)
        {
            playerInAttackRange = false;
            playerInSpellCastRange = false;
            playerWithinChaseHeight = false;
            return;
        }

        playerTargetDirection = GetPlayerDirection(playerTarget.position);

        float horizontalDistance = Mathf.Abs(playerTarget.position.x - transform.position.x);
        float verticalDistance = Mathf.Abs(playerTarget.position.y - transform.position.y);
        playerWithinChaseHeight = verticalDistance <= chaseVerticalDistance;
        playerInAttackRange = playerWithinChaseHeight && horizontalDistance <= AttackDistance;
        playerInSpellCastRange = playerWithinChaseHeight && horizontalDistance <= SpellCastDistance;

        if (isAlerted
            && !playerVisible
            && lastTimeSeenPlayer > 0f
            && Time.time - lastTimeSeenPlayer >= loseSightDuration
            && stateMachine.CurrentState != teleportState
            && stateMachine.CurrentState != spellCastState
            && stateMachine.CurrentState != attackState)
        {
            StopChasingPlayer();
            stateMachine.ChangeState(idleState);
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

    private Transform DetectPlayerAtDirection(Vector2 origin, float distance, int direction)
    {
        if (direction == 0)
        {
            return null;
        }

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer < 0)
        {
            return null;
        }

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * direction, distance, whatIsPlayer | whatIsGround);
        if (hit.collider == null || hit.collider.gameObject.layer != playerLayer)
        {
            return null;
        }

        float verticalDistance = Mathf.Abs(hit.collider.bounds.center.y - transform.position.y);
        if (verticalDistance > chaseVerticalDistance)
        {
            return null;
        }

        return hit.collider.GetComponentInParent<Player>()?.transform;
    }

    private Player FindAnyPlayerReference()
    {
        Player[] players = FindObjectsOfType<Player>(true);
        return players != null && players.Length > 0 ? players[0] : null;
    }

    public int GetPlayerDirection()
    {
        return playerTarget != null ? GetPlayerDirection(playerTarget.position) : FacingDirection;
    }

    private int GetPlayerDirection(Vector3 playerPosition)
    {
        return playerPosition.x >= transform.position.x ? 1 : -1;
    }

    public Bounds GetPlayerBounds()
    {
        if (playerTarget == null)
        {
            return new Bounds(transform.position, Vector3.one);
        }

        Player player = playerTarget.GetComponentInParent<Player>();
        if (player != null && player.cd != null)
        {
            return player.cd.bounds;
        }

        return new Bounds(playerTarget.position, Vector3.one);
    }

    public void CompleteAttackState()
    {
        attackCooldownTimer = attackCooldown;
    }

    public void StartAttackCooldown()
    {
        attackCooldownTimer = attackCooldown;
    }

    public bool CanUseSpellCast()
    {
        return CanDoSpellCast;
    }

    private void RefreshVisionAnchorReference()
    {
        visionAnchorComponent = visionAreaAnchor != null
            ? visionAreaAnchor.GetComponent<Enemy_ReaperVisionAnchor>()
            : null;
    }

    private void CacheVisionAnchorReference()
    {
        if (visionAreaAnchor == null)
        {
            visionAreaAnchor = FindChild("VisionArea")
                ?? FindChild("VisionRange")
                ?? FindChild("VisionAnchor")
                ?? FindChild("VisionCheck");
        }

        RefreshVisionAnchorReference();
    }

    private void CacheBattleRangeAnchorReferences()
    {
        if (attackRangeAnchor == null)
        {
            attackRangeAnchor = FindChild("AttackRange")
                ?? FindChild("AttackRangeAnchor")
                ?? FindChild("AttackRangeHandle");
        }

        if (spellCastRangeAnchor == null)
        {
            spellCastRangeAnchor = FindChild("SpellCastRange")
                ?? FindChild("SpellCastRangeAnchor")
                ?? FindChild("SpellCastRangeHandle");
        }

        EnsureBattleRangeAnchor(ref attackRangeAnchor, "AttackRange", attackDistance);
        EnsureBattleRangeAnchor(ref spellCastRangeAnchor, "SpellCastRange", spellCastDistance);

        if (attackRangeAnchor != null && spellCastRangeAnchor != null)
        {
            float attackRange = Mathf.Max(.1f, Mathf.Abs(attackRangeAnchor.localPosition.x));
            Vector3 spellLocalPosition = spellCastRangeAnchor.localPosition;
            float spellRange = Mathf.Max(.1f, Mathf.Abs(spellLocalPosition.x));
            if (spellRange < attackRange)
            {
                spellLocalPosition.x = attackRange;
                spellLocalPosition.y = 0f;
                spellLocalPosition.z = 0f;
                spellCastRangeAnchor.localPosition = spellLocalPosition;
            }
        }
    }

    private void EnsureBattleRangeAnchor(ref Transform anchor, string anchorName, float fallbackDistance)
    {
        if (anchor == null)
        {
            if (Application.isPlaying)
            {
                return;
            }

            GameObject anchorObject = new GameObject(anchorName);
            anchorObject.transform.SetParent(transform, false);
            anchor = anchorObject.transform;
        }

        if (anchor == null || anchor.parent == null)
        {
            return;
        }

        anchor.name = anchorName;

        Vector3 localPosition = anchor.localPosition;
        float distance = Mathf.Abs(localPosition.x);
        if (distance < .1f)
        {
            distance = Mathf.Max(.1f, fallbackDistance);
        }

        localPosition.x = distance;
        localPosition.y = 0f;
        localPosition.z = 0f;

        anchor.localPosition = localPosition;
        anchor.localRotation = Quaternion.identity;
        anchor.localScale = Vector3.one;
    }

    private float GetAttackDistance()
    {
        if (attackRangeAnchor != null)
        {
            return Mathf.Max(.1f, Mathf.Abs(attackRangeAnchor.localPosition.x));
        }

        return Mathf.Max(.1f, attackDistance);
    }

    private float GetSpellCastDistance()
    {
        if (spellCastRangeAnchor != null)
        {
            return Mathf.Max(AttackDistance, Mathf.Abs(spellCastRangeAnchor.localPosition.x));
        }

        return Mathf.Max(AttackDistance, spellCastDistance);
    }

    private Enemy_ReaperVisionAnchor GetVisionAnchorComponent()
    {
        if (visionAreaAnchor == null)
        {
            return null;
        }

        if (visionAnchorComponent == null || visionAnchorComponent.gameObject != visionAreaAnchor.gameObject)
        {
            visionAnchorComponent = visionAreaAnchor.GetComponent<Enemy_ReaperVisionAnchor>();
        }

        return visionAnchorComponent;
    }

    private float GetVisionFrontSightDistance()
    {
        Enemy_ReaperVisionAnchor anchor = GetVisionAnchorComponent();
        return anchor != null ? anchor.FrontSightDistance : frontSightDistance;
    }

    private float GetVisionBackSightDistance()
    {
        Enemy_ReaperVisionAnchor anchor = GetVisionAnchorComponent();
        return anchor != null ? anchor.BackSightDistance : backSightDistance;
    }

    private float GetVisionChaseVerticalDistance()
    {
        Enemy_ReaperVisionAnchor anchor = GetVisionAnchorComponent();
        return anchor != null ? anchor.ChaseVerticalDistance : chaseVerticalDistance;
    }

    private Vector2 GetTeleportAreaCenter()
    {
        if (teleportAreaAnchor != null)
        {
            return teleportAreaAnchor.position;
        }

        return transform.position;
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

    protected override void OnValidate()
    {
        base.OnValidate();

        CacheVisionAnchorReference();
        CacheBattleRangeAnchorReferences();
        attackDistance = Mathf.Max(.1f, attackDistance);
        spellCastDistance = Mathf.Max(attackDistance, spellCastDistance);
        battleMoveSpeed = Mathf.Max(.1f, battleMoveSpeed);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        spellCastRate = Mathf.Max(0f, spellCastRate);
        spellCastStateCooldown = Mathf.Max(0f, spellCastStateCooldown);
        teleportPostDelay = Mathf.Max(0f, teleportPostDelay);
        teleportImageEchoCount = Mathf.Max(1, teleportImageEchoCount);
        teleportImageEchoLifetime = Mathf.Max(.05f, teleportImageEchoLifetime);
        battleTimeDuration = Mathf.Max(.1f, battleTimeDuration);
        minRetreatDistance = Mathf.Max(0f, minRetreatDistance);
        battleStopDistance = Mathf.Max(0f, battleStopDistance);
        deadFallSpeed = Mathf.Max(0f, deadFallSpeed);
        deadSlideSpeed = Mathf.Max(0f, deadSlideSpeed);
        deadSlideAcceleration = Mathf.Max(0f, deadSlideAcceleration);
        deadDropThroughDelay = Mathf.Max(0f, deadDropThroughDelay);
        deadDisappearDelay = Mathf.Max(0f, deadDisappearDelay);
        deadFallAngle = Mathf.Clamp(deadFallAngle, 0f, 180f);
    }
}
