using System.Collections.Generic;
using UnityEngine;

public enum AdaptiveHabitType
{
    None,
    Chase,
    Retreat,
    Neutral,
    Attack,
    Counter,
    ChaseCommit,
    PostPunishCommit
}

public enum TimingAdaptMode
{
    None,
    Early,
    Late
}

public enum AdaptiveResponseAction
{
    None,
    NormalDecision,
    Parry,
    BackDodge,
    FrontStep,
    ChargeSlash,
    HeavySlash,
    BasicSlash,
    Approach,
    Neutral
}

public class BossAdaptiveDecisionSource : MonoBehaviour
{
    private struct WeightedResponse
    {
        public AdaptiveResponseAction action;
        public float weight;

        public WeightedResponse(
            AdaptiveResponseAction action,
            float weight)
        {
            this.action = action;
            this.weight = weight;
        }
    }

    [Header("References")]
    [SerializeField]
    private PlayerBehaviorModel behaviorModel;

    [SerializeField]
    private PlayerCounter playerCounter;

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

    [SerializeField, Range(0f, 1f)]
    private float chaseCommitHabitThreshold = 0.6f;

    [SerializeField]
    private float followUpOpportunityDuration = 1.2f;

    [SerializeField, Range(0f, 1f)]
    private float maxChaseParryChance = 0.35f;

    [SerializeField]
    private float maxChaseChargeMultiplier = 1.5f;

    [SerializeField, Range(0f, 1f)]
    private float frontStepAttackThreshold = 0.6f;

    [SerializeField, Range(0f, 1f)]
    private float maxAttackParryChance = 0.4f;

    [Header("Counter Bait")]
    [SerializeField, Range(0f, 1f)]
    private float frontStepCounterThreshold = 0.6f;

    [SerializeField, Range(0f, 1f)]
    private float maxCounterHoldChance = 0.4f;

    [SerializeField, Range(0f, 1f)]
    private float maxCounterBackDodgeChance = 0.55f;

    [SerializeField]
    private float counterBaitHoldDuration = 0.35f;

    [Header("Persistent Adaptive Policy")]
    [SerializeField, Range(0f, 1f)]
    private float chaseHabitReleaseThreshold = 0.4f;

    [SerializeField, Range(0f, 1f)]
    private float frontStepHabitReleaseThreshold = 0.4f;

    [SerializeField, Range(0f, 1f)]
    private float policySwitchMargin = 0.15f;

    [Header("Post-Punish Commit")]
    [SerializeField, Range(0f, 1f)]
    private float postPunishCommitThreshold = 0.6f;

    [SerializeField, Range(0f, 1f)]
    private float postPunishCommitReleaseThreshold = 0.4f;

    [Header("Adaptive Response Pool")]
    [SerializeField, Min(0f)]
    private float normalDecisionWeight = 1f;

    [SerializeField, Min(0f)]
    private float secondaryAttackResponseWeight = 0.35f;
    [SerializeField, Min(0f)]
    private float chaseCommitBackDodgeWeight = 3f;

    [SerializeField, Min(0f)]
    private float approachPressureWeight = 0.75f;

    [SerializeField]
    private bool preventSameAdaptiveResponse = true;
    

    [Header("Heavy Timing Adaptation")]
    [SerializeField, Range(0f, 1f)]
    private float heavyTimingHabitThreshold = 0.6f;

    [SerializeField, Range(0f, 1f)]
    private float heavyTimingReleaseThreshold = 0.3f;

    [SerializeField, Range(0f, 1f)]
    private float maxHeavyTimingAdaptChance = 0.7f;

    [SerializeField]
    private float heavyTimingMargin = 0.07f;

    [SerializeField]
    private float minHeavyHoldTime = 0.15f;

    [SerializeField]
    private float maxHeavyHoldTime = 0.9f;

    [SerializeField]
    private float heavyTimingJitter = 0.04f;

    [Header("Charge Timing Adaptation")]
    [SerializeField, Range(0f, 1f)]
    private float chargeTimingHabitThreshold = 0.6f;

    [SerializeField, Range(0f, 1f)]
    private float chargeTimingReleaseThreshold = 0.3f;

    [SerializeField, Range(0f, 1f)]
    private float maxChargeTimingAdaptChance = 0.7f;

    [SerializeField]
    private float chargeTimingMargin = 0.07f;

    [SerializeField]
    private float minChargePostPassDelay = 0.12f;

    [SerializeField]
    private float maxChargePostPassDelay = 0.8f;

    [SerializeField]
    private float chargeTimingJitter = 0.04f;

    [SerializeField]
    private bool chargeTimingPolicyActive;

    [Header("Timing Mode Reevaluation")]
    [SerializeField]
    private int timingModeReevaluationInterval = 3;

    private BossBehaviorType followUpContext;

    private AdaptiveHabitType followUpHabit =
        AdaptiveHabitType.None;

    private float followUpHabitScore;
    private float followUpTimer;
    private bool hasFollowUpOpportunity;

    private AdaptiveHabitType backDodgePolicy =
        AdaptiveHabitType.None;

    private AdaptiveHabitType frontStepPolicy =
        AdaptiveHabitType.None;

