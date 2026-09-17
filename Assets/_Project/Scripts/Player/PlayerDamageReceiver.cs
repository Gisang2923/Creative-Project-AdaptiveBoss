using System.Collections;
using UnityEngine;

public class PlayerDamageReceiver : DamageReceiver
{
    [Header("Hit Settings")]
    [SerializeField] private float hitStunDuration = 0.25f;
    [SerializeField] private float invincibilityDuration = 0.7f;
    [SerializeField] private HitFlash hitFlash;
    private bool isInvincible;
    private bool isStunned;

    public bool IsInvincible => isInvincible;
    public bool IsStunned => isStunned;
    private PlayerCombat playerCombat;

    private Rigidbody2D rb;
    private PlayerDash playerDash;
    private PlayerHeal playerHeal;
    public bool IsDead => health != null && health.IsDead;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCombat = GetComponent<PlayerCombat>();
        playerDash = GetComponent<PlayerDash>();
        playerHeal = GetComponent<PlayerHeal>();
        if (hitFlash == null)
            hitFlash = GetComponentInChildren<HitFlash>();
    }
    private void ApplyKnockback(DamageInfo damageInfo)
    {
        float directionX = Mathf.Sign(damageInfo.HitDirection.x);

        Vector2 knockbackDirection =
            new Vector2(directionX, 0.35f).normalized;

        rb.linearVelocity =
            knockbackDirection * damageInfo.KnockbackForce;
    }
    public override void TakeDamage(DamageInfo damageInfo)
    {
        if (isInvincible || health == null || health.IsDead)
            return;

        playerHeal?.ForceCancelHeal();
        playerCombat?.ForceCancelAttack();
        playerDash?.ForceCancelDash();

        health.TakeDamage(damageInfo);
        base.TakeDamage(damageInfo);

        hitFlash?.Flash();

        if (health.IsDead)
        {
            HandleDeath();
            return;
        }

        ApplyKnockback(damageInfo);
        StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        isStunned = true;
        isInvincible = true;

        yield return new WaitForSeconds(hitStunDuration);

        isStunned = false;

        float remainingInvincibility =
            Mathf.Max(0f, invincibilityDuration - hitStunDuration);

        yield return new WaitForSeconds(remainingInvincibility);

        isInvincible = false;
    }

    private void HandleDeath()
    {
        playerCombat?.ForceCancelAttack();
        playerDash?.ForceCancelDash();

        isStunned = false;
        isInvincible = true;

        rb.linearVelocity = Vector2.zero;
    }
}