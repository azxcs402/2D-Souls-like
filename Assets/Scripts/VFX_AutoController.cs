using UnityEngine;

[DisallowMultipleComponent]
public class VFX_AutoController : MonoBehaviour
{
    [SerializeField] private bool autoDestroy = true;
    [SerializeField, Min(.01f)] private float destroyDelay = 1f;

    [Header("Random Offset")]
    [SerializeField] private bool randomOffset = true;
    [SerializeField] private Vector2 xOffsetRange = new Vector2(-.3f, .3f);
    [SerializeField] private Vector2 yOffsetRange = new Vector2(-.3f, .3f);

    [Header("Random Rotation")]
    [SerializeField] private bool randomRotation = true;

    private void Start()
    {
        ApplyRandomOffset();
        ApplyRandomRotation();

        if (autoDestroy)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    private void ApplyRandomOffset()
    {
        if (!randomOffset)
        {
            return;
        }

        float xOffset = Random.Range(xOffsetRange.x, xOffsetRange.y);
        float yOffset = Random.Range(yOffsetRange.x, yOffsetRange.y);
        transform.position += new Vector3(xOffset, yOffset, 0f);
    }

    private void ApplyRandomRotation()
    {
        if (!randomRotation)
        {
            return;
        }

        transform.Rotate(0f, 0f, Random.Range(0f, 360f));
    }
}
