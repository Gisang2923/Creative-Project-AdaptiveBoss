using UnityEngine;

[System.Serializable]
public class AdaptiveAttackRule
{
    [Header("Observed Habit")]
    public BossAttackType observedAttack;
    public PlayerResponseType playerResponse;

    [Range(0f, 1f)]
    public float minHabitScore = 0.6f;

    [Header("Target Attack")]
    public BossAttackType targetAttack;

    [Min(0f)]
    public float maxMultiplier = 1.5f;
}

public class BossAdaptiveDecisionSource : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerBehaviorModel behaviorModel;

    [Header("Rules")]
    [SerializeField]
    private AdaptiveAttackRule[] attackRules;

    [Header("Safety")]
    [SerializeField]
    private float minMultiplier = 0.6f;

    [SerializeField]
    private float maxMultiplier = 1.5f;

    public float GetAttackWeightMultiplier(
        BossAttack attack,
        float distance)
    {
        if (behaviorModel == null ||
            attackRules == null)
            return 1f;

        float multiplier = 1f;

        foreach (AdaptiveAttackRule rule in attackRules)
        {
            if (rule == null)
                continue;

            if (attack.attackType != rule.targetAttack)
                continue;

            float habitScore =
                behaviorModel.GetHabitScore(
                    rule.observedAttack,
                    rule.playerResponse
                );

            if (habitScore < rule.minHabitScore)
                continue;

            float influence =
                Mathf.InverseLerp(
                    rule.minHabitScore,
                    1f,
                    habitScore
                );

            float ruleMultiplier =
                Mathf.Lerp(
                    1f,
                    rule.maxMultiplier,
                    influence
                );

            multiplier *= ruleMultiplier;

            Debug.Log(
                $"[Adaptive] " +
                $"{rule.observedAttack} → " +
                $"{rule.playerResponse} | " +
                $"Habit={habitScore:F2} | " +
                $"{attack.attackType} x{ruleMultiplier:F2}"
            );
        }

        return Mathf.Clamp(
            multiplier,
            minMultiplier,
            maxMultiplier
        );
    }

    public float GetPostActionWeightMultiplier(
        BossAttack attack,
        BossPostAction postAction,
        float distance)
    {
        return 1f;
    }
}