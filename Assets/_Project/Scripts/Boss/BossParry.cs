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
    [SerializeField] private float parryWindow = 0.40f;

    [Header("Counter Timing")]
    [SerializeField] private float counterDelay = 0.05f;
    [SerializeField] private float counterHitboxDuration = 0.15f;
    [SerializeField] private float counterDuration = 0.45f;

    private Coroutine parryRoutine;

    private bool isParrying;
    private bool counterSucceeded;

    public bool IsParrying => isParrying;
    public bool IsBusy => isParrying;

    // 나중에 Adaptive AI에서 읽거나 조절할 수 있도록 노출
    public float ParryWindow => parryWindow;
    public float CounterDuration => counterDuration;

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

        if (attacker != null)
        {
            PlayerCombat playerCombat =
                attacker.GetComponent<PlayerCombat>();

            playerCombat?.ForceCancelAttack();
        }

        // 반격 직전에 플레이어 방향을 다시 바라봄
        movement?.FaceTarget();

        bossAnimator?.PlayParrySuccess();

        yield return new WaitForSeconds(counterDelay);

        if (counterHitbox != null && counterAttackData != null)
        {
            counterHitbox.Activate(counterAttackData);
        }

        yield return new WaitForSeconds(counterHitboxDuration);

        if (counterHitbox != null)
        {
            counterHitbox.Deactivate();
        }

        float remainingDuration =
            Mathf.Max(
                0f,
                counterDuration
                - counterDelay
                - counterHitboxDuration
            );

        yield return new WaitForSeconds(remainingDuration);

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