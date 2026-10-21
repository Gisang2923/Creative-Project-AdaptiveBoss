using UnityEngine;

public class BossController : MonoBehaviour
{
    public enum BossState
    {
        Idle,
        Approach,
        Reposition,
        Attack,
        Parry,
        BackDodge,
        FrontStep,
        Stunned,
        Dead
    }

    public enum PrecisionStunPhase
    {
        None,
        Stunned,
        Recovering
    }

    [Header("References")]
    [SerializeField]
    private BossMovement movement;

    [SerializeField]
    private BossAction bossAction;

    [SerializeField]
    private BossAttackSelector attackSelector;

    [SerializeField]
    private BossAdaptiveDecisionSource adaptiveSource;

    [Header("Combat")]
    [SerializeField]
    private BossDamageReceiver damageReceiver;

    [Header("Reposition")]
    [SerializeField]
    private float repositionDuration = 0.4f;

    private BossPostAction currentPostAction;
    private BossAttack lastExecutedAttack;
    private float repositionTimer;

    private BossState currentState =
        BossState.Idle;

    private bool wasAttacking;

    [Header("Back Dodge")]
    [SerializeField]
    private float backDodgeTriggerDistance = 1.5f;

    [SerializeField]
    private float backDodgeCooldown = 3f;

    private bool wasDodging;
    private bool backDodgeLogPending;

    [SerializeField]
    private float backDodgeResponseWindow = 0.6f;

    private float backDodgeResponseTimer;

    [SerializeField]
    private float backDodgeRecoveryTime = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float backDodgeChance = 0.35f;

    private float backDodgeCooldownTimer;
    private float stunTimer;

    [Header("Front Step")]
    [SerializeField]
    private float frontStepTriggerDistance = 4f;

    [SerializeField, Range(0f, 1f)]
    private float frontStepChance = 0.25f;

    [SerializeField]
    private float frontStepCooldown = 3f;

    private float frontStepCooldownTimer;

    [SerializeField]
    private float frontStepRecoveryTime = 0.3f;

    private bool wasFrontStepping;
    private bool frontStepLogPending;

    [SerializeField]
    private float frontStepResponseWindow = 0.3f;

    private float frontStepResponseTimer;

    // Adaptive Retreat Response의 Re-FrontStep은
    // 다시 FrontStep Opportunity를 생성하지 않는다.
    private bool currentFrontStepIsAdaptiveFollowUp;

    [Header("Parry")]
    [SerializeField]
    private BossParry bossParry;

    [SerializeField, Range(0f, 1f)]
    private float parryChance = 0.15f;

    [SerializeField]
    private float parryCooldown = 4f;

    private float parryCooldownTimer;
    private bool wasParrying;

    [Header("Precision Stun")]
    [SerializeField]
    private BossAnimator bossAnimator;

    [SerializeField]
    private float precisionStunDuration = 2.5f;

    [SerializeField]
    private int precisionStunHitLimit = 3;

    private PrecisionStunPhase precisionStunPhase =
        PrecisionStunPhase.None;

    private float precisionStunTimer;
    private int precisionStunHitCount;

    public bool IsPrecisionStunned =>
        precisionStunPhase ==
        PrecisionStunPhase.Stunned;

    public bool IsInPrecisionStun =>
        precisionStunPhase !=
        PrecisionStunPhase.None;

    public BossState CurrentState =>
        currentState;

    private bool battleStarted = false;

    public bool BattleStarted =>
        battleStarted;

    private bool currentAttackIsAdaptive;

    public bool CurrentAttackIsAdaptive =>
        currentAttackIsAdaptive;    

