using System.Collections;
using UnityEngine;

public class BossAction : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;

    [Header("Charge Slash")]
    [SerializeField] private float chargeSpeed = 8f;
    [SerializeField] private float chargeDuration = 0.35f;
    [SerializeField] private Transform player;
    private bool isAttacking;
    private Hitbox currentHitbox;

    public bool IsAttacking => isAttacking;
    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }
    public void ExecuteAttack(BossAttack attack)
    {
        if (isAttacking || attack == null)
            return;

        switch (attack.attackType)
        {
            case BossAttackType.BasicSlash:
            case BossAttackType.HeavySlash:
                StartCoroutine(SlashRoutine(attack));
                break;

            case BossAttackType.ChargeSlash:
                StartCoroutine(ChargeSlashRoutine(attack));
                break;
        }
    }

    private IEnumerator SlashRoutine(BossAttack attack)
    {
        isAttacking = true;

        AttackData data = attack.attackData;
        Hitbox hitbox = attack.hitbox;

        currentHitbox = hitbox;

        // Startup
        yield return new WaitForSeconds(data.startupTime);

        // Active
        hitbox.Activate(data);

        yield return new WaitForSeconds(data.activeTime);

        hitbox.Deactivate();

        // Recovery
        yield return new WaitForSeconds(data.recoveryTime);

        currentHitbox = null;
        isAttacking = false;
    }

    public void ForceCancelAttack()
    {
        StopAllCoroutines();

        if (currentHitbox != null)
        {
            currentHitbox.Deactivate();
            currentHitbox = null;
        }

        isAttacking = false;
    }
    private IEnumerator ChargeSlashRoutine(BossAttack attack)
    {
        isAttacking = true;

        AttackData data = attack.attackData;
        Hitbox hitbox = attack.hitbox;

        currentHitbox = hitbox;

        // Startup
        yield return new WaitForSeconds(data.startupTime);

        float direction =
            Mathf.Sign(
                player.position.x - transform.position.x
            );

        // Active
        hitbox.Activate(data);

        float timer = 0f;

        while (timer < chargeDuration)
        {
            rb.linearVelocity =
                new Vector2(
                    direction * chargeSpeed,
                    rb.linearVelocity.y
                );

            timer += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity =
            new Vector2(0f, rb.linearVelocity.y);

        hitbox.Deactivate();
        currentHitbox = null;

        // Recovery
        yield return new WaitForSeconds(data.recoveryTime);

        isAttacking = false;
    }
}