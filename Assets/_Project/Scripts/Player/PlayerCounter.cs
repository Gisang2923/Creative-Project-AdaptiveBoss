using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCounter : MonoBehaviour
{
    [Header("Counter")]
    [SerializeField] private AttackData counterAttackData;

    [SerializeField] private GameObject parryBox;
    [SerializeField] private GameObject hurtbox;

    [Header("Timing")]
    [SerializeField] private float startupTime = 0.03f;
    [SerializeField] private float parryWindow = 0.15f;
    [SerializeField] private float failRecovery = 0.4f;

    private PlayerDamageReceiver damageReceiver;
    private PlayerCombat playerCombat;
    private PlayerDash playerDash;

    private bool isCountering;
    private bool counterSucceeded;
    private PlayerHeal playerHeal;
    public bool IsCountering => isCountering;
    private Coroutine counterRoutine;
    private void Awake()
    {
        damageReceiver = GetComponent<PlayerDamageReceiver>();
        playerCombat = GetComponent<PlayerCombat>();
        playerDash = GetComponent<PlayerDash>();
        playerHeal = GetComponent<PlayerHeal>();
        parryBox.SetActive(false);
    }

    public void OnCounter(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
        Debug.Log("Counter Input!");
        if (isCountering)
            return;

        if (damageReceiver != null &&
            (damageReceiver.IsStunned || damageReceiver.IsDead))
            return;

        if (playerCombat != null && playerCombat.IsAttacking)
            return;
        if (playerCombat != null && playerCombat.IsCharging)
            return;
        if (playerHeal != null && playerHeal.IsHealing)
            return;    
        counterRoutine = StartCoroutine(CounterRoutine());
    }

    private IEnumerator CounterRoutine()
    {
        isCountering = true;

        // Startup
        yield return new WaitForSeconds(startupTime);

        // Parry Window
        hurtbox.SetActive(false);
        parryBox.SetActive(true);

        yield return new WaitForSeconds(parryWindow);

        parryBox.SetActive(false);
        hurtbox.SetActive(true);

        // 여기까지 공격이 안 들어왔다 = 실패
        yield return new WaitForSeconds(failRecovery);

        isCountering = false;
        counterRoutine = null;
    }

    public void ResolveCounter(DamageInfo incomingDamage)
    {
        if (!isCountering)
            return;

        if (!incomingDamage.Parryable)
        {
            EndCounter();
            damageReceiver.TakeDamage(incomingDamage);
            return;
        }

        // 성공한 순간 Counter 종료
        EndCounter();

        GameObject attacker = incomingDamage.Attacker;

        // 보스 공격 중단
        BossAction bossAction = attacker.GetComponent<BossAction>();
        bossAction?.ForceCancelAttack();

        // 반격 데미지
        IDamageable target = attacker.GetComponent<IDamageable>();

        if (target != null)
        {
            Vector2 direction =
                (attacker.transform.position - transform.position).normalized;

            DamageInfo counterDamage = new DamageInfo(
                counterAttackData.damage,
                direction,
                counterAttackData.knockbackForce,
                gameObject,
                counterAttackData.attackId,
                false
            );

            target.TakeDamage(counterDamage);
        }
    }
    private void EndCounter()
    {
        if (counterRoutine != null)
        {
            StopCoroutine(counterRoutine);
            counterRoutine = null;
        }

        parryBox.SetActive(false);
        hurtbox.SetActive(true);

        isCountering = false;
    }
    private void FailAgainstUnparryableAttack(DamageInfo damageInfo)
    {
        parryBox.SetActive(false);
        hurtbox.SetActive(true);

        damageReceiver.TakeDamage(damageInfo);
    }
}