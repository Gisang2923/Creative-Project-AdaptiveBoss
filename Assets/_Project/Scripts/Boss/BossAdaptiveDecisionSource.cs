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
public enum AdaptiveHabitType
{
    None,
    Chase,
    Retreat,
    Neutral,
    Attack,
    Counter
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

    [Header("Contextual Follow-up")]

    [SerializeField, Range(0f, 1f)]
    private float frontStepRetreatThreshold = 0.6f;

    [SerializeField, Range(0f, 1f)]
    private float maxRetreatFrontStepChance = 0.45f;

    [SerializeField]
    private float maxRetreatChargeMultiplier = 1.5f;
    [SerializeField, Range(0f, 1f)]
    private float chaseHabitThreshold = 0.6f;

    [SerializeField]
    private float followUpOpportunityDuration = 1.2f;

    [SerializeField, Range(0f, 1f)]
    private float maxChaseParryChance = 0.35f;

    [SerializeField]
    private float maxChaseChargeMultiplier = 1.5f;

    private BossBehaviorType followUpContext;
    private AdaptiveHabitType followUpHabit =
        AdaptiveHabitType.None;

    private float followUpHabitScore;
    private float followUpTimer;

    private bool hasFollowUpOpportunity;
    [Header("Spatial Adaptation")]
    [SerializeField, Range(0f, 1f)]
    private float retreatHabitThreshold = 0.4f;

