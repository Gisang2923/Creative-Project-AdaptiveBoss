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

    private AttackType currentAttackType = AttackType.None;
    public AttackType CurrentAttackType => currentAttackType;
    public enum ChargeState
    {
        None,
        Charging,
        Charged
    }
    [SerializeField] private AttackData chargeAttackData;
    [SerializeField] private Hitbox chargeAttackHitbox;
    [SerializeField] private float chargeTime = 0.7f;
    

    private ChargeState chargeState = ChargeState.None;
    private float chargeTimer;
    private AttackPhase currentPhase = AttackPhase.None;

    public AttackPhase CurrentPhase => currentPhase;
    public bool IsAttacking => currentPhase != AttackPhase.None;

    // Active가 끝난 이후에는 대시 캔슬 허용
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
    private float enhancedDashDirection;

    private Vector2 enhancedDashSweepStart;

    private bool enhancedDashActive;

    private Coroutine enhancedDashMoveCoroutine;

    private void Awake()
    {
        damageReceiver = GetComponent<PlayerDamageReceiver>();
        playerCounter = GetComponent<PlayerCounter>();
        playerHeal = GetComponent<PlayerHeal>();
        playerDash = GetComponent<PlayerDash>();
        playerMovement = GetComponent<PlayerMovement>();
        precisionStack = GetComponent<PlayerPrecisionStack>();
    }

    private void Update()
    {
        if (chargeState != ChargeState.Charging)
            return;

        chargeTimer += Time.deltaTime;

        if (chargeTimer >= chargeTime)
        {
            chargeState = ChargeState.Charged;
        }
    }
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (playerHeal != null && playerHeal.IsHealing)
            return;

        if (damageReceiver != null &&
            (damageReceiver.IsStunned || damageReceiver.IsDead))
            return;

        if (playerCounter != null && playerCounter.IsCountering)
            return;

        if (!context.performed)
            return;

        if (IsAttacking || IsCharging)
            return;

        // Dash 중 공격 입력 → DashAttack
        if (playerDash != null && playerDash.IsDashing)
        {
            StartDashAttack();
            return;
        }


        // =====================================
        // Precision Point 5 / 5
        // 다음 기본공격 1회를 Heavy로 대체
        // =====================================

        if (precisionStack != null &&
            precisionStack.CanUseEnhancedAttack)
        {
            if (precisionStack.TryConsumeEnhancedAttack())
            {
                Debug.Log(
                    "[Precision Attack] " +
                    "Normal Attack -> Heavy"
                );

                StartCoroutine(
                    ChargeAttack()
                );

                return;
            }
        }


        // =====================================
        // 일반 기본공격
        // =====================================
        StartCoroutine(NormalAttack());
    
}

    public void CancelAttack()
    {
        if (currentPhase != AttackPhase.Recovery)
            return;

        StopAllCoroutines();

        currentAttackType = AttackType.None;
        normalAttackHitbox.Deactivate();
        chargeAttackHitbox.Deactivate();
        dashAttackHitbox.Deactivate();
        enhancedDashAttackHitbox?.Deactivate();
        playerMovement?.StopDashAttackMove();

        enhancedDashPhase = EnhancedDashPhase.None;
        enhancedDashActive = false;
        enhancedDashMoveCoroutine = null;

        currentPhase = AttackPhase.None;
    }
    
    public void ForceCancelAttack()
    {
        StopAllCoroutines();

        normalAttackHitbox?.Deactivate();
        chargeAttackHitbox?.Deactivate();
        dashAttackHitbox?.Deactivate();
        enhancedDashAttackHitbox?.Deactivate();
        playerMovement?.StopDashAttackMove();
        
        currentAttackType = AttackType.None;
        chargeState = ChargeState.None;
        chargeTimer = 0f;

        enhancedDashPhase = EnhancedDashPhase.None;
        enhancedDashActive = false;
        enhancedDashMoveCoroutine = null;

        currentPhase = AttackPhase.None;
    }
    private IEnumerator NormalAttack()
    {
        currentAttackType = AttackType.Normal;
        currentPhase = AttackPhase.Startup;

        CombatLogger.Instance?.RecordPlayerResponse(
            PlayerResponseType.NormalAttack
        );
        yield return new WaitForSeconds(
            normalAttackData.startupTime
        );

        currentPhase = AttackPhase.Active;

        normalAttackHitbox.Activate(normalAttackData);

        yield return new WaitForSeconds(
            normalAttackData.activeTime
        );

        normalAttackHitbox.Deactivate();

        currentPhase = AttackPhase.Recovery;

        yield return new WaitForSeconds(
            normalAttackData.recoveryTime
        );

        currentAttackType = AttackType.None;
        currentPhase = AttackPhase.None;
    }
    public void OnChargeAttack(InputAction.CallbackContext context)
    {
        if (damageReceiver != null &&
            (damageReceiver.IsStunned || damageReceiver.IsDead))
            return;

        if (playerHeal != null && playerHeal.IsHealing)
            return;

        if (playerCounter != null && playerCounter.IsCountering)
            return;

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
            return;

        if (damageReceiver != null &&
            (damageReceiver.IsStunned ||
            damageReceiver.IsDead))
            return;

        chargeState = ChargeState.Charging;
        chargeTimer = 0f;
    }
    private void ReleaseCharge()
    {
        if (chargeState == ChargeState.None)
            return;

        if (chargeState == ChargeState.Charged)
        {
            StartCoroutine(ChargeAttack());
        }

        chargeState = ChargeState.None;
        chargeTimer = 0f;
    }

    private IEnumerator ChargeAttack()
    {
        currentAttackType = AttackType.Charge;
        currentPhase = AttackPhase.Startup;
        CombatLogger.Instance?.RecordPlayerResponse(
            PlayerResponseType.ChargeAttack
        );
        yield return new WaitForSeconds(
            chargeAttackData.startupTime
        );

        currentPhase = AttackPhase.Active;

        chargeAttackHitbox.Activate(chargeAttackData);

        yield return new WaitForSeconds(
            chargeAttackData.activeTime
        );

        chargeAttackHitbox.Deactivate();

        currentPhase = AttackPhase.Recovery;

        yield return new WaitForSeconds(
            chargeAttackData.recoveryTime
        );

        currentAttackType = AttackType.None;
        currentPhase = AttackPhase.None;
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


        // =====================================
        // Precision 5 Point 소비
        // =====================================

        if (!precisionStack.TryConsumeEnhancedAttack())
            return false;


        // =====================================
        // Enhanced Dash 시작
        // =====================================

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


        // =====================================
        // Active 시작
        // =====================================

        currentPhase =
            AttackPhase.Active;

        enhancedDashActive = true;


        // Hitbox 활성화
        enhancedDashAttackHitbox.Activate(
            enhancedDashAttackData
        );


        // Sweep 시작점 기록
        enhancedDashSweepStart =
            enhancedDashAttackHitbox
                .GetWorldCenter();


        // 6.0 전진 시작
        playerMovement.StartDashAttackMove(
            enhancedDashDirection,
            enhancedDashMoveDistance,
            enhancedDashMoveDuration
        );


        Debug.Log(
            "[Enhanced Dash] Attack Start"
        );


        // 정확히 MoveDuration 후 이동/판정 종료
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


        // 애니메이션은 아직 끝나지 않았더라도
        // 공격 판정은 끝남
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


        // 현재 Hitbox 위치
        Vector2 sweepEnd =
            enhancedDashAttackHitbox
                .GetWorldCenter();


        // 이동 정지
        playerMovement?
            .StopDashAttackMove();


        // =====================================
        // 시작점 → 끝점 전체 타격 판정
        // =====================================

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


        // 애니메이션이 MoveDuration보다
        // 먼저 끝나는 경우도 안전하게 처리
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

        playerMovement?.StopDashAttackMove();
    }
    public void OnDashAttackHitboxOn()
    {
        if (currentAttackType != AttackType.DashAttack)
            return;

        currentPhase = AttackPhase.Active;

        if (dashAttackHitbox != null && dashAttackData != null)
        {
            dashAttackHitbox.Activate(dashAttackData);
        }
    }
    public void OnDashAttackHitboxOff()
    {
        if (dashAttackHitbox != null)
        {
            dashAttackHitbox.Deactivate();
        }

        if (currentAttackType == AttackType.DashAttack)
        {
            currentPhase = AttackPhase.Recovery;
        }
    }
    public void OnDashAttackEnd()
    {
        if (dashAttackHitbox != null)
        {
            dashAttackHitbox.Deactivate();
        }
        playerMovement?.StopDashAttackMove();

        if (currentAttackType != AttackType.DashAttack)
            return;

        currentAttackType = AttackType.None;
        currentPhase = AttackPhase.None;
    }
}