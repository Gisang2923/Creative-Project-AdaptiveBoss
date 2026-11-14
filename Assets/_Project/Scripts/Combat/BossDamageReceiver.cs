using UnityEngine;
using System.Collections;

public class BossDamageReceiver : DamageReceiver
{
    [Header("References")]
    [SerializeField] private BossAction bossAction;
    [SerializeField] private BossMovement bossMovement;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private BossController bossController;
    [SerializeField] private BossAnimator bossAnimator;
    [SerializeField] private HitFlash hitFlash;
    [SerializeField] private BossHitReaction hitReaction;
    [SerializeField] private HitVFXSpawner hitVfxSpawner;
    [Header("Act1 Clear")]
    [SerializeField]
    private float deathEventFallbackDelay =
        3f;

    private bool act1ClearPending;
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
        if (hitReaction == null)
            hitReaction = GetComponent<BossHitReaction>();
        if (hitVfxSpawner == null)
        {
            hitVfxSpawner = GetComponent<HitVFXSpawner>();
        }
            
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDied -= HandleDeath;
    }

    public override void TakeDamage(
        DamageInfo damageInfo)
    {
        if (isDead)
            return;


        bool wasPrecisionStunned =
            bossController != null &&
            bossController.IsPrecisionStunned;


        base.TakeDamage(
            damageInfo
        );

        // =====================================
        // Player Hit SFX
        // =====================================

        if (!damageInfo.SuppressHitSfx)
        {
            PlayerSFX attackerSFX =
                damageInfo.Attacker != null
                    ? damageInfo.Attacker
                        .GetComponent<PlayerSFX>()
                    : null;

            PlayerCombat attackerCombat =
                damageInfo.Attacker != null
                    ? damageInfo.Attacker
                        .GetComponent<PlayerCombat>()
                    : null;

            bool isComboFinisher =
                attackerCombat != null &&
                attackerCombat.CurrentNormalComboStep == 3;

            bool isStrongHit =
                isComboFinisher ||
                damageInfo.CausesBossStun;

            attackerSFX?
                .PlayHit(
                    isStrongHit
                );
        }
        if (damageInfo.CameraShakeStrength > 0f)
        {
            CameraShake.Instance?.Shake(
                damageInfo.CameraShakeDuration,
                damageInfo.CameraShakeStrength
            );
        }
        hitFlash?.Flash();
        hitReaction?.Play(
            damageInfo.HitDirection,
            damageInfo.CausesBossStun
        );
        if (!damageInfo.SuppressHitVfx)
        {
            hitVfxSpawner?.Play(
                damageInfo.HitDirection
            );
        }
        // 데미지 처리 중 사망했다면 종료
        if (isDead)
            return;


        // =====================================
        // 이미 기절 중
        // → 추가타 카운트
        // =====================================

        if (wasPrecisionStunned)
        {
            bossController?
                .RegisterPrecisionStunHit();

            return;
        }


        // =====================================
        // 강화공격 적중
        // → Precision Stun 시작
        // =====================================

        if (damageInfo.CausesBossStun &&
            bossController != null &&
            !bossController.IsInPrecisionStun)
        {
            bossController
                .EnterPrecisionStun();
        }
    }

    private void HandleDeath()
    {
        if (isDead)
            return;

        isDead = true;


        GameProgression progression =
            GameProgression.Instance;

        bool wasAct1 =
            progression != null &&
            progression.CurrentPhase ==
                GamePhase.Act1;


        if (bossAction != null)
            bossAction.ForceCancelAttack();

        if (bossController != null)
            bossController.SetDead();

        if (bossMovement != null)
            bossMovement.Stop();

        if (rb != null)
        {
            rb.linearVelocity =
                Vector2.zero;
        }

        if (bossAnimator != null)
        {
            bossAnimator.PlayDeath();
        }


        progression?
            .RegisterVictory();


        // =====================================
        // Act1 First Clear
        // =====================================

        if (wasAct1)
        {
            act1ClearPending = true;

            StartCoroutine(
                Act1ClearFallbackRoutine()
            );
        }
        else
        {
            // Act2 최종 승리 등
            GameFlowController.Instance?
                .HandleBossDefeat();
        }


        CombatLogger.Instance?
            .SaveBattleLog(
                "PlayerWin"
            );

        Debug.Log(
            $"[Boss] Dead | " +
            $"Act1 Clear = {wasAct1}"
        );
    }
    public void OnDeathAnimationEnd()
    {
        if (!act1ClearPending)
            return;

        act1ClearPending = false;

        Debug.Log(
            "[Boss] " +
            "Act1 Death Animation End"
        );

        if (Act1ClearPresentationController
            .Instance != null)
        {
            Act1ClearPresentationController
                .Instance
                .Play();

            return;
        }

        Debug.LogWarning(
            "[Boss] Act1ClearPresentation " +
            "missing."
        );

        GameFlowController.Instance?
            .CompleteAct1ClearPresentation();
    }
    private IEnumerator
    Act1ClearFallbackRoutine()
    {
        float elapsed = 0f;

        while (elapsed <
            deathEventFallbackDelay)
        {
            if (!act1ClearPending)
                yield break;

            elapsed +=
                Time.unscaledDeltaTime;

            yield return null;
        }

        if (!act1ClearPending)
            yield break;

        Debug.LogWarning(
            "[Boss] DeathAnimationEnd " +
            "missing. Using fallback."
        );

        OnDeathAnimationEnd();
    }
}