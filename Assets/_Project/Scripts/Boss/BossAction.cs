using System.Collections;
using UnityEngine;

public class BossAction : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;

    [Header("Charge Slash")]
    [SerializeField] private float chargeSpeed = 8f;
    [SerializeField] private float chargeDuration = 0.35f;
    [SerializeField] private Transform player;
    [SerializeField] private Transform visual;
    [SerializeField] private Collider2D bodyCollider;

    [SerializeField] private float jumpSlamVanishTime = 0.35f;
    [SerializeField] private float jumpSlamSpawnHeight = 5f;
    [SerializeField] private float jumpSlamFallSpeed = 12f;
    [SerializeField] private float jumpSlamGroundY = 0f;

    [Header("Jump Slam Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 10f;
    [SerializeField] private float groundOffset = 0.7f;

    [Header("Back Dodge")]
    [SerializeField] private float backDodgeDistance = 5f;
    [SerializeField] private float backDodgeDuration = 0.6f;

    private bool isDodging;

    public bool IsDodging => isDodging;
    public bool IsBusy => isAttacking || isDodging;
    private bool isAttacking;
    private Hitbox currentHitbox;

    public bool IsAttacking => isAttacking;
    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }
    public void ExecuteAttack(BossAttack attack)
    {
        if (isAttacking || attack == null)
            return;

        switch (attack.attackType)
        {
            case BossAttackType.BasicSlash:
            case BossAttackType.HeavySlash:
                StartCoroutine(SlashRoutine(attack));
                break;

            case BossAttackType.ChargeSlash:
                StartCoroutine(ChargeSlashRoutine(attack));
                break;

            case BossAttackType.JumpSlam:
                StartCoroutine(JumpSlamRoutine(attack));
                break;    
        }
    }

    private IEnumerator SlashRoutine(BossAttack attack)
    {
        isAttacking = true;

        AttackData data = attack.attackData;
        Hitbox hitbox = attack.hitbox;

        currentHitbox = hitbox;

        // Startup
        yield return new WaitForSeconds(data.startupTime);

        // Active
        hitbox.Activate(data);

        yield return new WaitForSeconds(data.activeTime);

        hitbox.Deactivate();

        // Recovery
        yield return new WaitForSeconds(data.recoveryTime);

        currentHitbox = null;
        isAttacking = false;
    }

    public void ForceCancelAttack()
    {
        StopAllCoroutines();

        if (currentHitbox != null)
        {
            currentHitbox.Deactivate();
            currentHitbox = null;
        }

        rb.linearVelocity =
            new Vector2(0f, rb.linearVelocity.y);

        isAttacking = false;
        isDodging = false;
    }
    private IEnumerator ChargeSlashRoutine(BossAttack attack)
    {
        isAttacking = true;

        AttackData data = attack.attackData;
        Hitbox hitbox = attack.hitbox;

        currentHitbox = hitbox;

        // Startup
        yield return new WaitForSeconds(data.startupTime);

        float direction =
            Mathf.Sign(
                player.position.x - transform.position.x
            );

        // Active
        hitbox.Activate(data);

        float timer = 0f;

        while (timer < chargeDuration)
        {
            rb.linearVelocity =
                new Vector2(
                    direction * chargeSpeed,
                    rb.linearVelocity.y
                );

            timer += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity =
            new Vector2(0f, rb.linearVelocity.y);

        hitbox.Deactivate();
        currentHitbox = null;

        // Recovery
        yield return new WaitForSeconds(data.recoveryTime);

        isAttacking = false;
    }
    private IEnumerator JumpSlamRoutine(BossAttack attack)
    {
        isAttacking = true;

        AttackData data = attack.attackData;
        Hitbox hitbox = attack.hitbox;

        currentHitbox = hitbox;

        // 1. 준비 동작
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(data.startupTime);

        // 2. 사라짐
        if (visual != null)
            visual.gameObject.SetActive(false);

        if (bodyCollider != null)
            bodyCollider.enabled = false;

        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(jumpSlamVanishTime);

        // 3. 플레이어 위쪽으로 위치 이동
        float targetX = player.position.x;

        transform.position = new Vector3(
            targetX,
            jumpSlamGroundY + jumpSlamSpawnHeight,
            transform.position.z
        );

        // 4. 재등장
        if (visual != null)
            visual.gameObject.SetActive(true);

        // 5. 아래로 낙하
        while (true)
        {
            rb.linearVelocity = new Vector2(
                0f,
                -jumpSlamFallSpeed
            );

            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                Vector2.down,
                groundCheckDistance,
                groundLayer
            );

            if (hit.collider != null)
            {
                float distanceToGround =
                    transform.position.y - hit.point.y;

                // 거의 착지한 상태
                if (distanceToGround <= groundOffset)
                {
                    rb.linearVelocity = Vector2.zero;

                    transform.position = new Vector3(
                        transform.position.x,
                        hit.point.y + groundOffset,
                        transform.position.z
                    );

                    break;
                }
            }

            yield return new WaitForFixedUpdate();
        }

        if (bodyCollider != null)
            bodyCollider.enabled = true;

        // 7. 착지 공격 판정
        hitbox.Activate(data);

        yield return new WaitForSeconds(
            data.activeTime
        );

        hitbox.Deactivate();
        currentHitbox = null;

        // 8. 후딜
        yield return new WaitForSeconds(
            data.recoveryTime
        );

        isAttacking = false;
    }

    public void ExecuteBackDodge()
    {
        if (IsBusy)
            return;

        StartCoroutine(BackDodgeRoutine());
    }

    private IEnumerator BackDodgeRoutine()
    {
        isDodging = true;

        // 시작 순간 플레이어 반대 방향 고정
        float direction =
            Mathf.Sign(transform.position.x - player.position.x);

        float speed =
            backDodgeDistance / backDodgeDuration;

        float movedDistance = 0f;

        while (movedDistance < backDodgeDistance)
        {
            float moveThisFrame =
                speed * Time.fixedDeltaTime;

            // 목표 거리 초과 방지
            moveThisFrame = Mathf.Min(
                moveThisFrame,
                backDodgeDistance - movedDistance
            );

            rb.linearVelocity =
                new Vector2(
                    direction * speed,
                    rb.linearVelocity.y
                );

            movedDistance += moveThisFrame;

            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity =
            new Vector2(0f, rb.linearVelocity.y);

        isDodging = false;
    }
}