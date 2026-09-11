using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    [SerializeField] private GameObject owner;

    private AttackData attackData;

    private readonly HashSet<Hurtbox> hitTargets = new();

    public void Activate(AttackData data)
    {
        attackData = data;
        hitTargets.Clear();

        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Hurtbox hurtbox = other.GetComponent<Hurtbox>();

        if (hurtbox == null)
            return;

        // 자기 자신 공격 방지
        if (hurtbox.transform.root.gameObject == owner)
            return;

        // 한 번의 공격에서 같은 대상 여러 번 타격 방지
        if (hitTargets.Contains(hurtbox))
            return;

        hitTargets.Add(hurtbox);

        Vector2 hitDirection =
            (other.transform.position - owner.transform.position).normalized;

        DamageInfo damageInfo = new DamageInfo(
            attackData.damage,
            hitDirection,
            attackData.knockbackForce,
            owner,
            attackData.attackId,
            attackData.parryable
        );

        hurtbox.ReceiveDamage(damageInfo);
    }
}