    private void Update()
    {
        if (!battleStarted)
            return;

        // =====================================
        // Precision Stun
        // =====================================

        if (precisionStunPhase !=
            PrecisionStunPhase.None)
        {
            if (precisionStunPhase ==
                PrecisionStunPhase.Stunned)
            {
                precisionStunTimer -=
                    Time.deltaTime;

                if (precisionStunTimer <= 0f)
                {
                    BeginPrecisionStunRecovery();
                }
            }

            return;
        }

        if (stunTimer > 0f)
        {
            stunTimer -=
                Time.deltaTime;
        }

        if (backDodgeCooldownTimer > 0f)
        {
            backDodgeCooldownTimer -=
                Time.deltaTime;
        }

        if (frontStepCooldownTimer > 0f)
        {
            frontStepCooldownTimer -=
                Time.deltaTime;
        }

        if (repositionTimer > 0f)
        {
            repositionTimer -=
                Time.deltaTime;
        }

        if (frontStepLogPending &&
            frontStepResponseTimer > 0f)
        {
            frontStepResponseTimer -=
                Time.deltaTime;
        }

        if (backDodgeLogPending &&
            backDodgeResponseTimer > 0f)
        {
            backDodgeResponseTimer -=
                Time.deltaTime;
        }

        if (frontStepLogPending &&
            !bossAction.IsFrontStepping &&
            repositionTimer <= 0f &&
            frontStepResponseTimer <= 0f)
        {
            CombatLogger.Instance?
                .EndBossBehavior(
                    movement.DistanceToTarget
                );

            frontStepLogPending = false;

            if (!currentFrontStepIsAdaptiveFollowUp)
            {
                adaptiveSource?
                    .OpenFollowUpOpportunity(
                        BossBehaviorType.FrontStep
                    );
            }

            currentFrontStepIsAdaptiveFollowUp =
                false;
        }

        if (backDodgeLogPending &&
            !bossAction.IsDodging &&
            repositionTimer <= 0f &&
            backDodgeResponseTimer <= 0f)
        {
            CombatLogger.Instance?
                .EndBossBehavior(
                    movement.DistanceToTarget
                );

            backDodgeLogPending = false;

            adaptiveSource?
                .OpenFollowUpOpportunity(
                    BossBehaviorType.BackDodge
                );
        }

        if (parryCooldownTimer > 0f)
        {
            parryCooldownTimer -=
                Time.deltaTime;
        }

        if (wasAttacking &&
            !bossAction.IsAttacking &&
            stunTimer <= 0f)
        {
            // BossAction의 isAttacking은 기존 Recovery가
            // 끝난 뒤 false가 되므로, 여기부터가
            // Post-Punish Commit 관찰의 기준점이다.
            CombatLogger.Instance?
                .EndBossAttack(
                    movement.DistanceToTarget
                );

            // 현재 입력을 읽고 대응하는 것이 아니라,
            // 이전 Post-Punish 로그로 이미 학습된 Policy만
            // 다음 판단에 사용한다.
            adaptiveSource?
                .OpenPostPunishOpportunity();

            StartPostAction();
            currentAttackIsAdaptive = false;
        }

        if (wasParrying &&
            (bossParry == null ||
             !bossParry.IsParrying) &&
            stunTimer <= 0f)
        {
            CombatLogger.Instance?
                .EndBossBehavior(
                    movement.DistanceToTarget
                );

            ChangeState(
                BossState.Idle
            );
        }

        if (wasDodging &&
            !bossAction.IsDodging)
        {
            StartBackDodgeRecovery();
        }

        if (wasFrontStepping &&
            !bossAction.IsFrontStepping)
        {
            StartFrontStepRecovery();
        }

        wasAttacking =
            bossAction.IsAttacking;

        wasDodging =
            bossAction.IsDodging;

        wasFrontStepping =
            bossAction.IsFrontStepping;

        wasParrying =
            bossParry != null &&
            bossParry.IsParrying;

        UpdateState();
    }

    private void FixedUpdate()
    {
        if (!battleStarted)
            return;

        ExecuteMovement();
    }

    private void UpdateState()
    {
        float distance =
            movement.DistanceToTarget;

        if (stunTimer > 0f)
        {
            ChangeState(
                BossState.Stunned
            );

            return;
        }

        if (bossParry != null &&
            bossParry.IsParrying)
        {
            ChangeState(
                BossState.Parry
            );

            return;
        }

        if (bossAction.IsBusy)
        {
            if (bossAction.IsDodging)
            {
                ChangeState(
                    BossState.BackDodge
                );
            }
            else if (
                bossAction.IsFrontStepping)
            {
                ChangeState(
                    BossState.FrontStep
                );
            }
            else
            {
                ChangeState(
                    BossState.Attack
                );
            }

            return;
        }

        if (repositionTimer > 0f)
        {
            if (currentPostAction ==
                BossPostAction.Hold)
            {
                ChangeState(
                    BossState.Idle
                );
            }
            else
            {
                ChangeState(
                    BossState.Reposition
                );
            }

            return;
        }

        // =====================================
        // Adaptive Response Pool
        // 기존 "확률 조금 증가"보다 먼저,
        // 학습된 Context의 명시적 Response를 판단한다.
        // =====================================

        if (TryAdaptiveFollowUp(
                distance))
        {
            return;
        }

        if (TryBackDodge(
                distance))
        {
            return;
        }

        if (TryParry())
            return;

        if (TryFrontStep(
                distance))
        {
            return;
        }

        if (attackSelector
            .HasValidAttack(
                distance))
        {
            StartAttack(
                distance
            );

            return;
        }

        ChangeState(
            BossState.Approach
        );
    }

