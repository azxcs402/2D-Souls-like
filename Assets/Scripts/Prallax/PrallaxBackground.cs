using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PrallaxBackground : MonoBehaviour
{
    [Serializable]
    public class BackgroundLayer
    {
        public Transform layer;
        [Tooltip("1 = follows the camera/player, 0 = fixed in world space.")]
        public Vector2 parallaxMultiplier = new Vector2(.08f, .02f);
    }

    [Header("Background")]
    [SerializeField] private Transform background;
    [SerializeField] private Transform cameraTransform;

    [Header("Background Layers")]
    [SerializeField] private BackgroundLayer[] backgroundLayers = Array.Empty<BackgroundLayer>();

    [Header("Infinite Background")]
    [SerializeField] private bool infiniteHorizontal = true;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform centerBackground;
    [SerializeField] private Transform rightBackground;
    [SerializeField] private Transform leftBackground;
    [SerializeField] private float teleportTriggerOffset;
    [SerializeField] private float triggerLineHeight = 8f;

    private Vector3 cameraStartPosition;
    private Vector3 backgroundStartPosition;
    private Vector3[] layerStartLocalPositions = Array.Empty<Vector3>();
    private float previousTargetX;
    private bool isInitialized;

    private void Reset()
    {
        background = transform;
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
    }

    private void OnValidate()
    {
        if (background == null)
        {
            background = transform;
        }

        if (backgroundLayers != null)
        {
            for (int i = 0; i < backgroundLayers.Length; i++)
            {
                BackgroundLayer layer = backgroundLayers[i];
                if (layer == null)
                {
                    continue;
                }

                layer.parallaxMultiplier = new Vector2(
                    Mathf.Clamp01(layer.parallaxMultiplier.x),
                    Mathf.Clamp01(layer.parallaxMultiplier.y)
                );
            }
        }

        triggerLineHeight = Mathf.Max(.1f, triggerLineHeight);
    }

    private void LateUpdate()
    {
        EnsureInitialized();

        if (cameraTransform == null || background == null)
        {
            return;
        }

        ApplyParallax();
        TeleportBackgroundIfNeeded();
    }

    private void EnsureInitialized()
    {
        if (!isInitialized)
        {
            Initialize();
            return;
        }

        if (background == null)
        {
            background = transform;
        }

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main != null ? Camera.main.transform : null;
        }

        if (targetTransform == null)
        {
            targetTransform = cameraTransform;
        }
    }

    private void Initialize()
    {
        if (background == null)
        {
            background = transform;
        }

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main != null ? Camera.main.transform : null;
        }

        if (targetTransform == null)
        {
            targetTransform = cameraTransform;
        }

        cameraStartPosition = cameraTransform != null ? cameraTransform.position : Vector3.zero;
        backgroundStartPosition = background != null ? background.position : transform.position;
        previousTargetX = targetTransform != null ? targetTransform.position.x : backgroundStartPosition.x;

        int layerCount = backgroundLayers != null ? backgroundLayers.Length : 0;
        layerStartLocalPositions = new Vector3[layerCount];

        for (int i = 0; i < layerCount; i++)
        {
            BackgroundLayer layer = backgroundLayers[i];
            if (layer == null || layer.layer == null)
            {
                continue;
            }

            layerStartLocalPositions[i] = layer.layer.localPosition;
        }

        isInitialized = true;
    }

    private void ApplyParallax()
    {
        Vector3 cameraOffset = cameraTransform.position - cameraStartPosition;
        background.position = backgroundStartPosition + new Vector3(cameraOffset.x, cameraOffset.y, 0f);

        if (backgroundLayers == null)
        {
            return;
        }

        for (int i = 0; i < backgroundLayers.Length; i++)
        {
            BackgroundLayer layer = backgroundLayers[i];
            if (layer == null || layer.layer == null || i >= layerStartLocalPositions.Length)
            {
                continue;
            }

            Vector2 multiplier = layer.parallaxMultiplier;
            Vector3 parallaxOffset = new Vector3(
                cameraOffset.x * (multiplier.x - 1f),
                cameraOffset.y * (multiplier.y - 1f),
                0f
            );

            layer.layer.localPosition = layerStartLocalPositions[i] + parallaxOffset;
        }
    }

    private void TeleportBackgroundIfNeeded()
    {
        if (!infiniteHorizontal || targetTransform == null || centerBackground == null)
        {
            return;
        }

        float targetX = targetTransform.position.x;
        float targetDeltaX = targetX - previousTargetX;

        if (targetDeltaX > 0f && rightBackground != null)
        {
            float rightTriggerX = rightBackground.position.x + teleportTriggerOffset;
            if (targetX >= rightTriggerX)
            {
                TeleportBackground(rightTriggerX - (centerBackground.position.x + teleportTriggerOffset));
            }
        }
        else if (targetDeltaX < 0f && leftBackground != null)
        {
            float leftTriggerX = leftBackground.position.x + teleportTriggerOffset;
            if (targetX <= leftTriggerX)
            {
                TeleportBackground(leftTriggerX - (centerBackground.position.x + teleportTriggerOffset));
            }
        }

        previousTargetX = targetTransform.position.x;
    }

    private void TeleportBackground(float xOffset)
    {
        if (Mathf.Approximately(xOffset, 0f))
        {
            return;
        }

        Vector3 teleportOffset = Vector3.right * xOffset;
        background.position += teleportOffset;
        backgroundStartPosition += teleportOffset;
        CompensateParallaxLayersAfterTeleport(teleportOffset);
        previousTargetX = targetTransform != null ? targetTransform.position.x : previousTargetX;
    }

    private void CompensateParallaxLayersAfterTeleport(Vector3 teleportOffset)
    {
        if (backgroundLayers == null || background == null)
        {
            return;
        }

        Vector3 localTeleportOffset = background.InverseTransformVector(teleportOffset);

        for (int i = 0; i < backgroundLayers.Length; i++)
        {
            BackgroundLayer layer = backgroundLayers[i];
            if (layer == null || layer.layer == null || i >= layerStartLocalPositions.Length)
            {
                continue;
            }

            if (LayerIsTeleportReference(layer.layer))
            {
                continue;
            }

            Vector3 compensation = new Vector3(
                localTeleportOffset.x * layer.parallaxMultiplier.x,
                localTeleportOffset.y * layer.parallaxMultiplier.y,
                0f
            );

            layerStartLocalPositions[i] -= compensation;
            layer.layer.localPosition -= compensation;
        }
    }

    private bool LayerIsTeleportReference(Transform layer)
    {
        return TransformBelongsToLayer(centerBackground, layer)
            || TransformBelongsToLayer(rightBackground, layer)
            || TransformBelongsToLayer(leftBackground, layer);
    }

    private bool TransformBelongsToLayer(Transform target, Transform layer)
    {
        return target != null && layer != null && (target == layer || target.IsChildOf(layer));
    }

    private void OnDrawGizmos()
    {
        if (!infiniteHorizontal)
        {
            return;
        }

        DrawTriggerLine(centerBackground, Color.white, "Center");
        DrawTriggerLine(rightBackground, Color.green, "Right teleport");
        DrawTriggerLine(leftBackground, Color.cyan, "Left teleport");
    }

    private void DrawTriggerLine(Transform reference, Color color, string label)
    {
        if (reference == null)
        {
            return;
        }

        Vector3 center = reference.position + Vector3.right * teleportTriggerOffset;
        Vector3 from = center + Vector3.down * triggerLineHeight * .5f;
        Vector3 to = center + Vector3.up * triggerLineHeight * .5f;

        Gizmos.color = color;
        Gizmos.DrawLine(from, to);

#if UNITY_EDITOR
        Handles.color = color;
        Handles.Label(to, label);
#endif
    }
}
