using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public class AbyssPower : MonoBehaviour
{
    private const string IgnoreRaycastLayerName = "Ignore Raycast";

    [SerializeField] private Renderer visualRenderer;
    [SerializeField] private bool visibleInEditor = true;

    public void SetVisible(bool visible)
    {
        if (Application.isPlaying)
        {
            gameObject.SetActive(visible);
            return;
        }

        CacheReferences();
        if (visualRenderer != null)
        {
            visualRenderer.enabled = visibleInEditor && visible;
        }
    }

    private void OnValidate()
    {
        EnsurePureBackgroundSetup();
        ApplyEditorPreviewState();
    }

    private void OnEnable()
    {
        EnsurePureBackgroundSetup();
        if (!Application.isPlaying)
        {
            ApplyEditorPreviewState();
            return;
        }
    }

    private void CacheReferences()
    {
        if (visualRenderer == null)
        {
            visualRenderer = GetComponent<Renderer>();
        }

        if (visualRenderer == null)
        {
            visualRenderer = GetComponentInChildren<Renderer>(true);
        }
    }

    private void ApplyEditorPreviewState()
    {
        if (Application.isPlaying)
        {
            return;
        }

        CacheReferences();
        if (visualRenderer == null)
        {
            return;
        }

        visualRenderer.enabled = visibleInEditor;
    }

    private void EnsurePureBackgroundSetup()
    {
        int ignoreRaycastLayer = LayerMask.NameToLayer(IgnoreRaycastLayerName);
        if (ignoreRaycastLayer >= 0 && gameObject.layer != ignoreRaycastLayer)
        {
            gameObject.layer = ignoreRaycastLayer;
        }

        ConvertPhysicsComponentsToTriggers();
    }

    private void ConvertPhysicsComponentsToTriggers()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null)
            {
                continue;
            }

            collider.isTrigger = true;
        }

        Rigidbody2D[] bodies = GetComponentsInChildren<Rigidbody2D>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody2D body = bodies[i];
            if (body == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(body);
            }
            else
            {
                Object.DestroyImmediate(body);
            }
        }
    }
}
