using UnityEngine;

public class BossController : MonoBehaviour
{
    public enum BossState
    {
        Idle,
        Approach,
        Reposition,
        Attack,
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

    public BossState CurrentState => currentState;

    private void Update()
    {
        if (repositionTimer > 0f)
            repositionTimer -= Time.deltaTime;

        if (wasAttacking && !bossAction.IsAttacking)
        {
            StartReposition();
        }

        wasAttacking = bossAction.IsAttacking;

        UpdateState();
    }

    private void FixedUpdate()
    {
        ExecuteMovement();
    }

    private void UpdateState()
    {
        if (damageReceiver != null && damageReceiver.IsDead)
        {
            ChangeState(BossState.Dead);
            return;
        }
        float distance = movement.DistanceToTarget;

        if (bossAction.IsAttacking)
        {
            ChangeState(BossState.Attack);
            return;
        }

        // 공격 종료 후 위치 조정
        if (repositionTimer > 0f)
        {
            if (currentReposition == RepositionType.Hold)
                ChangeState(BossState.Idle);
            else
                ChangeState(BossState.Reposition);

            return;
        }

        // 현재 거리에서 공격 가능한지 확인
        if (attackSelector.HasValidAttack(distance))
        {
            StartAttack(distance);
            return;
        }

        // 공격 가능한 거리가 아니면 접근
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
    public void SetDead()
    {
        ChangeState(BossState.Dead);

        movement.Stop();

        enabled = false;
    }
}