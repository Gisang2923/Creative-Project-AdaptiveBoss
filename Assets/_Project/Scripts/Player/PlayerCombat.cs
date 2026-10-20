using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    public enum AttackPhase
    {
        None,
        Startup,
        Active,
        Recovery
    }

    public ChargeState CurrentChargeState => chargeState;

    [Header("Normal Attack")]
    [SerializeField] private AttackData normalAttackData;
    [SerializeField] private Hitbox normalAttackHitbox;

    public enum AttackType
    {
        None,
        Normal,
        Charge,
        DashAttack,
        EnhancedDashAttack
    }

    public enum EnhancedDashPhase
    {
        None,
        Prepare,
        Attack
    }

    private EnhancedDashPhase enhancedDashPhase =
        EnhancedDashPhase.None;

    public EnhancedDashPhase CurrentEnhancedDashPhase =>
        enhancedDashPhase;

    private AttackType currentAttackType =
        AttackType.None;

    public AttackType CurrentAttackType =>
        currentAttackType;

    public enum ChargeState
    {
        None,
        Charging,
        Charged
    }

    [Header("Charge Attack")]
    [SerializeField] private AttackData chargeAttackData;
    [SerializeField] private Hitbox chargeAttackHitbox;
    [SerializeField] private float chargeTime = 0.7f;

    private ChargeState chargeState =
        ChargeState.None;

    private float chargeTimer;

    private AttackPhase currentPhase =
        AttackPhase.None;

    public AttackPhase CurrentPhase =>
        currentPhase;

    public bool IsAttacking =>
        currentPhase != AttackPhase.None;

    // Active가 끝난 Recovery에서는 Dash Cancel 허용
    public bool CanDashCancel =>
        currentPhase == AttackPhase.None ||
        currentPhase == AttackPhase.Recovery;

    public bool IsCharging =>
        chargeState == ChargeState.Charging ||
        chargeState == ChargeState.Charged;

    private PlayerDamageReceiver damageReceiver;
    private PlayerCounter playerCounter;
    private PlayerHeal playerHeal;
    private PlayerDash playerDash;
    private PlayerMovement playerMovement;
    private PlayerPrecisionStack precisionStack;

    [Header("Dash Attack")]
    [SerializeField] private AttackData dashAttackData;
    [SerializeField] private Hitbox dashAttackHitbox;
    [SerializeField] private float dashAttackMoveDistance = 1.2f;
    [SerializeField] private float dashAttackMoveDuration = 0.4f;

    [Header("Enhanced Dash Attack")]
    [SerializeField] private AttackData enhancedDashAttackData;
    [SerializeField] private Hitbox enhancedDashAttackHitbox;

    [SerializeField] private float enhancedDashMoveDistance = 6f;
    [SerializeField] private float enhancedDashMoveDuration = 0.4f;

    [Header("Enhanced Heavy")]
    [SerializeField] private AttackData enhancedHeavyAttackData;

    // Enhanced Heavy는 기존 ChargeAttack 애니메이션을 사용하지만
    // 판정 타이밍은 Animation Event로 제어한다.
    private bool enhancedHeavyAttackActive;
    private Coroutine enhancedHeavySafetyCoroutine;

    private const float EnhancedHeavySafetyTimeout = 2.5f;

    public bool IsEnhancedHeavyAttack =>
        enhancedHeavyAttackActive;

    private float enhancedDashDirection;
    private Vector2 enhancedDashSweepStart;
    private bool enhancedDashActive;
    private Coroutine enhancedDashMoveCoroutine;

    private void Awake()
    {
        damageReceiver =
            GetComponent<PlayerDamageReceiver>();

        playerCounter =
            GetComponent<PlayerCounter>();

        playerHeal =
            GetComponent<PlayerHeal>();

        playerDash =
            GetComponent<PlayerDash>();

        playerMovement =
            GetComponent<PlayerMovement>();

        precisionStack =
            GetComponent<PlayerPrecisionStack>();
    }

    private void Update()
    {
        if (chargeState !=
            ChargeState.Charging)
        {
            return;
        }

        chargeTimer += Time.deltaTime;

        if (chargeTimer >= chargeTime)
        {
            chargeState =
                ChargeState.Charged;
        }
    }

    public void OnAttack(
        InputAction.CallbackContext context)
    {
        if (playerHeal != null &&
            playerHeal.IsHealing)
        {
            return;
        }

        if (damageReceiver != null &&
            (damageReceiver.IsStunned ||
             damageReceiver.IsDead))
        {
            return;
        }

        if (playerCounter != null &&
            playerCounter.IsCountering)
        {
            return;
        }

        if (!context.performed)
            return;

        if (IsAttacking || IsCharging)
            return;

        // Dash 중 공격 입력 → DashAttack
        if (playerDash != null &&
            playerDash.IsDashing)
        {
            StartDashAttack();
            return;
        }

        // =====================================
        // Precision Point 최대치
        // 다음 기본공격 1회를 Enhanced Heavy로 대체
        // =====================================
        if (precisionStack != null &&
            precisionStack.CanUseEnhancedAttack)
        {
            if (precisionStack
                    .TryConsumeEnhancedAttack())
            {
                Debug.Log(
                    "[Precision Attack] " +
                    "Normal Attack -> Enhanced Heavy"
                );

                StartEnhancedHeavyAttack();
                return;
            }
        }

        // =====================================
        // 일반 기본공격
        // =====================================
        StartCoroutine(
            NormalAttack()
        );
    }

    public void CancelAttack()
    {
        if (currentPhase !=
            AttackPhase.Recovery)
        {
            return;
        }

        StopAllCoroutines();

        currentAttackType =
            AttackType.None;

        normalAttackHitbox?.Deactivate();
        chargeAttackHitbox?.Deactivate();
        dashAttackHitbox?.Deactivate();
        enhancedDashAttackHitbox?.Deactivate();

        playerMovement?
            .StopDashAttackMove();

        enhancedDashPhase =
            EnhancedDashPhase.None;

        enhancedDashActive = false;
        enhancedDashMoveCoroutine = null;

        ResetEnhancedHeavyState();

        currentPhase =
            AttackPhase.None;
    }

    public void ForceCancelAttack()
    {
        StopAllCoroutines();

        normalAttackHitbox?.Deactivate();
        chargeAttackHitbox?.Deactivate();
        dashAttackHitbox?.Deactivate();
        enhancedDashAttackHitbox?.Deactivate();

        playerMovement?
            .StopDashAttackMove();

        currentAttackType =
            AttackType.None;

        chargeState =
            ChargeState.None;

        chargeTimer = 0f;

        enhancedDashPhase =
            EnhancedDashPhase.None;

        enhancedDashActive = false;
        enhancedDashMoveCoroutine = null;

        ResetEnhancedHeavyState();

        currentPhase =
            AttackPhase.None;
    }

    private IEnumerator NormalAttack()
    {
        currentAttackType =
            AttackType.Normal;

        currentPhase =
            AttackPhase.Startup;

        CombatLogger.Instance?
            .RecordPlayerResponse(
                PlayerResponseType.NormalAttack
            );

        yield return new WaitForSeconds(
            normalAttackData.startupTime
        );

        currentPhase =
            AttackPhase.Active;

        normalAttackHitbox.Activate(
            normalAttackData
        );

        yield return new WaitForSeconds(
            normalAttackData.activeTime
        );

        normalAttackHitbox.Deactivate();

        currentPhase =
            AttackPhase.Recovery;

        yield return new WaitForSeconds(
            normalAttackData.recoveryTime
        );

        currentAttackType =
            AttackType.None;

        currentPhase =
            AttackPhase.None;
    }

    public void OnChargeAttack(
        InputAction.CallbackContext context)
    {
        if (damageReceiver != null &&
            (damageReceiver.IsStunned ||
             damageReceiver.IsDead))
        {
            return;
        }

        if (playerHeal != null &&
            playerHeal.IsHealing)
        {
            return;
        }

        if (playerCounter != null &&
            playerCounter.IsCountering)
        {
            return;
        }

        if (context.performed)
        {
            StartCharge();
        }
        else if (context.canceled)
        {
            ReleaseCharge();
        }
    }

    private void StartCharge()
    {
        if (IsAttacking)
            return;

        if (playerCounter != null &&
            playerCounter.IsCountering)
        {
            return;
        }

        if (damageReceiver != null &&
            (damageReceiver.IsStunned ||
             damageReceiver.IsDead))
        {
            return;
        }

        chargeState =
            ChargeState.Charging;

        chargeTimer = 0f;
    }

    private void ReleaseCharge()
    {
        if (chargeState ==
            ChargeState.None)
        {
            return;
        }

        if (chargeState ==
            ChargeState.Charged)
        {
            StartCoroutine(
                ChargeAttack(
                    chargeAttackData
                )
            );
        }

        chargeState =
            ChargeState.None;

        chargeTimer = 0f;
    }

    private IEnumerator ChargeAttack(
        AttackData data)
    {
        currentAttackType =
            AttackType.Charge;

        currentPhase =
            AttackPhase.Startup;

        CombatLogger.Instance?
            .RecordPlayerResponse(
                PlayerResponseType.ChargeAttack
            );

        yield return new WaitForSeconds(
            data.startupTime
        );

        currentPhase =
            AttackPhase.Active;

        chargeAttackHitbox.Activate(
            data
        );

        yield return new WaitForSeconds(
            data.activeTime
        );

        chargeAttackHitbox.Deactivate();

        currentPhase =
            AttackPhase.Recovery;

        yield return new WaitForSeconds(
            data.recoveryTime
        );

        currentAttackType =
            AttackType.None;

        currentPhase =
            AttackPhase.None;
    }

    // ==========================================
    // Enhanced Heavy
    //
    // 기존에는 enhancedHeavyAttackData의
    // startup / active / recovery 시간을 그대로 사용했다.
    //
    // 이제는 실제 공격 Sprite에 Animation Event를 배치해서
    // 판정 시작 / 종료 / 공격 종료를 직접 맞춘다.
    // ==========================================

    private void StartEnhancedHeavyAttack()
    {
        if (enhancedHeavyAttackData == null ||
            chargeAttackHitbox == null)
        {
            Debug.LogWarning(
                "[Enhanced Heavy] " +
                "AttackData or Hitbox is missing."
            );

            return;
        }

        enhancedHeavyAttackActive = true;

        // PlayerAnimator의 기존 ChargeAttack 상태를 그대로 사용하기 위해
        // AttackType은 Charge를 유지한다.
        currentAttackType =
            AttackType.Charge;

        currentPhase =
            AttackPhase.Startup;

        CombatLogger.Instance?
            .RecordPlayerResponse(
                PlayerResponseType.ChargeAttack
            );

        if (enhancedHeavySafetyCoroutine != null)
        {
            StopCoroutine(
                enhancedHeavySafetyCoroutine
            );
        }

        enhancedHeavySafetyCoroutine =
            StartCoroutine(
                EnhancedHeavySafetyRoutine()
            );

        Debug.Log(
            "[Enhanced Heavy] Start | " +
            "Waiting for Animation Events"
        );
    }

    // Animation Event:
    // 검이 실제로 적을 베기 시작하는 첫 프레임에 배치
    public void OnEnhancedHeavyHitboxOn()
    {
        if (!enhancedHeavyAttackActive)
            return;

        if (currentAttackType !=
            AttackType.Charge)
        {
            return;
        }

        if (currentPhase !=
            AttackPhase.Startup)
        {
            return;
        }

        currentPhase =
            AttackPhase.Active;

        chargeAttackHitbox.Activate(
            enhancedHeavyAttackData
        );

        Debug.Log(
            "[Enhanced Heavy] Hitbox ON"
        );
    }

    // Animation Event:
    // 검이 타격 구간을 완전히 지난 직후 프레임에 배치
    public void OnEnhancedHeavyHitboxOff()
    {
        if (!enhancedHeavyAttackActive)
            return;

        chargeAttackHitbox?.Deactivate();

        currentPhase =
            AttackPhase.Recovery;

        Debug.Log(
            "[Enhanced Heavy] Hitbox OFF " +
            "-> Recovery / Movement Unlocked"
        );
    }

    // Animation Event:
    // 공격 후속 모션이 끝나는 프레임에 배치
    public void OnEnhancedHeavyAttackEnd()
    {
        if (!enhancedHeavyAttackActive)
            return;

        chargeAttackHitbox?.Deactivate();

        enhancedHeavyAttackActive = false;

        currentAttackType =
            AttackType.None;

        currentPhase =
            AttackPhase.None;

        if (enhancedHeavySafetyCoroutine != null)
        {
            StopCoroutine(
                enhancedHeavySafetyCoroutine
            );

            enhancedHeavySafetyCoroutine = null;
        }

        Debug.Log(
            "[Enhanced Heavy] End"
        );
    }

    // Animation Event를 잘못 배치했거나 빠뜨렸을 때
    // 플레이어가 영구적으로 공격 상태에 갇히는 것만 방지한다.
    private IEnumerator EnhancedHeavySafetyRoutine()
    {
        yield return new WaitForSeconds(
            EnhancedHeavySafetyTimeout
        );

        if (!enhancedHeavyAttackActive)
            yield break;

        Debug.LogWarning(
            "[Enhanced Heavy] " +
            "Animation Event timeout. " +
            "Check HitboxOn / HitboxOff / AttackEnd events."
        );

        chargeAttackHitbox?.Deactivate();

        enhancedHeavyAttackActive = false;
        currentAttackType = AttackType.None;
        currentPhase = AttackPhase.None;
        enhancedHeavySafetyCoroutine = null;
    }

    private void ResetEnhancedHeavyState()
    {
        enhancedHeavyAttackActive = false;
        enhancedHeavySafetyCoroutine = null;
    }

    private void StartDashAttack()
    {
        if (playerDash == null ||
            !playerDash.IsDashing)
        {
            return;
        }

        float direction =
            playerMovement != null
                ? playerMovement.FacingDirection
                : 1f;

        playerDash.CancelDashForAttack();

        currentAttackType =
            AttackType.DashAttack;

        currentPhase =
            AttackPhase.Startup;

        if (playerMovement != null)
        {
            playerMovement.StartDashAttackMove(
                direction,
                dashAttackMoveDistance,
                dashAttackMoveDuration
            );
        }

        CombatLogger.Instance?
            .RecordPlayerResponse(
                PlayerResponseType.DashAttack
            );

        StartCoroutine(
            StopDashAttackMoveAfterDelay()
        );
    }

    public bool TryStartEnhancedDashAttack()
    {
        if (precisionStack == null)
            return false;

        if (!precisionStack.CanUseEnhancedAttack)
            return false;

        if (IsAttacking || IsCharging)
            return false;

        if (playerMovement == null)
            return false;

        if (enhancedDashAttackData == null ||
            enhancedDashAttackHitbox == null)
        {
            return false;
        }

        if (!precisionStack
                .TryConsumeEnhancedAttack())
        {
            return false;
        }

        enhancedDashDirection =
            playerMovement.FacingDirection;

        currentAttackType =
            AttackType.EnhancedDashAttack;

        currentPhase =
            AttackPhase.Startup;

        enhancedDashPhase =
            EnhancedDashPhase.Prepare;

        Debug.Log(
            "[Precision Attack] " +
            "Enhanced Dash Attack Prepare"
        );

        CombatLogger.Instance?
            .RecordPlayerResponse(
                PlayerResponseType.DashAttack
            );

        return true;
    }

    public void OnEnhancedDashPrepareEnd()
    {
        if (currentAttackType !=
            AttackType.EnhancedDashAttack)
        {
            return;
        }

        if (enhancedDashPhase !=
            EnhancedDashPhase.Prepare)
        {
            return;
        }

        enhancedDashPhase =
            EnhancedDashPhase.Attack;

        Debug.Log(
            "[Enhanced Dash] Prepare End"
        );
    }

    public void OnEnhancedDashAttackStart()
    {
        if (currentAttackType !=
            AttackType.EnhancedDashAttack)
        {
            return;
        }

        if (enhancedDashPhase !=
            EnhancedDashPhase.Attack)
        {
            return;
        }

        if (enhancedDashActive)
            return;

        currentPhase =
            AttackPhase.Active;

        enhancedDashActive = true;

        enhancedDashAttackHitbox.Activate(
            enhancedDashAttackData
        );

        enhancedDashSweepStart =
            enhancedDashAttackHitbox
                .GetWorldCenter();

        playerMovement.StartDashAttackMove(
            enhancedDashDirection,
            enhancedDashMoveDistance,
            enhancedDashMoveDuration
        );

        Debug.Log(
            "[Enhanced Dash] Attack Start"
        );

        enhancedDashMoveCoroutine =
            StartCoroutine(
                EnhancedDashMoveRoutine()
            );
    }

    private IEnumerator EnhancedDashMoveRoutine()
    {
        yield return new WaitForSeconds(
            enhancedDashMoveDuration
        );

        FinishEnhancedDashActive();

        enhancedDashMoveCoroutine = null;

        if (currentAttackType ==
            AttackType.EnhancedDashAttack)
        {
            currentPhase =
                AttackPhase.Recovery;
        }
    }

    private void FinishEnhancedDashActive()
    {
        if (!enhancedDashActive)
            return;

        Vector2 sweepEnd =
            enhancedDashAttackHitbox
                .GetWorldCenter();

        playerMovement?
            .StopDashAttackMove();

        enhancedDashAttackHitbox
            .SweepFromTo(
                enhancedDashSweepStart,
                sweepEnd
            );

        enhancedDashAttackHitbox
            .Deactivate();

        enhancedDashActive = false;

        Debug.Log(
            "[Enhanced Dash] Active End"
        );
    }

    public void OnEnhancedDashAttackEnd()
    {
        if (currentAttackType !=
            AttackType.EnhancedDashAttack)
        {
            return;
        }

        if (enhancedDashMoveCoroutine != null)
        {
            StopCoroutine(
                enhancedDashMoveCoroutine
            );

            enhancedDashMoveCoroutine = null;
        }

        FinishEnhancedDashActive();

        enhancedDashPhase =
            EnhancedDashPhase.None;

        currentAttackType =
            AttackType.None;

        currentPhase =
            AttackPhase.None;

        Debug.Log(
            "[Enhanced Dash] Attack End"
        );
    }

    private IEnumerator StopDashAttackMoveAfterDelay()
    {
        yield return new WaitForSeconds(
            dashAttackMoveDuration
        );

        playerMovement?
            .StopDashAttackMove();
    }

    public void OnDashAttackHitboxOn()
    {
        if (currentAttackType !=
            AttackType.DashAttack)
        {
            return;
        }

        currentPhase =
            AttackPhase.Active;

        if (dashAttackHitbox != null &&
            dashAttackData != null)
        {
            dashAttackHitbox.Activate(
                dashAttackData
            );
        }
    }

    public void OnDashAttackHitboxOff()
    {
        if (dashAttackHitbox != null)
        {
            dashAttackHitbox.Deactivate();
        }

        if (currentAttackType ==
            AttackType.DashAttack)
        {
            currentPhase =
                AttackPhase.Recovery;
        }
    }

    public void OnDashAttackEnd()
    {
        if (dashAttackHitbox != null)
        {
            dashAttackHitbox.Deactivate();
        }

        playerMovement?
            .StopDashAttackMove();

        if (currentAttackType !=
            AttackType.DashAttack)
        {
            return;
        }

        currentAttackType =
            AttackType.None;

        currentPhase =
            AttackPhase.None;
    }
}
