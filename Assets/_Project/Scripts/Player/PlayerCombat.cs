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
        Charge
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
    private void Awake()
    {
        damageReceiver = GetComponent<PlayerDamageReceiver>();
        playerCounter = GetComponent<PlayerCounter>();
        playerHeal = GetComponent<PlayerHeal>();
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
        if (damageReceiver != null && (damageReceiver.IsStunned || damageReceiver.IsDead))
            return;
        if (playerCounter != null && playerCounter.IsCountering)
            return;
        if (!context.performed)
            return;

        if (IsAttacking || IsCharging)
            return;

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

        currentPhase = AttackPhase.None;
    }
    
    public void ForceCancelAttack()
    {
        StopAllCoroutines();

        normalAttackHitbox.Deactivate();
        chargeAttackHitbox.Deactivate();

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
}