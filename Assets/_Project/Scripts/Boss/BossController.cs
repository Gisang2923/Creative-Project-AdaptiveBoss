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
    [SerializeField] private float attackCooldown = 0.6f;
    
    [SerializeField] private BossDamageReceiver damageReceiver;
    private enum RepositionType
    {
        Hold,
        Approach,
        Retreat
    }

    [Header("Reposition")]
    [SerializeField] private float repositionDuration = 0.4f;

    [SerializeField, Range(0f, 1f)]
    private float retreatChance = 0.4f;

    [SerializeField, Range(0f, 1f)]
    private float approachChance = 0.25f;

    private RepositionType currentReposition;
    private float repositionTimer;
    private BossState currentState = BossState.Idle;
    private float attackCooldownTimer;
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
            StartReposition();
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
            if (currentReposition == RepositionType.Hold)
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

                switch (currentReposition)
                {
                    case RepositionType.Approach:
                        movement.MoveTowardTarget();
                        break;

                    case RepositionType.Retreat:
                        movement.MoveAwayFromTarget();
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
    private void StartReposition()
    {
        repositionTimer = repositionDuration;

        float random = Random.value;

        if (random < retreatChance)
        {
            currentReposition = RepositionType.Retreat;
        }
        else if (random < retreatChance + approachChance)
        {
            currentReposition = RepositionType.Approach;
        }
        else
        {
            currentReposition = RepositionType.Hold;
        }

        Debug.Log($"Reposition → {currentReposition}");
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
        repositionTimer = backDodgeRecoveryTime;
        currentReposition = RepositionType.Hold;

        Debug.Log("BackDodge Recovery → Hold");
    }
    public void SetDead()
    {
        ChangeState(BossState.Dead);

        movement.Stop();

        enabled = false;
    }
}