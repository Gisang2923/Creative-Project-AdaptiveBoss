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

    [Header("Normal Attack")]
    [SerializeField] private AttackData normalAttackData;
    [SerializeField] private Hitbox normalAttackHitbox;

    private AttackPhase currentPhase = AttackPhase.None;

    public AttackPhase CurrentPhase => currentPhase;
    public bool IsAttacking => currentPhase != AttackPhase.None;

    // Active가 끝난 이후에는 대시 캔슬 허용
    public bool CanDashCancel =>
        currentPhase == AttackPhase.None ||
        currentPhase == AttackPhase.Recovery;

    private PlayerDamageReceiver damageReceiver;
    private PlayerCounter playerCounter;
    private void Awake()
    {
        damageReceiver = GetComponent<PlayerDamageReceiver>();
        playerCounter = GetComponent<PlayerCounter>();
    }
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (damageReceiver != null && (damageReceiver.IsStunned || damageReceiver.IsDead))
            return;
        if (playerCounter != null && playerCounter.IsCountering)
            return;
        if (!context.performed)
            return;

        if (IsAttacking)
            return;

        StartCoroutine(NormalAttack());
    }

    public void CancelAttack()
    {
        if (currentPhase != AttackPhase.Recovery)
            return;

        StopAllCoroutines();

        normalAttackHitbox.Deactivate();

        currentPhase = AttackPhase.None;
    }
    
    public void ForceCancelAttack()
    {
        StopAllCoroutines();

        normalAttackHitbox.Deactivate();

        currentPhase = AttackPhase.None;
    }
    private IEnumerator NormalAttack()
    {
        currentPhase = AttackPhase.Startup;

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

        currentPhase = AttackPhase.None;
    }
}