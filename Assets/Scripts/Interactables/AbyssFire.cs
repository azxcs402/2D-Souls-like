using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class AbyssFire : MonoBehaviour
{
    private static readonly string[] DefaultSpritePaths =
    {
        "Assets/Graphics/Decorations/Bonfire/Bonfire_1.png",
        "Assets/Graphics/Decorations/Bonfire/Bonfire_2.png",
        "Assets/Graphics/Decorations/Bonfire/Bonfire_3.png",
        "Assets/Graphics/Decorations/Bonfire/Bonfire_4.png"
    };

    [Header("Animation")]
    [SerializeField] private Sprite[] flameFrames;
    [SerializeField, Min(1f)] private float framesPerSecond = 8f;
    [SerializeField] private bool loopAnimation = true;
    [SerializeField] private bool autoLoadDefaultFrames = true;
    [SerializeField] private Color flameTint = new Color(1f, 0.92f, 0.78f, 1f);

    [Header("References")]
    [SerializeField] private SpriteRenderer flameRenderer;

    private int currentFrameIndex;
    private float frameTimer;

    private void Awake()
    {
        CacheReferences();
        TryLoadDefaultFrames();
        ApplyVisuals(0);
    }

    private void OnValidate()
    {
        CacheReferences();
        framesPerSecond = Mathf.Max(1f, framesPerSecond);
        TryLoadDefaultFrames();
        ApplyVisuals(0);
    }

    private void Update()
    {
        UpdateAnimation();
    }

    private void CacheReferences()
    {
        if (flameRenderer == null)
        {
            flameRenderer = GetComponent<SpriteRenderer>();
        }

    }

    private void UpdateAnimation()
    {
        if (flameRenderer == null || flameFrames == null || flameFrames.Length == 0)
        {
            return;
        }

        if (flameFrames.Length == 1)
        {
            ApplyFrame(0);
            return;
        }

        float secondsPerFrame = 1f / Mathf.Max(1f, framesPerSecond);
        frameTimer += Time.deltaTime;

        while (frameTimer >= secondsPerFrame)
        {
            frameTimer -= secondsPerFrame;
            AdvanceFrame();
        }

        ApplyFrame(currentFrameIndex);
    }

    private void AdvanceFrame()
    {
        if (currentFrameIndex < flameFrames.Length - 1)
        {
            currentFrameIndex++;
            return;
        }

        if (loopAnimation)
        {
            currentFrameIndex = 0;
        }
    }

    private void ApplyVisuals(int frameIndex)
    {
        currentFrameIndex = Mathf.Clamp(frameIndex, 0, Mathf.Max(0, flameFrames != null ? flameFrames.Length - 1 : 0));
        frameTimer = 0f;
        ApplyFrame(currentFrameIndex);
    }

    private void ApplyFrame(int frameIndex)
    {
        if (flameFrames == null || flameFrames.Length == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(frameIndex, 0, flameFrames.Length - 1);
        Sprite frame = flameFrames[clampedIndex];
        if (frame == null)
        {
            return;
        }

        if (flameRenderer != null)
        {
            flameRenderer.sprite = frame;
            flameRenderer.color = flameTint;
        }

    }

    private void TryLoadDefaultFrames()
    {
        if (!autoLoadDefaultFrames || flameFrames != null && flameFrames.Length > 0)
        {
            return;
        }

#if UNITY_EDITOR
        flameFrames = new Sprite[DefaultSpritePaths.Length];
        for (int i = 0; i < DefaultSpritePaths.Length; i++)
        {
            flameFrames[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(DefaultSpritePaths[i]);
        }
#endif
    }
}
