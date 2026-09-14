using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    [SerializeField] private GameObject owner;
    [SerializeField] private BoxCollider2D hitCollider;

    private AttackData attackData;

    private readonly HashSet<Hurtbox> hitTargets = new();

    public void Activate(AttackData data)
    {
        attackData = data;
        hitTargets.Clear();

        gameObject.SetActive(true);

        // 활성화되는 순간 이미 범위 안에 있는 대상 검사
        CheckInitialOverlap();
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    private void CheckInitialOverlap()
    {
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(
            hitCollider.bounds.center,
            hitCollider.bounds.size,
            transform.eulerAngles.z
        );

        foreach (Collider2D other in overlaps)
        {
            ProcessCollision(other);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ProcessCollision(other);
    }

    private void ProcessCollision(Collider2D other)
    {
        // Counter 먼저 확인
        ParryBox parryBox = other.GetComponent<ParryBox>();

        if (parryBox != null)
        {
            DamageInfo damageInfo = CreateDamageInfo(other);
            parryBox.ReceiveHit(damageInfo);
            return;
        }

        Hurtbox hurtbox = other.GetComponent<Hurtbox>();

        if (hurtbox == null)
            return;

        if (hurtbox.transform.root.gameObject == owner)
            return;

        // 한 번의 공격에서 같은 Hurtbox 중복 타격 방지
        if (hitTargets.Contains(hurtbox))
            return;

        hitTargets.Add(hurtbox);

        DamageInfo info = CreateDamageInfo(other);

        hurtbox.ReceiveDamage(info);
    }

    private DamageInfo CreateDamageInfo(Collider2D other)
    {
        Vector2 hitDirection =
            (other.transform.position - owner.transform.position).normalized;

        return new DamageInfo(
            attackData.damage,
            hitDirection,
            attackData.knockbackForce,
            owner,
            attackData.attackId,
            attackData.parryable
        );
    }
}