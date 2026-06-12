using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Entity_VFX : MonoBehaviour
{
    [Header("On Damage VFX")]
    [SerializeField] private Material onDamageVFXMaterial;
    [SerializeField, Min(.01f)] private float onDamageVFXDuration = .15f;

    [Header("On Hit VFX")]
    [SerializeField] private Color hitVFXColor = Color.white;
    [SerializeField] private GameObject hitVFX;

    private SpriteRenderer[] spriteRenderers;
    private Material[] originalMaterials;
    private Coroutine onDamageVFXCoroutine;

    private void Awake()
    {
        CacheRenderers();
        TryAssignOnDamageMaterial();
        TryAssignHitVFX();
    }

    private void Reset()
    {
        CacheRenderers();
        TryAssignOnDamageMaterial();
        TryAssignHitVFX();
    }

    private void OnValidate()
    {
        onDamageVFXDuration = Mathf.Max(.01f, onDamageVFXDuration);
        TryAssignOnDamageMaterial();
        TryAssignHitVFX();
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

        CacheRenderers();
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

    private void CacheRenderers()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalMaterials = new Material[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalMaterials[i] = spriteRenderers[i] != null
                ? spriteRenderers[i].sharedMaterial
                : null;
        }
    }

    private void RestoreOriginalMaterials()
    {
        if (spriteRenderers == null || originalMaterials == null)
        {
            return;
        }

        int rendererCount = Mathf.Min(spriteRenderers.Length, originalMaterials.Length);
        for (int i = 0; i < rendererCount; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].sharedMaterial = originalMaterials[i];
            }
        }
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
