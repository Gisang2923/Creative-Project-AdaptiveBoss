using UnityEngine;

public class BossController : MonoBehaviour
{
    public enum BossState
    {
        Idle,
        Approach,
        Reposition,
        Attack,
        BackDodge,
        Dead
    }

    [Header("References")]
    [SerializeField] private BossMovement movement;
    [SerializeField] private BossAction bossAction;
    [SerializeField] private BossAttackSelector attackSelector;

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
    [SerializeField, Range(0f, 1f)]
    private bool wasDodging;

    [SerializeField] private float backDodgeRecoveryTime = 0.5f;
    private float backDodgeChance = 0.35f;

    private float backDodgeCooldownTimer;

    public BossState CurrentState => currentState;

    private void Update()
    {
        if (backDodgeCooldownTimer > 0f)
            backDodgeCooldownTimer -= Time.deltaTime;

        if (repositionTimer > 0f)
            repositionTimer -= Time.deltaTime;

        if (wasAttacking && !bossAction.IsAttacking)
        {
            StartPostAction();
        }

        if (wasDodging && !bossAction.IsDodging)
        {
            StartBackDodgeRecovery();
        }

        wasAttacking = bossAction.IsAttacking;
        wasDodging = bossAction.IsDodging;

        UpdateState();
    }

    private void FixedUpdate()
    {
        ExecuteMovement();
    }

    private void UpdateState()
    {
        float distance = movement.DistanceToTarget;

        if (bossAction.IsBusy)
        {
            if (bossAction.IsDodging)
                ChangeState(BossState.BackDodge);
            else
                ChangeState(BossState.Attack);

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

        if (TryBackDodge(distance))
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

            case BossState.Attack:
                // 공격 중 이동은 BossAction 담당
                break;
            case BossState.BackDodge:
                // 이동은 BossAction이 직접 제어
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

        bossAction.ExecuteAttack(selectedAttack);

        ChangeState(BossState.Attack);
    }

    private void ChangeState(BossState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        Debug.Log($"Boss State → {currentState}");
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

        Debug.Log(
            $"Post Action → {currentPostAction}"
        );
    }
    private bool TryBackDodge(float distance)
    {
        if (distance > backDodgeTriggerDistance)
            return false;

        if (backDodgeCooldownTimer > 0f)
            return false;

        if (Random.value > backDodgeChance)
            return false;

        bossAction.ExecuteBackDodge();

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

        Debug.Log(
            "BackDodge Recovery → Hold"
        );
    }
    public void SetDead()
    {
        ChangeState(BossState.Dead);

        movement.Stop();

        enabled = false;
    }
}