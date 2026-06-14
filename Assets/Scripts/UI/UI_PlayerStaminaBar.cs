using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UI_PlayerStaminaBar : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Slider staminaSlider;

    public void Configure(Slider slider)
    {
        staminaSlider = slider;
        BindToPlayer();
        Refresh();
    }

    private void Awake()
    {
        if (staminaSlider == null)
        {
            staminaSlider = GetComponentInChildren<Slider>(true);
        }
    }

    private void OnEnable()
    {
        BindToPlayer();
        Refresh();
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.OnStaminaChanged -= HandleStaminaChanged;
        }
    }

    private void BindToPlayer()
    {
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }

        if (player == null)
        {
            return;
        }

        player.OnStaminaChanged -= HandleStaminaChanged;
        player.OnStaminaChanged += HandleStaminaChanged;
    }

    private void HandleStaminaChanged(Player changedPlayer)
    {
        if (changedPlayer == player)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        if (staminaSlider == null)
        {
            return;
        }

        if (player == null)
        {
            BindToPlayer();
        }

        if (player == null)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = 1f;
            staminaSlider.value = 0f;
            return;
        }

        staminaSlider.minValue = 0f;
        staminaSlider.maxValue = Mathf.Max(1f, player.MaxStamina);
        staminaSlider.value = Mathf.Clamp(player.CurrentStamina, 0f, player.MaxStamina);
    }
}
