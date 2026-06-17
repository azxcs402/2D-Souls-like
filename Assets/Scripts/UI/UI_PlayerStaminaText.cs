using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UI_PlayerStaminaText : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Text staminaText;

    public void Configure(Text textComponent)
    {
        staminaText = textComponent;
        BindToPlayer();
        Refresh();
    }

    private void Awake()
    {
        if (staminaText == null)
        {
            staminaText = GetComponentInChildren<Text>(true);
        }

        BindToPlayer();
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
        if (staminaText == null)
        {
            return;
        }

        if (player == null)
        {
            BindToPlayer();
        }

        if (player == null)
        {
            staminaText.text = "Stamina: -- / --";
            return;
        }

        staminaText.text = $"Stamina: {player.CurrentStaminaRounded} / {Mathf.RoundToInt(player.MaxStamina)}";
    }
}
