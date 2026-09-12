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
        ParryBox parryBox = other.GetComponent<ParryBox>();

        if (parryBox != null)
        {
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

            parryBox.ReceiveHit(damageInfo);
            return;
        }

        Hurtbox hurtbox = other.GetComponent<Hurtbox>();

        if (hurtbox == null)
            return;

        if (hurtbox.transform.root.gameObject == owner)
            return;

        if (hitTargets.Contains(hurtbox))
            return;

        hitTargets.Add(hurtbox);

        Vector2 direction =
            (other.transform.position - owner.transform.position).normalized;

        DamageInfo info = new DamageInfo(
            attackData.damage,
            direction,
            attackData.knockbackForce,
            owner,
            attackData.attackId,
            attackData.parryable
        );

        hurtbox.ReceiveDamage(info);
    }
}
