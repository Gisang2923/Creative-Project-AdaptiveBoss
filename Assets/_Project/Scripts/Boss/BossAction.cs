using System.Collections;
using UnityEngine;

public class BossAction : MonoBehaviour
{
    [SerializeField] private Hitbox attackHitbox;

    private bool isAttacking;

    public bool IsAttacking => isAttacking;

    public void ExecuteAttack(AttackData attackData)
    {
        if (isAttacking || attackData == null)
            return;

        StartCoroutine(AttackRoutine(attackData));
    }

    private IEnumerator AttackRoutine(AttackData attackData)
    {
        isAttacking = true;

        // Startup
        yield return new WaitForSeconds(attackData.startupTime);

        // Active
        attackHitbox.Activate(attackData);

        yield return new WaitForSeconds(attackData.activeTime);

        attackHitbox.Deactivate();

        // Recovery
        yield return new WaitForSeconds(attackData.recoveryTime);

        isAttacking = false;
    }

    public void ForceCancelAttack()
    {
        StopAllCoroutines();

        attackHitbox.Deactivate();

        isAttacking = false;
    }
}