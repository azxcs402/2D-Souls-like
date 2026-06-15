using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UI_FadeScreen : MonoBehaviour
{
    public Coroutine fadeEffectCo { get; private set; }

    [SerializeField] private Image fadeImage;

    private void Awake()
    {
        if (fadeImage == null)
        {
            fadeImage = GetComponent<Image>();
        }
    }

    public void DoFadeIn(float duration = 0.5f)
    {
        if (fadeImage == null)
        {
            return;
        }

        fadeImage.color = new Color(0f, 0f, 0f, 1f);
        FadeEffect(0f, duration);
    }

    public void DoFadeOut(float duration = 0.5f)
    {
        if (fadeImage == null)
        {
            return;
        }

        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        FadeEffect(1f, duration);
    }

    private void FadeEffect(float targetAlpha, float duration)
    {
        if (fadeEffectCo != null)
        {
            StopCoroutine(fadeEffectCo);
        }

        fadeEffectCo = StartCoroutine(FadeEffectCo(targetAlpha, duration));
    }

    private IEnumerator FadeEffectCo(float targetAlpha, float duration)
    {
        float startAlpha = fadeImage.color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            Color color = fadeImage.color;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            fadeImage.color = color;
            yield return null;
        }

        Color finalColor = fadeImage.color;
        finalColor.a = targetAlpha;
        fadeImage.color = finalColor;
    }
}
