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
        DashAttack
    }
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
    [Header("Dash Attack")]
    [SerializeField] private AttackData dashAttackData;
    [SerializeField] private Hitbox dashAttackHitbox;
    [SerializeField] private float dashAttackMoveDistance = 0.63f;
    [SerializeField] private float dashAttackMoveDuration = 0.20f;
    private void Awake()
    {
        damageReceiver = GetComponent<PlayerDamageReceiver>();
        playerCounter = GetComponent<PlayerCounter>();
        playerHeal = GetComponent<PlayerHeal>();
        playerDash = GetComponent<PlayerDash>();
        playerMovement = GetComponent<PlayerMovement>();
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

        currentPhase = AttackPhase.None;
    }
    
    public void ForceCancelAttack()
    {
        StopAllCoroutines();

        normalAttackHitbox?.Deactivate();
        chargeAttackHitbox?.Deactivate();
        dashAttackHitbox?.Deactivate();
        playerMovement?.StopDashAttackMove();
        
        currentAttackType = AttackType.None;
        chargeState = ChargeState.None;
        chargeTimer = 0f;

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

        if (playerCounter != null && playerCounter.IsCountering)
            return;

        if (damageReceiver != null &&
            (damageReceiver.IsStunned || damageReceiver.IsDead))
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
        if (playerDash == null || !playerDash.IsDashing)
            return;

        float direction =
            playerMovement != null
            ? playerMovement.FacingDirection
            : 1f;

        playerDash.CancelDashForAttack();

        currentAttackType = AttackType.DashAttack;
        currentPhase = AttackPhase.Startup;

        if (playerMovement != null)
        {
            playerMovement.StartDashAttackMove(
                direction,
                dashAttackMoveDistance,
                dashAttackMoveDuration
            );
        }

        CombatLogger.Instance?.RecordPlayerResponse(
            PlayerResponseType.DashAttack
        );

        StartCoroutine(
            StopDashAttackMoveAfterDelay()
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