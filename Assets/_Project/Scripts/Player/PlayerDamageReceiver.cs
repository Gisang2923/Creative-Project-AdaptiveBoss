using System.Collections;
using UnityEngine;

public class PlayerDamageReceiver : DamageReceiver
{
    [Header("Hit Settings")]
    [SerializeField] private float hitStunDuration = 0.4f;
    [SerializeField] private float invincibilityDuration = 0.7f;
    [SerializeField] private HitFlash hitFlash;
    [Header("Hit VFX")]
    [SerializeField] private HitVFXSpawner hitVfxSpawner;
    private bool isInvincible;
    private bool isStunned;

    public bool IsInvincible => isInvincible;
    public bool IsStunned => isStunned;

    private bool isKnockedDown;
    private bool isGettingUp;

    private PlayerMovement playerMovement;

    private Coroutine knockdownRoutine;

    public bool IsKnockedDown =>
        isKnockedDown;

    public bool IsGettingUp =>
        isGettingUp;
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
        playerMovement =GetComponent<PlayerMovement>();
        if (hitVfxSpawner == null)
        {
            hitVfxSpawner = GetComponent<HitVFXSpawner>();
        }
    }
    private void ApplyKnockback(DamageInfo damageInfo)
    {
        float directionX = Mathf.Sign(damageInfo.HitDirection.x);

        Vector2 knockbackDirection =
            new Vector2(directionX, 0.35f).normalized;

        rb.linearVelocity =
            knockbackDirection * damageInfo.KnockbackForce;
    }
    private void ApplyKnockdown(
        DamageInfo damageInfo)
    {
        float directionX =
            Mathf.Sign(
                damageInfo.HitDirection.x
            );

        if (directionX == 0f)
            directionX = 1f;


        rb.linearVelocity =
            new Vector2(
                directionX *
                    damageInfo.KnockdownX,
                damageInfo.KnockdownY
            );
    }
    public override void TakeDamage(DamageInfo damageInfo)
    {
        if (health == null || health.IsDead)
            return;

        if (isInvincible)
            return;

        // 실제 공격 판정이 닿은 순간
        // Dash 시작 직후라면 Perfect Dodge
        if (playerDash != null &&
            playerDash.TryPerfectDodge())
        {
            return;
        }

        playerHeal?.ForceCancelHeal();
        playerCombat?.ForceCancelAttack();
        playerDash?.ForceCancelDash();


        base.TakeDamage(damageInfo);
        CombatLogger.Instance?.RecordPlayerHit();
        
        hitFlash?.Flash();
        hitVfxSpawner?.Play(
            damageInfo.HitDirection
        );
        if (health.IsDead)
        {
            HandleDeath();
            return;
        }

        // =====================================
        // Jump Slam 등의 Knockdown 공격
        // =====================================

        if (damageInfo.CausesKnockdown)
        {
            ApplyKnockdown(
                damageInfo
            );

            if (knockdownRoutine != null)
            {
                StopCoroutine(
                    knockdownRoutine
                );
            }

            knockdownRoutine =
                StartCoroutine(
                    KnockdownRoutine(
                        damageInfo.KnockdownDuration
                    )
                );

            return;
        }


        // =====================================
        // 일반 피격
        // =====================================

        ApplyKnockback(
            damageInfo
        );

        StartCoroutine(
            HitRoutine()
        );
    }
    public void OnGetUpEnd()
    {
        if (!isGettingUp)
            return;

        isGettingUp = false;
        isKnockedDown = false;

        isStunned = false;
        isInvincible = false;

        Debug.Log(
            "[Player Knockdown] GetUp End"
        );
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
    private IEnumerator KnockdownRoutine(
        float duration)
    {
        isStunned = true;
        isInvincible = true;

        isKnockedDown = true;
        isGettingUp = false;


        // 최소 Knockdown 시간
        yield return new WaitForSeconds(
            duration
        );


        // 아직 공중이면 착지할 때까지 기다림
        if (playerMovement != null)
        {
            while (!playerMovement.IsGroundedState)
            {
                yield return null;
            }
        }


        // 일어나는 동안 미끄러지지 않도록
        rb.linearVelocity =
            new Vector2(
                0f,
                rb.linearVelocity.y
            );


        isKnockedDown = false;
        isGettingUp = true;

        knockdownRoutine = null;

        // 여기서는 Stunned를 풀지 않음.
        // GetUp 애니메이션이 끝나야 조작 가능.
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