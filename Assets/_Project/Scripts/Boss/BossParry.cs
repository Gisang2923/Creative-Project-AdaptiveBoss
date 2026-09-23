using System.Collections;
using UnityEngine;

public class BossParry : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject parryBox;
    [SerializeField] private GameObject hurtbox;
    [SerializeField] private BossAnimator bossAnimator;
    [SerializeField] private BossMovement movement;

    [SerializeField] private Hitbox counterHitbox;
    [SerializeField] private AttackData counterAttackData;
    [SerializeField] private float startupTime = 0.10f;
    [SerializeField] private float parryWindow = 0.80f;

    [Header("Counter Timing")]
    [SerializeField] private float parrySuccessDuration = 0.20f;
    [SerializeField] private float thrustHitDelay = 0.12f;
    [SerializeField] private float thrustHitboxDuration = 0.10f;
    [SerializeField] private float counterThrustDuration = 0.42f;
    private Coroutine parryRoutine;

    private bool isParrying;
    private bool counterSucceeded;

    public bool IsParrying => isParrying;
    public bool IsBusy => isParrying;

    // 나중에 Adaptive AI에서 읽거나 조절할 수 있도록 노출
    public float ParryWindow => parryWindow;
    public float CounterThrustDuration => counterThrustDuration;

    private void Awake()
    {
        if (parryBox != null)
            parryBox.SetActive(false);

        if (counterHitbox != null)
            counterHitbox.Deactivate();
    }

    public void StartParry()
    {
        if (isParrying)
            return;

        parryRoutine = StartCoroutine(ParryRoutine());
    }

    private IEnumerator ParryRoutine()
    {
        isParrying = true;
        counterSucceeded = false;

        // 1. 패링 준비 자세
        bossAnimator?.PlayParry();

        yield return new WaitForSeconds(startupTime);

        // 2. 실제 패링 가능 구간
        if (hurtbox != null)
            hurtbox.SetActive(false);
        if (parryBox != null)
            parryBox.SetActive(true);

        yield return new WaitForSeconds(parryWindow);

        // 이미 성공했다면 ResolveParry 쪽에서 처리
        if (counterSucceeded)
            yield break;

        // 3. 공격이 들어오지 않음
        // 별도의 실패 Recovery 없이 바로 종료
        EndParry();
    }

    public void ResolveParry(DamageInfo incomingDamage)
    {
        if (!isParrying)
            return;

        if (counterSucceeded)
            return;

        counterSucceeded = true;

        if (parryBox != null)
            parryBox.SetActive(false);

        if (parryRoutine != null)
        {
            StopCoroutine(parryRoutine);
            parryRoutine = null;
        }

        StartCoroutine(CounterRoutine(incomingDamage));
    }

    private IEnumerator CounterRoutine(DamageInfo incomingDamage)
    {
        GameObject attacker = incomingDamage.Attacker;

        // 플레이어의 현재 공격 취소
        if (attacker != null)
        {
            PlayerCombat playerCombat =
                attacker.GetComponent<PlayerCombat>();

            playerCombat?.ForceCancelAttack();
        }

        // 1. 공격을 튕겨내는 모션
        bossAnimator?.PlayParrySuccess();

        yield return new WaitForSeconds(parrySuccessDuration);

        // 2. 찌르기 직전에 플레이어 방향 다시 확인
        movement?.FaceTarget();

        // 3. 찌르기 반격 시작
        bossAnimator?.PlayCounterThrust();

        // 4. 실제 검이 나가는 시점까지 대기
        yield return new WaitForSeconds(thrustHitDelay);

        // 5. 실제 공격 판정 활성화
        if (counterHitbox != null && counterAttackData != null)
        {
            counterHitbox.Activate(counterAttackData);
        }

        yield return new WaitForSeconds(thrustHitboxDuration);

        // 6. 공격 판정 종료
        if (counterHitbox != null)
        {
            counterHitbox.Deactivate();
        }

        // 7. 찌르기 애니메이션 남은 시간
        float remainingDuration =
            Mathf.Max(
                0f,
                counterThrustDuration
                - thrustHitDelay
                - thrustHitboxDuration
            );

        yield return new WaitForSeconds(remainingDuration);

        // 8. 패링 전체 행동 종료
        EndParry();
    }

    private void EndParry()
    {
        if (counterHitbox != null)
            counterHitbox.Deactivate();

        if (parryBox != null)
            parryBox.SetActive(false);

        if (hurtbox != null)
            hurtbox.SetActive(true);

        isParrying = false;
        counterSucceeded = false;
        parryRoutine = null;
    }

    public void ForceCancelParry()
    {
        if (parryRoutine != null)
        {
            StopCoroutine(parryRoutine);
            parryRoutine = null;
        }

        StopAllCoroutines();

        if (counterHitbox != null)
            counterHitbox.Deactivate();

        if (parryBox != null)
            parryBox.SetActive(false);

        if (hurtbox != null)
            hurtbox.SetActive(true);

        isParrying = false;
        counterSucceeded = false;
    }
}