    private void ExecuteMovement()
    {
        switch (currentState)
        {
            case BossState.Approach:
                movement.MoveTowardTarget();
                break;

            case BossState.Reposition:
                switch (currentPostAction)
                {
                    case BossPostAction.Approach:
                        movement.MoveTowardTarget();
                        break;

                    case BossPostAction.Retreat:
                        movement.MoveAwayFromTarget();
                        break;

                    case BossPostAction.Hold:
                        movement.Stop();
                        break;
                }

                break;

            case BossState.Idle:
                movement.Stop();
                break;

            case BossState.Parry:
                movement.Stop();
                break;

            case BossState.Attack:
                break;

            case BossState.BackDodge:
                break;

            case BossState.FrontStep:
                break;

            case BossState.Stunned:
                break;

            case BossState.Dead:
                movement.Stop();
                break;
        }
    }

    // =====================================
    // Adaptive Response 실행
    // =====================================

    private bool TryAdaptiveFollowUp(
        float distance)
    {
        if (adaptiveSource == null ||
            attackSelector == null)
        {
            return false;
        }

        bool canParry =
            bossParry != null &&
            !bossParry.IsParrying &&
            parryCooldownTimer <= 0f;

        bool canBackDodge =
            distance <=
                backDodgeTriggerDistance &&
            backDodgeCooldownTimer <= 0f;

        // 기존 Adaptive Re-FrontStep과 동일하게
        // 일반 FrontStep cooldown은 무시한다.
        // 단, 거리 조건은 유지한다.
        bool canFrontStep =
            distance > 1.5f &&
            distance <=
                frontStepTriggerDistance;

        bool canCharge =
            attackSelector
                .CanUseAttackType(
                    BossAttackType.ChargeSlash,
                    distance
                );

        bool canHeavy =
            attackSelector
                .CanUseAttackType(
                    BossAttackType.HeavySlash,
                    distance
                );

        bool canBasic =
            attackSelector
                .CanUseAttackType(
                    BossAttackType.BasicSlash,
                    distance
                );

        if (!adaptiveSource
                .TrySelectFollowUpResponse(
                    canParry,
                    canBackDodge,
                    canFrontStep,
                    canCharge,
                    canHeavy,
                    canBasic,
                    parryChance,
                    backDodgeChance,
                    out AdaptiveResponseAction response))
        {
            return false;
        }

        // Normal을 뽑은 경우에도 이번 Adaptive Opportunity의
        // 판단은 끝난 것이므로 소비한 뒤 기존 AI로 돌아간다.
        if (response ==
            AdaptiveResponseAction.NormalDecision)
        {
            adaptiveSource
                .ConsumeFollowUpOpportunity(
                    "NormalDecision",
                    response
                );

            return false;
        }

        switch (response)
        {
            case AdaptiveResponseAction.Parry:
                StartParry();

                adaptiveSource
                    .ConsumeFollowUpOpportunity(
                        "Parry",
                        response
                    );

                return true;

            case AdaptiveResponseAction.BackDodge:
                ExecuteAdaptiveBackDodge(
                    distance
                );

                adaptiveSource
                    .ConsumeFollowUpOpportunity(
                        "BackDodge",
                        response
                    );

                return true;

            case AdaptiveResponseAction.FrontStep:
                ExecuteAdaptiveFrontStep(
                    distance
                );

                adaptiveSource
                    .ConsumeFollowUpOpportunity(
                        "FrontStep",
                        response
                    );

                return true;

            case AdaptiveResponseAction.ChargeSlash:
                if (StartSpecificAttack(
                        distance,
                        BossAttackType.ChargeSlash))
                {
                    adaptiveSource
                        .ConsumeFollowUpOpportunity(
                            "ChargeSlash",
                            response
                        );

                    return true;
                }

                break;

            case AdaptiveResponseAction.HeavySlash:
                if (StartSpecificAttack(
                        distance,
                        BossAttackType.HeavySlash))
                {
                    adaptiveSource
                        .ConsumeFollowUpOpportunity(
                            "HeavySlash",
                            response
                        );

                    return true;
                }

                break;

            case AdaptiveResponseAction.BasicSlash:
                if (StartSpecificAttack(
                        distance,
                        BossAttackType.BasicSlash))
                {
                    adaptiveSource
                        .ConsumeFollowUpOpportunity(
                            "BasicSlash",
                            response
                        );

                    return true;
                }

                break;

            case AdaptiveResponseAction.Approach:
                movement.Stop();
                movement.FaceTarget();

                currentPostAction =
                    BossPostAction.Approach;

                repositionTimer =
                    repositionDuration;

                ChangeState(
                    BossState.Reposition
                );

                adaptiveSource
                    .ConsumeFollowUpOpportunity(
                        "ApproachPressure",
                        response
                    );

                Debug.Log(
                    "[Adaptive Response] " +
                    "Approach Pressure"
                );

                return true;

            case AdaptiveResponseAction.Neutral:
                movement.Stop();
                movement.FaceTarget();

                currentPostAction =
                    BossPostAction.Hold;

                repositionTimer =
                    adaptiveSource
                        .GetAdaptiveNeutralDuration();

                ChangeState(
                    BossState.Idle
                );

                adaptiveSource
                    .ConsumeFollowUpOpportunity(
                        "Neutral",
                        response
                    );

                Debug.Log(
                    "[Adaptive Response] " +
                    "Intentional Neutral"
                );

                return true;
        }

        return false;
    }

