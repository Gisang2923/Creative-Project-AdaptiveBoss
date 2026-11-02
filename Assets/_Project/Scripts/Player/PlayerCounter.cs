using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCounter : MonoBehaviour
{
    [SerializeField]
    private HitFlash hitFlash;
    [Header("Counter")]
    [SerializeField] private AttackData counterAttackData;

    [SerializeField] private GameObject parryBox;
    [SerializeField] private GameObject hurtbox;

    [Header("Timing")]
    [SerializeField] private float parryWindow = 0.35f;

    [SerializeField]
    private float perfectParryWindow = 0.13f;

    [SerializeField] private float failRecovery = 0.4f;
    [SerializeField] private float successAnimationDuration = 0.35f;

    [SerializeField]
    private float normalParryHitStop = 0.08f;
    private float perfectParryHitStop = 0.18f;    
    private PlayerDamageReceiver damageReceiver;
    private PlayerCombat playerCombat;

    
    private PlayerHeal playerHeal;
    private PlayerAnimator playerAnimator;
    private bool isCountering;
    public bool IsCountering => isCountering;
    private bool counterSucceeded;
    public bool CounterSucceeded => counterSucceeded;


    private bool perfectParrySucceeded;

    public bool PerfectParrySucceeded =>
        perfectParrySucceeded;

    private float counterStartTime;
    private PlayerPrecisionStack precisionStack;    
    public float ParryWindow =>
        parryWindow;
    private Coroutine counterRoutine;
    private GameObject pendingCounterTarget;

    private void Awake()
    {
        damageReceiver =
            GetComponent<PlayerDamageReceiver>();

        playerCombat =
            GetComponent<PlayerCombat>();

        playerHeal =
            GetComponent<PlayerHeal>();

        precisionStack =
            GetComponent<PlayerPrecisionStack>();
        playerAnimator =
            GetComponent<PlayerAnimator>();    
        if (hitFlash == null)
        {
            hitFlash =
                GetComponentInChildren<HitFlash>();
        }
        parryBox.SetActive(false);
        hurtbox.SetActive(true);
    }

    public void OnCounter(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
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
        CombatLogger.Instance?.RecordPlayerResponse(
            PlayerResponseType.Counter
        );      
        counterRoutine = StartCoroutine(CounterRoutine());
    }

    private IEnumerator CounterRoutine()
    {
        isCountering = true;
        counterSucceeded = false;
        perfectParrySucceeded = false;

        counterStartTime = Time.time;

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

            damageReceiver.TakeDamage(
                incomingDamage
            );

            return;
        }


        // =====================================
        // Perfect 판정
        // Counter 입력 후 실제 공격이
        // 얼마나 빨리 도착했는지 확인
        // =====================================

        float responseTime =
            Time.time - counterStartTime;

        bool isPerfect =
            responseTime <= perfectParryWindow;

        perfectParrySucceeded = isPerfect;
        // Parry 자체는 성공
        counterSucceeded = true;

        EndCounter();

        playerAnimator?
            .PlayCounterSuccess(
                isPerfect
            );

        StartCoroutine(
            CounterSuccessRoutine()
        );


        // Normal / Perfect 모두
        // AI 입장에서는 Counter 성공으로 기록
        CombatLogger.Instance?
            .RecordParrySuccess();


        // =====================================
        // Normal Parry
        // 피해만 무효화하고 종료
        // =====================================

        if (!isPerfect)
        {
            Debug.Log(
                $"[Normal Parry] " +
                $"Timing={responseTime:F3}"
            );

            return;
        }

        // =====================================
        // Perfect Parry
        // =====================================

        Debug.Log(
            $"[Perfect Parry] " +
            $"Timing={responseTime:F3}"    
        );
        CombatLogger.Instance?
            .RecordPlayerAction(
                "PerfectParry"
            );

        // Precision Stack 획득
        precisionStack?.GainStack();


        GameObject attacker =
            incomingDamage.Attacker;
            
        pendingCounterTarget = attacker;
        // 기존 Perfect Counter 보상 유지
        BossAction bossAction =
            attacker.GetComponent<BossAction>();

        bossAction?.ForceCancelAttack();


        BossAnimator bossAnimator =
            attacker.GetComponent<BossAnimator>();

        BossController bossController =
            attacker.GetComponent<BossController>();


        bossController?
            .EnterCounterStun(0.5f);

        bossAnimator?
            .PlayCounterHit();

    }
    private IEnumerator CounterSuccessRoutine()
    {
        counterSucceeded = true;

        yield return new WaitForSeconds(successAnimationDuration);

        counterSucceeded = false;
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
    public void OnParryImpactFrame()
    {
        float duration =
            perfectParrySucceeded
                ? perfectParryHitStop
                : normalParryHitStop;

        // Perfect Parry Counter Damage
        if (perfectParrySucceeded &&
            pendingCounterTarget != null &&
            counterAttackData != null)
        {
            IDamageable target =
                pendingCounterTarget
                    .GetComponentInParent<IDamageable>();

            if (target != null)
            {
                Vector2 direction =
                    (
                        pendingCounterTarget.transform.position -
                        transform.position
                    ).normalized;

                DamageInfo counterDamage =
                    new DamageInfo(
                        counterAttackData.damage,
                        direction,
                        counterAttackData.knockbackForce,
                        gameObject,
                        counterAttackData.attackId,
                        false,
                        suppressHitVfx: true
                    );

                target.TakeDamage(
                    counterDamage
                );
            }

            pendingCounterTarget = null;
        }

        // 보스를 살짝 하얗게
        hitFlash?.Flash(
            perfectParrySucceeded
                ? 0.8f
                : 0.5f,
            duration,
            true
        );

        // 같은 프레임에 Hit Stop
        HitStopManager.Instance?
            .Play(duration);

        if (perfectParrySucceeded)
        {
            CameraShake.Instance?
                .Shake(
                    0.07f,
                    0.10f
                );
        }
    }
}