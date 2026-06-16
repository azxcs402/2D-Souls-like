using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Bonfire : MonoBehaviour, ISaveable
{
    private static readonly string[] DefaultSpriteNames =
    {
        "Bonfire_1",
        "Bonfire_2",
        "Bonfire_3",
        "Bonfire_4"
    };

    [Header("Checkpoint")]
    [SerializeField] private string bonfireId = "bonfire_default";
    [SerializeField] private bool restorePlayerOnRest = true;
    [SerializeField] private bool reloadSceneOnRest = true;
    [SerializeField, Min(0f)] private float restDelay = 0.05f;
    [SerializeField] private bool startLit = false;

    [Header("Animation")]
    [SerializeField] private Sprite[] flameFrames;
    [SerializeField, Min(1f)] private float framesPerSecond = 8f;
    [SerializeField] private bool loopAnimation = true;
    [SerializeField] private bool autoLoadDefaultFrames = true;
    [SerializeField] private Color unlitTint = new Color(0.65f, 0.65f, 0.65f, 0.9f);
    [SerializeField] private Color litTint = new Color(1f, 0.92f, 0.78f, 1f);
    [SerializeField, Min(0f)] private float ignitePulseScale = 1.1f;
    [SerializeField, Min(0.01f)] private float ignitePulseDuration = 0.35f;

    [Header("Interaction")]
    [SerializeField] private string interactPrompt = "Press F to rest";
    [SerializeField] private string ignitePrompt = "Press F to light";
    [SerializeField] private int playerSortingOrderOffset = 10;
    [SerializeField] private Vector3 promptLocalOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField, Min(1)] private int promptFontSize = 24;
    [SerializeField] private Color promptColor = Color.white;

    [Header("References")]
    [SerializeField] private SpriteRenderer flameRenderer;
    [SerializeField] private BoxCollider2D interactionCollider;

    public string BonfireId => bonfireId;
    public string InteractPrompt => isLit ? interactPrompt : ignitePrompt;
    public bool IsLit => isLit;

    private Player currentPlayer;
    private bool isResting;
    private bool isLit;
    private int currentFrameIndex;
    private float frameTimer;
    private readonly List<PlayerRendererState> playerRendererStates = new();
    private Coroutine igniteCoroutine;
    private Canvas promptCanvas;
    private Text promptText;
    private GameObject promptRoot;

    private struct PlayerRendererState
    {
        public SpriteRenderer renderer;
        public int sortingOrder;
    }

    private void Awake()
    {
        CacheReferences();
        ConfigureCollider();
        EnsurePromptVisual();
        TryLoadDefaultFrames();
        isLit = startLit;
        ApplyLitVisuals();
        UpdatePromptVisual();
        ApplyFrame(0);
    }

    private void OnValidate()
    {
        CacheReferences();
        ConfigureCollider();
        framesPerSecond = Mathf.Max(1f, framesPerSecond);
        promptFontSize = Mathf.Max(1, promptFontSize);
        ignitePulseScale = Mathf.Max(1f, ignitePulseScale);
        ignitePulseDuration = Mathf.Max(0.01f, ignitePulseDuration);

        if (!Application.isPlaying)
        {
            return;
        }

        EnsurePromptVisual();
        TryLoadDefaultFrames();
        ApplyLitVisuals();
        UpdatePromptVisual();
        ApplyFrame(0);
    }

    private void Update()
    {
        UpdateAnimation();

        if (isResting || currentPlayer == null)
        {
            return;
        }

        if (currentPlayer.IsDead)
        {
            return;
        }

        if (Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame)
        {
            return;
        }

        StartCoroutine(RestAtBonfireRoutine());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player != null)
        {
            currentPlayer = player;
            ApplyPlayerForeground(player);
            UpdatePromptVisual();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player != null && player == currentPlayer)
        {
            RestorePlayerForeground();
            currentPlayer = null;
            UpdatePromptVisual();
        }
    }

    private IEnumerator RestAtBonfireRoutine()
    {
        isResting = true;

        if (!isLit)
        {
            SetLit(true, true);
        }

        Player player = currentPlayer;
        if (player != null && restorePlayerOnRest)
        {
            player.RestoreHealthToFull();
            player.ResetForBonfire();
        }

        if (SaveManager.instance != null)
        {
            SaveManager.instance.SaveCheckpoint(transform.position, SceneManager.GetActiveScene().name);
        }

        if (restDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(restDelay);
        }

        if (reloadSceneOnRest)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (GameManager.instance != null)
            {
                GameManager.instance.ChangeScene(sceneName, false);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        isResting = false;
    }

    public void LoadData(GameData data)
    {
        if (data == null)
        {
            return;
        }

        isLit = data.litBonfireIds != null && data.litBonfireIds.Contains(GetSaveId());
        if (startLit)
        {
            isLit = true;
        }

        ApplyLitVisuals();
    }

    public void SaveData(ref GameData data)
    {
        if (data == null)
        {
            data = new GameData();
        }

        data.litBonfireIds ??= new List<string>();
        string saveId = GetSaveId();
        if (isLit && !data.litBonfireIds.Contains(saveId))
        {
            data.litBonfireIds.Add(saveId);
        }
    }

    private void CacheReferences()
    {
        if (flameRenderer == null)
        {
            flameRenderer = GetComponent<SpriteRenderer>();
        }

        if (interactionCollider == null)
        {
            interactionCollider = GetComponent<BoxCollider2D>();
        }
    }

    private void ConfigureCollider()
    {
        if (interactionCollider == null)
        {
            return;
        }

        interactionCollider.isTrigger = true;

        if (interactionCollider.size == Vector2.zero)
        {
            interactionCollider.size = new Vector2(1.25f, 1.75f);
        }

        if (interactionCollider.offset == Vector2.zero)
        {
            interactionCollider.offset = new Vector2(0f, 0.85f);
        }
    }

    private void ApplyPlayerForeground(Player player)
    {
        if (player == null)
        {
            return;
        }

        RestorePlayerForeground();
        playerRendererStates.Clear();

        SpriteRenderer[] renderers = player.GetComponentsInChildren<SpriteRenderer>(true);
        int baseOrder = GetForegroundOrderBase();

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            playerRendererStates.Add(new PlayerRendererState
            {
                renderer = renderer,
                sortingOrder = renderer.sortingOrder
            });

            renderer.sortingOrder = baseOrder + i;
        }
    }

    private void RestorePlayerForeground()
    {
        for (int i = 0; i < playerRendererStates.Count; i++)
        {
            PlayerRendererState state = playerRendererStates[i];
            if (state.renderer != null)
            {
                state.renderer.sortingOrder = state.sortingOrder;
            }
        }

        playerRendererStates.Clear();
    }

    private int GetForegroundOrderBase()
    {
        return (flameRenderer != null ? flameRenderer.sortingOrder : 0) + playerSortingOrderOffset;
    }

    private void TryLoadDefaultFrames()
    {
        if (!autoLoadDefaultFrames || (flameFrames != null && flameFrames.Length > 0))
        {
            return;
        }

        flameFrames = new Sprite[DefaultSpriteNames.Length];
        for (int i = 0; i < DefaultSpriteNames.Length; i++)
        {
            flameFrames[i] = LoadSpriteByName(DefaultSpriteNames[i]);
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

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / framesPerSecond;

        if (frameTimer < frameDuration)
        {
            return;
        }

        frameTimer -= frameDuration;
        currentFrameIndex++;

        if (currentFrameIndex >= flameFrames.Length)
        {
            if (!loopAnimation)
            {
                currentFrameIndex = flameFrames.Length - 1;
                frameTimer = 0f;
                ApplyFrame(currentFrameIndex);
                return;
            }

            currentFrameIndex = 0;
        }

        ApplyFrame(currentFrameIndex);
    }

    private void ApplyFrame(int frameIndex)
    {
        if (flameRenderer == null || flameFrames == null || flameFrames.Length == 0)
        {
            return;
        }

        frameIndex = Mathf.Clamp(frameIndex, 0, flameFrames.Length - 1);
        Sprite frame = flameFrames[frameIndex];
        if (frame != null)
        {
            flameRenderer.sprite = frame;
        }
    }

    private void ApplyLitVisuals()
    {
        if (flameRenderer == null)
        {
            return;
        }

        flameRenderer.color = isLit ? litTint : unlitTint;
        flameRenderer.sortingOrder = isLit ? 12 : 10;
        UpdatePromptVisual();
    }

    private void SetLit(bool lit, bool playIgniteEffect)
    {
        bool changed = isLit != lit;
        isLit = lit;
        ApplyLitVisuals();

        if (!changed)
        {
            return;
        }

        if (igniteCoroutine != null)
        {
            StopCoroutine(igniteCoroutine);
            igniteCoroutine = null;
        }

        if (lit && playIgniteEffect)
        {
            igniteCoroutine = StartCoroutine(IgnitePulseCo());
        }
    }

    private IEnumerator IgnitePulseCo()
    {
        if (flameRenderer == null)
        {
            yield break;
        }

        Vector3 baseScale = transform.localScale;
        Vector3 pulseScale = baseScale * ignitePulseScale;
        float halfDuration = ignitePulseDuration * 0.5f;

        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            transform.localScale = Vector3.Lerp(baseScale, pulseScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            transform.localScale = Vector3.Lerp(pulseScale, baseScale, t);
            yield return null;
        }

        transform.localScale = baseScale;
        igniteCoroutine = null;
    }

    private Sprite LoadSpriteByName(string spriteName)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
        {
            return null;
        }

#if UNITY_EDITOR
        string[] candidatePaths =
        {
            "Assets/Graphics/Decorations/Bonfire/Bonfire_1.png",
            "Assets/Graphics/Decorations/Bonfire/Bonfire_2.png",
            "Assets/Graphics/Decorations/Bonfire/Bonfire_3.png",
            "Assets/Graphics/Decorations/Bonfire/Bonfire_4.png"
        };

        foreach (string path in candidatePaths)
        {
            Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null && sprite.name == spriteName)
            {
                return sprite;
            }
        }