    private float backDodgePolicyScore;
    private float frontStepPolicyScore;

    private bool postPunishPolicyActive;
    private float postPunishPolicyScore;

    private AdaptiveResponseAction lastAdaptiveResponse =
        AdaptiveResponseAction.None;

    // Debug UI에서 "어떤 Habit 때문에 어떤 대응이 선택됐는지"
    // 짧게 표시하기 위한 읽기 전용 기록.
    // 실제 AI 의사결정에는 사용하지 않는다.
    private AdaptiveHabitType lastAdaptiveResponseHabit =
        AdaptiveHabitType.None;

    private int adaptiveResponseSerial = 0;

    private bool heavyTimingPolicyActive;
    private TimingAdaptMode heavyTimingMode =
        TimingAdaptMode.None;

    private TimingAdaptMode chargeTimingMode =
        TimingAdaptMode.None;

    private int heavyAdaptiveUsesSinceMode = 0;
    private int chargeAdaptiveUsesSinceMode = 0;

    // =====================================
    // Adaptive Debug Read-Only Data
    // =====================================

    public AdaptiveHabitType DebugBackDodgePolicy =>
        backDodgePolicy;

    public float DebugBackDodgeChaseScore =>
        behaviorModel != null
            ? behaviorModel.GetBackDodgeChaseHabitScore()
            : 0f;

    public float DebugBackDodgeChaseCommitScore =>
        behaviorModel != null
            ? behaviorModel.GetBackDodgeChaseCommitHabitScore()
            : 0f;

    public float DebugBackDodgeLearnThreshold =>
        chaseHabitThreshold;

    public float DebugBackDodgeCommitThreshold =>
        chaseCommitHabitThreshold;

    public AdaptiveHabitType DebugFrontStepPolicy =>
        frontStepPolicy;

    public float DebugFrontStepRetreatScore =>
        behaviorModel != null
            ? behaviorModel.GetFrontStepRetreatHabitScore()
            : 0f;

    public float DebugFrontStepAttackScore =>
        behaviorModel != null
            ? behaviorModel.GetFrontStepAttackHabitScore()
            : 0f;

    public float DebugFrontStepCounterScore =>
        behaviorModel != null
            ? behaviorModel.GetFrontStepCounterHabitScore()
            : 0f;

    public float DebugFrontStepRetreatThreshold =>
        frontStepRetreatThreshold;

    public float DebugFrontStepAttackThreshold =>
        frontStepAttackThreshold;

    public float DebugFrontStepCounterThreshold =>
        frontStepCounterThreshold;

    public AdaptiveHabitType DebugPostPunishPolicy =>
        postPunishPolicyActive
            ? AdaptiveHabitType.PostPunishCommit
            : AdaptiveHabitType.None;

    public float DebugPostPunishCommitScore =>
        behaviorModel != null
            ? behaviorModel.GetPostPunishCommitHabitScore()
            : 0f;

    public float DebugPostPunishThreshold =>
        postPunishCommitThreshold;

    public bool DebugHeavyTimingActive =>
        heavyTimingPolicyActive;

    public TimingAdaptMode DebugHeavyTimingMode =>
        heavyTimingMode;

    public float DebugHeavyTimingScore =>
        behaviorModel != null
            ? behaviorModel.GetHeavyTimingHabitScore()
            : 0f;

    public float DebugHeavyTimingThreshold =>
        heavyTimingHabitThreshold;

    public bool DebugChargeTimingActive =>
        chargeTimingPolicyActive;

    public TimingAdaptMode DebugChargeTimingMode =>
        chargeTimingMode;

    public float DebugChargeTimingScore =>
        behaviorModel != null
            ? behaviorModel.GetChargeTimingHabitScore()
            : 0f;

    public float DebugChargeTimingThreshold =>
        chargeTimingHabitThreshold;

    public AdaptiveResponseAction DebugLastAdaptiveResponse =>
        lastAdaptiveResponse;

    public AdaptiveHabitType DebugLastAdaptiveResponseHabit =>
        lastAdaptiveResponseHabit;

    // 같은 Response가 나중에 다시 선택되어도 UI가 새 이벤트로 인식하도록
    // 실제 Adaptive Response가 선택될 때마다 증가한다.
    public int DebugAdaptiveResponseSerial =>
        adaptiveResponseSerial;

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

    // =====================================
    // Existing weight/chance hooks
    // Normal decision에서 기존 수치를 그대로 보존한다.
    // 새 Response Pool이 Normal을 선택하면 Opportunity가
    // 먼저 소비되므로 이 보정이 중복 적용되지는 않는다.
    // =====================================

