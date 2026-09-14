using UnityEngine;

public class BossDamageReceiver : DamageReceiver
{
    [Header("References")]
    [SerializeField] private BossAction bossAction;
    [SerializeField] private BossMovement bossMovement;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private BossController bossController;
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

        // 일반 공격으로는 현재 Boss 공격을 끊지 않음.
        // Counter 성공은 PlayerCounter에서
        // bossAction.ForceCancelAttack() 처리.
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

        Debug.Log("Boss Dead");
    }
}