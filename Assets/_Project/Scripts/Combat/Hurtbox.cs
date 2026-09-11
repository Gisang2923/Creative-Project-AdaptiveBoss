using UnityEngine;

public class Hurtbox : MonoBehaviour
{
    private IDamageable damageable;

    private void Awake()
    {
        damageable = GetComponentInParent<IDamageable>();
    }

    public void ReceiveDamage(DamageInfo damageInfo)
    {
        damageable?.TakeDamage(damageInfo);
    }
}