    public float GetAttackWeightMultiplier(
        BossAttack attack,
        float distance)
    {
        if (behaviorModel == null)
            return 1f;

        float multiplier = 1f;

        bool chaseContext =
            IsActiveFollowUp(
                BossBehaviorType.BackDodge,
                AdaptiveHabitType.Chase
            ) ||
            IsActiveFollowUp(
                BossBehaviorType.BackDodge,
                AdaptiveHabitType.ChaseCommit
            );

        if (chaseContext &&
            attack.attackType ==
                BossAttackType.ChargeSlash)
        {
            float influence =
                Mathf.InverseLerp(
                    chaseHabitReleaseThreshold,
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
        }

        if (IsActiveFollowUp(
                BossBehaviorType.FrontStep,
                AdaptiveHabitType.Retreat) &&
            attack.attackType ==
                BossAttackType.ChargeSlash)
        {
            float influence =
                Mathf.InverseLerp(
                    frontStepHabitReleaseThreshold,
                    1f,
                    followUpHabitScore
                );

            float chargeMultiplier =
                Mathf.Lerp(
                    1f,
                    maxRetreatChargeMultiplier,
                    influence
                );

            multiplier *= chargeMultiplier;
        }

        return Mathf.Clamp(
            multiplier,
            minMultiplier,
            maxMultiplier
        );
    }

    public float GetParryChance(
        float baseChance)
    {
        bool chaseContext =
            IsActiveFollowUp(
                BossBehaviorType.BackDodge,
                AdaptiveHabitType.Chase
            ) ||
            IsActiveFollowUp(
                BossBehaviorType.BackDodge,
                AdaptiveHabitType.ChaseCommit
            );

        if (chaseContext)
        {
            float influence =
                Mathf.InverseLerp(
                    chaseHabitReleaseThreshold,
                    1f,
                    followUpHabitScore
                );

            return Mathf.Lerp(
                baseChance,
                maxChaseParryChance,
                influence
            );
        }

        if (IsActiveFollowUp(
                BossBehaviorType.FrontStep,
                AdaptiveHabitType.Attack))
        {
            float influence =
                Mathf.InverseLerp(
                    frontStepHabitReleaseThreshold,
                    1f,
                    followUpHabitScore
                );

            return Mathf.Lerp(
                baseChance,
                maxAttackParryChance,
                influence
            );
        }

        return baseChance;
    }

    public float GetBackDodgeChance(
        float baseChance)
    {
        if (!IsActiveFollowUp(
                BossBehaviorType.FrontStep,
                AdaptiveHabitType.Counter))
        {
            return baseChance;
        }

        float influence =
            Mathf.InverseLerp(
                frontStepHabitReleaseThreshold,
                1f,
                followUpHabitScore
            );

        float targetChance =
            Mathf.Max(
                baseChance,
                maxCounterBackDodgeChance
            );

        return Mathf.Lerp(
            baseChance,
            targetChance,
            influence
        );
    }

    // =====================================
    // Follow-up Opportunity
    // =====================================

    public void OpenFollowUpOpportunity(
        BossBehaviorType context)
    {
        if (behaviorModel == null)
            return;

        UpdatePersistentPolicy(context);

        if (context ==
            BossBehaviorType.BackDodge)
        {
            if (backDodgePolicy ==
                AdaptiveHabitType.None)
            {
                ClearFollowUpOpportunity();
                return;
            }

            SetFollowUpOpportunity(
                BossBehaviorType.BackDodge,
                backDodgePolicy,
                backDodgePolicyScore
            );

            return;
        }

        if (context ==
            BossBehaviorType.FrontStep)
        {
            if (frontStepPolicy ==
                AdaptiveHabitType.None)
            {
                ClearFollowUpOpportunity();
                return;
            }

            SetFollowUpOpportunity(
                BossBehaviorType.FrontStep,
                frontStepPolicy,
                frontStepPolicyScore
            );
        }
    }

    public void OpenPostPunishOpportunity()
    {
        if (behaviorModel == null)
            return;

        UpdatePostPunishPolicy();

        if (!postPunishPolicyActive)
            return;

        SetFollowUpOpportunity(
            BossBehaviorType.Attack,
            AdaptiveHabitType.PostPunishCommit,
            postPunishPolicyScore
        );
    }

    private void SetFollowUpOpportunity(
        BossBehaviorType context,
        AdaptiveHabitType habit,
        float score)
    {
        followUpContext = context;
        followUpHabit = habit;
        followUpHabitScore = score;
        followUpTimer =
            followUpOpportunityDuration;
        hasFollowUpOpportunity = true;

        Debug.Log(
            $"[Adaptive Opportunity] " +
            $"Context={context} | " +
            $"Policy={habit} | " +
            $"Score={score:F2}"
        );
    }

    private void UpdatePersistentPolicy(
        BossBehaviorType context)
    {
        if (context ==
            BossBehaviorType.BackDodge)
        {
            UpdateBackDodgePolicy();
            return;
        }

        if (context ==
            BossBehaviorType.FrontStep)
        {
            UpdateFrontStepPolicy();
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

    // =====================================
    // Explicit Adaptive Response Pool
    // =====================================

    public bool TrySelectFollowUpResponse(
        bool canParry,
        bool canBackDodge,
        bool canFrontStep,
        bool canCharge,
        bool canHeavy,
        bool canBasic,
        float baseParryChance,
        float baseBackDodgeChance,
        out AdaptiveResponseAction selected)
    {
        selected =
            AdaptiveResponseAction.None;

        if (!hasFollowUpOpportunity)
            return false;

        List<WeightedResponse> candidates =
            new List<WeightedResponse>();

        // Normal Decision은 항상 탈출구로 남긴다.
        AddCandidate(
            candidates,
            AdaptiveResponseAction.NormalDecision,
            normalDecisionWeight,
            true
        );

        if (followUpContext ==
                BossBehaviorType.BackDodge &&
            followUpHabit ==
                AdaptiveHabitType.Chase)
        {
            AddCandidate(
                candidates,
                AdaptiveResponseAction.ChargeSlash,
                maxChaseChargeMultiplier,
                canCharge
            );

            AddCandidate(
                candidates,
                AdaptiveResponseAction.HeavySlash,
                secondaryAttackResponseWeight,
                canHeavy
            );
        }
        else if (
            followUpContext ==
                BossBehaviorType.BackDodge &&
            followUpHabit ==
                AdaptiveHabitType.ChaseCommit)
        {
            AddCandidate(
                candidates,
                AdaptiveResponseAction.BackDodge,
                chaseCommitBackDodgeWeight,
                canBackDodge
            );
            AddCandidate(
                candidates,
                AdaptiveResponseAction.Parry,
                maxChaseParryChance,
                canParry
            );

            AddCandidate(
                candidates,
                AdaptiveResponseAction.HeavySlash,
                secondaryAttackResponseWeight,
                canHeavy
            );

            AddCandidate(
                candidates,
                AdaptiveResponseAction.ChargeSlash,
                maxChaseChargeMultiplier,
                canCharge
            );
        }
        else if (
            followUpContext ==
                BossBehaviorType.FrontStep &&
            followUpHabit ==
                AdaptiveHabitType.Retreat)
        {
            AddCandidate(
                candidates,
                AdaptiveResponseAction.ChargeSlash,
                maxRetreatChargeMultiplier,
                canCharge
            );

            AddCandidate(
                candidates,
                AdaptiveResponseAction.FrontStep,
                maxRetreatFrontStepChance,
                canFrontStep
            );

            AddCandidate(
                candidates,
                AdaptiveResponseAction.Approach,
                approachPressureWeight,
                true
            );
        }
        else if (
            followUpContext ==
                BossBehaviorType.FrontStep &&
            followUpHabit ==
                AdaptiveHabitType.Attack)
        {
            AddCandidate(
                candidates,
                AdaptiveResponseAction.Parry,
                maxAttackParryChance,
                canParry
            );

            AddCandidate(
                candidates,
                AdaptiveResponseAction.BackDodge,
                baseBackDodgeChance,
                canBackDodge
            );

            AddCandidate(
                candidates,
                AdaptiveResponseAction.HeavySlash,
                secondaryAttackResponseWeight,
                canHeavy
            );
        }
        else if (
            followUpContext ==
                BossBehaviorType.FrontStep &&
            followUpHabit ==
                AdaptiveHabitType.Counter)
        {
            // 기존 Hold 확률/시간은 버리지 않고
            // "공격하지 않는 Neutral"의 강도/길이로 재사용한다.
            AddCandidate(
                candidates,
                AdaptiveResponseAction.Neutral,
                maxCounterHoldChance,
                true
            );

            AddCandidate(
                candidates,
                AdaptiveResponseAction.BackDodge,
                maxCounterBackDodgeChance,
                canBackDodge
            );

            AddCandidate(
                candidates,
                AdaptiveResponseAction.Parry,
                baseParryChance,
                canParry
            );

            AddCandidate(
                candidates,
                AdaptiveResponseAction.BasicSlash,
                secondaryAttackResponseWeight,
                canBasic
            );
        }
        else if (
            followUpContext ==
                BossBehaviorType.Attack &&
            followUpHabit ==
                AdaptiveHabitType.PostPunishCommit)
        {
            AddCandidate(
                candidates,
                AdaptiveResponseAction.Parry,
                maxChaseParryChance,
                canParry
            );

            AddCandidate(
                candidates,
                AdaptiveResponseAction.BackDodge,
                maxCounterBackDodgeChance,
                canBackDodge
            );

            AddCandidate(
                candidates,
                AdaptiveResponseAction.Neutral,
                maxCounterHoldChance,
                true
            );
        }

        if (candidates.Count == 0)
            return false;

        float totalWeight = 0f;

        foreach (WeightedResponse candidate
                 in candidates)
        {
            totalWeight +=
                Mathf.Max(
                    0f,
                    candidate.weight
                );
        }

        if (totalWeight <= 0f)
            return false;

        float random =
            Random.Range(
                0f,
                totalWeight
            );

        float accumulated = 0f;

        foreach (WeightedResponse candidate
                 in candidates)
        {
            accumulated +=
                Mathf.Max(
                    0f,
                    candidate.weight
                );

            if (random <= accumulated)
            {
                selected =
                    candidate.action;

                return true;
            }
        }

        selected =
            candidates[
                candidates.Count - 1
            ].action;

        return true;
    }

    private void AddCandidate(
        List<WeightedResponse> candidates,
        AdaptiveResponseAction action,
        float weight,
        bool available)
    {
        if (!available ||
            weight <= 0f)
        {
            return;
        }

        if (preventSameAdaptiveResponse &&
            action !=
                AdaptiveResponseAction.NormalDecision &&
            action ==
                lastAdaptiveResponse)
        {
            return;
        }

        candidates.Add(
            new WeightedResponse(
                action,
                weight
            )
        );
    }

    public float GetAdaptiveNeutralDuration()
    {
        return counterBaitHoldDuration;
    }

    public void ConsumeFollowUpOpportunity(
        string selectedAction,
        AdaptiveResponseAction responseAction =
            AdaptiveResponseAction.None)
    {
        if (!hasFollowUpOpportunity)
            return;

        Debug.Log(
            $"[Adaptive Decision] " +
            $"Context={followUpContext} | " +
            $"SourceHabit={followUpHabit} | " +
            $"SelectedAction={selectedAction}"
        );

        if (responseAction ==
            AdaptiveResponseAction.NormalDecision)
        {
            // Normal Decision이 한 번 끼면
            // "같은 Adaptive Response 연속"이 아니므로 리셋한다.
            lastAdaptiveResponse =
                AdaptiveResponseAction.None;

            lastAdaptiveResponseHabit =
                AdaptiveHabitType.None;
        }
        else if (responseAction !=
                 AdaptiveResponseAction.None)
        {
            lastAdaptiveResponse =
                responseAction;

            // ClearFollowUpOpportunity() 전에 Source Habit을 저장한다.
            lastAdaptiveResponseHabit =
                followUpHabit;

            adaptiveResponseSerial++;
        }

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

    // =====================================
    // Persistent Policy
    // =====================================

    private void UpdateBackDodgePolicy()
    {
        float chaseHabit =
            behaviorModel
                .GetBackDodgeChaseHabitScore();

        float commitHabit =
            behaviorModel
                .GetBackDodgeChaseCommitHabitScore();

        bool chaseValid =
            chaseHabit >=
            chaseHabitThreshold;

        bool commitValid =
            chaseValid &&
            commitHabit >=
                chaseCommitHabitThreshold;

        if (backDodgePolicy ==
            AdaptiveHabitType.None)
        {
            if (commitValid)
            {
                SetBackDodgePolicy(
                    AdaptiveHabitType.ChaseCommit,
                    commitHabit,
                    "Learned"
                );
            }
            else if (chaseValid)
            {
                SetBackDodgePolicy(
                    AdaptiveHabitType.Chase,
                    chaseHabit,
                    "Learned"
                );
            }

            return;
        }

        if (backDodgePolicy ==
            AdaptiveHabitType.Chase)
        {
            backDodgePolicyScore =
                chaseHabit;

            if (commitValid &&
                commitHabit >=
                    chaseHabit +
                    policySwitchMargin)
            {
                SetBackDodgePolicy(
                    AdaptiveHabitType.ChaseCommit,
                    commitHabit,
                    "Switched"
                );

                return;
            }

            if (chaseHabit <
                chaseHabitReleaseThreshold)
            {
                ReleaseBackDodgePolicy();
            }

            return;
        }

        if (backDodgePolicy ==
            AdaptiveHabitType.ChaseCommit)
        {
            backDodgePolicyScore =
                commitHabit;

            if (chaseValid &&
                chaseHabit >=
                    commitHabit +
                    policySwitchMargin)
            {
                SetBackDodgePolicy(
                    AdaptiveHabitType.Chase,
                    chaseHabit,
                    "Switched"
                );

                return;
            }

            if (commitHabit <
                chaseHabitReleaseThreshold)
            {
                if (chaseValid)
                {
                    SetBackDodgePolicy(
                        AdaptiveHabitType.Chase,
                        chaseHabit,
                        "Switched"
                    );
                }
                else
                {
                    ReleaseBackDodgePolicy();
                }
            }
        }
    }

    private void SetBackDodgePolicy(
        AdaptiveHabitType policy,
        float score,
        string reason)
    {
        AdaptiveHabitType previous =
            backDodgePolicy;

        backDodgePolicy = policy;
        backDodgePolicyScore = score;

        Debug.Log(
            $"[Adaptive Policy {reason}] " +
            $"Context=BackDodge | " +
            $"{previous} → {policy} | " +
            $"Score={score:F2}"
        );
    }

    private void ReleaseBackDodgePolicy()
    {
        Debug.Log(
            $"[Adaptive Policy Released] " +
            $"Context=BackDodge | " +
            $"Policy={backDodgePolicy} | " +
            $"Score={backDodgePolicyScore:F2}"
        );

        backDodgePolicy =
            AdaptiveHabitType.None;
        backDodgePolicyScore = 0f;
    }

    private void UpdateFrontStepPolicy()
    {
        float retreatHabit =
            behaviorModel
                .GetFrontStepRetreatHabitScore();

        float attackHabit =
            behaviorModel
                .GetFrontStepAttackHabitScore();

        float counterHabit =
            behaviorModel
                .GetFrontStepCounterHabitScore();

        AdaptiveHabitType bestPolicy =
            ChooseBestFrontStepPolicy(
                retreatHabit,
                attackHabit,
                counterHabit,
                out float bestScore
            );

        if (frontStepPolicy ==
            AdaptiveHabitType.None)
        {
            if (bestPolicy !=
                AdaptiveHabitType.None)
            {
                SetFrontStepPolicy(
                    bestPolicy,
                    bestScore,
                    "Learned"
                );
            }

            return;
        }

        float currentScore =
            GetFrontStepScore(
                frontStepPolicy,
                retreatHabit,
                attackHabit,
                counterHabit
            );

        frontStepPolicyScore =
            currentScore;

        if (bestPolicy !=
                AdaptiveHabitType.None &&
            bestPolicy !=
                frontStepPolicy &&
            bestScore >=
                currentScore +
                policySwitchMargin)
        {
            SetFrontStepPolicy(
                bestPolicy,
                bestScore,
                "Switched"
            );

            return;
        }

        if (currentScore <
            frontStepHabitReleaseThreshold)
        {
            if (bestPolicy !=
                    AdaptiveHabitType.None &&
                bestPolicy !=
                    frontStepPolicy)
            {
                SetFrontStepPolicy(
                    bestPolicy,
                    bestScore,
                    "Switched"
                );
            }
            else
            {
                ReleaseFrontStepPolicy();
            }
        }
    }

    private AdaptiveHabitType
        ChooseBestFrontStepPolicy(
            float retreatHabit,
            float attackHabit,
            float counterHabit,
            out float bestScore)
    {
        AdaptiveHabitType best =
            AdaptiveHabitType.None;

        bestScore = 0f;

        if (retreatHabit >=
                frontStepRetreatThreshold &&
            retreatHabit > bestScore)
        {
            best =
                AdaptiveHabitType.Retreat;
            bestScore =
                retreatHabit;
        }

        if (attackHabit >=
                frontStepAttackThreshold &&
            attackHabit > bestScore)
        {
            best =
                AdaptiveHabitType.Attack;
            bestScore =
                attackHabit;
        }

        if (counterHabit >=
                frontStepCounterThreshold &&
            counterHabit > bestScore)
        {
            best =
                AdaptiveHabitType.Counter;
            bestScore =
                counterHabit;
        }

        return best;
    }

    private float GetFrontStepScore(
        AdaptiveHabitType policy,
        float retreatHabit,
        float attackHabit,
        float counterHabit)
    {
        switch (policy)
        {
            case AdaptiveHabitType.Retreat:
                return retreatHabit;

            case AdaptiveHabitType.Attack:
                return attackHabit;

            case AdaptiveHabitType.Counter:
                return counterHabit;

            default:
                return 0f;
        }
    }

    private void SetFrontStepPolicy(
        AdaptiveHabitType policy,
        float score,
        string reason)
    {
        AdaptiveHabitType previous =
            frontStepPolicy;

        frontStepPolicy = policy;
        frontStepPolicyScore = score;

        Debug.Log(
            $"[Adaptive Policy {reason}] " +
            $"Context=FrontStep | " +
            $"{previous} → {policy} | " +
            $"Score={score:F2}"
        );
    }

    private void ReleaseFrontStepPolicy()
    {
        Debug.Log(
            $"[Adaptive Policy Released] " +
            $"Context=FrontStep | " +
            $"Policy={frontStepPolicy} | " +
            $"Score={frontStepPolicyScore:F2}"
        );

        frontStepPolicy =
            AdaptiveHabitType.None;
        frontStepPolicyScore = 0f;
    }

    private void UpdatePostPunishPolicy()
    {
        float score =
            behaviorModel
                .GetPostPunishCommitHabitScore();

        if (!postPunishPolicyActive)
        {
            if (score <
                postPunishCommitThreshold)
            {
                return;
            }

            postPunishPolicyActive = true;
            postPunishPolicyScore = score;

            Debug.Log(
                $"[Adaptive Policy Learned] " +
                $"Context=PostPunish | " +
                $"Policy=PostPunishCommit | " +
                $"Score={score:F2}"
            );

            return;
        }

        postPunishPolicyScore = score;

        if (score <
            postPunishCommitReleaseThreshold)
        {
            postPunishPolicyActive = false;
            postPunishPolicyScore = 0f;

            Debug.Log(
                $"[Adaptive Policy Released] " +
                $"Context=PostPunish | " +
                $"Policy=PostPunishCommit | " +
                $"Score={score:F2}"
            );
        }
    }

    // =====================================
    // Heavy Timing Adaptation
    // 기존 구조 / 수치 그대로 유지
    // =====================================

    public float GetHeavyHoldTime(
        float baseHoldTime,
        float prepareCueTime,
        float heavyStartupTime)
    {
        if (behaviorModel == null ||
            playerCounter == null)
        {
            return baseHoldTime;
        }

        float habitScore =
            behaviorModel.GetHeavyTimingHabitScore();

        if (!heavyTimingPolicyActive)
        {
            if (habitScore <
                heavyTimingHabitThreshold)
            {
                return baseHoldTime;
            }

            heavyTimingPolicyActive = true;

            Debug.Log(
                $"[Heavy Timing Policy Learned] " +
                $"SuccessScore={habitScore:F2}"
            );
        }
        else if (habitScore <
                 heavyTimingReleaseThreshold)
        {
            heavyTimingPolicyActive = false;
            heavyTimingMode =
                TimingAdaptMode.None;

            heavyAdaptiveUsesSinceMode = 0;

            Debug.Log(
                $"[Heavy Timing Policy Released] " +
                $"SuccessScore={habitScore:F2}"
            );

            return baseHoldTime;
        }

        if (!behaviorModel
                .TryGetHeavyCounterDelay(
                    out float responseDelay))
        {
            return baseHoldTime;
        }

        float counterWindowStart =
            responseDelay;

        float counterWindowEnd =
            counterWindowStart +
            playerCounter.ParryWindow;

        float counterWindowMid =
            (
                counterWindowStart +
                counterWindowEnd
            ) * 0.5f;

        float baseHitTime =
            prepareCueTime +
            baseHoldTime +
            heavyStartupTime;

        if (heavyTimingMode ==
            TimingAdaptMode.None)
        {
            if (baseHitTime >=
                counterWindowMid)
            {
                heavyTimingMode =
                    TimingAdaptMode.Late;
            }
            else
            {
                heavyTimingMode =
                    TimingAdaptMode.Early;
            }

            heavyAdaptiveUsesSinceMode = 0;

            Debug.Log(
                $"[Heavy Timing Mode] " +
                $"학습 → {heavyTimingMode}"
            );
        }
        else if (
            heavyAdaptiveUsesSinceMode >=
                timingModeReevaluationInterval &&
            habitScore >=
                heavyTimingHabitThreshold)
        {
            TimingAdaptMode newMode;

            if (baseHitTime >=
                counterWindowMid)
            {
                newMode =
                    TimingAdaptMode.Late;
            }
            else
            {
                newMode =
                    TimingAdaptMode.Early;
            }

            if (newMode !=
                heavyTimingMode)
            {
                Debug.Log(
                    $"[Heavy Timing Mode] 재적응 | " +
                    $"{heavyTimingMode} → {newMode} | " +
                    $"이유: 적응된 Heavy에도 계속 Counter 성공"
                );

                heavyTimingMode =
                    newMode;
            }
            else
            {
                Debug.Log(
                    $"[Heavy Timing Mode] 재평가 | " +
                    $"{heavyTimingMode} 유지"
                );
            }

            heavyAdaptiveUsesSinceMode = 0;
        }

        float influence =
            Mathf.InverseLerp(
                heavyTimingReleaseThreshold,
                1f,
                habitScore
            );

        float adaptChance =
            Mathf.Lerp(
                0.75f,
                maxHeavyTimingAdaptChance,
                influence
            );

        if (Random.value >
            adaptChance)
        {
            Debug.Log(
                $"[Heavy Timing] 기본 공격 | " +
                $"Hold {baseHoldTime:F2}"
            );

            return baseHoldTime;
        }

        float targetHitTime;

        if (heavyTimingMode ==
            TimingAdaptMode.Late)
        {
            targetHitTime =
                counterWindowEnd +
                heavyTimingMargin;
        }
        else
        {
            targetHitTime =
                counterWindowStart -
                heavyTimingMargin;
        }

        float targetHoldTime =
            targetHitTime -
            prepareCueTime -
            heavyStartupTime;

        targetHoldTime +=
            Random.Range(
                -heavyTimingJitter,
                heavyTimingJitter
            );

        targetHoldTime =
            Mathf.Clamp(
                targetHoldTime,
                minHeavyHoldTime,
                maxHeavyHoldTime
            );

        heavyAdaptiveUsesSinceMode++;

        if (targetHoldTime >
            baseHoldTime)
        {
            Debug.Log(
                $"[Heavy Timing] 느려진 공격 | " +
                $"이유: 플레이어가 일찍 Counter해서 " +
                $"Counter 종료 뒤를 노림 | " +
                $"Hold {baseHoldTime:F2} → " +
                $"{targetHoldTime:F2}"
            );
        }
        else if (targetHoldTime <
                 baseHoldTime)
        {
            Debug.Log(
                $"[Heavy Timing] 빠른 공격 | " +
                $"이유: 플레이어가 늦게 Counter해서 " +
                $"Counter 시작 전에 공격 | " +
                $"Hold {baseHoldTime:F2} → " +
                $"{targetHoldTime:F2}"
            );
        }

        return targetHoldTime;
    }

    // =====================================
    // Charge Timing Adaptation
    // 구조는 유지하고 AdaptChance 역전만 수정
    // =====================================

    public float GetChargePostPassDelay(
        float basePostPassDelay,
        float dashAttackStartupTime)
    {
        if (behaviorModel == null ||
            playerCounter == null)
        {
            return basePostPassDelay;
        }

        float habitScore =
            behaviorModel
                .GetChargeTimingHabitScore();

        if (!chargeTimingPolicyActive)
        {
            if (habitScore <
                chargeTimingHabitThreshold)
            {
                return basePostPassDelay;
            }

            chargeTimingPolicyActive = true;

            Debug.Log(
                $"[Charge Timing Policy Learned] " +
                $"SuccessScore={habitScore:F2}"
            );
        }
        else if (habitScore <
                 chargeTimingReleaseThreshold)
        {
            chargeTimingPolicyActive = false;
            chargeTimingMode =
                TimingAdaptMode.None;

            chargeAdaptiveUsesSinceMode = 0;

            Debug.Log(
                $"[Charge Timing Policy Released] " +
                $"SuccessScore={habitScore:F2}"
            );

            return basePostPassDelay;
        }

        if (!behaviorModel
                .TryGetChargeCounterDelayFromPass(
                    out float responseDelay))
        {
            return basePostPassDelay;
        }

        float counterWindowStart =
            responseDelay;

        float counterWindowEnd =
            counterWindowStart +
            playerCounter.ParryWindow;

        float counterWindowMid =
            (
                counterWindowStart +
                counterWindowEnd
            ) * 0.5f;

        float baseHitTime =
            basePostPassDelay +
            dashAttackStartupTime;

        if (chargeTimingMode ==
            TimingAdaptMode.None)
        {
            float timingDifference =
                baseHitTime -
                counterWindowMid;

            const float centerTolerance =
                0.08f;

            if (Mathf.Abs(timingDifference) <
                centerTolerance)
            {
                chargeTimingMode =
                    Random.value < 0.5f
                        ? TimingAdaptMode.Early
                        : TimingAdaptMode.Late;
            }
            else if (timingDifference > 0f)
            {
                chargeTimingMode =
                    TimingAdaptMode.Late;
            }
            else
            {
                chargeTimingMode =
                    TimingAdaptMode.Early;
            }

            chargeAdaptiveUsesSinceMode = 0;

            Debug.Log(
                $"[Charge Timing Mode] " +
                $"학습 → {chargeTimingMode}"
            );
        }
        else if (
            chargeAdaptiveUsesSinceMode >=
                timingModeReevaluationInterval &&
            habitScore >=
                chargeTimingHabitThreshold)
        {
            TimingAdaptMode newMode;

            float timingDifference =
                baseHitTime -
                counterWindowMid;

            if (timingDifference > 0f)
            {
                newMode =
                    TimingAdaptMode.Late;
            }
            else
            {
                newMode =
                    TimingAdaptMode.Early;
            }

            if (newMode !=
                chargeTimingMode)
            {
                Debug.Log(
                    $"[Charge Timing Mode] 재적응 | " +
                    $"{chargeTimingMode} → {newMode} | " +
                    $"이유: 적응된 Charge에도 계속 Counter 성공"
                );

                chargeTimingMode =
                    newMode;
            }
            else
            {
                Debug.Log(
                    $"[Charge Timing Mode] 재평가 | " +
                    $"{chargeTimingMode} 유지"
                );
            }

            chargeAdaptiveUsesSinceMode = 0;
        }

        float influence =
            Mathf.InverseLerp(
                chargeTimingReleaseThreshold,
                1f,
                habitScore
            );

        // 기존 코드:
        // 0.75 → maxChargeTimingAdaptChance(현재 Scene 0.70)
        // 로 보간되어 Habit이 강해질수록 확률이 낮아졌다.
        // 기존 threshold / max 값은 유지하고 방향만 정상화한다.
        float minimumAdaptChance =
            Mathf.Min(
                chargeTimingHabitThreshold,
                maxChargeTimingAdaptChance
            );

        float adaptChance =
            Mathf.Lerp(
                minimumAdaptChance,
                maxChargeTimingAdaptChance,
                influence
            );

        if (Random.value >
            adaptChance)
        {
            Debug.Log(
                $"[Charge Timing] 기본 공격 | " +
                $"PostPass {basePostPassDelay:F2}"
            );

            return basePostPassDelay;
        }

        float targetHitTime;

        if (chargeTimingMode ==
            TimingAdaptMode.Late)
        {
            targetHitTime =
                counterWindowEnd +
                chargeTimingMargin;
        }
        else
        {
            targetHitTime =
                counterWindowStart -
                chargeTimingMargin;
        }

        float targetPostPassDelay =
            targetHitTime -
            dashAttackStartupTime;

        targetPostPassDelay +=
            Random.Range(
                -chargeTimingJitter,
                chargeTimingJitter
            );

        targetPostPassDelay =
            Mathf.Clamp(
                targetPostPassDelay,
                minChargePostPassDelay,
                maxChargePostPassDelay
            );

        chargeAdaptiveUsesSinceMode++;

        if (targetPostPassDelay >
            basePostPassDelay)
        {
            Debug.Log(
                $"[Charge Timing] 느려진 공격 | " +
                $"이유: 플레이어가 일찍 Counter해서 " +
                $"Counter 종료 뒤를 노림 | " +
                $"PostPass {basePostPassDelay:F2} → " +
                $"{targetPostPassDelay:F2}"
            );
        }
        else if (targetPostPassDelay <
                 basePostPassDelay)
        {
            Debug.Log(
                $"[Charge Timing] 빠른 공격 | " +
                $"이유: 플레이어가 늦게 Counter해서 " +
                $"Counter 시작 전에 공격 | " +
                $"PostPass {basePostPassDelay:F2} → " +
                $"{targetPostPassDelay:F2}"
            );
        }

        return targetPostPassDelay;
    }
}
