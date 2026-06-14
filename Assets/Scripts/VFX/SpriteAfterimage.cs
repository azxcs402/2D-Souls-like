using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAfterimage : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color startColor;
    private float lifetime;
    private float elapsed;

    public void Initialize(SpriteRenderer targetRenderer, float duration)
    {
        spriteRenderer = targetRenderer != null ? targetRenderer : GetComponent<SpriteRenderer>();
        lifetime = Mathf.Max(.01f, duration);
        elapsed = 0f;

        if (spriteRenderer != null)
        {
            startColor = spriteRenderer.color;
        }
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        if (spriteRenderer == null)
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(.01f, lifetime));

        Color color = startColor;
        color.a = Mathf.Lerp(startColor.a, 0f, t);
        spriteRenderer.color = color;

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