    private void ExecuteAdaptiveBackDodge(
        float distance)
    {
        movement.Stop();
        movement.FaceTarget();

        backDodgeResponseTimer =
            backDodgeResponseWindow;

        CombatLogger.Instance?
            .BeginBossBehavior(
                BossBehaviorType.BackDodge,
                distance
            );

        backDodgeLogPending = true;

        bossAction.ExecuteBackDodge();

        backDodgeCooldownTimer =
            backDodgeCooldown;

        ChangeState(
            BossState.BackDodge
        );

        Debug.Log(
            "[Adaptive Response] " +
            "BackDodge"
        );
    }

    private void ExecuteAdaptiveFrontStep(
        float distance)
    {
        movement.Stop();
        movement.FaceTarget();

        currentFrontStepIsAdaptiveFollowUp =
            true;

        frontStepResponseTimer =
            frontStepResponseWindow;

        CombatLogger.Instance?
            .BeginBossBehavior(
                BossBehaviorType.FrontStep,
                distance
            );

        frontStepLogPending = true;

        bossAction.ExecuteFrontStep();

        frontStepCooldownTimer =
            frontStepCooldown;

        ChangeState(
            BossState.FrontStep
        );

        Debug.Log(
            "[Adaptive Response] " +
            "Re-FrontStep"
        );
    }

    // =====================================
    // Attack
    // =====================================

    private void StartAttack(
        float distance)
    {
        currentAttackIsAdaptive = false;
        BossAttack selectedAttack =
            attackSelector
                .SelectAttack(
                    distance
                );

        ExecuteSelectedAttack(
            selectedAttack,
            distance
        );
    }

    private bool StartSpecificAttack(
        float distance,
        BossAttackType attackType)
    {
        BossAttack selectedAttack =
            attackSelector
                .SelectAttackType(
                    attackType,
                    distance
                );

        if (selectedAttack == null)
            return false;
        currentAttackIsAdaptive = true;
        ExecuteSelectedAttack(
            selectedAttack,
            distance
        );

        return true;
    }

