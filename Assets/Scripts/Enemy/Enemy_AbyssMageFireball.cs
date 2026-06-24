using System.Collections;
using UnityEngine;

public class Enemy_AbyssMageFireball : MonoBehaviour, IProjectileBreakable
{
    private const string ExplosionAudioSourceName = "FireballExplosionAudioSource";
    private const string WindLoopAudioSourceName = "WindLoopAudioSource";
    private static AudioDatabaseSO cachedAudioDatabase;

    [Header("Flight")]
    [SerializeField, Min(0f)] private float arrivalDuration = .22f;
    [SerializeField, Min(0f)] private float hoverDuration = 1f;
    [SerializeField, Min(0f)] private float homingDuration = .5f;
    [SerializeField, Min(.1f)] private float flightSpeed = 10f;

    [Header("Hybrid Orbit Visuals")]
    [SerializeField] private bool useHybridOrbitVisuals;
    [SerializeField] private Transform orbitVisualRoot;
    [SerializeField, Min(0f)] private float orbitRotationSpeed = 180f;

    [Header("Hybrid Orbit Damage")]
    [SerializeField, Min(0.01f)] private float hybridOrbHitboxRadius = 0.32f;
    [SerializeField, Min(0f)] private float hybridOrbHitCooldown = 0.18f;
    [SerializeField, Min(0f)] private float hybridSharedPlayerHitCooldown = 0.3f;
    [SerializeField, Min(0f)] private float hybridOuterOrbDamageMultiplier = 1f;
    [SerializeField, Min(1f)] private float hybridCoreOrbDamageMultiplier = 3f;
    [SerializeField, Min(0f)] private float hybridTrackingTurnSpeed = 1440f;
    [SerializeField, Min(1f)] private float hybridCoreOrbitRadiusScale = 1.1f;

    [Header("Hover Drift")]
    [SerializeField, Min(0f)] private float hoverDriftRadius = .18f;
    [SerializeField, Min(.01f)] private float hoverDriftChangeInterval = .18f;
    [SerializeField, Min(0f)] private float hoverDriftMoveSpeed = 1.6f;

    [Header("Giant Hover Drift")]
    [SerializeField, Min(0f)] private float giantHoverDriftRadius = .18f;
    [SerializeField, Min(.01f)] private float giantHoverDriftChangeInterval = .18f;
    [SerializeField, Min(0f)] private float giantHoverDriftMoveSpeed = 1.6f;

    [Header("Impact")]
    [SerializeField] private LayerMask whatCanCollideWith;
    [SerializeField] private Vector2 impactKnockback = new Vector2(4f, 2f);
    [SerializeField, Min(0f)] private float destroyDelayAfterImpact = 2f;
    [SerializeField, Min(0f)] private float collisionArmDistance = 0.45f;
    [SerializeField, Min(0f), Tooltip("Base explosion damage radius used by giant fireballs when no override is supplied.")]
    private float giantExplosionRadius = 1.75f;
    [SerializeField, HideInInspector, Min(0f)] private float giantHorizontalWallClearance = 0.2f;
    [SerializeField] private AudioKey explosionSfxKey = AudioKey.AbyssMageFireballExplosion;
    [SerializeField, Min(0.01f)] private float explosionAudioMaxDistance = 12f;
    [SerializeField] private AudioKey windLoopSfxKey = AudioKey.SpellWindLoop;
    [SerializeField] private AudioClip windLoopClip;
    [SerializeField, Range(0f, 1f)] private float windLoopVolume = 0.8f;
    [SerializeField, Min(0.01f)] private float windLoopMinDistance = 0.5f;
    [SerializeField, Min(0.01f)] private float windLoopMaxDistance = 8f;

    [Header("Player Attack Break Range")]
    [SerializeField] private CircleCollider2D breakRangeCollider;
    [SerializeField, Min(0f)] private float breakRangeRadius = .6f;
    [SerializeField] private Vector2 breakRangeOffset = Vector2.zero;

    private Enemy_AbyssMage owner;
    private Entity_Combat combat;
    private Entity_Combat selfCombat;
    private int projectileDamage = 1;
    private Rigidbody2D rb;
    private Collider2D col;
    private Animator anim;
    private Transform target;
    private AudioSource explosionAudioSource;
    private AudioSource windLoopAudioSource;
    private bool hasImpacted;
    private bool windLoopStarted;
    private bool colliderEnabled;
    private bool hoverSlotReleased;
    private float elapsedTime;
    private int hoverReservationId = -1;
    private Vector2 hoverLocalOffset;
    private Vector2 spawnPosition;
    private Vector2 lockedFlightDirection = Vector2.right;
    private Vector2 hoverDriftOffset;
    private Vector2 hoverDriftTargetOffset;
    private float nextHoverDriftChangeTime;
    private Vector2 giantHoverDriftOffset;
    private Vector2 giantHoverDriftTargetOffset;
    private float nextGiantHoverDriftChangeTime;
    private int hoverLaneIndex;
    private Vector2 flightStartPosition;
    private bool flightStarted;
    private bool giantFireballMode;
    private Transform giantCeilingReferencePoint;
    private float giantHoverDuration;
    private bool giantFollowTargetDuringHover = true;
    private bool giantFixedSpawnX;
    private float giantFixedSpawnXValue;
    private float giantHoverDriftPhaseOffset;
    private float giantScaleMultiplier = 10f;
    private float giantFallSpeedMultiplier = 3f;
    private float giantFollowSpeed = 18f;
    private float giantCeilingY;
    private bool giantFalling;
    private float giantMinFollowX = float.NegativeInfinity;
    private float giantMaxFollowX = float.PositiveInfinity;
    private float nextHybridPlayerDamageTime;
    private float hybridOrbitScaleMultiplier = 1f;
    private float hybridOrbitRotationSpeedMultiplier = 1f;
    private float hybridAutoExplodeAfterSeconds = -1f;
    private bool hybridImmuneToPlayerAttacks;
    private bool hybridIgnoreEnvironmentCollisions;
    private bool hybridAutoExplosionTriggered;
    private bool hybridOrbitDamageConfigured;
    private bool immuneToAttacks;
    private int hybridOuterOrbDamage = 18;
    private int hybridCoreOrbDamage = 36;
    private Collider2D[] ignoredAbyssPowerColliders = System.Array.Empty<Collider2D>();
    private Collider2D[] ignoredOwnerColliders = System.Array.Empty<Collider2D>();
    private readonly System.Collections.Generic.List<Enemy_AbyssMageHybridOrb> hybridOrbs = new System.Collections.Generic.List<Enemy_AbyssMageHybridOrb>();
    private Enemy_AbyssMageHybridOrb hybridCoreOrb;
    private bool impactResolvedEventRaised;

    public event System.Action<Enemy_AbyssMageFireball> ImpactResolved;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponentInChildren<Animator>(true);
        EnsureExplosionAudioSource();
        EnsureWindLoopAudioSource();
        TryStartWindLoopAudio();
        selfCombat = GetComponent<Entity_Combat>();
        if (selfCombat != null)
        {
            selfCombat.enabled = false;
        }

        Entity_Health selfHealth = GetComponent<Entity_Health>();
        if (selfHealth != null)
        {
            Destroy(selfHealth);
        }

