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

    private Coroutine parryRoutine;

    private bool isParrying;
    private bool counterSucceeded;

    public bool IsParrying => isParrying;
    public bool IsBusy => isParrying;

    // 나중에 Adaptive AI에서 읽거나 조절할 수 있도록 노출
    public float ParryWindow => parryWindow;


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

        // 플레이어 현재 공격 취소
        if (attacker != null)
        {
            PlayerCombat playerCombat =
                attacker.GetComponent<PlayerCombat>();

            playerCombat?.ForceCancelAttack();
        }

        // 1. 튕겨내는 모션
        bossAnimator?.PlayParrySuccess();

        yield return new WaitForSeconds(parrySuccessDuration);

        // 2. 찌르기 직전 플레이어 방향 재확인
        movement?.FaceTarget();

        // 3. CounterThrust 시작
        // 이후 Hitbox ON/OFF/End는 Animation Event가 담당
        bossAnimator?.PlayCounterThrust();
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
    public void OnCounterHitboxOn()
    {
        if (counterHitbox == null || counterAttackData == null)
            return;

        counterHitbox.Activate(counterAttackData);
    }
    public void OnCounterHitboxOff()
    {
        if (counterHitbox == null)
            return;

        counterHitbox.Deactivate();
    }
    public void OnCounterThrustEnd()
    {
        EndParry();
    }
}