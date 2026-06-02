using UnityEngine;

public abstract class Entity : MonoBehaviour
{
    [Header("Ground Check")]
    [SerializeField] protected float groundCheckDistance = .08f;
    [SerializeField] protected LayerMask whatIsGround;

    [Header("Wall Check")]
    [SerializeField] protected float wallCheckDistance = .03f;
    [SerializeField, Range(.1f, 1.5f)] protected float wallCheckVerticalSpan = .65f;
    [SerializeField] protected Transform primaryWallCheck;
    [SerializeField] protected Transform secondaryWallCheck;

    public Rigidbody2D rb { get; protected set; }
    public CapsuleCollider2D cd { get; protected set; }
    public Animator anim { get; protected set; }
    public int FacingDirection => facingDirection;
    public float GroundCheckDistance => groundCheckDistance;
    public float WallCheckDistance => wallCheckDistance;
    public float WallCheckVerticalSpan => wallCheckVerticalSpan;

    protected int facingDirection = 1;
    protected virtual bool UseWallChecks => true;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cd = GetComponent<CapsuleCollider2D>();
        anim = GetComponentInChildren<Animator>();

        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (cd != null && cd.sharedMaterial == null)
        {
            cd.sharedMaterial = new PhysicsMaterial2D("Entity_NoFriction")
            {
                friction = 0,
                bounciness = 0
            };
        }

        if (anim != null)
        {
            anim.applyRootMotion = false;
            anim.updateMode = AnimatorUpdateMode.Normal;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            anim.speed = 1f;
        }

