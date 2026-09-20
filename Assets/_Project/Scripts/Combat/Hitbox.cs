using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    [SerializeField] private GameObject owner;
    [SerializeField] private BoxCollider2D hitCollider;

    private AttackData attackData;

    private readonly HashSet<GameObject> processedTargets = new();
    public void Activate(AttackData data)
    {
        attackData = data;

        processedTargets.Clear();

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

    public void SweepFromTo(
        Vector2 from,
        Vector2 to)
    {
        if (attackData == null || hitCollider == null)
            return;

        Vector2 delta = to - from;
        float distance = delta.magnitude;

        if (distance <= Mathf.Epsilon)
            return;

        Vector2 direction = delta.normalized;

        RaycastHit2D[] hits =
            Physics2D.BoxCastAll(
                from,
                hitCollider.bounds.size,
                transform.eulerAngles.z,
                direction,
                distance
            );

        Debug.Log($"Sweep Hit Count: {hits.Length}");

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            ProcessCollision(hit.collider);
        }
    }

    public Vector2 GetWorldCenter()
    {
        return hitCollider.transform.TransformPoint(
            hitCollider.offset
        );
    }

    private void ProcessCollision(Collider2D other)
    {
        ParryBox parryBox = other.GetComponent<ParryBox>();

        if (parryBox != null)
        {
            GameObject targetRoot =
                parryBox.transform.root.gameObject;

            if (targetRoot == owner)
                return;

            if (processedTargets.Contains(targetRoot))
                return;

            processedTargets.Add(targetRoot);

            DamageInfo damageInfo = CreateDamageInfo(other);
            parryBox.ReceiveHit(damageInfo);

            return;
        }

        Hurtbox hurtbox = other.GetComponent<Hurtbox>();

        if (hurtbox == null)
            return;

        GameObject hurtTargetRoot =
            hurtbox.transform.root.gameObject;

        if (hurtTargetRoot == owner)
            return;

        if (processedTargets.Contains(hurtTargetRoot))
            return;

        processedTargets.Add(hurtTargetRoot);

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
    private void OnDrawGizmosSelected()
    {
        if (hitCollider == null)
            return;

        Gizmos.matrix = hitCollider.transform.localToWorldMatrix;

        Gizmos.DrawWireCube(
            hitCollider.offset,
            hitCollider.size
        );
    }
}