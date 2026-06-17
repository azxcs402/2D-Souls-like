using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UI_DashCooldownImage : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Image cooldownImage;

    public void Configure(Image image)
    {
        cooldownImage = image;
        BindToPlayer();
        Refresh();
    }

    private void Awake()
    {
        BindToPlayer();
    }

    private void OnEnable()
    {
        BindToPlayer();
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void BindToPlayer()
    {
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
    }

    private void Refresh()
    {
        if (cooldownImage == null)
        {
            return;
        }

        if (player == null)
        {
            BindToPlayer();
        }

        float fillAmount = 0f;
        if (player != null)
        {
            float cooldownDuration = player.DashCooldownDuration;
            float cooldownRemaining = player.DashCooldownRemaining;
            if (cooldownDuration > 0f && cooldownRemaining > 0f)
            {
                fillAmount = Mathf.Clamp01(cooldownRemaining / cooldownDuration);
            }
        }

        cooldownImage.fillAmount = fillAmount;
        cooldownImage.enabled = fillAmount > 0f;
    }
}
