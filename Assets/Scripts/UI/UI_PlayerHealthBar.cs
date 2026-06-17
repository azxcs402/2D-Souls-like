using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UI_PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Entity_Health playerHealth;

    public void Configure(Slider slider)
    {
        healthSlider = slider;
        BindToPlayerHealth();
    }

    private void Awake()
    {
        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>(true);
        }
    }

    private void OnEnable()
    {
        BindToPlayerHealth();
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthBar;
        }
    }

    private void BindToPlayerHealth()
    {
        if (playerHealth == null)
        {
            Player player = FindObjectOfType<Player>();
            playerHealth = player != null ? player.GetComponent<Entity_Health>() : null;
        }

        if (playerHealth == null)
        {
            return;
        }

        playerHealth.OnHealthChanged -= UpdateHealthBar;
        playerHealth.OnHealthChanged += UpdateHealthBar;
        UpdateHealthBar(playerHealth);
    }

    private void UpdateHealthBar(Entity_Health health)
    {
        if (healthSlider == null || health == null)
        {
            return;
        }

        healthSlider.minValue = 0;
        healthSlider.maxValue = Mathf.Max(1, health.MaxHealth);
        healthSlider.value = Mathf.Clamp(health.CurrentHealth, 0, health.MaxHealth);
    }
}