#endif

        return null;
    }

    private string GetSaveId()
    {
        return string.IsNullOrWhiteSpace(bonfireId) ? name : bonfireId.Trim();
    }

    private void EnsurePromptVisual()
    {
        if (promptRoot == null)
        {
            Transform existing = transform.Find("Prompt");
            promptRoot = existing != null ? existing.gameObject : new GameObject("Prompt");

            if (existing == null)
            {
                promptRoot.transform.SetParent(transform, false);
            }
        }

        if (promptCanvas == null)
        {
            promptCanvas = promptRoot.GetComponent<Canvas>();
        }

        if (promptCanvas == null)
        {
            promptCanvas = promptRoot.AddComponent<Canvas>();
        }

        promptCanvas.renderMode = RenderMode.WorldSpace;
        promptCanvas.overrideSorting = true;
        promptCanvas.sortingOrder = 50;

        RectTransform rect = promptRoot.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = promptRoot.AddComponent<RectTransform>();
        }

        rect.localPosition = promptLocalOffset;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one * 0.01f;
        rect.sizeDelta = new Vector2(400f, 120f);

        if (promptText == null)
        {
            promptText = promptRoot.GetComponentInChildren<Text>(true);
        }

        if (promptText == null)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(promptRoot.transform, false);
            promptText = textObject.GetComponent<Text>();
        }

        RectTransform textRect = promptText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        promptText.font = LoadPromptFont();
        promptText.fontSize = promptFontSize;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = promptColor;
        promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
        promptText.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private void UpdatePromptVisual()
    {
        EnsurePromptVisual();

        if (promptRoot == null || promptText == null)
        {
            return;
        }

        promptRoot.SetActive(currentPlayer != null && !isResting);
        promptText.text = InteractPrompt;
    }

    private Font LoadPromptFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        return Font.CreateDynamicFontFromOSFont("Arial", 16);
    }

    private void OnDisable()
    {
        RestorePlayerForeground();
        UpdatePromptVisual();
    }

    private void OnDestroy()
    {
        RestorePlayerForeground();
    }
}