    [SerializeField, Range(0f, 1f)]
    private float maxFrontStepChance = 0.65f;
    private void Update()
    {
        if (!hasFollowUpOpportunity)
            return;

        followUpTimer -= Time.deltaTime;

        if (followUpTimer <= 0f)
        {
            ClearFollowUpOpportunity();
        }
    }
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
        if (IsActiveFollowUp(
                BossBehaviorType.BackDodge,
                AdaptiveHabitType.Chase) &&
            attack.attackType ==
                BossAttackType.ChargeSlash)
        {
            float influence =
                Mathf.InverseLerp(
                    chaseHabitThreshold,
                    1f,
                    followUpHabitScore
                );

            float chargeMultiplier =
                Mathf.Lerp(
                    1f,
                    maxChaseChargeMultiplier,
                    influence
                );

            multiplier *= chargeMultiplier;

            Debug.Log(
                $"[Chase Punish] " +
                $"ChargeSlash Weight " +
                $"x{chargeMultiplier:F2}"
            );
        }
        if (IsActiveFollowUp(
                BossBehaviorType.FrontStep,
                AdaptiveHabitType.Retreat) &&
            attack.attackType ==
                BossAttackType.ChargeSlash)
        {
            float influence =
                Mathf.InverseLerp(
                    frontStepRetreatThreshold,
                    1f,
                    followUpHabitScore
                );

            float chargeMultiplier =
                Mathf.Lerp(
                    1f,
                    maxRetreatChargeMultiplier,
                    influence
                );

            multiplier *=
                chargeMultiplier;

            Debug.Log(
                $"[Retreat Pressure] " +
                $"ChargeSlash Weight " +
                $"x{chargeMultiplier:F2}"
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
    public float GetFrontStepChance(float baseChance)
    {
        if (behaviorModel == null)
            return baseChance;

        float retreatHabit =
            behaviorModel.GetRetreatHabitScore();

        if (retreatHabit <
            retreatHabitThreshold)
        {
            return baseChance;
        }

        float influence =
            Mathf.InverseLerp(
                retreatHabitThreshold,
                1f,
                retreatHabit
            );

        float adaptedChance =
            Mathf.Lerp(
                baseChance,
                maxFrontStepChance,
                influence
            );

        Debug.Log(
            $"[Spatial Adaptive] " +
            $"RetreatHabit={retreatHabit:F2} | " +
            $"FrontStepChance " +
            $"{baseChance:F2} → {adaptedChance:F2}"
        );

        return adaptedChance;
    }
    public float GetParryChance(
        float baseChance)
    {
        if (!IsActiveFollowUp(
                BossBehaviorType.BackDodge,
                AdaptiveHabitType.Chase))
        {
            return baseChance;
        }

        float influence =
            Mathf.InverseLerp(
                chaseHabitThreshold,
                1f,
                followUpHabitScore
            );

        float adaptedChance =
            Mathf.Lerp(
                baseChance,
                maxChaseParryChance,
                influence
            );

        Debug.Log(
            $"[Chase Punish] " +
            $"Parry Chance " +
            $"{baseChance:F2} → " +
            $"{adaptedChance:F2}"
        );

        return adaptedChance;
    }
    public void OpenFollowUpOpportunity(
        BossBehaviorType context)
    {
        if (behaviorModel == null)
            return;

        if (context == BossBehaviorType.BackDodge)
        {
            float chaseHabit =
                behaviorModel
                    .GetBackDodgeChaseHabitScore();

            if (chaseHabit < chaseHabitThreshold)
            {
                ClearFollowUpOpportunity();
                return;
            }

            followUpContext =
                BossBehaviorType.BackDodge;

            followUpHabit =
                AdaptiveHabitType.Chase;

            followUpHabitScore =
                chaseHabit;

            followUpTimer =
                followUpOpportunityDuration;

            hasFollowUpOpportunity = true;

            Debug.Log(
                $"[Adaptive Opportunity] " +
                $"Context=BackDodge | " +
                $"Habit=Chase | " +
                $"Score={chaseHabit:F2}"
            );
        }
        if (context == BossBehaviorType.FrontStep)
        {
            float retreatHabit =
                behaviorModel
                    .GetFrontStepRetreatHabitScore();

            if (retreatHabit < frontStepRetreatThreshold)
            {
                ClearFollowUpOpportunity();
                return;
            }

            followUpContext =
                BossBehaviorType.FrontStep;

            followUpHabit =
                AdaptiveHabitType.Retreat;

            followUpHabitScore =
                retreatHabit;

            followUpTimer =
                followUpOpportunityDuration;

            hasFollowUpOpportunity = true;

            Debug.Log(
                $"[Adaptive Opportunity] " +
                $"Context=FrontStep | " +
                $"Habit=Retreat | " +
                $"Score={retreatHabit:F2}"
            );
        }
    }
    private bool IsActiveFollowUp(
        BossBehaviorType context,
        AdaptiveHabitType habit)
    {
        return
            hasFollowUpOpportunity &&
            followUpContext == context &&
            followUpHabit == habit;
    }
    public bool ShouldUseRetreatPressureFrontStep()
    {
        if (!IsActiveFollowUp(
                BossBehaviorType.FrontStep,
                AdaptiveHabitType.Retreat))
        {
            return false;
        }

        float influence =
            Mathf.InverseLerp(
                frontStepRetreatThreshold,
                1f,
                followUpHabitScore
            );

        float chance =
            Mathf.Lerp(
                0.20f,
                maxRetreatFrontStepChance,
                influence
            );

        bool selected =
            Random.value <= chance;

        if (selected)
        {
            Debug.Log(
                $"[Retreat Pressure] " +
                $"Adaptive FrontStep Selected | " +
                $"Chance={chance:F2} | " +
                $"Habit={followUpHabitScore:F2}"
            );
        }

        return selected;
    }
    public void ConsumeFollowUpOpportunity()
    {
        if (!hasFollowUpOpportunity)
            return;

        Debug.Log(
            $"[Adaptive Opportunity Consumed] " +
            $"{followUpContext} → " +
            $"{followUpHabit}"
        );

        ClearFollowUpOpportunity();
    }

    private void ClearFollowUpOpportunity()
    {
        hasFollowUpOpportunity = false;

        followUpHabit =
            AdaptiveHabitType.None;

        followUpHabitScore = 0f;
        followUpTimer = 0f;
    }
}