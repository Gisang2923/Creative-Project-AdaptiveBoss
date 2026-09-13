using System.Collections;
using UnityEngine;

public class BossAction : MonoBehaviour
{
    private bool isAttacking;
    private Hitbox currentHitbox;

    public bool IsAttacking => isAttacking;

    public void ExecuteAttack(BossAttack attack)
    {
        if (isAttacking || attack == null)
            return;

        StartCoroutine(AttackRoutine(attack));
    }

    private IEnumerator AttackRoutine(BossAttack attack)
    {
        isAttacking = true;

        AttackData data = attack.attackData;
        Hitbox hitbox = attack.hitbox;

        currentHitbox = hitbox;

        // Startup
        yield return new WaitForSeconds(data.startupTime);

        // Active
        hitbox.Activate(data);

        yield return new WaitForSeconds(data.activeTime);

        hitbox.Deactivate();

        // Recovery
        yield return new WaitForSeconds(data.recoveryTime);

        currentHitbox = null;
        isAttacking = false;
    }

    public void ForceCancelAttack()
    {
        StopAllCoroutines();

        if (currentHitbox != null)
        {
            currentHitbox.Deactivate();
            currentHitbox = null;
        }

        isAttacking = false;
    }
}