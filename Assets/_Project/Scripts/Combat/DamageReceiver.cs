using UnityEngine;

public class DamageReceiver : MonoBehaviour, IDamageable
{
    [SerializeField] protected Health health;

    public virtual void TakeDamage(DamageInfo damageInfo)
    {
        if (health == null || health.IsDead)
            return;

        health.TakeDamage(damageInfo);
    }
}