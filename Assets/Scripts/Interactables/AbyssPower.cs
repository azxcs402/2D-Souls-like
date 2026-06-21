using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public class AbyssPower : MonoBehaviour
{
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
        ApplyEditorPreviewState();
    }

    private void OnEnable()
    {
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
}
