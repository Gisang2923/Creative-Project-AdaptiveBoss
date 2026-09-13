using UnityEngine;

public class BossAttackSelector : MonoBehaviour
{
    [SerializeField] private AttackData[] attacks;

    private int lastAttackIndex = -1;

    public AttackData SelectAttack()
    {
        if (attacks == null || attacks.Length == 0)
            return null;

        if (attacks.Length == 1)
            return attacks[0];

        int index;

        do
        {
            index = Random.Range(0, attacks.Length);
        }
        while (index == lastAttackIndex);

        lastAttackIndex = index;

        return attacks[index];
    }
}