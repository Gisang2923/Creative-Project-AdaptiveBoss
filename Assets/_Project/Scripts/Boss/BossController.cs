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

    [Header("References")]
    [SerializeField] private BossMovement movement;
    [SerializeField] private BossAction bossAction;
    [SerializeField] private BossAttackSelector attackSelector;
    [SerializeField] private BossAdaptiveDecisionSource adaptiveSource;
    [Header("Combat")]
    
    [SerializeField] private BossDamageReceiver damageReceiver;

    [Header("Reposition")]
    [SerializeField] private float repositionDuration = 0.4f;

    private BossPostAction currentPostAction;
    private BossAttack lastExecutedAttack;
    private float repositionTimer;
    private BossState currentState = BossState.Idle;

    private bool wasAttacking;

    [Header("Back Dodge")]
    [SerializeField] private float backDodgeTriggerDistance = 1.5f;
    [SerializeField] private float backDodgeCooldown = 3f;
    private bool wasDodging;
    private bool backDodgeLogPending;
    [SerializeField] private float backDodgeResponseWindow = 0.6f;

    private float backDodgeResponseTimer;
    [SerializeField] private float backDodgeRecoveryTime = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float backDodgeChance = 0.35f;

    private float backDodgeCooldownTimer;

    private float stunTimer;

    [Header("Front Step")]
    [SerializeField] private float frontStepTriggerDistance = 4f;

    [SerializeField, Range(0f, 1f)]
    private float frontStepChance = 0.25f;

    [SerializeField] private float frontStepCooldown = 3f;

    private float frontStepCooldownTimer;
    [SerializeField] private float frontStepRecoveryTime = 0.3f;

    private bool wasFrontStepping;
    private bool frontStepLogPending;
    [SerializeField] private float frontStepResponseWindow = 0.3f;

    private float frontStepResponseTimer;
    private bool currentFrontStepIsAdaptiveFollowUp;
    [Header("Parry")]
    [SerializeField] private BossParry bossParry;
    [SerializeField, Range(0f, 1f)]
    private float parryChance = 0.15f;

    [SerializeField] private float parryCooldown = 4f;

    private float parryCooldownTimer;
    private bool wasParrying;

    public BossState CurrentState => currentState;

    private bool battleStarted = false;

    public bool BattleStarted => battleStarted;
    private void Update()
    {
        if (!battleStarted)
            return;
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
        }
        if (backDodgeCooldownTimer > 0f)
            backDodgeCooldownTimer -= Time.deltaTime;
        if (frontStepCooldownTimer > 0f)
        {
            frontStepCooldownTimer -= Time.deltaTime;
        }
        if (repositionTimer > 0f)
            repositionTimer -= Time.deltaTime;

        // FrontStep 반응 관찰 시간
        if (frontStepLogPending &&
            frontStepResponseTimer > 0f)
        {
            frontStepResponseTimer -= Time.deltaTime;
        }

        // BackDodge 반응 관찰 시간
        if (backDodgeLogPending &&
            backDodgeResponseTimer > 0f)
        {
            backDodgeResponseTimer -= Time.deltaTime;
        }
        if (frontStepLogPending &&
            !bossAction.IsFrontStepping &&
            repositionTimer <= 0f &&
            frontStepResponseTimer <= 0f)
        {
            CombatLogger.Instance?.EndBossBehavior(
                movement.DistanceToTarget
            );

            frontStepLogPending = false;

            // 일반 FrontStep에서만
            // Retreat 적응 기회를 새로 생성
            if (!currentFrontStepIsAdaptiveFollowUp)
            {
                adaptiveSource?
                    .OpenFollowUpOpportunity(
                        BossBehaviorType.FrontStep
                    );
            }

            currentFrontStepIsAdaptiveFollowUp = false;
        }
        if (backDodgeLogPending &&
            !bossAction.IsDodging &&
            repositionTimer <= 0f &&
            backDodgeResponseTimer <= 0f)
        {
            CombatLogger.Instance?.EndBossBehavior(
                movement.DistanceToTarget
            );

            backDodgeLogPending = false;

            adaptiveSource?.OpenFollowUpOpportunity(
                BossBehaviorType.BackDodge
            );
        }
        if (parryCooldownTimer > 0f)
        {
            parryCooldownTimer -= Time.deltaTime;
        }
        if (wasAttacking &&
            !bossAction.IsAttacking &&
            stunTimer <= 0f)
        {
            CombatLogger.Instance?.EndBossAttack(
                movement.DistanceToTarget
            );

            StartPostAction();
        }
        if (wasParrying &&
            (bossParry == null || !bossParry.IsParrying) &&
            stunTimer <= 0f)
        {
            CombatLogger.Instance?.EndBossBehavior(
                movement.DistanceToTarget
            );

            ChangeState(BossState.Idle);
        }
        if (wasDodging && !bossAction.IsDodging)
        {
            StartBackDodgeRecovery();
        }
        if (wasFrontStepping &&
            !bossAction.IsFrontStepping)
        {
            StartFrontStepRecovery();
        }
        wasAttacking = bossAction.IsAttacking;
        wasDodging = bossAction.IsDodging;
        wasFrontStepping = bossAction.IsFrontStepping;
        wasParrying = bossParry != null && bossParry.IsParrying;
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
        float distance = movement.DistanceToTarget;
        if (stunTimer > 0f)
        {
            ChangeState(BossState.Stunned);
            return;
        }
        if (bossParry != null && bossParry.IsParrying)
        {
            ChangeState(BossState.Parry);
            return;
        }
        if (bossAction.IsBusy)
        {
            if (bossAction.IsDodging)
            {
                ChangeState(BossState.BackDodge);
            }
            else if (bossAction.IsFrontStepping)
            {
                ChangeState(BossState.FrontStep);
            }
            else
            {
                ChangeState(BossState.Attack);
            }

            return;
        }

        if (repositionTimer > 0f)
        {
            if (currentPostAction == BossPostAction.Hold)
                ChangeState(BossState.Idle);
            else
                ChangeState(BossState.Reposition);

            return;
        }

        if (TryCounterBaitHold())
            return;

        if (TryAdaptiveFrontStep(distance))
            return;

        if (TryBackDodge(distance))
            return;

        if (TryParry())
            return;

        if (TryFrontStep(distance))
            return;

        if (attackSelector.HasValidAttack(distance))
        {
            StartAttack(distance);
            return;
        }
        
        ChangeState(BossState.Approach);
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
                // 공격 중 이동은 BossAction 담당
                break;
            case BossState.BackDodge:
                // 이동은 BossAction이 직접 제어
                break;
            case BossState.FrontStep:
                // 이동은 BossAction의 FrontStepRoutine이 담당
                break;    
            case BossState.Stunned:
                break;
            case BossState.Dead:
                movement.Stop();
                break;    
        }
    }

    private void StartAttack(float distance)
    {
        movement.Stop();
        movement.FaceTarget();

        BossAttack selectedAttack =
            attackSelector.SelectAttack(distance);

        if (selectedAttack == null)
            return;

        lastExecutedAttack = selectedAttack;

        CombatLogger.Instance?.BeginBossAttack(
            selectedAttack.attackType,
            distance
        );

        bossAction.ExecuteAttack(selectedAttack);

        adaptiveSource?
            .ConsumeFollowUpOpportunity(
                selectedAttack.attackType.ToString());

        ChangeState(BossState.Attack);
    }

    private void ChangeState(BossState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

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
            attackSelector.SelectPostAction(
                lastExecutedAttack,
                distance
            );

        repositionTimer =
            repositionDuration;

    }
    private bool TryBackDodge(float distance)
    {
        if (distance > backDodgeTriggerDistance)
            return false;

        if (backDodgeCooldownTimer > 0f)
            return false;

        float currentChance =
            backDodgeChance;

        if (adaptiveSource != null)
        {
            currentChance =
                adaptiveSource.GetBackDodgeChance(
                    backDodgeChance
                );
        }

        if (Random.value > currentChance)
            return false;

        movement.Stop();
        movement.FaceTarget();
        
        backDodgeResponseTimer =
            backDodgeResponseWindow;

        // BackDodge 시작과 동시에 로그 시작
        CombatLogger.Instance?.BeginBossBehavior(
            BossBehaviorType.BackDodge,
            distance
        );

        backDodgeLogPending = true;

        bossAction.ExecuteBackDodge();
        adaptiveSource?
            .ConsumeFollowUpOpportunity("BackDodge");
        backDodgeCooldownTimer = backDodgeCooldown;

        ChangeState(BossState.BackDodge);

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

        if (currentState == BossState.Dead)
            return;

        bossAction.ForceCancelAttack();

        movement.Stop();
        movement.FaceTarget();

        bossAction.ExecuteBackDodge();

        backDodgeCooldownTimer = backDodgeCooldown;

        ChangeState(BossState.BackDodge);
    }
    private bool TryFrontStep(float distance)
    {
        if (frontStepCooldownTimer > 0f)
            return false;

        // 가까운데 굳이 접근하지 않음
        if (distance <= 1.5f)
            return false;

        // 너무 멀면 기존 Approach 사용
        if (distance > frontStepTriggerDistance)
            return false;

        float currentChance = frontStepChance;

        if (Random.value > currentChance)
            return false;

        movement.Stop();
        movement.FaceTarget();

        currentFrontStepIsAdaptiveFollowUp = false;

        frontStepResponseTimer =
            frontStepResponseWindow;

        // FrontStep 시작과 동시에 로그 시작
        CombatLogger.Instance?.BeginBossBehavior(
            BossBehaviorType.FrontStep,
            distance
        );

        frontStepLogPending = true;

        bossAction.ExecuteFrontStep();
        
        adaptiveSource?
            .ConsumeFollowUpOpportunity("FrontStep");

        frontStepCooldownTimer =
            frontStepCooldown;

        ChangeState(
            BossState.FrontStep
        );

        return true;
    }
    private bool TryAdaptiveFrontStep(
        float distance)
    {
        if (adaptiveSource == null)
            return false;

        // 너무 가까우면 추가 접근하지 않음
        if (distance <= 1.5f)
            return false;

        // FrontStep이 의미 있는 거리에서만
        if (distance > frontStepTriggerDistance)
            return false;

        if (!adaptiveSource
                .ShouldUseRetreatPressureFrontStep())
        {
            return false;
        }

        movement.Stop();
        movement.FaceTarget();

        currentFrontStepIsAdaptiveFollowUp = true;

        frontStepResponseTimer =
            frontStepResponseWindow;

        CombatLogger.Instance?.BeginBossBehavior(
            BossBehaviorType.FrontStep,
            distance
        );

        frontStepLogPending = true;

        bossAction.ExecuteFrontStep();

        // 일반 FrontStep 쿨다운도 다시 갱신
        frontStepCooldownTimer =
            frontStepCooldown;

        adaptiveSource
            .ConsumeFollowUpOpportunity("FrontStep");

        ChangeState(
            BossState.FrontStep
        );

        Debug.Log(
            "[Retreat Pressure] " +
            "Execute Adaptive FrontStep"
        );

        return true;
    }
    private bool TryCounterBaitHold()
    {
        if (adaptiveSource == null)
            return false;

        if (!adaptiveSource
                .ShouldUseCounterBaitHold())
        {
            return false;
        }

        movement.Stop();
        movement.FaceTarget();

        currentPostAction =
            BossPostAction.Hold;

        repositionTimer =
            adaptiveSource
                .GetCounterBaitHoldDuration();

        ChangeState(BossState.Idle);

        Debug.Log(
            "[Counter Bait] " +
            "Intentional Hold"
        );

        return true;
    }
    public void EnterCounterStun(float duration)
    {
        bossParry?.ForceCancelParry();

        stunTimer = duration;

        ChangeState(BossState.Stunned);
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
                adaptiveSource.GetParryChance(
                    parryChance
                );
        }

        if (Random.value > currentChance)
            return false;

        StartParry();

        adaptiveSource?
            .ConsumeFollowUpOpportunity("Parry");

        return true;
    }
    private void StartParry()
    {
        movement.Stop();
        movement.FaceTarget();

        CombatLogger.Instance?.BeginBossBehavior(
            BossBehaviorType.Parry,
            movement.DistanceToTarget
        );
        ChangeState(BossState.Parry);

        parryCooldownTimer = parryCooldown;

        bossParry.StartParry();
    }
    public void SetDead()
    {
        bossParry?.ForceCancelParry();

        ChangeState(BossState.Dead);

        movement.Stop();

        enabled = false;
    }
    public void StartBattle()
    {
        if (battleStarted)
            return;

        battleStarted = true;

        ChangeState(BossState.Idle);
    }
}