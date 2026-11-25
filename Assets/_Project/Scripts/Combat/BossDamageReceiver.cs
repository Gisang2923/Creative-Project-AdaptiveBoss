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
    [SerializeField] private FinalEndingPresentationController finalEndingController;
    [Header("Act1 Clear")]
    [SerializeField]
    private float deathEventFallbackDelay =
        3f;

    private bool act1ClearPending;
    private bool isDead;
    private bool finalEndingActive;
    private bool finalDeathAnimationEnded;
    private bool bindingDefeatPending;

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
        if (isDead || finalEndingActive || bindingDefeatPending)
            return;

        bool triggerBindingDefeat = false;
        GameProgression progression = GameProgression.Instance;
        if (progression != null &&
            progression.CurrentPhase == GamePhase.Act1)
        {
            int stage = progression.FearRecoveryStage;
            if (stage <= 1)
            {
                // Counter damage does not pass through PlayerProgressionRuntime.
                damageInfo.Damage = 0;
            }
            else if (stage < GameProgression.MaxFearRecoveryStage &&
                     health != null &&
                     damageInfo.Damage > 0 &&
                     damageInfo.Damage >= health.CurrentHealth)
            {
                damageInfo.Damage = Mathf.Max(0, health.CurrentHealth - 1);
                bindingDefeatPending = true;
                triggerBindingDefeat = true;
            }
        }


        bool wasPrecisionStunned =
            bossController != null &&
            bossController.IsPrecisionStunned;

        PlayerCombat attackerCombat =
            damageInfo.Attacker != null
                ? damageInfo.Attacker.GetComponent<PlayerCombat>()
                : null;

        bool isComboFinisher =
            attackerCombat != null &&
            attackerCombat.CurrentNormalComboStep == 3;


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

        if (triggerBindingDefeat)
        {
            StartCoroutine(BindingDefeatRoutine(damageInfo.Attacker));
            return;
        }

        // 데미지 처리 중 사망했다면 종료
        if (isDead || finalEndingActive)
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

    private IEnumerator BindingDefeatRoutine(GameObject attacker)
    {
        FindFirstObjectByType<BossBattleIntroController>()?
            .ShowBindingDefeatFeedback();

        yield return new WaitForSecondsRealtime(0.65f);

        PlayerDamageReceiver player = attacker != null
            ? attacker.GetComponentInParent<PlayerDamageReceiver>()
            : null;
        if (player == null)
            player = FindFirstObjectByType<PlayerDamageReceiver>();

        if (player != null)
            player.ForceDefeatByWorldBinding();
        else
            Debug.LogError("[World Binding] Player not found for defeat.");
    }

    private void HandleDeath()
    {
        if (isDead || finalEndingActive)
            return;

        GameProgression progression =
            GameProgression.Instance;

        if (progression != null &&
            progression.CurrentPhase == GamePhase.Act2Adaptive)
        {
            finalEndingActive = true;

            bossAction?.ForceCancelAttack();
            bossController?.SetDead();
            bossMovement?.Stop();

            if (rb != null)
                rb.linearVelocity =
                    new Vector2(0f, rb.linearVelocity.y);

            if (finalEndingController != null)
                finalEndingController.BeginFinale();
            else
                Debug.LogError(
                    "[Final Ending] Controller is missing."
                );

            return;
        }

        isDead = true;

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
    public bool CompleteFinalDeath()
    {
        GameProgression progression =
            GameProgression.Instance;

        if (!finalEndingActive || isDead ||
            progression == null ||
            progression.CurrentPhase != GamePhase.Act2Adaptive)
            return false;

        isDead = true;
        finalDeathAnimationEnded = false;

        bossAction?.ForceCancelAttack();
        bossController?.SetDead();
        bossMovement?.Stop();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        bossAnimator?.PlayDeath();
        progression.RegisterVictory();

        CombatLogger.Instance?
            .SaveBattleLog("PlayerWin");

        return true;
    }

    public bool FinalDeathAnimationEnded =>
        finalDeathAnimationEnded;

    public void OnDeathAnimationEnd()
    {
        if (finalEndingActive && isDead)
        {
            finalDeathAnimationEnded = true;
            return;
        }

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
