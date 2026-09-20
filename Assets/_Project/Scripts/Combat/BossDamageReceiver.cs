using UnityEngine;

public class BossDamageReceiver : DamageReceiver
{
    [Header("References")]
    [SerializeField] private BossAction bossAction;
    [SerializeField] private BossMovement bossMovement;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private BossController bossController;
    [SerializeField] private BossAnimator bossAnimator;
    [SerializeField] private HitFlash hitFlash;
    private bool isDead;

    public bool IsDead => isDead;

    private void Awake()
    {
        if (bossAction == null)
            bossAction = GetComponent<BossAction>();

        if (bossMovement == null)
            bossMovement = GetComponent<BossMovement>();

        if (bossController == null)
            bossController = GetComponent<BossController>();
        if (bossAnimator == null)
            bossAnimator = GetComponent<BossAnimator>();
        if (hitFlash == null)
            hitFlash = GetComponentInChildren<HitFlash>();
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (health != null)
            health.OnDied += HandleDeath;
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDied -= HandleDeath;
    }

    public override void TakeDamage(DamageInfo damageInfo)
    {
        if (isDead)
            return;

        base.TakeDamage(damageInfo);

        hitFlash?.Flash();

        if (!isDead)
            bossController?.TriggerHitBackDodge();
    }

    private void HandleDeath()
    {
        if (isDead)
            return;

        isDead = true;

        if (bossAction != null)
            bossAction.ForceCancelAttack();

        if (bossController != null)
            bossController.SetDead();

        if (bossMovement != null)
            bossMovement.Stop();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // Stop()이 BattleIdle을 재생할 수 있으므로 Death는 마지막에
        if (bossAnimator != null)
            bossAnimator.PlayDeath();

        Debug.Log("Boss Dead");
    }
}