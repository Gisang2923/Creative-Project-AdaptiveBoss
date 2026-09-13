using UnityEngine;

public class BossController : MonoBehaviour
{
    public enum BossState
    {
        Idle,
        Approach,
        Attack
    }

    [Header("References")]
    [SerializeField] private BossMovement movement;
    [SerializeField] private BossAction bossAction;

    [Header("Combat")]
    [SerializeField] private float attackDistance = 1.5f;
    [SerializeField] private float attackCooldown = 0.6f;
    [SerializeField] private BossAttackSelector attackSelector;
    private BossState currentState = BossState.Idle;
    private float attackCooldownTimer;

    public BossState CurrentState => currentState;
    private bool wasAttacking;
    private void Update()
    {
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        if (wasAttacking && !bossAction.IsAttacking)
        {
            attackCooldownTimer = attackCooldown;
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
        float distance = movement.DistanceToTarget;

        // 공격 실행 중
        if (bossAction.IsAttacking)
        {
            ChangeState(BossState.Attack);
            return;
        }

        // 공격 종료 직후 쿨타임
        if (attackCooldownTimer > 0f)
        {
            if (distance > attackDistance)
                ChangeState(BossState.Approach);
            else
                ChangeState(BossState.Idle);

            return;
        }

        // 공격 범위 밖이면 접근
        if (distance > attackDistance)
        {
            ChangeState(BossState.Approach);
            return;
        }

        // 공격 범위 안이면 공격
        StartAttack();
    }

    private void ExecuteMovement()
    {
        switch (currentState)
        {
            case BossState.Approach:
                movement.MoveTowardTarget();
                break;

            case BossState.Idle:
            case BossState.Attack:
                movement.Stop();
                break;
        }
    }

    private void StartAttack()
    {
        movement.Stop();
        movement.FaceTarget();

        AttackData selectedAttack =
            attackSelector.SelectAttack();

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
}