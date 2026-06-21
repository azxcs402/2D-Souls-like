using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class ArenaDoorController : MonoBehaviour
{
    [FormerlySerializedAs("doorRoot")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private GameObject blockingRoot;
    [SerializeField] private GameObject blockingDropRoot;
    [SerializeField] private Collider2D[] blockingColliders = Array.Empty<Collider2D>();
    [SerializeField] private Animator animator;
    [SerializeField] private string openBoolParameter = "open";
    [SerializeField] private bool openOnStart = true;
    [SerializeField, Min(0f)] private float openVerticalOffset = 2f;
    [SerializeField, Min(0.01f)] private float openCloseDuration = 0.18f;
    [SerializeField] private bool animateOpenClose = true;

    public bool IsOpen { get; private set; }

    private Vector3 closedLocalPosition;
    private Vector3 openLocalPosition;
    private float resolvedOpenVerticalOffset;
    private Coroutine movementRoutine;

    private void Awake()
    {
        if (visualRoot == null)
        {
            Transform visualChild = transform.Find("Visual");
            if (visualChild != null)
            {
                visualRoot = visualChild.gameObject;
            }
        }

        if (blockingRoot == null)
        {
            Transform blockingChild = transform.Find("Blocking_1");
            if (blockingChild == null)
            {
                blockingChild = transform.Find("Blocking");
            }
            if (blockingChild != null)
            {
                blockingRoot = blockingChild.gameObject;
            }
        }

        if (blockingDropRoot == null)
        {
            Transform blockingDropChild = transform.Find("Blocking_2");
            if (blockingDropChild == null && blockingRoot != null && blockingRoot.transform.parent == transform)
            {
                blockingDropChild = blockingRoot.transform;
            }

            if (blockingDropChild != null)
            {
                blockingDropRoot = blockingDropChild.gameObject;
            }
        }

        ApplyBossDoorGroundLayerIfNeeded();

        if (animator == null)
        {
            animator = visualRoot != null
                ? visualRoot.GetComponentInChildren<Animator>(true)
                : GetComponentInChildren<Animator>(true);
        }

        if (blockingColliders == null || blockingColliders.Length == 0)
        {
            blockingColliders = CollectBlockingColliders();
        }

        resolvedOpenVerticalOffset = ResolveOpenVerticalOffset();

        Transform animatedBlocking = blockingDropRoot != null ? blockingDropRoot.transform : blockingRoot != null ? blockingRoot.transform : null;
        if (animatedBlocking != null)
        {
            closedLocalPosition = animatedBlocking.localPosition;
            openLocalPosition = closedLocalPosition + Vector3.up * resolvedOpenVerticalOffset;
        }
    }

    private void Start()
    {
        SetOpen(openOnStart, true);
    }

    public void SetLocked(bool locked)
    {
        SetOpen(!locked);
    }

    public void SetOpen(bool open)
    {
        SetOpen(open, false);
    }

    public void SetOpen(bool open, bool instant)
    {
        IsOpen = open;

        Vector3 targetLocalPosition = open ? openLocalPosition : closedLocalPosition;

        if (movementRoutine != null)
        {
            StopCoroutine(movementRoutine);
            movementRoutine = null;
        }

        if (visualRoot != null && !visualRoot.activeSelf)
        {
            visualRoot.SetActive(true);
        }

        if (blockingColliders != null)
        {
            for (int i = 0; i < blockingColliders.Length; i++)
            {
                if (blockingColliders[i] != null)
                {
                    blockingColliders[i].enabled = !open;
                }
            }
        }

        SetBlockingVisibility(!open);

        if (animator != null && !string.IsNullOrWhiteSpace(openBoolParameter))
        {
            animator.SetBool(openBoolParameter, open);
        }

        if (instant || !animateOpenClose || openVerticalOffset <= 0f || openCloseDuration <= 0f)
        {
            Transform animatedBlocking = blockingDropRoot != null ? blockingDropRoot.transform : blockingRoot != null ? blockingRoot.transform : transform;
            animatedBlocking.localPosition = targetLocalPosition;
            return;
        }

        Transform animatedBlockingRoot = blockingDropRoot != null ? blockingDropRoot.transform : blockingRoot != null ? blockingRoot.transform : transform;
        movementRoutine = StartCoroutine(MoveDoorRoutine(animatedBlockingRoot, targetLocalPosition, openCloseDuration));
    }

    private void ApplyBossDoorGroundLayerIfNeeded()
    {
        if (!IsInsideBossEncounter())
        {
            return;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
        {
            return;
        }

        SetLayerRecursively(blockingRoot, groundLayer);
        SetLayerRecursively(blockingDropRoot, groundLayer);
    }

    private float ResolveOpenVerticalOffset()
    {
        float resolvedOffset = openVerticalOffset;
        float measuredHeight = MeasureDoorHeight();
        if (measuredHeight > 0f)
        {
            resolvedOffset = Mathf.Max(resolvedOffset, measuredHeight);
        }

        return resolvedOffset;
    }

    private float MeasureDoorHeight()
    {
        float height = 0f;
        height = Mathf.Max(height, MeasureTilemapHeight(blockingDropRoot));
        height = Mathf.Max(height, MeasureTilemapHeight(blockingRoot));

        if (height <= 0f)
        {
            height = MeasureTilemapHeight(visualRoot);
        }

        return height;
    }

    private static float MeasureTilemapHeight(GameObject root)
    {
        if (root == null)
        {
            return 0f;
        }

        float height = 0f;
        Tilemap[] tilemaps = root.GetComponentsInChildren<Tilemap>(true);
        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];
            if (tilemap == null)
            {
                continue;
            }

            BoundsInt cellBounds = tilemap.cellBounds;
            height = Mathf.Max(height, cellBounds.size.y);
        }

        return height;
    }

    private bool IsInsideBossEncounter()
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name == "ArenaBossEncounter")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null || layer < 0)
        {
            return;
        }

        SetLayerRecursively(root.transform, layer);
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null || layer < 0)
        {
            return;
        }

        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
    }

    private void SetBlockingVisibility(bool visible)
    {
        if (blockingRoot != null && blockingRoot.activeSelf != visible)
        {
            blockingRoot.SetActive(visible);
        }

        if (blockingDropRoot != null && blockingDropRoot.activeSelf != visible)
        {
            blockingDropRoot.SetActive(visible);
        }
    }

    private Collider2D[] CollectBlockingColliders()
    {
        if (blockingRoot == null && blockingDropRoot == null)
        {
            return GetComponentsInChildren<Collider2D>(true);
        }

        Collider2D[] staticColliders = blockingRoot != null ? blockingRoot.GetComponentsInChildren<Collider2D>(true) : Array.Empty<Collider2D>();
        Collider2D[] dropColliders = blockingDropRoot != null && blockingDropRoot != blockingRoot
            ? blockingDropRoot.GetComponentsInChildren<Collider2D>(true)
            : Array.Empty<Collider2D>();

        Collider2D[] result = new Collider2D[staticColliders.Length + dropColliders.Length];
        int index = 0;
        for (int i = 0; i < staticColliders.Length; i++)
        {
            result[index++] = staticColliders[i];
        }

        for (int i = 0; i < dropColliders.Length; i++)
        {
            result[index++] = dropColliders[i];
        }

        return result;
    }

    private System.Collections.IEnumerator MoveDoorRoutine(Transform animatedBlockingRoot, Vector3 targetLocalPosition, float duration)
    {
        Vector3 startLocalPosition = animatedBlockingRoot.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            animatedBlockingRoot.localPosition = Vector3.LerpUnclamped(startLocalPosition, targetLocalPosition, t);
            yield return null;
        }

        animatedBlockingRoot.localPosition = targetLocalPosition;
        movementRoutine = null;
    }
}
