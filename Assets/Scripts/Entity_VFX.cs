using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Entity_VFX : MonoBehaviour
{
    [Header("Image Echo VFX")]
    [SerializeField, Min(.01f)] private float imageEchoInterval = .05f;
    [SerializeField, Min(.05f)] private float imageEchoLifetime = .3f;
    [SerializeField, Range(0f, 1f)] private float imageEchoStartAlpha = .55f;
    [SerializeField] private Color imageEchoTint = Color.white;

    [Header("On Damage VFX")]
    [SerializeField] private Material onDamageVFXMaterial;
    [SerializeField, Min(.01f)] private float onDamageVFXDuration = .15f;

    [Header("On Hit VFX")]
    [SerializeField] private Color hitVFXColor = Color.white;
    [SerializeField] private GameObject hitVFX;

    private SpriteRenderer[] spriteRenderers;
    private readonly List<RendererSnapshot> originalRendererStates = new List<RendererSnapshot>();
    private Coroutine imageEchoCoroutine;
    private Coroutine onDamageVFXCoroutine;

    private struct RendererSnapshot
    {
        public SpriteRenderer renderer;
        public Material material;
        public Color color;
        public bool enabled;
    }

    private void Awake()
    {
        CacheRenderers(true);
        TryAssignOnDamageMaterial();
        TryAssignHitVFX();
    }

    private void Reset()
    {
        CacheRenderers(true);
        TryAssignOnDamageMaterial();
        TryAssignHitVFX();
    }

    private void OnValidate()
    {
        imageEchoInterval = Mathf.Max(.01f, imageEchoInterval);
        imageEchoLifetime = Mathf.Max(.05f, imageEchoLifetime);
        imageEchoStartAlpha = Mathf.Clamp01(imageEchoStartAlpha);
        onDamageVFXDuration = Mathf.Max(.01f, onDamageVFXDuration);
        TryAssignOnDamageMaterial();
        TryAssignHitVFX();
    }

    public void DoImageEchoEffect(float duration)
    {
        StopImageEchoEffect();

        if (duration <= 0f)
        {
            return;
        }

        imageEchoCoroutine = StartCoroutine(ImageEchoEffectCoroutine(duration));
    }

    public void StopImageEchoEffect()
    {
        if (imageEchoCoroutine == null)
        {
            return;
        }

        StopCoroutine(imageEchoCoroutine);
        imageEchoCoroutine = null;
    }

    public void RestoreVisualState()
    {
        StopImageEchoEffect();

        if (onDamageVFXCoroutine != null)
        {
            StopCoroutine(onDamageVFXCoroutine);
            onDamageVFXCoroutine = null;
        }

        CacheRenderers();
        RestoreOriginalMaterials();
        RestoreOriginalColors();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Entity_VFX] RestoreVisualState on {name}, renderers={(spriteRenderers != null ? spriteRenderers.Length : 0)}", this);
        LogRendererStates("after restore");
#endif
    }

    public void CreateImageEchoTrail(Vector3 start, Vector3 end, int echoCount, float lifetime = -1f)
    {
        echoCount = Mathf.Max(1, echoCount);
        float resolvedLifetime = lifetime > 0f ? lifetime : imageEchoLifetime;

        for (int i = 0; i < echoCount; i++)
        {
            float t = echoCount == 1 ? 1f : i / (echoCount - 1f);
            CreateImageEchoSnapshot(Vector3.Lerp(start, end, t), resolvedLifetime);
        }
    }

    public void CreateOnHitVFX(Transform target)
    {
        if (target == null)
        {
            return;
        }

        TryAssignHitVFX();

        if (hitVFX == null)
        {
            return;
        }

        GameObject vfx = Instantiate(hitVFX, target.position, Quaternion.identity);
        vfx.SetActive(true);
        vfx.transform.SetParent(target, true);
        vfx.transform.position = target.position;
        vfx.transform.rotation = Quaternion.identity;

        SpriteRenderer spriteRenderer = vfx.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hitVFXColor;
        }
    }

    public void PlayOnDamageVFX()
    {
        if (onDamageVFXCoroutine != null)
        {
            StopCoroutine(onDamageVFXCoroutine);
            RestoreOriginalMaterials();
            onDamageVFXCoroutine = null;
        }

        CacheRenderers(true);
        TryAssignOnDamageMaterial();

        if (onDamageVFXMaterial == null || spriteRenderers.Length == 0)
        {
            return;
        }

        onDamageVFXCoroutine = StartCoroutine(OnDamageVFXCoroutine());
    }

    private IEnumerator OnDamageVFXCoroutine()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].material = onDamageVFXMaterial;
            }
        }

        yield return new WaitForSeconds(onDamageVFXDuration);

        RestoreOriginalMaterials();
        onDamageVFXCoroutine = null;
    }

    private IEnumerator ImageEchoEffectCoroutine(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            CreateImageEchoSnapshot(GetPrimaryRendererWorldPosition(), imageEchoLifetime);
            yield return new WaitForSeconds(imageEchoInterval);
            elapsed += imageEchoInterval;
        }

        imageEchoCoroutine = null;
    }

    private void CacheRenderers(bool refreshOriginalMaterials = false)
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (!refreshOriginalMaterials)
        {
            return;
        }

        originalRendererStates.Clear();

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer renderer = spriteRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            originalRendererStates.Add(new RendererSnapshot
            {
                renderer = renderer,
                material = renderer.sharedMaterial,
                color = renderer.color,
                enabled = renderer.enabled
            });
        }
    }

    private void RestoreOriginalMaterials()
    {
        if (originalRendererStates.Count == 0)
        {
            return;
        }

        for (int i = 0; i < originalRendererStates.Count; i++)
        {
            RendererSnapshot snapshot = originalRendererStates[i];
            if (snapshot.renderer != null)
            {
                snapshot.renderer.enabled = snapshot.enabled;
                snapshot.renderer.material = snapshot.material;
                snapshot.renderer.sharedMaterial = snapshot.material;
            }
        }
    }

    private void RestoreOriginalColors()
    {
        if (originalRendererStates.Count == 0)
        {
            return;
        }

        for (int i = 0; i < originalRendererStates.Count; i++)
        {
            RendererSnapshot snapshot = originalRendererStates[i];
            if (snapshot.renderer != null)
            {
                snapshot.renderer.color = snapshot.color;
            }
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void LogRendererStates(string context)
    {
        if (spriteRenderers == null)
        {
            return;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer renderer = spriteRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.sharedMaterial != null ? renderer.sharedMaterial : renderer.material;
            Debug.Log(
                $"[Entity_VFX] {context} renderer[{i}] name={renderer.name}, enabled={renderer.enabled}, color={renderer.color}, material={(material != null ? material.name : "null")}, sortingOrder={renderer.sortingOrder}",
                renderer);
        }
    }
#endif

    private void CreateImageEchoSnapshot(Vector3 worldPosition, float lifetime)
    {
        SpriteRenderer sourceRenderer = GetPrimaryRenderer();
        if (sourceRenderer == null || sourceRenderer.sprite == null)
        {
            return;
        }

        GameObject echoObject = new GameObject($"{name}_ImageEcho");
        echoObject.transform.position = worldPosition;
        echoObject.transform.rotation = sourceRenderer.transform.rotation;
        echoObject.transform.localScale = sourceRenderer.transform.lossyScale;

        SpriteRenderer echoRenderer = echoObject.AddComponent<SpriteRenderer>();
        echoRenderer.sprite = sourceRenderer.sprite;
        echoRenderer.flipX = sourceRenderer.flipX;
        echoRenderer.flipY = sourceRenderer.flipY;
        echoRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        echoRenderer.sortingOrder = sourceRenderer.sortingOrder - 1;

        Color echoColor = imageEchoTint;
        echoColor.a = Mathf.Clamp01(imageEchoStartAlpha);
        echoRenderer.color = echoColor;

        SpriteAfterimage afterimage = echoObject.AddComponent<SpriteAfterimage>();
        afterimage.Initialize(echoRenderer, lifetime);
    }

    private SpriteRenderer GetPrimaryRenderer()
    {
        CacheRenderers();

        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && spriteRenderers[i].sprite != null)
            {
                return spriteRenderers[i];
            }
        }

        return spriteRenderers[0];
    }

    private Vector3 GetPrimaryRendererWorldPosition()
    {
        SpriteRenderer renderer = GetPrimaryRenderer();
        return renderer != null ? renderer.transform.position : transform.position;
    }

    private void TryAssignOnDamageMaterial()
    {
#if UNITY_EDITOR
        if (onDamageVFXMaterial == null)
        {
            onDamageVFXMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/OnDamageVFX_Material.mat"
            );
        }
#endif
    }

    private void TryAssignHitVFX()
    {
        if (hitVFX == null)
        {
            Transform childTemplate = transform.Find("OnHitVFX");
            if (childTemplate != null)
            {
                hitVFX = childTemplate.gameObject;
            }
        }

#if UNITY_EDITOR
        if (hitVFX == null)
        {
            hitVFX = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/VFX/OnHitVFX.prefab"
            );
        }
#endif
    }
}
