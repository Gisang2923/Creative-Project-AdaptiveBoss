using System.Collections.Generic;
using UnityEngine;

public class BossAttackSelector : MonoBehaviour
{
    [SerializeField] private BossAttack[] attacks;

    [Header("Adaptive")]
    [SerializeField]
    private BossAdaptiveDecisionSource adaptiveSource;

    private BossAttack lastAttack;

    private readonly Dictionary<BossAttack, float> cooldowns =
        new Dictionary<BossAttack, float>();

    private void Update()
    {
        if (cooldowns.Count == 0)
            return;

        List<BossAttack> keys =
            new List<BossAttack>(cooldowns.Keys);

        foreach (BossAttack attack in keys)
        {
            cooldowns[attack] -= Time.deltaTime;

            if (cooldowns[attack] <= 0f)
            {
                cooldowns.Remove(attack);
            }
        }
    }

    public bool HasValidAttack(float distance)
    {
        foreach (BossAttack attack in attacks)
        {
            if (CanUseAttack(attack, distance))
                return true;
        }

        return false;
    }

    public BossAttack SelectAttack(float distance)
    {
        List<BossAttack> candidates =
            new List<BossAttack>();

        List<float> weights =
            new List<float>();

        float totalWeight = 0f;

        foreach (BossAttack attack in attacks)
        {
            if (!CanUseAttack(attack, distance))
                continue;

            float weight =
                CalculateAttackWeight(
                    attack,
                    distance
                );

            if (weight <= 0f)
                continue;

            candidates.Add(attack);
            weights.Add(weight);

            totalWeight += weight;
        }

        if (candidates.Count == 0)
            return null;

        float random =
            Random.Range(0f, totalWeight);

        float accumulated = 0f;

        for (int i = 0; i < candidates.Count; i++)
        {
            accumulated += weights[i];

            if (random <= accumulated)
            {
                BossAttack selected =
                    candidates[i];

                RegisterAttack(selected);

                return selected;
            }
        }

        BossAttack fallback =
            candidates[candidates.Count - 1];

        RegisterAttack(fallback);

        return fallback;
    }

    public BossPostAction SelectPostAction(
        BossAttack attack,
        float distance)
    {
        float hold =
            attack.holdWeight;

        float approach =
            attack.approachWeight;

        float retreat =
            attack.retreatWeight;

        if (adaptiveSource != null)
        {
            hold *=
                adaptiveSource.GetPostActionWeightMultiplier(
                    attack,
                    BossPostAction.Hold,
                    distance
                );

            approach *=
                adaptiveSource.GetPostActionWeightMultiplier(
                    attack,
                    BossPostAction.Approach,
                    distance
                );

            retreat *=
                adaptiveSource.GetPostActionWeightMultiplier(
                    attack,
                    BossPostAction.Retreat,
                    distance
                );
        }

        float total =
            hold + approach + retreat;

        if (total <= 0f)
            return BossPostAction.Hold;

        float random =
            Random.Range(0f, total);

        if (random < hold)
            return BossPostAction.Hold;

        random -= hold;

        if (random < approach)
            return BossPostAction.Approach;

        return BossPostAction.Retreat;
    }

    private float CalculateAttackWeight(
        BossAttack attack,
        float distance)
    {
        float weight =
            attack.baseWeight;

        // 같은 공격 연속 사용 억제
        if (attack == lastAttack)
        {
            weight *=
                attack.repeatWeightMultiplier;
        }

        // Adaptive는 지금 1.0
        if (adaptiveSource != null)
        {
            weight *=
                adaptiveSource.GetAttackWeightMultiplier(
                    attack,
                    distance
                );
        }

        return Mathf.Max(0f, weight);
    }

    private bool CanUseAttack(
        BossAttack attack,
        float distance)
    {
        if (!IsInRange(attack, distance))
            return false;

        if (cooldowns.ContainsKey(attack))
            return false;

        return true;
    }

    private void RegisterAttack(
        BossAttack attack)
    {
        lastAttack = attack;

        if (attack.decisionCooldown > 0f)
        {
            cooldowns[attack] =
                attack.decisionCooldown;
        }

        Debug.Log(
            $"Boss Attack → {attack.attackType}"
        );
    }

    private bool IsInRange(
        BossAttack attack,
        float distance)
    {
        return distance >= attack.minRange &&
               distance <= attack.maxRange;
    }
}