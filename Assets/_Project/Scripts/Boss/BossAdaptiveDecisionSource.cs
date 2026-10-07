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
public enum TimingAdaptMode
{
    None,
    Early,
    Late
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
    private float minHeavyHoldTime = 0.15f;

    [SerializeField]
    private float maxHeavyHoldTime = 0.9f;

    [SerializeField]
    private float heavyTimingJitter = 0.04f;

    

    [SerializeField]
    private PlayerCounter playerCounter;

    private bool heavyTimingPolicyActive;
    private TimingAdaptMode heavyTimingMode =
        TimingAdaptMode.None;
    
    // 현재 학습되어 유지 중인 Policy
    private AdaptiveHabitType backDodgePolicy =
        AdaptiveHabitType.None;

    private AdaptiveHabitType frontStepPolicy =
        AdaptiveHabitType.None;


        
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
    private TimingAdaptMode chargeTimingMode =
        TimingAdaptMode.None;

    [Header("Timing Mode Reevaluation")]

    [SerializeField]
    private int timingModeReevaluationInterval = 3;

    private int heavyAdaptiveUsesSinceMode = 0;
    private int chargeAdaptiveUsesSinceMode = 0;    
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
                    counterHabit > attackHabit) &&
                (!retreatValid ||
                    counterHabit > retreatHabit))
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


        // =============================
        // Policy 학습 / 해제
        // =============================

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


        // 성공했던 Counter Timing
        if (!behaviorModel
                .TryGetHeavyCounterDelay(
                    out float responseDelay))
        {
            return baseHoldTime;
        }


        float counterWindowStart =
            responseDelay +
            playerCounter.CounterStartupTime;

        float counterWindowEnd =
            counterWindowStart +
            playerCounter.ParryWindow;

        float counterWindowMid =
            (
                counterWindowStart +
                counterWindowEnd
            ) * 0.5f;


        // 기본 Heavy 실제 Hit
        float baseHitTime =
            prepareCueTime +
            baseHoldTime +
            heavyStartupTime;


        // =============================
        // 처음 Timing Mode 학습
        // =============================

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


        // =============================
        // 적응된 Heavy에도 계속 성공하면
        // Timing Mode 재평가
        // =============================

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


            if (newMode != heavyTimingMode)
            {
                Debug.Log(
                    $"[Heavy Timing Mode] 재적응 | " +
                    $"{heavyTimingMode} → {newMode} | " +
                    $"이유: 적응된 Heavy에도 계속 Counter 성공"
                );

                heavyTimingMode = newMode;
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


        // =============================
        // 대부분 적응 타이밍,
        // 가끔 Normal
        // =============================

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


        // =============================
        // Policy 학습 / 해제
        // =============================

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


        // Dash 종료 이후 Counter Timing
        if (!behaviorModel
                .TryGetChargeCounterDelayFromPass(
                    out float responseDelay))
        {
            return basePostPassDelay;
        }


        float counterWindowStart =
            responseDelay +
            playerCounter.CounterStartupTime;

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


        // =============================
        // 처음 한 번만 Early/Late 결정
        // =============================

        // =============================
        // 처음 Timing Mode 학습
        // =============================

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


        // =============================
        // 적응된 Charge에도 계속 성공하면
        // Timing Mode 재평가
        // =============================

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


            if (newMode != chargeTimingMode)
            {
                Debug.Log(
                    $"[Charge Timing Mode] 재적응 | " +
                    $"{chargeTimingMode} → {newMode} | " +
                    $"이유: 적응된 Charge에도 계속 Counter 성공"
                );

                chargeTimingMode = newMode;
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

        // =============================
        // 대부분 Policy 적용
        // 가끔 Normal
        // =============================

        float influence =
            Mathf.InverseLerp(
                chargeTimingReleaseThreshold,
                1f,
                habitScore
            );

        float adaptChance =
            Mathf.Lerp(
                0.75f,
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