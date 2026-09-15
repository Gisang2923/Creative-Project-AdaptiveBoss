using System.Collections.Generic;
using UnityEngine;

public class BossAttackSelector : MonoBehaviour
{
    [SerializeField] private BossAttack[] attacks;

    private BossAttack lastAttack;
    private BossActionIntent? lastIntent;

    public bool HasValidAttack(float distance)
    {
        foreach (BossAttack attack in attacks)
        {
            if (IsInRange(attack, distance))
                return true;
        }

        return false;
    }

    public BossAttack SelectAttack(float distance)
    {
        List<BossAttack> validAttacks = new List<BossAttack>();

        // 현재 거리에서 사용 가능한 공격 수집
        foreach (BossAttack attack in attacks)
        {
            if (IsInRange(attack, distance))
                validAttacks.Add(attack);
        }

        if (validAttacks.Count == 0)
            return null;

        // 사용 가능한 Intent 수집
        List<BossActionIntent> validIntents =
            new List<BossActionIntent>();

        foreach (BossAttack attack in validAttacks)
        {
            if (!validIntents.Contains(attack.intent))
                validIntents.Add(attack.intent);
        }

        // 가능하면 직전 Intent 반복 방지
        if (lastIntent.HasValue && validIntents.Count > 1)
        {
            validIntents.Remove(lastIntent.Value);
        }

        // Intent 먼저 선택
        BossActionIntent selectedIntent =
            validIntents[Random.Range(0, validIntents.Count)];

        // 선택된 Intent 내부의 공격 수집
        List<BossAttack> intentAttacks =
            new List<BossAttack>();

        foreach (BossAttack attack in validAttacks)
        {
            if (attack.intent != selectedIntent)
                continue;

            if (attack == lastAttack)
                continue;

            intentAttacks.Add(attack);
        }

        // 해당 Intent에 공격이 하나뿐이면 반복 허용
        if (intentAttacks.Count == 0)
        {
            foreach (BossAttack attack in validAttacks)
            {
                if (attack.intent == selectedIntent)
                    intentAttacks.Add(attack);
            }
        }

        BossAttack selected =
            intentAttacks[Random.Range(0, intentAttacks.Count)];

        lastIntent = selectedIntent;
        lastAttack = selected;

        Debug.Log(
            $"Boss Intent: {selectedIntent} | Attack: {selected.attackType}"
        );

        return selected;
    }

    private bool IsInRange(BossAttack attack, float distance)
    {
        return distance >= attack.minRange &&
               distance <= attack.maxRange;
    }
}