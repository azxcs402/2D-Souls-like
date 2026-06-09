using UnityEngine;

[DisallowMultipleComponent]
public class UI_MiniHealthBar : MonoBehaviour
{
    private Entity entity;

    private void Awake()
    {
        entity = GetComponentInParent<Entity>();
    }

    private void OnEnable()
    {
        if (entity == null)
        {
            entity = GetComponentInParent<Entity>();
        }

        if (entity != null)
        {
            entity.OnFlipped += HandleFlip;
        }

        HandleFlip();
    }

    private void OnDisable()
    {
        if (entity != null)
        {
            entity.OnFlipped -= HandleFlip;
        }
    }

    private void HandleFlip()
    {
        transform.rotation = Quaternion.identity;
    }
}
