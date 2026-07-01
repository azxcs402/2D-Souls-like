using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealingPotionWorldIcon : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public void Configure(Player targetPlayer, SpriteRenderer renderer)
    {
        player = targetPlayer;
        spriteRenderer = renderer;
        RefreshSprite();
    }

    public void SetVisible(bool visible)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (visible)
        {
            gameObject.SetActive(true);
        }

        spriteRenderer.enabled = visible;
    }

    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            Destroy(gameObject);
            return;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        RefreshSprite();

        bool visible = player.IsHealingPotionInUse;
        spriteRenderer.enabled = visible;
        if (!visible)
        {
            return;
        }

        if (player.TryGetActiveColliderBounds(out Bounds bounds))
        {
            transform.position = new Vector3(
                bounds.center.x,
                bounds.max.y,
                bounds.center.z
            ) + player.HealingPotionWorldIconOffset;
        }
        else
        {
            transform.position = player.transform.position + player.HealingPotionWorldIconOffset;
        }

        transform.rotation = Quaternion.identity;
    }

    private void RefreshSprite()
    {
        if (spriteRenderer == null || player == null)
        {
            return;
        }

        if (spriteRenderer.sprite != player.HealingPotionWorldIconSprite)
        {
            spriteRenderer.sprite = player.HealingPotionWorldIconSprite;
        }
    }
}
