using UnityEngine;

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
    [SerializeField, Range(0f, 1f)]
    private float frontStepAttackThreshold = 0.6f;

    [SerializeField, Range(0f, 1f)]
    private float maxAttackParryChance = 0.4f;
    private BossBehaviorType followUpContext;

    private AdaptiveHabitType followUpHabit =
        AdaptiveHabitType.None;

    [Header("Counter Bait")]

    [SerializeField, Range(0f, 1f)]
    private float frontStepCounterThreshold = 0.6f;

    [SerializeField, Range(0f, 1f)]
    private float maxCounterHoldChance = 0.4f;

    [SerializeField, Range(0f, 1f)]
    private float maxCounterBackDodgeChance = 0.55f;

    [SerializeField]
    private float counterBaitHoldDuration = 0.35f;

    private bool counterBaitHoldEvaluated;   

    [Header("Persistent Adaptive Policy")]

    [SerializeField, Range(0f, 1f)]
    private float chaseHabitReleaseThreshold = 0.4f;

    [SerializeField, Range(0f, 1f)]
    private float frontStepHabitReleaseThreshold = 0.4f;

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
    private float minHeavyPrepareTime = 0.34f;

    [SerializeField]
    private float maxHeavyPrepareTime = 1.1f;

    [SerializeField]
    private float heavyTimingJitter = 0.04f;

    [SerializeField]
    private PlayerCounter playerCounter;

    private bool heavyTimingPolicyActive;
    
    // 현재 학습되어 유지 중인 Policy
    private AdaptiveHabitType backDodgePolicy =
        AdaptiveHabitType.None;

    private AdaptiveHabitType frontStepPolicy =
        AdaptiveHabitType.None;

    private float backDodgePolicyScore;
    private float frontStepPolicyScore;
    private float followUpHabitScore;
    private float followUpTimer;

    private bool hasFollowUpOpportunity;
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
        if (behaviorModel == null)
            return 1f;

        float multiplier = 1f;

        if (IsActiveFollowUp(
                BossBehaviorType.BackDodge,
                AdaptiveHabitType.Chase) &&
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
    
    public float GetParryChance(
        float baseChance)
    {
        // BackDodge + Chase
        if (IsActiveFollowUp(
                BossBehaviorType.BackDodge,
                AdaptiveHabitType.Chase))
        {
            float influence =
                Mathf.InverseLerp(
                    chaseHabitReleaseThreshold,
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

        // FrontStep + Attack
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

            float adaptedChance =
                Mathf.Lerp(
                    baseChance,
                    maxAttackParryChance,
                    influence
                );

            Debug.Log(
                $"[Aggression Punish] " +
                $"FrontStep Attack Habit=" +
                $"{followUpHabitScore:F2} | " +
                $"Parry Chance " +
                $"{baseChance:F2} → " +
                $"{adaptedChance:F2}"
            );

            return adaptedChance;
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

        float adaptedChance =
            Mathf.Lerp(
                baseChance,
                targetChance,
                influence
            );

        Debug.Log(
            $"[Counter Bait] " +
            $"BackDodge Chance " +
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

        // 먼저 현재 Habit을 보고
        // Persistent Policy를 갱신
        UpdatePersistentPolicy(context);
        counterBaitHoldEvaluated = false;

        // BackDodge
        if (context == BossBehaviorType.BackDodge)
        {
            if (backDodgePolicy ==
                AdaptiveHabitType.None)
            {
                ClearFollowUpOpportunity();
                return;
            }

            followUpContext =
                BossBehaviorType.BackDodge;

            followUpHabit =
                backDodgePolicy;

            followUpHabitScore =
                backDodgePolicyScore;

            followUpTimer =
                followUpOpportunityDuration;

            hasFollowUpOpportunity = true;

            Debug.Log(
                $"[Adaptive Opportunity] " +
                $"Context=BackDodge | " +
                $"Policy={backDodgePolicy} | " +
                $"Score={backDodgePolicyScore:F2}"
            );

            return;
        }


        // FrontStep
        if (context == BossBehaviorType.FrontStep)
        {
            if (frontStepPolicy ==
                AdaptiveHabitType.None)
            {
                ClearFollowUpOpportunity();
                return;
            }

            followUpContext =
                BossBehaviorType.FrontStep;

            followUpHabit =
                frontStepPolicy;

            followUpHabitScore =
                frontStepPolicyScore;

            followUpTimer =
                followUpOpportunityDuration;

            hasFollowUpOpportunity = true;

            Debug.Log(
                $"[Adaptive Opportunity] " +
                $"Context=FrontStep | " +
                $"Policy={frontStepPolicy} | " +
                $"Score={frontStepPolicyScore:F2}"
            );
        }
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
                frontStepHabitReleaseThreshold,
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
    public bool ShouldUseCounterBaitHold()
    {
        if (!IsActiveFollowUp(
                BossBehaviorType.FrontStep,
                AdaptiveHabitType.Counter))
        {
            return false;
        }

        // 하나의 Opportunity에서
        // Hold 추첨은 딱 한 번만
        if (counterBaitHoldEvaluated)
            return false;

        counterBaitHoldEvaluated = true;

        float influence =
            Mathf.InverseLerp(
                frontStepHabitReleaseThreshold,
                1f,
                followUpHabitScore
            );

        float chance =
            Mathf.Lerp(
                0.15f,
                maxCounterHoldChance,
                influence
            );

        bool selected =
            Random.value <= chance;

        if (selected)
        {
            Debug.Log(
                $"[Counter Bait] " +
                $"Hold Selected | " +
                $"Chance={chance:F2} | " +
                $"Habit={followUpHabitScore:F2}"
            );
        }

        return selected;
    }

    public float GetCounterBaitHoldDuration()
    {
        return counterBaitHoldDuration;
    }
    public void ConsumeFollowUpOpportunity(
        string selectedAction)
    {
        if (!hasFollowUpOpportunity)
            return;

        Debug.Log(
            $"[Adaptive Decision] " +
            $"Context={followUpContext} | " +
            $"SourceHabit={followUpHabit} | " +
            $"SelectedAction={selectedAction}"
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
        counterBaitHoldEvaluated = false;
    }
    private void UpdateBackDodgePolicy()
    {
        float chaseHabit =
            behaviorModel
                .GetBackDodgeChaseHabitScore();

        // 아직 아무 Policy도 학습하지 않은 상태
        if (backDodgePolicy ==
            AdaptiveHabitType.None)
        {
            if (chaseHabit >=
                chaseHabitThreshold)
            {
                backDodgePolicy =
                    AdaptiveHabitType.Chase;

                backDodgePolicyScore =
                    chaseHabit;

                Debug.Log(
                    $"[Adaptive Policy Learned] " +
                    $"Context=BackDodge | " +
                    $"Policy=Chase | " +
                    $"Score={chaseHabit:F2}"
                );
            }

            return;
        }


        // 이미 Chase Policy가 활성화된 상태
        if (backDodgePolicy ==
            AdaptiveHabitType.Chase)
        {
            backDodgePolicyScore =
                chaseHabit;

            // 충분히 습관이 사라졌을 때만 해제
            if (chaseHabit <
                chaseHabitReleaseThreshold)
            {
                Debug.Log(
                    $"[Adaptive Policy Released] " +
                    $"Context=BackDodge | " +
                    $"Policy=Chase | " +
                    $"Score={chaseHabit:F2}"
                );

                backDodgePolicy =
                    AdaptiveHabitType.None;

                backDodgePolicyScore = 0f;
            }
        }
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

        // 아직 학습된 Policy 없음
        if (frontStepPolicy ==
            AdaptiveHabitType.None)
        {
            bool retreatValid =
                retreatHabit >=
                frontStepRetreatThreshold;

            bool attackValid =
                attackHabit >=
                frontStepAttackThreshold;
            bool counterValid =
                counterHabit >=
                frontStepCounterThreshold;

            if (!retreatValid &&
                !attackValid &&
                !counterValid)
            {
                return;
            }


            if (counterValid &&
                (!attackValid ||
                    counterHabit >= attackHabit) &&
                (!retreatValid ||
                    counterHabit >= retreatHabit))
            {
                frontStepPolicy =
                    AdaptiveHabitType.Counter;

                frontStepPolicyScore =
                    counterHabit;
            }
            else if (attackValid &&
                    (!retreatValid ||
                    attackHabit > retreatHabit))
            {
                frontStepPolicy =
                    AdaptiveHabitType.Attack;

                frontStepPolicyScore =
                    attackHabit;
            }
            else
            {
                frontStepPolicy =
                    AdaptiveHabitType.Retreat;

                frontStepPolicyScore =
                    retreatHabit;
            }


            Debug.Log(
                $"[Adaptive Policy Learned] " +
                $"Context=FrontStep | " +
                $"Policy={frontStepPolicy} | " +
                $"Score={frontStepPolicyScore:F2}"
            );

            return;
        }


        // Retreat Policy 유지 중
        if (frontStepPolicy ==
            AdaptiveHabitType.Retreat)
        {
            frontStepPolicyScore =
                retreatHabit;

            if (retreatHabit <
                frontStepHabitReleaseThreshold)
            {
                Debug.Log(
                    $"[Adaptive Policy Released] " +
                    $"Context=FrontStep | " +
                    $"Policy=Retreat | " +
                    $"Score={retreatHabit:F2}"
                );

                frontStepPolicy =
                    AdaptiveHabitType.None;

                frontStepPolicyScore = 0f;
            }

            return;
        }


        // Attack Policy 유지 중
        if (frontStepPolicy ==
            AdaptiveHabitType.Attack)
        {
            frontStepPolicyScore =
                attackHabit;

            if (attackHabit <
                frontStepHabitReleaseThreshold)
            {
                Debug.Log(
                    $"[Adaptive Policy Released] " +
                    $"Context=FrontStep | " +
                    $"Policy=Attack | " +
                    $"Score={attackHabit:F2}"
                );

                frontStepPolicy =
                    AdaptiveHabitType.None;

                frontStepPolicyScore = 0f;
            }

            return;
        }

        if (frontStepPolicy ==
            AdaptiveHabitType.Counter)
        {
            frontStepPolicyScore =
                counterHabit;

            if (counterHabit <
                frontStepHabitReleaseThreshold)
            {
                Debug.Log(
                    $"[Adaptive Policy Released] " +
                    $"Context=FrontStep | " +
                    $"Policy=Counter | " +
                    $"Score={counterHabit:F2}"
                );

                frontStepPolicy =
                    AdaptiveHabitType.None;

                frontStepPolicyScore = 0f;
            }
        }
    }
    public float GetHeavyPrepareTime(
    float basePrepareTime,
    float heavyStartupTime)
{
    if (behaviorModel == null ||
        playerCounter == null)
    {
        return basePrepareTime;
    }

    float habitScore =
        behaviorModel
            .GetHeavyTimingHabitScore();


    // =============================
    // Policy 학습 / 해제
    // 오직 성공률만 판단
    // =============================

    if (!heavyTimingPolicyActive)
    {
        if (habitScore <
            heavyTimingHabitThreshold)
        {
            return basePrepareTime;
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

        Debug.Log(
            $"[Heavy Timing Policy Released] " +
            $"SuccessScore={habitScore:F2}"
        );

        return basePrepareTime;
    }


    // =============================
    // 성공했던 Counter Delay
    // =============================

    if (!behaviorModel
            .TryGetHeavyCounterDelay(
                out float responseDelay))
    {
        return basePrepareTime;
    }


    // Policy가 있어도
    // 항상 변칙을 사용하지 않음
    float influence =
        Mathf.InverseLerp(
            heavyTimingReleaseThreshold,
            1f,
            habitScore
        );

    float adaptChance =
        Mathf.Lerp(
            0.35f,
            maxHeavyTimingAdaptChance,
            influence
        );

    if (Random.value >
        adaptChance)
    {
        return basePrepareTime;
    }


    // =============================
    // 실제 Counter 활성 구간 계산
    // =============================

    float counterWindowStart =
        responseDelay +
        playerCounter.CounterStartupTime;

    float counterWindowEnd =
        counterWindowStart +
        playerCounter.ParryWindow;


    float targetHitTime;
    string variant;


    // 기존 Heavy의 실제 타격 시점
    float baseHitTime =
        basePrepareTime +
        heavyStartupTime;

    // Counter Window의 중앙
    float counterWindowMid =
        (
            counterWindowStart +
            counterWindowEnd
        ) * 0.5f;


    // =====================================
    // 기본 Hit이 Window 뒤쪽에 있음
    //
    // 플레이어가 비교적 일찍 Counter해서
    // Hit 시점까지 Counter를 유지한 경우
    //
    // → Counter가 끝난 뒤까지 기다림
    // =====================================
    if (baseHitTime >=
        counterWindowMid)
    {
        targetHitTime =
            counterWindowEnd +
            heavyTimingMargin;

        variant = "Late";
    }

    // =====================================
    // 기본 Hit이 Window 앞쪽에 있음
    //
    // 플레이어가 비교적 늦게 Counter해서
    // 공격 직전에 Counter를 켠 경우
    //
    // → Counter가 켜지기 전에 공격
    // =====================================
    else
    {
        targetHitTime =
            counterWindowStart -
            heavyTimingMargin;

        variant = "Early";
    }


    // 실제 타격은
    // Prepare 이후 startup을 거쳐 발생하므로 역산
    float targetPrepareTime =
        targetHitTime -
        heavyStartupTime;


    // 완전히 고정된 새 타이밍 방지
    targetPrepareTime +=
        Random.Range(
            -heavyTimingJitter,
            heavyTimingJitter
        );


    targetPrepareTime =
        Mathf.Clamp(
            targetPrepareTime,
            minHeavyPrepareTime,
            maxHeavyPrepareTime
        );


    Debug.Log(
        $"[Heavy Timing] " +
        $"Response={responseDelay:F2} | " +
        $"BaseHit={baseHitTime:F2} | " +
        $"CounterWindow=" +
        $"{counterWindowStart:F2}" +
        $"~{counterWindowEnd:F2} | " +
        $"Variant={variant} | " +
        $"TargetHit={targetHitTime:F2} | " +
        $"Prepare " +
        $"{basePrepareTime:F2} → " +
        $"{targetPrepareTime:F2}"
    );


    return targetPrepareTime;
}
}