    private void ExecuteSelectedAttack(
        BossAttack selectedAttack,
        float distance)
    {
        if (selectedAttack == null)
            return;

        movement.Stop();
        movement.FaceTarget();

        lastExecutedAttack =
            selectedAttack;

        // Heavy Timing은 기존 구조 그대로 유지
        if (selectedAttack.attackType ==
                BossAttackType.HeavySlash &&
            adaptiveSource != null)
        {
            float adaptedHoldTime =
                adaptiveSource
                    .GetHeavyHoldTime(
                        bossAction
                            .HeavyBaseHoldTime,
                        bossAction
                            .HeavyPrepareCueTime,
                        selectedAttack
                            .attackData
                            .startupTime
                    );
            if (!Mathf.Approximately(
                    adaptedHoldTime,
                    bossAction.HeavyBaseHoldTime))
            {
                currentAttackIsAdaptive = true;
            }
            bossAction
                .SetNextHeavyHoldTime(
                    adaptedHoldTime
                );
        }

        // Charge Timing은 기존 구조를 유지하고
        // DecisionSource 내부의 역전된 확률만 수정
        if (selectedAttack.attackType ==
                BossAttackType.ChargeSlash &&
            adaptiveSource != null)
        {
            float adaptedPostPassDelay =
                adaptiveSource
                    .GetChargePostPassDelay(
                        bossAction
                            .ChargePostPassDelay,
                        selectedAttack
                            .attackData
                            .startupTime
                    );
            if (!Mathf.Approximately(
                    adaptedPostPassDelay,
                    bossAction.ChargePostPassDelay))
            {
                currentAttackIsAdaptive = true;
            }
            bossAction
                .SetNextChargePostPassDelay(
                    adaptedPostPassDelay
                );
        }

        CombatLogger.Instance?
            .BeginBossAttack(
                selectedAttack.attackType,
                distance
            );

        bossAction.ExecuteAttack(
            selectedAttack
        );

        ChangeState(
            BossState.Attack
        );
    }

    private void ChangeState(
        BossState newState)
    {
        if (currentState ==
            newState)
        {
            return;
        }

        currentState =
            newState;
    }

    private void StartPostAction()
    {
        if (lastExecutedAttack == null)
        {
            currentPostAction =
                BossPostAction.Hold;

            repositionTimer =
                repositionDuration;

            return;
        }

        float distance =
            movement.DistanceToTarget;

        currentPostAction =
            attackSelector
                .SelectPostAction(
                    lastExecutedAttack,
                    distance
                );

        repositionTimer =
            repositionDuration;
    }

    // =====================================
    // Normal BackDodge
    // =====================================

    private bool TryBackDodge(
        float distance)
    {
        if (distance >
            backDodgeTriggerDistance)
        {
            return false;
        }

        if (backDodgeCooldownTimer > 0f)
            return false;

        float currentChance =
            backDodgeChance;

        // 기존 hook은 유지.
        // 새 Response Pool이 Normal을 선택했을 때는
        // Opportunity가 이미 소비되어 baseChance 그대로다.
        if (adaptiveSource != null)
        {
            currentChance =
                adaptiveSource
                    .GetBackDodgeChance(
                        backDodgeChance
                    );
        }

        if (Random.value >
            currentChance)
        {
            return false;
        }

        movement.Stop();
        movement.FaceTarget();

        backDodgeResponseTimer =
            backDodgeResponseWindow;

        CombatLogger.Instance?
            .BeginBossBehavior(
                BossBehaviorType.BackDodge,
                distance
            );

        backDodgeLogPending = true;

        bossAction.ExecuteBackDodge();

        backDodgeCooldownTimer =
            backDodgeCooldown;

        ChangeState(
            BossState.BackDodge
        );

        return true;
    }

    private void StartBackDodgeRecovery()
    {
        repositionTimer =
            backDodgeRecoveryTime;

        currentPostAction =
            BossPostAction.Hold;
    }

    private void StartFrontStepRecovery()
    {
        repositionTimer =
            frontStepRecoveryTime;

        currentPostAction =
            BossPostAction.Hold;
    }

    public void TriggerHitBackDodge()
    {
        if (!battleStarted)
            return;

        if (currentState ==
            BossState.Dead)
        {
            return;
        }

        bossAction
            .ForceCancelAttack();

        movement.Stop();
        movement.FaceTarget();

        bossAction.ExecuteBackDodge();

        backDodgeCooldownTimer =
            backDodgeCooldown;

        ChangeState(
            BossState.BackDodge
        );
    }

    // =====================================
    // Normal FrontStep
    // =====================================

    private bool TryFrontStep(
        float distance)
    {
        if (frontStepCooldownTimer > 0f)
            return false;

        if (distance <= 1.5f)
            return false;

        if (distance >
            frontStepTriggerDistance)
        {
            return false;
        }

        if (Random.value >
            frontStepChance)
        {
            return false;
        }

        movement.Stop();
        movement.FaceTarget();

        currentFrontStepIsAdaptiveFollowUp =
            false;

        frontStepResponseTimer =
            frontStepResponseWindow;

        CombatLogger.Instance?
            .BeginBossBehavior(
                BossBehaviorType.FrontStep,
                distance
            );

        frontStepLogPending = true;

        bossAction.ExecuteFrontStep();

        frontStepCooldownTimer =
            frontStepCooldown;

        ChangeState(
            BossState.FrontStep
        );

        return true;
    }

