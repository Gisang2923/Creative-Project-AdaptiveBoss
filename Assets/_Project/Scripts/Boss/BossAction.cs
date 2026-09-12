using System.Collections;
using UnityEngine;

public class BossAction : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private AttackData attackData;
    [SerializeField] private Hitbox attackHitbox;
    
    private bool isAttacking;

    public bool IsAttacking => isAttacking;

    [ContextMenu("Test Attack")]
    private void TestAttack()
    {
        ExecuteAttack();
    }

    public void ExecuteAttack()
    {
        if (isAttacking)
            return;

        StartCoroutine(AttackRoutine());
    }
    public void ForceCancelAttack()
    {
        StopAllCoroutines();

        attackHitbox.Deactivate();

        isAttacking = false;
    }
    private IEnumerator AttackRoutine()
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


}