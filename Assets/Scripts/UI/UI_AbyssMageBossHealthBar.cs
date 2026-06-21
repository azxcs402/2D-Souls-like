using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[ExecuteAlways]
public class UI_AbyssMageBossHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform rootRect;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image[] abyssFireMarkers;
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private TextMeshProUGUI percentText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Text")]
    [SerializeField] private string bossDisplayName = "\u6df1\u6e0a\u6cd5\u5e08";
    [SerializeField] private Color bossNameColor = new Color(1f, 0.95f, 0.88f, 1f);

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float fadeInDuration = 0.18f;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float fillLerpSpeed = 42f;
    [SerializeField, Range(0.01f, 1f)] private float hiddenScale = 0.96f;
    [SerializeField, Range(1f, 1.2f)] private float visibleScale = 1.03f;
    [SerializeField, Range(0f, 1f)] private float lowHealthThreshold = 0.35f;
    [SerializeField, Min(0f)] private float lowHealthPulseSpeed = 3.2f;
    [SerializeField, Range(0f, 1f)] private float lowHealthPulseStrength = 0.12f;

    [Header("Colors")]
    [SerializeField] private Color healthyFillColor = new Color(0.88f, 0.25f, 0.18f, 1f);
    [SerializeField] private Color midHealthFillColor = new Color(0.92f, 0.58f, 0.2f, 1f);
    [SerializeField] private Color lowHealthFillColor = new Color(0.98f, 0.78f, 0.2f, 1f);
    [SerializeField] private Color backgroundIdleColor = new Color(0.04f, 0.04f, 0.05f, 0.82f);
    [SerializeField] private Color backgroundLowHealthColor = new Color(0.08f, 0.02f, 0.02f, 0.9f);
    [SerializeField] private Color frameIdleColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color frameLowHealthColor = new Color(1f, 0.55f, 0.2f, 1f);
    [SerializeField] private Color abyssFireActiveColor = new Color(0.96f, 0.58f, 0.16f, 1f);
    [SerializeField] private Color abyssFireInactiveColor = new Color(0.3f, 0.16f, 0.12f, 0.22f);
    [SerializeField] private Color abyssFireLowHealthGlowColor = new Color(1f, 0.76f, 0.35f, 1f);
    [SerializeField, Min(0f)] private float abyssFirePulseSpeed = 5.2f;
    [SerializeField, Range(0f, 0.35f)] private float abyssFirePulseStrength = 0.14f;
    [SerializeField, Range(0.8f, 1.25f)] private float abyssFireInactiveScale = 0.88f;
    [SerializeField, Range(0.8f, 1.4f)] private float abyssFireActiveScale = 1.08f;
    [SerializeField, Min(0.05f)] private float introCompletionBurstDuration = 0.45f;
    [SerializeField, Range(1f, 1.5f)] private float introCompletionBurstScale = 1.12f;
    [SerializeField, Range(0f, 1f)] private float introCompletionFlashStrength = 0.75f;
    [SerializeField] private Color introCompletionFlashColor = new Color(1f, 0.92f, 0.62f, 1f);

    [Header("Editor Preview")]
    [SerializeField] private bool previewInEditor = true;

    private Entity_Health bossHealth;
    private bool isVisible;
    private float targetHealthPercent;
    private float displayedHealthPercent;
    private float visibilityT;
    private bool hasReceivedState;
    private float introCompletionBurstT = -1f;

    public void Configure(Slider slider, TextMeshProUGUI nameText, CanvasGroup group)
    {
        healthSlider = slider;
        bossNameText = nameText;
        canvasGroup = group;
        CacheReferences();
        RefreshVisibleState();
        ApplyImmediateState();
    }

    public void ShowIntroProgress(int revealedCount, int totalCount)
    {
        isVisible = true;
        CacheReferences();
        RefreshVisibleState();

        if (bossNameText != null)
        {
            bossNameText.text = bossDisplayName;
            bossNameText.color = bossNameColor;
        }

        UpdatePercentText();

        UpdateIntroProgress(revealedCount, totalCount);
    }

    public void SetIntroProgressPercent(float progressPercent)
    {
        isVisible = true;
        CacheReferences();
        RefreshVisibleState();

        targetHealthPercent = Mathf.Clamp(progressPercent, 0f, 100f);
        displayedHealthPercent = targetHealthPercent;
        hasReceivedState = true;

        if (bossNameText != null)
        {
            bossNameText.text = bossDisplayName;
            bossNameText.color = bossNameColor;
        }

        UpdatePercentText();
        ApplyImmediateState();
    }

    public void TriggerIntroCompletionBurst()
    {
        isVisible = true;
        introCompletionBurstT = 0f;
        ApplyImmediateState();
    }

    public void BindBossHealth(Entity_Health health)
    {
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged -= HandleBossHealthChanged;
        }

        bossHealth = health;
        isVisible = bossHealth != null;

        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged += HandleBossHealthChanged;
        }

        RefreshVisibleState();
        RefreshBossHealth();
    }

    public void Hide()
    {
        isVisible = false;
        RefreshVisibleState();
    }

    private void Awake()
    {
        CacheReferences();
        RefreshVisibleState();
        ApplyImmediateState();
    }

    private void OnEnable()
    {
        CacheReferences();

        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged -= HandleBossHealthChanged;
            bossHealth.OnHealthChanged += HandleBossHealthChanged;
            isVisible = true;
            RefreshBossHealth();
        }

        RefreshVisibleState();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            if (previewInEditor)
            {
                ApplyEditorPreviewState();
            }

            return;
        }

        UpdateIntroCompletionBurst();
        UpdateVisibilityAnimation();
        UpdateHealthAnimation();
        UpdateTheme();
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged -= HandleBossHealthChanged;
        }
    }

    private void CacheReferences()
    {
        if (rootRect == null)
        {
            rootRect = transform as RectTransform;
        }

        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>(true);
        }

        if (backgroundImage == null)
        {
            backgroundImage = FindImageChild("Background");
        }

        if (frameImage == null)
        {
            frameImage = FindImageChild("Frame");
        }

        if (fillImage == null)
        {
            fillImage = FindImageChild("Fill");
        }

        if (bossNameText == null)
        {
            bossNameText = FindTextChild("BossName");
        }

        if (percentText == null)
        {
            percentText = FindTextChild("BossPercentText");
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (abyssFireMarkers == null || abyssFireMarkers.Length != 6)
        {
            abyssFireMarkers = new Image[6];
        }

        for (int i = 0; i < abyssFireMarkers.Length; i++)
        {
            if (abyssFireMarkers[i] == null)
            {
                abyssFireMarkers[i] = FindImageChild($"Fire_{i + 1}");
            }
        }
    }

    private void HandleBossHealthChanged(Entity_Health health)
    {
        if (health == bossHealth)
        {
            RefreshBossHealth();
        }
    }

    private void RefreshBossHealth()
    {
        if (healthSlider == null)
        {
            return;
        }

        healthSlider.wholeNumbers = false;
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 100f;

        if (bossHealth == null)
        {
            targetHealthPercent = 0f;
            displayedHealthPercent = 0f;
            ApplyImmediateState();
            return;
        }

        targetHealthPercent = GetHealthPercent(bossHealth);
        if (!hasReceivedState)
        {
            displayedHealthPercent = targetHealthPercent;
            hasReceivedState = true;
        }

        if (bossNameText != null)
        {
            bossNameText.text = bossDisplayName;
            bossNameText.color = bossNameColor;
        }

        UpdatePercentText();
    }

    private void UpdateIntroProgress(int revealedCount, int totalCount)
    {
        if (healthSlider == null)
        {
            return;
        }

        healthSlider.wholeNumbers = false;
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 100f;

        int safeTotal = Mathf.Max(1, totalCount);
        int clampedRevealed = Mathf.Clamp(revealedCount, 0, safeTotal);
        targetHealthPercent = (clampedRevealed / (float)safeTotal) * 100f;
        displayedHealthPercent = targetHealthPercent;
        ApplyImmediateState();
    }

    private void RefreshVisibleState()
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void UpdateVisibilityAnimation()
    {
        if (canvasGroup == null || rootRect == null)
        {
            return;
        }

        float duration = isVisible ? fadeInDuration : fadeOutDuration;
        float target = isVisible ? 1f : 0f;
        if (duration <= 0f)
        {
            visibilityT = target;
        }
        else
        {
            visibilityT = Mathf.MoveTowards(visibilityT, target, Time.unscaledDeltaTime / duration);
        }

        canvasGroup.alpha = visibilityT;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        float burstScale = GetIntroCompletionBurstScale();
        rootRect.localScale = Vector3.one * Mathf.Lerp(hiddenScale, visibleScale * burstScale, visibilityT);
    }

    private void UpdateHealthAnimation()
    {
        if (healthSlider == null)
        {
            return;
        }

        float step = fillLerpSpeed * Time.unscaledDeltaTime;
        displayedHealthPercent = Mathf.MoveTowards(displayedHealthPercent, targetHealthPercent, step);
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 100f;
        healthSlider.value = displayedHealthPercent;
    }

    private void UpdateTheme()
    {
        float percent = Mathf.Clamp01(displayedHealthPercent / 100f);
        bool lowHealth = percent <= lowHealthThreshold;
        float pulse = lowHealth
            ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * lowHealthPulseSpeed)) * lowHealthPulseStrength
            : 0f;
        float burstFactor = GetIntroCompletionBurstFactor();

        if (fillImage != null)
        {
            Color color = GetFillColor(percent, pulse);
            if (burstFactor > 0f)
            {
                color = Color.Lerp(color, introCompletionFlashColor, introCompletionFlashStrength * burstFactor);
            }

            fillImage.color = color;
        }

        if (backgroundImage != null)
        {
            Color color = Color.Lerp(backgroundIdleColor, backgroundLowHealthColor, lowHealth ? 1f : 0f);
            if (burstFactor > 0f)
            {
                color = Color.Lerp(color, introCompletionFlashColor, introCompletionFlashStrength * 0.5f * burstFactor);
            }

            backgroundImage.color = color;
        }

        if (frameImage != null)
        {
            Color color = Color.Lerp(frameIdleColor, frameLowHealthColor, lowHealth ? 1f : 0f);
            if (burstFactor > 0f)
            {
                color = Color.Lerp(color, introCompletionFlashColor, burstFactor);
            }

            frameImage.color = color;
        }

        if (bossNameText != null)
        {
            Color color = Color.Lerp(bossNameColor, frameLowHealthColor, lowHealth ? pulse : 0f);
            if (burstFactor > 0f)
            {
                color = Color.Lerp(color, introCompletionFlashColor, burstFactor);
            }

            bossNameText.color = color;
        }

        if (percentText != null)
        {
            Color color = Color.Lerp(bossNameColor, frameLowHealthColor, lowHealth ? pulse : 0f);
            if (burstFactor > 0f)
            {
                color = Color.Lerp(color, introCompletionFlashColor, burstFactor);
            }

            percentText.color = color;
        }

        UpdateAbyssFireMarkers(percent, pulse);
    }

    private void ApplyImmediateState()
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 100f;
            healthSlider.value = displayedHealthPercent;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visibilityT;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (rootRect != null)
        {
            rootRect.localScale = Vector3.one * Mathf.Lerp(hiddenScale, visibleScale * GetIntroCompletionBurstScale(), isVisible ? 1f : 0f);
        }

        UpdateTheme();
        UpdatePercentText();
    }

    private void ApplyEditorPreviewState()
    {
        CacheReferences();

        isVisible = true;
        visibilityT = 1f;
        targetHealthPercent = 100f;
        displayedHealthPercent = 100f;

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 100f;
            healthSlider.value = 100f;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (rootRect != null)
        {
            rootRect.localScale = Vector3.one * visibleScale;
        }

        if (bossNameText != null)
        {
            bossNameText.text = bossDisplayName;
            bossNameText.color = bossNameColor;
        }

        if (percentText != null)
        {
            percentText.text = "100%";
            percentText.color = bossNameColor;
        }

        UpdateAbyssFireMarkers(1f, 0f);
    }

    private void UpdateIntroCompletionBurst()
    {
        if (introCompletionBurstT < 0f)
        {
            return;
        }

        introCompletionBurstT += Time.unscaledDeltaTime;
        if (introCompletionBurstT >= introCompletionBurstDuration)
        {
            introCompletionBurstT = -1f;
        }
    }

    private float GetIntroCompletionBurstFactor()
    {
        if (introCompletionBurstT < 0f || introCompletionBurstDuration <= 0f)
        {
            return 0f;
        }

        float t = Mathf.Clamp01(introCompletionBurstT / introCompletionBurstDuration);
        return 1f - Mathf.SmoothStep(0f, 1f, t);
    }

    private float GetIntroCompletionBurstScale()
    {
        float factor = GetIntroCompletionBurstFactor();
        return Mathf.Lerp(1f, introCompletionBurstScale, factor);
    }

    private void UpdatePercentText()
    {
        if (percentText != null)
        {
            percentText.text = $"{Mathf.RoundToInt(displayedHealthPercent)}%";
        }
    }

    private Color GetFillColor(float percent, float pulse)
    {
        Color baseColor;
        if (percent >= lowHealthThreshold)
        {
            float t = Mathf.InverseLerp(lowHealthThreshold, 1f, percent);
            baseColor = Color.Lerp(midHealthFillColor, healthyFillColor, t);
        }
        else
        {
            float t = Mathf.InverseLerp(0f, lowHealthThreshold, percent);
            baseColor = Color.Lerp(lowHealthFillColor, midHealthFillColor, t);
        }

        if (pulse > 0f)
        {
            baseColor = Color.Lerp(baseColor, frameLowHealthColor, pulse);
        }

        return baseColor;
    }

    private void UpdateAbyssFireMarkers(float percent, float pulse)
    {
        if (abyssFireMarkers == null || abyssFireMarkers.Length == 0)
        {
            return;
        }

        int activeCount = GetAbyssFireCountFromPercent(percent);
        for (int i = 0; i < abyssFireMarkers.Length; i++)
        {
            Image marker = abyssFireMarkers[i];
            if (marker == null)
            {
                continue;
            }

            bool active = i < activeCount;
            Color baseColor = active ? abyssFireActiveColor : abyssFireInactiveColor;
            if (active)
            {
                float firePhase = Time.unscaledTime * abyssFirePulseSpeed + i * 0.72f;
                float firePulse = (Mathf.Sin(firePhase) * 0.5f + 0.5f) * abyssFirePulseStrength;
                baseColor = Color.Lerp(baseColor, abyssFireLowHealthGlowColor, Mathf.Clamp01(pulse + firePulse));
                marker.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, abyssFireActiveScale, Mathf.Clamp01(pulse + firePulse));
            }
            else
            {
                marker.rectTransform.localScale = Vector3.one * abyssFireInactiveScale;
            }

            marker.color = baseColor;
        }
    }

    private int GetAbyssFireCountFromPercent(float percent)
    {
        if (percent >= 100f)
        {
            return 6;
        }

        if (percent >= 80f)
        {
            return 5;
        }

        if (percent >= 60f)
        {
            return 4;
        }

        if (percent >= 40f)
        {
            return 3;
        }

        if (percent >= 20f)
        {
            return 2;
        }

        if (percent > 0f)
        {
            return 1;
        }

        return 0;
    }

    private float GetHealthPercent(Entity_Health health)
    {
        if (health == null || health.MaxHealth <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01(health.CurrentHealth / (float)health.MaxHealth) * 100f;
    }

    private TextMeshProUGUI FindTextChild(string childName)
    {
        if (string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.name == childName)
            {
                return child.GetComponent<TextMeshProUGUI>();
            }
        }

        return null;
    }

    private Image FindImageChild(string childName)
    {
        if (string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.name == childName)
            {
                return child.GetComponent<Image>();
            }
        }

        return null;
    }
}
