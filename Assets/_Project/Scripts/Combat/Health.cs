using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        currentHealth -= damageInfo.Damage;

        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log(
            $"{gameObject.name} HP: {currentHealth}/{maxHealth}"
        );

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} Dead");
    }
}