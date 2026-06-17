using UnityEngine;

[System.Serializable]
public class ParallaxLayer
{
    [SerializeField] private Transform background;
    [SerializeField] private float parallaxMultiplier;
    [SerializeField] private float imageWidthOffset = 10;

    private Vector3 startPosition;
    private float imageFullWidth;
    private float imageHalfWidth;
    private float loopOffsetX;

    public void Initialize()
    {
        if (background == null)
        {
            return;
        }

        startPosition = background.position;
        loopOffsetX = 0f;

        SpriteRenderer spriteRenderer = background.GetComponent<SpriteRenderer>();
        imageFullWidth = spriteRenderer != null ? spriteRenderer.bounds.size.x : 0f;
        imageHalfWidth = imageFullWidth / 2;
    }

    public void Move(Vector3 currentCameraPosition, Vector3 startCameraPosition)
    {
        if (background == null)
        {
            return;
        }

        Vector3 cameraDelta = currentCameraPosition - startCameraPosition;
        Vector3 targetPosition = startPosition;
        targetPosition.x += (cameraDelta.x * parallaxMultiplier) + loopOffsetX;

        if (parallaxMultiplier >= 1f)
        {
            targetPosition.y += cameraDelta.y;
        }

        background.position = targetPosition;
    }

    public void LoopBackground(float cameraLefteEdge, float cameraRightEdge)
    {
        if (background == null || imageFullWidth <= 0f)
        {
            return;
        }

        if (parallaxMultiplier >= 1f)
        {
            return;
        }

        float imageRightEdge = (background.position.x + imageHalfWidth) - imageWidthOffset;
        float imageLeftEdge = (background.position.x - imageHalfWidth) + imageWidthOffset;

        if (imageRightEdge < cameraLefteEdge)
        {
            loopOffsetX += imageFullWidth;
            background.position += Vector3.right * imageFullWidth;
        }
        else if (imageLeftEdge > cameraRightEdge)
        {
            loopOffsetX -= imageFullWidth;
            background.position += Vector3.right * -imageFullWidth;
        }
    }
}