        EnsureWallCheckTransforms();
    }

    protected virtual void Reset()
    {
        EnsureWallCheckTransforms();
    }

    protected virtual void OnValidate()
    {
        groundCheckDistance = Mathf.Max(.01f, groundCheckDistance);
        wallCheckDistance = Mathf.Max(.005f, wallCheckDistance);
        wallCheckVerticalSpan = Mathf.Max(.1f, wallCheckVerticalSpan);

        if (!Application.isPlaying && UseWallChecks)
        {
            EnsureWallCheckTransforms();
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Vector2 groundRayOrigin = GetGroundCheckRayOrigin();

        Gizmos.color = Color.red;
        Gizmos.DrawLine(groundRayOrigin, groundRayOrigin + Vector2.down * groundCheckDistance);

        Gizmos.color = Color.red;
        if (UseWallChecks)
        {
            DrawWallCheckGizmo(primaryWallCheck);
            DrawWallCheckGizmo(secondaryWallCheck);
        }
    }

    public void SetVelocity(float xVelocity, float yVelocity)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        rb.velocity = new Vector2(xVelocity, yVelocity);
    }

    public void PlayAnimatorState(string stateName, float normalizedTime = 0f, int layer = 0)
    {
        if (anim == null || string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        if (!anim.isActiveAndEnabled)
        {
            anim.enabled = true;
        }

        anim.Rebind();
        string fullStateName = stateName.Contains(".") ? stateName : $"Base Layer.{stateName}";
        anim.Play(fullStateName, layer, normalizedTime);
        anim.Update(0f);
    }

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

    public bool WallDetected()
    {
        if (!UseWallChecks)
        {
            return false;
        }

        return WallDetectedAtTransform(primaryWallCheck)
            && WallDetectedAtTransform(secondaryWallCheck);
    }

    public bool WallContactDetected()
    {
        if (!UseWallChecks)
        {
            return false;
        }

        return WallDetectedAtTransform(primaryWallCheck, 1)
            && WallDetectedAtTransform(secondaryWallCheck, 1)
            || WallDetectedAtTransform(primaryWallCheck, -1)
            && WallDetectedAtTransform(secondaryWallCheck, -1);
    }

    public bool FacingWallContactDetected()
    {
        if (!UseWallChecks)
        {
            return false;
        }

        return FacingWallContactDetected(facingDirection);
    }

    public bool FacingWallContactDetected(int direction)
    {
        if (direction == 0)
        {
            return false;
        }

        if (!UseWallChecks)
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

    public bool EdgeDetected(float forwardOffset, float checkDistance)
    {
        return EdgeDetected(facingDirection, forwardOffset, checkDistance);
    }

    public bool EdgeDetected(int direction, float forwardOffset, float checkDistance)
    {
        if (direction == 0)
        {
            return false;
        }

        Bounds bounds = GetColliderBounds();
        float facingSign = direction > 0 ? 1f : -1f;
        float originX = (direction > 0 ? bounds.max.x : bounds.min.x) + facingSign * Mathf.Max(0f, forwardOffset);
        Vector2 origin = new Vector2(originX, bounds.min.y + .02f);

        return !Physics2D.Raycast(
            origin,
            Vector2.down,
            Mathf.Max(.01f, checkDistance),
            whatIsGround
        );
    }

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

    public void TurnAround()
    {
        Flip(-facingDirection);
    }

    public void FaceDirection(int direction)
    {
        if (direction != 1 && direction != -1)
        {
            return;
        }

        if (facingDirection != direction)
        {
            Flip(direction);
        }
    }

    protected Bounds GetColliderBounds()
    {
        if (cd == null)
        {
            cd = GetComponent<CapsuleCollider2D>();
        }

        return cd != null
            ? cd.bounds
            : new Bounds(transform.position, Vector3.one);
    }

    protected Vector2 GetGroundCheckRayOrigin()
    {
        Bounds bounds = GetColliderBounds();
        return new Vector2(bounds.center.x, bounds.min.y + .02f);
    }

    protected Vector2 GetGroundCheckBoxSize(Bounds bounds)
    {
        float height = Mathf.Min(.08f, bounds.size.y * .1f);
        return new Vector2(bounds.size.x * .8f, height);
    }

    protected Vector2 GetGroundCheckBoxOrigin(Bounds bounds, Vector2 boxSize)
    {
        return new Vector2(bounds.center.x, bounds.min.y + boxSize.y * .5f);
    }

    protected void EnsureWallCheckTransforms()
    {
        if (!UseWallChecks)
        {
            return;
        }

        primaryWallCheck = EnsureWallCheckTransform(primaryWallCheck, "PrimaryWallCheck", new Vector2(.32f, wallCheckVerticalSpan * .5f));
        secondaryWallCheck = EnsureWallCheckTransform(secondaryWallCheck, "SecondaryWallCheck", new Vector2(.32f, -wallCheckVerticalSpan * .5f));
        ApplyWallCheckVerticalSpan();
    }

    protected void ApplyWallCheckVerticalSpan()
    {
        if (!UseWallChecks)
        {
            return;
        }

        ApplyWallCheckLocalY(primaryWallCheck, wallCheckVerticalSpan * .5f);
        ApplyWallCheckLocalY(secondaryWallCheck, -wallCheckVerticalSpan * .5f);
    }

    protected void ApplyWallCheckLocalY(Transform wallCheckTransform, float localY)
    {
        if (wallCheckTransform == null || wallCheckTransform.parent != transform)
        {
            return;
        }

        Vector3 localPosition = wallCheckTransform.localPosition;
        localPosition.y = localY;
        wallCheckTransform.localPosition = localPosition;
    }

    protected Transform EnsureWallCheckTransform(Transform wallCheckTransform, string wallCheckName, Vector2 localPosition)
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

    protected void DrawWallCheckGizmo(Transform wallCheckTransform)
    {
        if (!UseWallChecks)
        {
            return;
        }

        if (wallCheckTransform == null)
        {
            return;
        }

        Vector2 origin = wallCheckTransform.position;
        Gizmos.DrawLine(origin, origin + Vector2.right * facingDirection * wallCheckDistance);
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

    protected void Flip(int direction)
    {
        facingDirection = direction;

        transform.rotation =
            facingDirection == 1
            ? Quaternion.identity
            : Quaternion.Euler(0, 180, 0);
    }
}
