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

    [Header("Audio")]
    [SerializeField] private AudioKey flameLoopSfxKey = AudioKey.AbyssFireFlameLoop;

    [Header("Flame Audio")]
    [SerializeField, Range(0f, 1f), Tooltip("Volume multiplier for the abyss fire loop.")]
    private float flameLoopVolume = 0.35f;
    [SerializeField, Min(0f), Tooltip("Distance where the abyss fire loop remains at full volume.")]
    private float flameLoopMinDistance = 0.75f;
    [SerializeField, Min(0f), Tooltip("Distance where the abyss fire loop fades to silence.")]
    private float flameLoopMaxDistance = 3.5f;

    [Header("References")]
    [SerializeField] private SpriteRenderer flameRenderer;
    [SerializeField] private AudioSource flameLoopSource;

    private int currentFrameIndex;
    private float frameTimer;

    private void Awake()
    {
        CacheReferences();
        TryLoadDefaultFrames();
        ApplyVisuals(0);
        UpdateFlameLoopAudio();
    }

    private void OnValidate()
    {
        CacheReferences();
        framesPerSecond = Mathf.Max(1f, framesPerSecond);
        flameLoopMaxDistance = Mathf.Max(flameLoopMinDistance, flameLoopMaxDistance);
        TryLoadDefaultFrames();
        ApplyVisuals(0);
        if (Application.isPlaying)
        {
            UpdateFlameLoopAudio();
        }
    }

    private void OnEnable()
    {
        UpdateFlameLoopAudio();
    }

    private void OnDisable()
    {
        StopFlameLoopAudio();
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

        if (flameLoopSource == null)
        {
            flameLoopSource = GetComponent<AudioSource>();
        }

        if (flameLoopSource == null && Application.isPlaying)
        {
            flameLoopSource = gameObject.AddComponent<AudioSource>();
        }

        if (flameLoopSource != null)
        {
            flameLoopSource.playOnAwake = false;
            flameLoopSource.loop = true;
            flameLoopSource.spatialBlend = 1f;
            flameLoopSource.rolloffMode = AudioRolloffMode.Logarithmic;
            flameLoopSource.dopplerLevel = 0f;
            flameLoopSource.minDistance = flameLoopMinDistance;
            flameLoopSource.maxDistance = flameLoopMaxDistance;
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

    private void UpdateFlameLoopAudio()
    {
        if (flameLoopSource == null)
        {
            return;
        }

        AudioManager.instance?.PlayLoopingSFX(flameLoopSfxKey, flameLoopSource, flameLoopVolume);
    }

    private void StopFlameLoopAudio()
    {
        if (flameLoopSource == null)
        {
            return;
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.StopLoopingSFX(flameLoopSource);
            return;
        }

        if (flameLoopSource.isPlaying)
        {
            flameLoopSource.Stop();
        }
    }

    public void CopyFlameAudioSettingsFrom(AbyssFire source)
    {
        if (source == null || source == this)
        {
            return;
        }

        flameLoopSfxKey = source.flameLoopSfxKey;
        flameLoopVolume = source.flameLoopVolume;
        flameLoopMinDistance = source.flameLoopMinDistance;
        flameLoopMaxDistance = source.flameLoopMaxDistance;

        if (Application.isPlaying)
        {
            UpdateFlameLoopAudio();
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
