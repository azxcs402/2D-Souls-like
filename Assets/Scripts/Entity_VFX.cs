using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Entity_VFX : MonoBehaviour
{
    [Header("On Damage VFX")]
    [SerializeField] private Material onDamageVFXMaterial;
    [SerializeField, Min(.01f)] private float onDamageVFXDuration = .15f;

    private SpriteRenderer[] spriteRenderers;
    private Material[] originalMaterials;
    private Coroutine onDamageVFXCoroutine;

    private void Awake()
    {
        CacheRenderers();
        TryAssignOnDamageMaterial();
    }

    private void Reset()
    {
        CacheRenderers();
        TryAssignOnDamageMaterial();
    }

    private void OnValidate()
    {
        onDamageVFXDuration = Mathf.Max(.01f, onDamageVFXDuration);
        TryAssignOnDamageMaterial();
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
}
