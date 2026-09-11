using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [Header("Normal Attack")]
    [SerializeField] private AttackData normalAttackData;
    [SerializeField] private Hitbox normalAttackHitbox;

    private bool isAttacking;

    public bool IsAttacking => isAttacking;

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (isAttacking)
            return;

        StartCoroutine(NormalAttack());
    }

    private IEnumerator NormalAttack()
    {
        isAttacking = true;

        // Startup
        yield return new WaitForSeconds(
            normalAttackData.startupTime
        );

        // Active
        normalAttackHitbox.Activate(normalAttackData);

        yield return new WaitForSeconds(
            normalAttackData.activeTime
        );

        normalAttackHitbox.Deactivate();

        // Recovery
        yield return new WaitForSeconds(
            normalAttackData.recoveryTime
        );

        isAttacking = false;
    }
}