        EnsureBreakRange();
        EnsureOrbitVisualRoot();
    }

    private void OnValidate()
    {
        arrivalDuration = Mathf.Max(0f, arrivalDuration);
        hoverDuration = Mathf.Max(0f, hoverDuration);
        homingDuration = Mathf.Max(0f, homingDuration);
        flightSpeed = Mathf.Max(.1f, flightSpeed);
        hoverDriftRadius = Mathf.Max(0f, hoverDriftRadius);
        hoverDriftChangeInterval = Mathf.Max(.01f, hoverDriftChangeInterval);
        hoverDriftMoveSpeed = Mathf.Max(0f, hoverDriftMoveSpeed);
        giantHoverDriftRadius = Mathf.Max(0f, giantHoverDriftRadius);
        giantHoverDriftChangeInterval = Mathf.Max(.01f, giantHoverDriftChangeInterval);
        giantHoverDriftMoveSpeed = Mathf.Max(0f, giantHoverDriftMoveSpeed);
        orbitRotationSpeed = Mathf.Max(0f, orbitRotationSpeed);
        hybridOrbHitboxRadius = Mathf.Max(0.01f, hybridOrbHitboxRadius);
        hybridOrbHitCooldown = Mathf.Max(0f, hybridOrbHitCooldown);
        hybridSharedPlayerHitCooldown = Mathf.Max(0f, hybridSharedPlayerHitCooldown);
        hybridOuterOrbDamageMultiplier = Mathf.Max(0f, hybridOuterOrbDamageMultiplier);
        hybridCoreOrbDamageMultiplier = Mathf.Max(1f, hybridCoreOrbDamageMultiplier);
        hybridTrackingTurnSpeed = Mathf.Max(0f, hybridTrackingTurnSpeed);
        hybridCoreOrbitRadiusScale = Mathf.Max(1f, hybridCoreOrbitRadiusScale);
        destroyDelayAfterImpact = Mathf.Max(0f, destroyDelayAfterImpact);
        collisionArmDistance = Mathf.Max(0f, collisionArmDistance);
        giantExplosionRadius = Mathf.Max(0f, giantExplosionRadius);
        giantHorizontalWallClearance = Mathf.Max(0f, giantHorizontalWallClearance);
        explosionAudioMaxDistance = Mathf.Max(0.01f, explosionAudioMaxDistance);
        windLoopVolume = Mathf.Clamp01(windLoopVolume);
        windLoopMinDistance = Mathf.Max(0.01f, windLoopMinDistance);
        windLoopMaxDistance = Mathf.Max(windLoopMinDistance, windLoopMaxDistance);
        breakRangeRadius = Mathf.Max(0f, breakRangeRadius);
        EnsureExplosionAudioSource();
        EnsureWindLoopAudioSource();
        EnsureBreakRange();
        EnsureOrbitVisualRoot();

        if (whatCanCollideWith == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            int groundLayer = LayerMask.NameToLayer("Ground");
            int mask = 0;

            if (playerLayer >= 0)
            {
                mask |= 1 << playerLayer;
            }

            if (groundLayer >= 0)
            {
                mask |= 1 << groundLayer;
            }

            whatCanCollideWith = mask;
        }
    }

    private void Update()
    {
        if (windLoopAudioSource != null && !windLoopAudioSource.isPlaying)
        {
            windLoopStarted = false;
        }

        if (!useHybridOrbitVisuals || orbitVisualRoot == null || hasImpacted)
        {
            return;
        }

        orbitVisualRoot.Rotate(0f, 0f, orbitRotationSpeed * hybridOrbitRotationSpeedMultiplier * Time.deltaTime);

        if (!windLoopStarted)
        {
            TryStartWindLoopAudio();
        }
    }

    private void OnDisable()
    {
        StopWindLoopAudio();
        windLoopStarted = false;
    }

    public bool IsImmuneToAttacks => immuneToAttacks;

    public void SetupProjectile(Enemy_AbyssMage owner, Transform target, Entity_Combat combat, int hoverReservationId, Vector2 hoverLocalOffset)
    {
        SetupProjectile(owner, target, combat, 0, hoverReservationId, hoverLocalOffset);
    }

    public void SetupProjectile(Enemy_AbyssMage owner, Transform target, Entity_Combat combat, int laneIndex, int hoverReservationId, Vector2 hoverLocalOffset)
    {
        this.owner = owner;
        this.target = target;
        this.combat = combat != null ? combat : selfCombat;
        projectileDamage = Mathf.Max(1, selfCombat != null ? selfCombat.Damage : (combat != null ? combat.Damage : 1));
        this.hoverLaneIndex = Mathf.Clamp(laneIndex, 0, 1);
        this.hoverReservationId = hoverReservationId;
        this.hoverLocalOffset = hoverLocalOffset;
        hasImpacted = false;
        impactResolvedEventRaised = false;
        colliderEnabled = false;
        hoverSlotReleased = false;
        elapsedTime = 0f;
        spawnPosition = transform.position;
        lockedFlightDirection = GetInitialFlightDirection();
        hoverDriftOffset = Vector2.zero;
        hoverDriftTargetOffset = Vector2.zero;
        nextHoverDriftChangeTime = arrivalDuration;
        giantHoverDriftOffset = Vector2.zero;
        giantHoverDriftTargetOffset = Vector2.zero;
        nextGiantHoverDriftChangeTime = arrivalDuration;
        flightStartPosition = Vector2.zero;
        flightStarted = false;
        giantFireballMode = false;
        giantCeilingReferencePoint = null;
        giantHoverDuration = 0f;
        giantFollowTargetDuringHover = true;
        giantFixedSpawnX = false;
        giantFixedSpawnXValue = 0f;
        giantHoverDriftPhaseOffset = 0f;
        giantScaleMultiplier = 10f;
        giantFallSpeedMultiplier = 3f;
        giantFollowSpeed = 18f;
        giantCeilingY = 0f;
        giantFalling = false;
        giantMinFollowX = float.NegativeInfinity;
        giantMaxFollowX = float.PositiveInfinity;
        nextHybridPlayerDamageTime = 0f;
        projectileDamage = Mathf.Max(1, selfCombat != null ? selfCombat.Damage : (combat != null ? combat.Damage : 1));
        hybridOrbitScaleMultiplier = 1f;
        hybridOrbitRotationSpeedMultiplier = 1f;
        hybridAutoExplodeAfterSeconds = -1f;
        hybridImmuneToPlayerAttacks = false;
        hybridIgnoreEnvironmentCollisions = false;
        hybridAutoExplosionTriggered = false;
        hybridOrbitDamageConfigured = false;
        immuneToAttacks = false;
        hybridOuterOrbDamage = 18;
        hybridCoreOrbDamage = 36;
        ignoredAbyssPowerColliders = System.Array.Empty<Collider2D>();
        ignoredOwnerColliders = System.Array.Empty<Collider2D>();

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (col == null)
        {
            col = GetComponent<Collider2D>();
        }

        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>(true);
        }

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = spawnPosition;
        }

        if (col != null)
        {
            col.enabled = false;
        }

        if (anim != null)
        {
            anim.enabled = false;
        }

        ApplyBreakRangeSettings();
        TryStartWindLoopAudio();
        PrepareHybridOrbitDamageEntities();
    }

    public void ConfigureGiantFireball(
        Transform ceilingReferencePoint,
        float hoverDuration,
        float scaleMultiplier,
        int damage,
        float fallSpeedMultiplier,
        float followSpeed,
        float horizontalWallClearance,
        float colliderRadiusMultiplier,
        float explosionRadiusMultiplier,
        bool followTargetDuringHover = true,
        float hoverDriftPhaseOffset = 0f,
        float explosionRadius = -1f)
    {
        giantFireballMode = true;
        giantCeilingReferencePoint = ceilingReferencePoint;
        giantHoverDuration = Mathf.Max(0f, hoverDuration);
        giantFollowTargetDuringHover = followTargetDuringHover;
        giantFixedSpawnX = !followTargetDuringHover;
        giantFixedSpawnXValue = spawnPosition.x;
        giantHoverDriftPhaseOffset = Mathf.Max(0f, hoverDriftPhaseOffset);
        giantScaleMultiplier = Mathf.Max(1f, scaleMultiplier);
        giantFallSpeedMultiplier = Mathf.Max(0f, fallSpeedMultiplier);
        giantFollowSpeed = Mathf.Max(0f, followSpeed);
        giantHorizontalWallClearance = Mathf.Max(0f, horizontalWallClearance);
        float effectiveColliderRadiusMultiplier = Mathf.Clamp(colliderRadiusMultiplier, 0.1f, 3f);
        if (explosionRadius <= 0f)
        {
            ArenaBossEncounterController controller = ArenaBossEncounterController.GetActiveInstance();
            if (controller != null)
            {
                explosionRadius = controller.GiantFireballExplosionRadius;
            }
        }

        if (explosionRadius > 0f)
        {
            giantExplosionRadius = Mathf.Max(0f, explosionRadius);
        }
        giantCeilingY = giantCeilingReferencePoint != null
            ? giantCeilingReferencePoint.position.y
            : transform.position.y;
        giantFalling = false;
        immuneToAttacks = true;
        giantHoverDriftOffset = Vector2.zero;
        giantHoverDriftTargetOffset = Vector2.zero;
        nextGiantHoverDriftChangeTime = arrivalDuration + giantHoverDriftPhaseOffset;

        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider != null)
        {
            circleCollider.radius *= effectiveColliderRadiusMultiplier;
        }

        transform.localScale *= giantScaleMultiplier;
        CacheGiantHorizontalFollowRange();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            float initialX = giantFixedSpawnX
                ? giantFixedSpawnXValue
                : GetClampedGiantFollowX(target != null ? target.position.x : transform.position.x);
            rb.position = new Vector2(initialX, giantCeilingY);
        }

        projectileDamage = Mathf.Max(1, damage);

        EnemyProjectileHealth projectileHealth = GetComponent<EnemyProjectileHealth>();
        if (projectileHealth != null)
        {
            projectileHealth.SetImmuneToAttacks(true);
        }

        if (col != null)
        {
            col.enabled = false;
            colliderEnabled = false;
        }

        IgnoreAbyssPowerCollisions(true);
        IgnoreOwnerCollisions(true);
    }

    public void ConfigureExplicitDamage(int damage)
    {
        if (combat == null && selfCombat == null)
        {
            return;
        }

        projectileDamage = Mathf.Max(1, damage);
    }

    public void ConfigureHybridOrbitDamage(int outerOrbDamage, int coreOrbDamage)
    {
        hybridOrbitDamageConfigured = true;
        hybridOuterOrbDamage = Mathf.Max(1, outerOrbDamage);
        hybridCoreOrbDamage = Mathf.Max(1, coreOrbDamage);
        PrepareHybridOrbitDamageEntities();
    }

    public void ConfigureHybridOrbitMode(
        float orbitScaleMultiplier,
        float orbitRotationSpeedMultiplier,
        float autoExplodeAfterSeconds,
        bool immuneToPlayerAttacks,
        bool ignoreEnvironmentCollisions)
    {
        if (!useHybridOrbitVisuals)
        {
            return;
        }

        hybridOrbitScaleMultiplier = Mathf.Max(1f, orbitScaleMultiplier);
        hybridOrbitRotationSpeedMultiplier = Mathf.Max(0f, orbitRotationSpeedMultiplier);
        hybridAutoExplodeAfterSeconds = autoExplodeAfterSeconds > 0f ? autoExplodeAfterSeconds : -1f;
        hybridImmuneToPlayerAttacks = immuneToPlayerAttacks;
        hybridIgnoreEnvironmentCollisions = ignoreEnvironmentCollisions;
        hybridAutoExplosionTriggered = false;

        transform.localScale = Vector3.Scale(transform.localScale, Vector3.one * hybridOrbitScaleMultiplier);
        PrepareHybridOrbitDamageEntities();
    }

    private void FixedUpdate()
    {
        AdvanceMotion(Time.fixedDeltaTime, false);
    }

    public void EditorPreviewTick(float deltaTime)
    {
        if (Application.isPlaying)
        {
            return;
        }

        AdvanceMotion(deltaTime, true);
    }

    private void AdvanceMotion(float deltaTime, bool editorPreviewMode)
    {
        if (hasImpacted || rb == null)
        {
            return;
        }

        float step = Mathf.Max(0f, deltaTime);
        elapsedTime += step;

        if (useHybridOrbitVisuals
            && !hasImpacted
            && hybridAutoExplodeAfterSeconds > 0f
            && elapsedTime >= hybridAutoExplodeAfterSeconds
            && !hybridAutoExplosionTriggered)
        {
            TriggerHybridAutoExplosion();
            return;
        }

        if (giantFireballMode)
        {
            UpdateGiantFireballMotion(step, editorPreviewMode);
            return;
        }

        if (elapsedTime < arrivalDuration)
        {
            SetVelocity(Vector2.zero);
            SetWorldPosition(Vector2.Lerp(spawnPosition, GetHoverWorldPosition(), arrivalDuration <= 0f ? 1f : Mathf.Clamp01(elapsedTime / arrivalDuration)));
            return;
        }

        if (elapsedTime < arrivalDuration + hoverDuration)
        {
            UpdateHoverDrift();
            SetVelocity(Vector2.zero);
            SetWorldPosition(GetHoverWorldPosition() + hoverDriftOffset);
            return;
        }

        if (!flightStarted)
        {
            flightStarted = true;
            flightStartPosition = rb.position;
            Log($"Flight started at {flightStartPosition}");
        }

        if (!useHybridOrbitVisuals && !colliderEnabled && col != null && CanEnableCollider())
        {
            col.enabled = true;
            colliderEnabled = true;
            Log($"Collider enabled at {rb.position}, armDistance={GetCollisionArmDistance():0.###}, hoverBounds={GetHoverAreaBounds()}");
        }

        Vector2 direction = GetFlightDirection();
        if (direction.sqrMagnitude > .0001f)
        {
            lockedFlightDirection = direction.normalized;
        }

        SetVelocity(lockedFlightDirection * flightSpeed);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Impact(collision, null);
    }

    public void BreakProjectile()
    {
        if (useHybridOrbitVisuals && hybridImmuneToPlayerAttacks)
        {
            TriggerHybridAutoExplosion();
            return;
        }

        Impact(null, null);
    }

    public void BreakProjectile(Component damageSource)
    {
        if (useHybridOrbitVisuals && hybridImmuneToPlayerAttacks)
        {
            TriggerHybridAutoExplosion();
            return;
        }

        Impact(null, damageSource);
    }

    public bool CanBeBrokenByAttack(Entity_Combat damageSource)
    {
        if (!hybridImmuneToPlayerAttacks)
        {
            return true;
        }

        return damageSource == null || damageSource.GetComponentInParent<Player>() == null;
    }

    private void TriggerHybridAutoExplosion()
    {
        if (hybridAutoExplosionTriggered || hasImpacted)
        {
            return;
        }

        hybridAutoExplosionTriggered = true;
        hasImpacted = true;
        ReleaseHoverReservation();

        if (hybridCoreOrb != null)
        {
            hybridCoreOrb.ForceBreakWithoutNotify();
            hybridCoreOrb = null;
        }

        BreakRemainingHybridOrbs(null, null);

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        if (col != null)
        {
            col.enabled = false;
        }

        if (orbitVisualRoot != null)
        {
            orbitVisualRoot.gameObject.SetActive(false);
        }

        PlayExplosionAudio();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (giantFireballMode)
        {
            RaiseImpactResolved();
        }

        StopWindLoopAudio();
        windLoopStarted = false;
        IgnoreAbyssPowerCollisions(false);
        IgnoreOwnerCollisions(false);
        ReleaseHoverReservation();
    }

    private void Impact(Collider2D collision, Component playerInteractionSource)
    {
        if (hasImpacted)
        {
            return;
        }

        if (useHybridOrbitVisuals && collision != null)
        {
            return;
        }

        if (collision != null)
        {
            if (giantFireballMode && !IsValidGiantImpactCollision(collision))
            {
                return;
            }

            if (((1 << collision.gameObject.layer) & whatCanCollideWith) == 0)
            {
                Log($"Ignored collision with {collision.name} on layer {LayerMask.LayerToName(collision.gameObject.layer)}");
                return;
            }

            if (owner != null
                && (collision.transform == owner.transform || collision.transform.IsChildOf(owner.transform)))
            {
                Log($"Ignored owner collision with {collision.name}");
                return;
            }

            if (collision.transform == transform || collision.transform.IsChildOf(transform))
            {
                Log($"Ignored self collision with {collision.name}");
                return;
            }

            if (IsDashingPlayer(collision))
            {
                Log($"Ignored dash collision with {collision.name}");
                return;
            }

            if (TryBlockByCounterAttack(collision))
            {
                return;
            }
        }

        ResolveImpact(collision, true, playerInteractionSource);
    }

    private bool TryBlockByCounterAttack(Collider2D collision)
    {
        Player player = collision != null ? collision.GetComponentInParent<Player>() : null;
        if (player == null || !player.IsCounterAttacking)
        {
            return false;
        }

        if (!player.TryConsumeProjectileBlockStamina())
        {
            return false;
        }

        ResolveImpact(collision, false, player);
        return true;
    }

    private void ResolveImpact(Collider2D collision, bool dealDamage, Component playerInteractionSource)
    {
        if (hasImpacted)
        {
            return;
        }

        hasImpacted = true;
        Log(collision != null
            ? $"Impact with {collision.name} on layer {LayerMask.LayerToName(collision.gameObject.layer)} at {transform.position}"
            : $"Broken by player attack at {transform.position}");
        ReleaseHoverReservation();

        if (dealDamage && collision != null)
        {
            if (giantFireballMode)
            {
                ArenaBossEncounterController controller = ArenaBossEncounterController.GetActiveInstance();
                controller?.TryForceTimedPlatformDown(collision);
                ApplyGiantExplosionDamage(collision);
            }
            else
            {
                Entity_Combat targetCombat = collision.GetComponentInParent<Entity_Combat>();
                if (targetCombat != null && combat != null)
                {
                    Vector2 knockback = collision.transform.position.x >= transform.position.x
                        ? new Vector2(impactKnockback.x, impactKnockback.y)
                        : new Vector2(-impactKnockback.x, impactKnockback.y);

                    Entity_Health targetHealth = targetCombat.GetComponentInParent<Entity_Health>();
                    targetHealth?.TakeDamage(projectileDamage, combat, knockback);
                }
            }
        }

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        if (anim != null)
        {
            anim.enabled = true;
        }

        if (col != null)
        {
            col.enabled = false;
        }

        if (owner != null && IsPlayerInteractionSource(playerInteractionSource))
        {
            owner.NotifyFireballPlayerInteracted();
        }

        PlayExplosionAudio();
        RaiseImpactResolved();
        Destroy(gameObject, destroyDelayAfterImpact);
    }

    private void RaiseImpactResolved()
    {
        if (impactResolvedEventRaised)
        {
            return;
        }

        impactResolvedEventRaised = true;
        ImpactResolved?.Invoke(this);
    }

    private void UpdateGiantFireballMotion(float deltaTime, bool editorPreviewMode)
    {
        if (rb == null)
        {
            return;
        }

        Vector2 currentPosition = rb.position;
        float targetX = giantFixedSpawnX
            ? giantFixedSpawnXValue
            : GetClampedGiantFollowX(target != null ? target.position.x : currentPosition.x);
        if (!giantFollowTargetDuringHover && !giantFixedSpawnX)
        {
            targetX = currentPosition.x;
        }

        if (!giantFalling && elapsedTime < giantHoverDuration)
        {
            Vector2 hoverTarget = new Vector2(targetX, giantCeilingY);
            float followStep = giantFollowSpeed * deltaTime;
            UpdateGiantHoverDrift();
            SetVelocity(Vector2.zero);
            SetWorldPosition(Vector2.MoveTowards(currentPosition, hoverTarget + giantHoverDriftOffset, followStep));
            giantCeilingY = giantCeilingReferencePoint != null ? giantCeilingReferencePoint.position.y : giantCeilingY;
            return;
        }

        if (!giantFalling)
        {
            giantFalling = true;
            if (col != null)
            {
                col.enabled = true;
                colliderEnabled = true;
            }
        }

        giantCeilingY = giantCeilingReferencePoint != null ? giantCeilingReferencePoint.position.y : giantCeilingY;
        float fallSpeed = Mathf.Max(.1f, flightSpeed * giantFallSpeedMultiplier);
        Vector2 nextVelocity = Vector2.down * fallSpeed;
        Vector2 nextPosition = currentPosition + nextVelocity * deltaTime;
        nextPosition.x = giantFixedSpawnX ? giantFixedSpawnXValue : GetClampedGiantFollowX(nextPosition.x);

        if (editorPreviewMode && TryResolveEditorPreviewGiantGroundImpact(currentPosition, nextPosition))
        {
            return;
        }

        SetVelocity(nextVelocity);
        SetWorldPosition(nextPosition);
    }

    private bool TryResolveEditorPreviewGiantGroundImpact(Vector2 currentPosition, Vector2 nextPosition)
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
        {
            return false;
        }

        float travelDistance = Mathf.Abs(currentPosition.y - nextPosition.y);
        float castDistance = travelDistance + Mathf.Max(0.05f, GetColliderRadius());
        RaycastHit2D hit = Physics2D.Raycast(currentPosition, Vector2.down, castDistance, 1 << groundLayer);
        if (hit.collider == null)
        {
            return false;
        }

        float visualOffset = Mathf.Max(0.02f, GetColliderRadius() * 0.2f);
        SetWorldPosition(new Vector2(currentPosition.x, hit.point.y + visualOffset));
        Impact(hit.collider, null);
        return true;
    }

    private bool IsValidGiantImpactCollision(Collider2D collision)
    {
        if (collision == null)
        {
            return false;
        }

        if (collision.GetComponentInParent<Player>() != null)
        {
            return true;
        }

        return IsGroundLayer(collision.gameObject.layer);
    }

    private void ApplyGiantExplosionDamage(Collider2D impactCollision)
    {
        float explosionRadius = GetGiantExplosionDamageRadius();
        if (combat == null || explosionRadius <= 0f)
        {
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            Player player = hit.GetComponentInParent<Player>();
            if (player == null)
            {
                continue;
            }

            if (IsDashingPlayer(hit))
            {
                continue;
            }

            Entity_Health targetHealth = player.GetComponent<Entity_Health>();
            if (targetHealth == null)
            {
                targetHealth = player.GetComponentInChildren<Entity_Health>();
            }

            if (targetHealth == null || targetHealth.IsDead)
            {
                continue;
            }

            if (IsExplosionBlockedByGround(player, impactCollision))
            {
                continue;
            }

            Vector2 knockback = player.transform.position.x >= transform.position.x
                ? new Vector2(impactKnockback.x, impactKnockback.y)
                : new Vector2(-impactKnockback.x, impactKnockback.y);

            targetHealth.TakeDamage(projectileDamage, combat, knockback);
            return;
        }
    }

    private bool IsExplosionBlockedByGround(Player player, Collider2D impactCollision)
    {
        if (player == null)
        {
            return false;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
        {
            return false;
        }

        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null)
        {
            playerCollider = player.GetComponentInChildren<Collider2D>();
        }

        Bounds? impactBounds = null;
        Vector2 origin = transform.position;
        if (impactCollision != null)
        {
            impactBounds = impactCollision.bounds;
            float originOffset = Mathf.Max(0.02f, GetColliderRadius() * 0.1f);
            if (IsGroundLayer(impactCollision.gameObject.layer))
            {
                float playerBottomY = playerCollider != null ? playerCollider.bounds.min.y : player.transform.position.y;
                if (playerBottomY >= impactBounds.Value.max.y - 0.02f)
                {
                    return false;
                }

                origin = new Vector2(impactBounds.Value.center.x, impactBounds.Value.max.y + originOffset);
            }
            else
            {
                origin = new Vector2(impactBounds.Value.center.x, impactBounds.Value.center.y + originOffset);
            }
        }

        Vector2 targetPoint = playerCollider != null
            ? playerCollider.bounds.center
            : (Vector2)player.transform.position;
        Vector2 direction = targetPoint - origin;
        float distance = direction.magnitude;
        if (distance <= 0.01f)
        {
            return false;
        }

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction / distance, distance, 1 << groundLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D blocker = hits[i].collider;
            if (blocker == null)
            {
                continue;
            }

            if (hits[i].distance <= 0.01f)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool IsGroundLayer(int layer)
    {
        return (whatCanCollideWith.value & (1 << layer)) != 0
            && layer == LayerMask.NameToLayer("Ground");
    }

    private static LayerMask BuildPlayerOnlyCollisionMask()
    {
        LayerMask mask = 0;
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
        {
            mask |= 1 << playerLayer;
        }

        return mask;
    }

    private void IgnoreAbyssPowerCollisions(bool ignore)
    {
        if (col == null)
        {
            return;
        }

        if (ignore)
        {
            if (ignoredAbyssPowerColliders.Length > 0)
            {
                return;
            }

            AbyssPower[] abyssPowers = Object.FindObjectsByType<AbyssPower>(FindObjectsSortMode.None);
            if (abyssPowers == null || abyssPowers.Length == 0)
            {
                return;
            }

            System.Collections.Generic.List<Collider2D> colliders = new System.Collections.Generic.List<Collider2D>();
            for (int i = 0; i < abyssPowers.Length; i++)
            {
                AbyssPower abyssPower = abyssPowers[i];
                if (abyssPower == null)
                {
                    continue;
                }

                Collider2D[] powerColliders = abyssPower.GetComponentsInChildren<Collider2D>(true);
                if (powerColliders == null || powerColliders.Length == 0)
                {
                    continue;
                }

                for (int j = 0; j < powerColliders.Length; j++)
                {
                    Collider2D powerCollider = powerColliders[j];
                    if (powerCollider == null || powerCollider == col)
                    {
                        continue;
                    }

                    Physics2D.IgnoreCollision(col, powerCollider, true);
                    colliders.Add(powerCollider);
                }
            }

            ignoredAbyssPowerColliders = colliders.ToArray();
            return;
        }

        for (int i = 0; i < ignoredAbyssPowerColliders.Length; i++)
        {
            Collider2D powerCollider = ignoredAbyssPowerColliders[i];
            if (powerCollider != null)
            {
                Physics2D.IgnoreCollision(col, powerCollider, false);
            }
        }

        ignoredAbyssPowerColliders = System.Array.Empty<Collider2D>();
    }

    private void IgnoreOwnerCollisions(bool ignore)
    {
        if (col == null || owner == null)
        {
            return;
        }

        if (ignore)
        {
            if (ignoredOwnerColliders.Length > 0)
            {
                return;
            }

            Collider2D[] ownerColliders = owner.GetComponentsInChildren<Collider2D>(true);
            if (ownerColliders == null || ownerColliders.Length == 0)
            {
                return;
            }

            System.Collections.Generic.List<Collider2D> colliders = new System.Collections.Generic.List<Collider2D>();
            for (int i = 0; i < ownerColliders.Length; i++)
            {
                Collider2D ownerCollider = ownerColliders[i];
                if (ownerCollider == null || ownerCollider == col)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(col, ownerCollider, true);
                colliders.Add(ownerCollider);
            }

            ignoredOwnerColliders = colliders.ToArray();
            return;
        }

        for (int i = 0; i < ignoredOwnerColliders.Length; i++)
        {
            Collider2D ownerCollider = ignoredOwnerColliders[i];
            if (ownerCollider != null)
            {
                Physics2D.IgnoreCollision(col, ownerCollider, false);
            }
        }

        ignoredOwnerColliders = System.Array.Empty<Collider2D>();
    }

    private Vector2 GetFlightDirection()
    {
        if (elapsedTime < arrivalDuration + hoverDuration)
        {
            return Vector2.zero;
        }

        if (useHybridOrbitVisuals)
        {
            Vector2 towardTarget = GetDirectionTowardTarget();
            if (towardTarget.sqrMagnitude > .0001f)
            {
                Vector2 desiredDirection = towardTarget.normalized;
                if (lockedFlightDirection.sqrMagnitude <= .0001f || hybridTrackingTurnSpeed <= 0f)
                {
                    lockedFlightDirection = desiredDirection;
                }
                else
                {
                    float maxRadiansDelta = hybridTrackingTurnSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime;
                    lockedFlightDirection = RotateVectorTowards(lockedFlightDirection, desiredDirection, maxRadiansDelta);
                }
            }

            return lockedFlightDirection;
        }

        float homingTime = elapsedTime - arrivalDuration - hoverDuration;
        if (homingTime <= homingDuration)
        {
            Vector2 towardTarget = GetDirectionTowardTarget();
            if (towardTarget.sqrMagnitude > .0001f)
            {
                return towardTarget.normalized;
            }
        }

        return lockedFlightDirection;
    }

    private Vector2 GetDirectionTowardTarget()
    {
        if (target == null)
        {
            return lockedFlightDirection;
        }

        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        if (toTarget.sqrMagnitude <= .0001f)
        {
            return lockedFlightDirection;
        }

        return toTarget.normalized;
    }

    private static Vector2 RotateVectorTowards(Vector2 currentDirection, Vector2 targetDirection, float maxRadiansDelta)
    {
        if (currentDirection.sqrMagnitude <= .0001f)
        {
            return targetDirection.normalized;
        }

        if (targetDirection.sqrMagnitude <= .0001f || maxRadiansDelta <= 0f)
        {
            return currentDirection.normalized;
        }

        currentDirection.Normalize();
        targetDirection.Normalize();

        float currentAngle = Mathf.Atan2(currentDirection.y, currentDirection.x);
        float targetAngle = Mathf.Atan2(targetDirection.y, targetDirection.x);
        float delta = Mathf.DeltaAngle(currentAngle * Mathf.Rad2Deg, targetAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        delta = Mathf.Clamp(delta, -maxRadiansDelta, maxRadiansDelta);
        float newAngle = currentAngle + delta;
        return new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle));
    }

    private Vector2 GetInitialFlightDirection()
    {
        if (target != null)
        {
            Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
            if (toTarget.sqrMagnitude > .0001f)
            {
                return toTarget.normalized;
            }

            if (Mathf.Abs(target.position.x - transform.position.x) > .0001f)
            {
                return target.position.x >= transform.position.x ? Vector2.right : Vector2.left;
            }
        }

        return transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
    }

    private Vector2 GetHoverWorldPosition()
    {
        if (owner == null)
        {
            return rb != null ? rb.position : (Vector2)transform.position;
        }

        return (Vector2)owner.transform.position + hoverLocalOffset;
    }

    private void UpdateHoverDrift()
    {
        UpdateHoverDrift(
            hoverDriftRadius,
            hoverDriftChangeInterval,
            hoverDriftMoveSpeed,
            ref hoverDriftOffset,
            ref hoverDriftTargetOffset,
            ref nextHoverDriftChangeTime
        );
    }

    private void UpdateGiantHoverDrift()
    {
        UpdateHoverDrift(
            giantHoverDriftRadius,
            giantHoverDriftChangeInterval,
            giantHoverDriftMoveSpeed,
            ref giantHoverDriftOffset,
            ref giantHoverDriftTargetOffset,
            ref nextGiantHoverDriftChangeTime
        );
    }

    private void UpdateHoverDrift(
        float driftRadius,
        float driftChangeInterval,
        float driftMoveSpeed,
        ref Vector2 driftOffset,
        ref Vector2 driftTargetOffset,
        ref float nextDriftChangeTime)
    {
        if (driftRadius <= 0f)
        {
            driftOffset = Vector2.zero;
            return;
        }

        if (elapsedTime >= nextDriftChangeTime)
        {
            driftTargetOffset = GetRandomHoverDriftOffset(driftRadius);
            nextDriftChangeTime = elapsedTime + driftChangeInterval;
        }

        driftOffset = Vector2.MoveTowards(
            driftOffset,
            driftTargetOffset,
            driftMoveSpeed * Time.fixedDeltaTime
        );
    }

    private Vector2 GetRandomHoverDriftOffset(float driftRadius)
    {
        if (driftRadius <= 0f)
        {
            return Vector2.zero;
        }

        float randomX = Random.Range(-driftRadius, driftRadius);
        float randomY = Random.Range(-driftRadius, driftRadius);
        return new Vector2(randomX, randomY);
    }

    private bool IsDashingPlayer(Collider2D collision)
    {
        Player player = collision.GetComponentInParent<Player>();
        return player != null
            && player.stateMachine != null
            && player.dashState != null
            && player.stateMachine.CurrentState == player.dashState;
    }

    private static bool IsPlayerInteractionSource(Component source)
    {
        if (source == null)
        {
            return false;
        }

        return source.GetComponentInParent<Player>() != null;
    }

    private void PlayExplosionAudio()
    {
        if (AudioManager.instance == null)
        {
            return;
        }

        EnsureExplosionAudioSource();
        if (explosionAudioSource == null)
        {
            return;
        }

        AudioManager.instance.PlayLocalizedSFX(explosionSfxKey, explosionAudioSource, explosionAudioMaxDistance);
    }

    private void TryStartWindLoopAudio()
    {
        if (windLoopStarted)
        {
            return;
        }

        EnsureWindLoopAudioSource();
        if (windLoopAudioSource == null)
        {
            return;
        }

        if (TryPlayConfiguredWindLoop())
        {
            windLoopStarted = true;
            return;
        }

        if (windLoopClip != null && TryPlayWindLoopClip(windLoopClip))
        {
            windLoopStarted = true;
            return;
        }

        if (AudioManager.instance != null && AudioManager.instance.PlayLoopingSFX(windLoopSfxKey, windLoopAudioSource, windLoopVolume))
        {
            windLoopStarted = true;
            return;
        }

        if (!TryPlayWindLoopFromDatabase())
        {
            return;
        }

        windLoopStarted = true;
    }

    private bool TryPlayConfiguredWindLoop()
    {
        if (windLoopAudioSource == null || windLoopAudioSource.clip == null)
        {
            return false;
        }

        windLoopAudioSource.playOnAwake = true;
        windLoopAudioSource.enabled = true;
        windLoopAudioSource.loop = true;
        windLoopAudioSource.spatialBlend = 1f;
        windLoopAudioSource.rolloffMode = AudioRolloffMode.Linear;
        windLoopAudioSource.dopplerLevel = 0f;
        windLoopAudioSource.minDistance = windLoopMinDistance;
        windLoopAudioSource.maxDistance = windLoopMaxDistance;
        windLoopAudioSource.volume = Mathf.Clamp01(windLoopVolume);

        if (!windLoopAudioSource.isPlaying)
        {
            windLoopAudioSource.Play();
        }

        return windLoopAudioSource.isPlaying;
    }

    private void StopWindLoopAudio()
    {
        if (windLoopAudioSource == null)
        {
            return;
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.StopLoopingSFX(windLoopAudioSource);
        }
        else if (windLoopAudioSource.isPlaying)
        {
            windLoopAudioSource.Stop();
        }
    }

    private void EnsureExplosionAudioSource()
    {
        if (explosionAudioSource == null)
        {
            Transform child = transform.Find(ExplosionAudioSourceName);
            if (child != null)
            {
                explosionAudioSource = child.GetComponent<AudioSource>();
            }
        }

        if (explosionAudioSource == null)
        {
            GameObject childObject = new GameObject(ExplosionAudioSourceName);
            childObject.transform.SetParent(transform, false);
            explosionAudioSource = childObject.AddComponent<AudioSource>();
        }

        if (explosionAudioSource != null)
        {
            explosionAudioSource.playOnAwake = false;
        }
    }

    private void EnsureWindLoopAudioSource()
    {
        if (windLoopAudioSource == null)
        {
            windLoopAudioSource = GetComponent<AudioSource>();
            if (windLoopAudioSource == null)
            {
                windLoopAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (windLoopAudioSource == null)
        {
            return;
        }

        windLoopAudioSource.playOnAwake = true;
        windLoopAudioSource.enabled = true;
        windLoopAudioSource.loop = true;
        windLoopAudioSource.spatialBlend = 1f;
        windLoopAudioSource.rolloffMode = AudioRolloffMode.Linear;
        windLoopAudioSource.dopplerLevel = 0f;
        windLoopAudioSource.minDistance = windLoopMinDistance;
        windLoopAudioSource.maxDistance = windLoopMaxDistance;
    }

    private bool TryPlayWindLoopClip(AudioClip clip)
    {
        if (clip == null || windLoopAudioSource == null)
        {
            return false;
        }

        bool clipChanged = windLoopAudioSource.clip != clip;
        float targetVolume = Mathf.Clamp01(windLoopVolume);

        windLoopAudioSource.playOnAwake = true;
        windLoopAudioSource.enabled = true;
        windLoopAudioSource.loop = true;
        windLoopAudioSource.pitch = 1f;
        windLoopAudioSource.volume = targetVolume;
        windLoopAudioSource.clip = clip;
        windLoopAudioSource.spatialBlend = 1f;
        windLoopAudioSource.rolloffMode = AudioRolloffMode.Linear;
        windLoopAudioSource.dopplerLevel = 0f;
        windLoopAudioSource.minDistance = windLoopMinDistance;
        windLoopAudioSource.maxDistance = windLoopMaxDistance;

        if (clipChanged && windLoopAudioSource.isPlaying)
        {
            windLoopAudioSource.Stop();
        }

        if (!windLoopAudioSource.isPlaying)
        {
            windLoopAudioSource.Play();
        }

        return windLoopAudioSource.isPlaying;
    }

    private bool TryPlayWindLoopFromDatabase()
    {
        AudioDatabaseSO database = GetAudioDatabase();
        if (database == null || !database.TryGet(windLoopSfxKey, out AudioClipData data))
        {
            return false;
        }

        if (!data.TryGetRandomClip(out AudioClip clip))
        {
            return false;
        }

        return TryPlayWindLoopClip(clip);
    }

    private static AudioDatabaseSO GetAudioDatabase()
    {
        if (cachedAudioDatabase == null)
        {
            cachedAudioDatabase = Resources.Load<AudioDatabaseSO>("Audio/AUDIO DATABASE");
        }

        return cachedAudioDatabase;
    }

    private void EnsureOrbitVisualRoot()
    {
        if (orbitVisualRoot == null)
        {
            Transform child = transform.Find("OrbitVisuals");
            if (child != null)
            {
                orbitVisualRoot = child;
            }
        }
    }

    private void PrepareHybridOrbitDamageEntities()
    {
        if (!useHybridOrbitVisuals || orbitVisualRoot == null)
        {
            return;
        }

        hybridOrbs.Clear();
        hybridCoreOrb = null;

        if (col != null)
        {
            col.enabled = false;
        }

        if (breakRangeCollider != null)
        {
            breakRangeCollider.enabled = false;
        }

        LayerMask hybridCollisionMask = hybridIgnoreEnvironmentCollisions
            ? BuildPlayerOnlyCollisionMask()
            : whatCanCollideWith;
        if (hybridIgnoreEnvironmentCollisions && hybridCollisionMask.value == 0)
        {
            hybridCollisionMask = whatCanCollideWith;
        }
        int baseDamage = combat != null ? Mathf.Max(1, combat.Damage) : 1;
        int childCount = orbitVisualRoot.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform orbitChild = orbitVisualRoot.GetChild(i);
            if (orbitChild == null)
            {
                continue;
            }

            if (orbitChild.name == "CoreOrb")
            {
                continue;
            }

            CircleCollider2D childCollider = orbitChild.GetComponent<CircleCollider2D>();
            if (childCollider == null)
            {
                childCollider = orbitChild.gameObject.AddComponent<CircleCollider2D>();
            }

            childCollider.isTrigger = true;
            childCollider.radius = hybridOrbHitboxRadius;
            childCollider.offset = Vector2.zero;

            Enemy_AbyssMageHybridOrb hybridOrb = orbitChild.GetComponent<Enemy_AbyssMageHybridOrb>();
            if (hybridOrb == null)
            {
                hybridOrb = orbitChild.gameObject.AddComponent<Enemy_AbyssMageHybridOrb>();
            }

            int orbDamage = hybridOrbitDamageConfigured
                ? hybridOuterOrbDamage
                : Mathf.Max(1, Mathf.RoundToInt(baseDamage * hybridOuterOrbDamageMultiplier));
            hybridOrb.Configure(
                this,
                Enemy_AbyssMageHybridOrb.OrbKind.Outer,
                orbDamage,
                hybridOrbHitCooldown,
                hybridOrbHitboxRadius,
                1f,
                hybridCollisionMask,
                impactKnockback,
                GetHybridOrbExplosionAnimatorController(),
                !hybridImmuneToPlayerAttacks
            );
            hybridOrbs.Add(hybridOrb);
        }

        Transform coreTransform = orbitVisualRoot.Find("CoreOrb");
        if (coreTransform == null)
        {
            GameObject coreObject = new GameObject("CoreOrb");
            coreTransform = coreObject.transform;
            coreTransform.SetParent(orbitVisualRoot, false);
            coreTransform.localPosition = Vector3.zero;
            coreTransform.localRotation = Quaternion.identity;
            coreTransform.localScale = Vector3.one * 0.55f;

            SpriteRenderer sourceRenderer = orbitVisualRoot.childCount > 0
                ? orbitVisualRoot.GetChild(0).GetComponent<SpriteRenderer>()
                : null;
            if (sourceRenderer != null)
            {
                SpriteRenderer coreRenderer = coreObject.AddComponent<SpriteRenderer>();
                coreRenderer.sprite = sourceRenderer.sprite;
                coreRenderer.color = sourceRenderer.color;
                coreRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
                coreRenderer.sortingOrder = sourceRenderer.sortingOrder + 1;
            }
        }

        Enemy_AbyssMageHybridOrb coreOrb = coreTransform.GetComponent<Enemy_AbyssMageHybridOrb>();
        if (coreOrb == null)
        {
            coreOrb = coreTransform.gameObject.AddComponent<Enemy_AbyssMageHybridOrb>();
        }

        int coreDamage = hybridOrbitDamageConfigured
            ? hybridCoreOrbDamage
            : Mathf.Max(1, Mathf.RoundToInt(baseDamage * hybridCoreOrbDamageMultiplier));
        coreOrb.Configure(
            this,
            Enemy_AbyssMageHybridOrb.OrbKind.Core,
            coreDamage,
            hybridOrbHitCooldown,
            hybridOrbHitboxRadius,
            hybridCoreOrbitRadiusScale,
            hybridCollisionMask,
            impactKnockback,
            GetHybridOrbExplosionAnimatorController(),
            !hybridImmuneToPlayerAttacks
        );
        hybridCoreOrb = coreOrb;
    }

    private RuntimeAnimatorController GetHybridOrbExplosionAnimatorController()
    {
        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>(true);
        }

        return anim != null ? anim.runtimeAnimatorController : null;
    }

    private void ReleaseHoverReservation()
    {
        if (hoverSlotReleased)
        {
            return;
        }

        hoverSlotReleased = true;

        if (owner != null && hoverReservationId >= 0)
        {
            owner.ReleaseProjectileHoverSlot(hoverReservationId);
        }
    }

    private float GetCollisionArmDistance()
    {
        float armDistance = collisionArmDistance;
        armDistance = Mathf.Max(armDistance, GetHoverEscapeDistance());
        armDistance = Mathf.Max(armDistance, GetColliderRadius() + 0.05f);
        return armDistance;
    }

    private float GetHoverEscapeDistance()
    {
        if (owner == null)
        {
            return collisionArmDistance;
        }

        Vector2 hoverAreaSize = GetHoverAreaSize();

        if (hoverAreaSize.sqrMagnitude <= 0.0001f)
        {
            return collisionArmDistance;
        }

        return hoverAreaSize.magnitude * 0.5f;
    }

    private bool CanEnableCollider()
    {
        if (rb == null)
        {
            return false;
        }

        if (Vector2.Distance(flightStartPosition, rb.position) < GetCollisionArmDistance())
        {
            return false;
        }

        if (owner == null)
        {
            return true;
        }

        Bounds hoverBounds = GetHoverAreaBounds();
        hoverBounds.Expand(GetColliderRadius() * 2f + 0.1f);
        return !hoverBounds.Contains(rb.position);
    }

    private Bounds GetHoverAreaBounds()
    {
        Vector2 center = GetHoverAreaCenter();
        Vector2 size = GetHoverAreaSize();
        return new Bounds(center, size);
    }

    private Vector2 GetHoverAreaCenter()
    {
        if (owner == null)
        {
            return Vector2.zero;
        }

        return hoverLaneIndex == 1
            ? owner.ProjectileHoverAreaCenter2
            : owner.ProjectileHoverAreaCenter1;
    }

    private Vector2 GetHoverAreaSize()
    {
        if (owner == null)
        {
            return Vector2.zero;
        }

        return hoverLaneIndex == 1
            ? owner.ProjectileHoverAreaSize2
            : owner.ProjectileHoverAreaSize1;
    }

    private float GetColliderRadius()
    {
        if (col is CircleCollider2D circleCollider)
        {
            Vector2 scale = transform.lossyScale;
            return circleCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        }

        if (col != null)
        {
            Bounds bounds = col.bounds;
            return Mathf.Max(bounds.extents.x, bounds.extents.y);
        }

        return 0.4f;
    }

    private float GetGiantExplosionDamageRadius()
    {
        return Mathf.Max(0f, giantExplosionRadius);
    }

    private void CacheGiantHorizontalFollowRange()
    {
        giantMinFollowX = float.NegativeInfinity;
        giantMaxFollowX = float.PositiveInfinity;

        ArenaBossEncounterController controller = ArenaBossEncounterController.GetActiveInstance();
        if (controller == null || !controller.TryGetBossArenaHorizontalBounds(out float leftX, out float rightX))
        {
            return;
        }

        float radius = GetColliderRadius();
        float clearance = GetEffectiveGiantHorizontalWallClearance();
        giantMinFollowX = leftX + radius + clearance;
        giantMaxFollowX = rightX - radius - clearance;

        if (giantMaxFollowX < giantMinFollowX)
        {
            float midpoint = (leftX + rightX) * 0.5f;
            giantMinFollowX = midpoint;
            giantMaxFollowX = midpoint;
        }
    }

    private float GetEffectiveGiantHorizontalWallClearance()
    {
        float clearance = Mathf.Max(0f, giantHorizontalWallClearance);
        Player player = target != null ? target.GetComponentInParent<Player>() : null;
        if (player == null)
        {
            return clearance;
        }

        if (!player.TryGetActiveColliderBounds(out Bounds playerBounds))
        {
            return clearance;
        }

        float playerHalfWidth = Mathf.Max(0f, playerBounds.extents.x);
        if (playerHalfWidth <= 0f)
        {
            return clearance;
        }

        return Mathf.Min(clearance, playerHalfWidth * 0.49f);
    }

    private float GetClampedGiantFollowX(float desiredX)
    {
        if (float.IsNegativeInfinity(giantMinFollowX) || float.IsPositiveInfinity(giantMaxFollowX))
        {
            return desiredX;
        }

        return Mathf.Clamp(desiredX, giantMinFollowX, giantMaxFollowX);
    }

    private void SetVelocity(Vector2 velocity)
    {
        if (rb != null)
        {
            rb.velocity = velocity;
        }
    }

    private void SetWorldPosition(Vector2 position)
    {
        if (rb != null)
        {
            rb.position = position;
        }

        Vector3 worldPosition = transform.position;
        worldPosition.x = position.x;
        worldPosition.y = position.y;
        transform.position = worldPosition;
    }

    private void Log(string message)
    {
        // Intentionally disabled to keep the console quiet.
    }

    private void EnsureBreakRange()
    {
        if (breakRangeCollider == null)
        {
            Transform existing = transform.Find("ProjectileBreakRange");
            if (existing != null)
            {
                breakRangeCollider = existing.GetComponent<CircleCollider2D>();
            }
        }

        if (breakRangeCollider == null)
        {
            GameObject breakRangeObject = new GameObject("ProjectileBreakRange");
            breakRangeObject.transform.SetParent(transform, false);
            breakRangeCollider = breakRangeObject.AddComponent<CircleCollider2D>();
        }

        ApplyBreakRangeSettings();
    }

    private void ApplyBreakRangeSettings()
    {
        if (breakRangeCollider == null)
        {
            return;
        }

        int projectileBreakableLayer = LayerMask.NameToLayer("ProjectileBreakable");
        if (projectileBreakableLayer >= 0)
        {
            breakRangeCollider.gameObject.layer = projectileBreakableLayer;
        }

        breakRangeCollider.isTrigger = true;
        breakRangeCollider.radius = breakRangeRadius;
        breakRangeCollider.offset = Vector2.zero;
        breakRangeCollider.transform.localPosition = breakRangeOffset;
    }

    private void OnDrawGizmos()
    {
        float giantDamageRadius = GetGiantExplosionDamageRadius();
        if (giantDamageRadius > 0f)
        {
            Gizmos.color = new Color(1f, .45f, .1f, .85f);
            Gizmos.DrawWireSphere(transform.position, giantDamageRadius);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(.15f, .95f, 1f, .95f);
        Gizmos.DrawWireSphere(GetBreakRangeWorldPosition(), Mathf.Max(0f, breakRangeRadius));
    }

    private Vector3 GetBreakRangeWorldPosition()
    {
        Transform breakRangeTransform = breakRangeCollider != null ? breakRangeCollider.transform : transform.Find("ProjectileBreakRange");
        if (breakRangeTransform != null)
        {
            return breakRangeTransform.position;
        }

        return transform.position + (Vector3)breakRangeOffset;
    }

    public bool IsBroken => hasImpacted;
    public Entity_Combat CombatComponent => combat;
    public Vector2 ImpactKnockback => impactKnockback;
    public LayerMask WhatCanCollideWith => whatCanCollideWith;
    public Transform OrbitVisualRoot => orbitVisualRoot;
    public bool IsHybridCoreOrb(Enemy_AbyssMageHybridOrb orb)
    {
        return hybridCoreOrb != null && orb == hybridCoreOrb;
    }

    public void BreakProjectileFromOrb(Component damageSource)
    {
        BreakProjectile(damageSource);
    }

    public void NotifyHybridOrbBroken(Enemy_AbyssMageHybridOrb orb, Component damageSource)
    {
        if (orb == null)
        {
            return;
        }

        if (hybridCoreOrb != null && orb == hybridCoreOrb)
        {
            hybridCoreOrb = null;
            hasImpacted = true;
            ReleaseHoverReservation();
            BreakRemainingHybridOrbs(orb, damageSource);
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }

            if (col != null)
            {
                col.enabled = false;
            }

            if (orbitVisualRoot != null)
            {
                orbitVisualRoot.gameObject.SetActive(false);
            }

            Destroy(gameObject);
            return;
        }

        hybridOrbs.Remove(orb);

        if (hybridOrbs.Count == 0 && hybridCoreOrb == null)
        {
            Destroy(gameObject);
        }
    }

    public bool TryConsumeHybridPlayerDamageWindow()
    {
        if (Time.time < nextHybridPlayerDamageTime)
        {
            return false;
        }

        nextHybridPlayerDamageTime = Time.time + hybridSharedPlayerHitCooldown;
        return true;
    }

    private void BreakRemainingHybridOrbs(Enemy_AbyssMageHybridOrb sourceOrb, Component damageSource)
    {
        for (int i = hybridOrbs.Count - 1; i >= 0; i--)
        {
            Enemy_AbyssMageHybridOrb orb = hybridOrbs[i];
            if (orb == null || orb == sourceOrb)
            {
                continue;
            }

            orb.ForceBreakWithoutNotify();
            hybridOrbs.RemoveAt(i);
        }
    }
}