    // =====================================
    // Stun / Parry
    // =====================================

    public void EnterCounterStun(
        float duration)
    {
        bossParry?
            .ForceCancelParry();

        // 강제 중단된 공격은 정상 Recovery 종료가 아니므로
        // Post-Punish sample을 열지 않는다.
        CombatLogger.Instance?
            .EndBossAttack(
                movement.DistanceToTarget,
                false
            );

        stunTimer =
            duration;

        ChangeState(
            BossState.Stunned
        );
    }

    private bool TryParry()
    {
        if (bossParry == null)
            return false;

        if (bossParry.IsParrying)
            return false;

        if (parryCooldownTimer > 0f)
            return false;

        float currentChance =
            parryChance;

        if (adaptiveSource != null)
        {
            currentChance =
                adaptiveSource
                    .GetParryChance(
                        parryChance
                    );
        }

        if (Random.value >
            currentChance)
        {
            return false;
        }

        StartParry();

        return true;
    }

    private void StartParry()
    {
        movement.Stop();
        movement.FaceTarget();

        CombatLogger.Instance?
            .BeginBossBehavior(
                BossBehaviorType.Parry,
                movement.DistanceToTarget
            );

        ChangeState(
            BossState.Parry
        );

        parryCooldownTimer =
            parryCooldown;

        bossParry.StartParry();
    }

    // =====================================
    // Precision Stun
    // =====================================

    public void EnterPrecisionStun()
    {
        if (currentState ==
            BossState.Dead)
        {
            return;
        }

        if (precisionStunPhase !=
            PrecisionStunPhase.None)
        {
            return;
        }

        if (bossAction != null &&
            bossAction.IsAttacking)
        {
            CombatLogger.Instance?
                .EndBossAttack(
                    movement.DistanceToTarget,
                    false
                );
        }

        bossAction?
            .ForceCancelAttack();

        // Precision Stun Update는 early return하므로
        // 이전 공격 상태가 복구 후까지 남지 않게 직접 정리한다.
        wasAttacking = false;

        bossParry?
            .ForceCancelParry();

        movement?
            .Stop();

        stunTimer = 0f;

        precisionStunTimer =
            precisionStunDuration;

        precisionStunHitCount = 0;

        precisionStunPhase =
            PrecisionStunPhase.Stunned;

        ChangeState(
            BossState.Stunned
        );

        bossAnimator?
            .PlayPrecisionStunIn();

        Debug.Log(
            "[Boss Precision Stun] Start | 0/" +
            precisionStunHitLimit
        );
    }

    public void RegisterPrecisionStunHit()
    {
        if (precisionStunPhase !=
            PrecisionStunPhase.Stunned)
        {
            return;
        }

        precisionStunHitCount++;

        Debug.Log(
            $"[Boss Precision Stun] Hit | " +
            $"{precisionStunHitCount}/" +
            $"{precisionStunHitLimit}"
        );

        if (precisionStunHitCount >=
            precisionStunHitLimit)
        {
            BeginPrecisionStunRecovery();
        }
    }

    private void BeginPrecisionStunRecovery()
    {
        if (precisionStunPhase !=
            PrecisionStunPhase.Stunned)
        {
            return;
        }

        precisionStunPhase =
            PrecisionStunPhase.Recovering;

        precisionStunTimer = 0f;

        bossAnimator?
            .PlayPrecisionStunOut();

        Debug.Log(
            "[Boss Precision Stun] Recover"
        );
    }

    public void OnPrecisionStunRecoverEnd()
    {
        if (precisionStunPhase !=
            PrecisionStunPhase.Recovering)
        {
            return;
        }

        precisionStunPhase =
            PrecisionStunPhase.None;

        precisionStunHitCount = 0;
        precisionStunTimer = 0f;

        ChangeState(
            BossState.Idle
        );

        Debug.Log(
            "[Boss Precision Stun] End"
        );
    }

    public void SetDead()
    {
        bossParry?
            .ForceCancelParry();

        ChangeState(
            BossState.Dead
        );

        movement.Stop();

        precisionStunPhase =
            PrecisionStunPhase.None;

        precisionStunTimer = 0f;
        precisionStunHitCount = 0;

        enabled = false;
    }

    public void StartBattle()
    {
        if (battleStarted)
            return;

        battleStarted = true;

        ChangeState(
            BossState.Idle
        );
    }
}
