using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Image fillImage;

    private void OnEnable()
    {
        if (health == null)
            return;

        health.OnHealthChanged += UpdateHealthBar;
    }
    private void Start()
    {
        if (health == null)
            return;

        UpdateHealthBar(
            health.CurrentHealth,
            health.MaxHealth
        );
    }
    private void OnDisable()
    {
        if (health == null)
            return;

        health.OnHealthChanged -= UpdateHealthBar;
    }

    private void UpdateHealthBar(
        int currentHealth,
        int maxHealth)
    {
        if (fillImage == null ||
            maxHealth <= 0)
        {
            return;
        }

        fillImage.fillAmount =
            (float)currentHealth / maxHealth;
    }
}