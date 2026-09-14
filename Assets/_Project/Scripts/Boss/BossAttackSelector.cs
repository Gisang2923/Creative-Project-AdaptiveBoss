using System.Collections.Generic;
using UnityEngine;

public class BossAttackSelector : MonoBehaviour
{
    [SerializeField] private BossAttack[] attacks;

    private BossAttack lastAttack;
    public bool HasValidAttack(float distance)
    {
        foreach (BossAttack attack in attacks)
        {
            if (distance >= attack.minRange &&
                distance <= attack.maxRange)
            {
                return true;
            }
        }

        return false;
    }
    public BossAttack SelectAttack(float distance)
    {
        List<BossAttack> validAttacks = new List<BossAttack>();

        foreach (BossAttack attack in attacks)
        {
            if (distance < attack.minRange ||
                distance > attack.maxRange)
            {
                continue;
            }

            // 바로 직전 공격 반복 방지
            if (attack == lastAttack && attacks.Length > 1)
                continue;

            validAttacks.Add(attack);
        }

        // 직전 공격 제외 때문에 후보가 사라졌다면
        // 거리 조건만 만족하는 공격을 다시 허용
        if (validAttacks.Count == 0)
        {
            foreach (BossAttack attack in attacks)
            {
                if (distance >= attack.minRange &&
                    distance <= attack.maxRange)
                {
                    validAttacks.Add(attack);
                }
            }
        }

        if (validAttacks.Count == 0)
            return null;

        BossAttack selected =
            validAttacks[Random.Range(0, validAttacks.Count)];

        lastAttack = selected;

        return selected;
    }
}