using System.Collections;
using UnityEngine;

public class BossAction : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;

    [Header("Charge Slash")]
    [SerializeField] private float dashSpeed = 12f;

    // 공격을 결정한 순간 플레이어 위치 기준,
    // 플레이어 뒤로 얼마나 넘어갈지
    [SerializeField] private float dashBehindDistance = 1.5f;

    // 벽 등에 막혔을 때 무한 Dash 방지
    [SerializeField] private float maxDashDuration = 0.6f;
    [SerializeField] private Transform player;
    [SerializeField] private Transform visual;
    [SerializeField] private Collider2D bodyCollider;

    [SerializeField] private float jumpSlamVanishTime = 0.35f;
    [SerializeField] private float jumpSlamSpawnHeight = 5f;
    [SerializeField] private float jumpSlamFallSpeed = 12f;
    [SerializeField] private float jumpSlamGroundY = 0f;
    [SerializeField] private float jumpLandingOffset = 0.6f;
    [Header("Jump Slam Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 10f;
    [SerializeField] private float groundOffset = 0.7f;

    [Header("Back Dodge")]
    [SerializeField] private float backDodgeDistance = 5f;
    [SerializeField] private float backDodgeDuration = 0.6f;
    [SerializeField] private BossAnimator bossAnimator;
    private bool isDodging;

    public bool IsDodging => isDodging;
    public bool IsBusy => isAttacking || isDodging;
    private bool isAttacking;
    private Hitbox currentHitbox;

    [SerializeField] private LayerMask playerLayer;
    public bool IsAttacking => isAttacking;
    [SerializeField] private BossMovement movement;
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
                bossAnimator?.PlayLightAttack();
                StartCoroutine(SlashRoutine(attack));
                break;

            case BossAttackType.HeavySlash:
                bossAnimator?.PlayHeavyAttack();
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

        if (rb != null)
        {
            rb.linearVelocity =
                new Vector2(0f, rb.linearVelocity.y);
        }

        RestoreBossState();

        isAttacking = false;
        isDodging = false;
    }
    private IEnumerator ChargeSlashRoutine(BossAttack attack)
    {
        isAttacking = true;
        currentHitbox = attack.hitbox;

        AttackData data = attack.attackData;

        // 공격을 결정한 순간 방향 고정
        float direction =
            Mathf.Sign(player.position.x - transform.position.x);

        // 공격 결정 당시 플레이어 위치
        float playerXAtStart = player.position.x;

        // 플레이어 뒤쪽까지 통과
        float targetX =
            playerXAtStart + direction * dashBehindDistance;

        // ==========================================
        // 1. Dash 시작 위치 저장
        // ==========================================

        Vector2 dashStartCenter =
            currentHitbox.GetWorldCenter();

        bossAnimator?.PlayDash();

        float elapsed = 0f;

        // ==========================================
        // 2. Dash
        // 노데미지
        // ==========================================

        while (
            (targetX - transform.position.x) * direction > 0f &&
            elapsed < maxDashDuration
        )
        {
            rb.linearVelocity =
                new Vector2(
                    direction * dashSpeed,
                    rb.linearVelocity.y
                );

            elapsed += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity =
            new Vector2(0f, rb.linearVelocity.y);

        // 중요:
        // 플레이어를 지나쳤어도 뒤돌지 않는다.

        // Dash 종료 위치
        Vector2 dashEndCenter =
            currentHitbox.GetWorldCenter();

        // ==========================================
        // 3. 제자리에서 DashAttack 모션
        // ==========================================

        bossAnimator?.PlayDashAttack();

        // 실제 검을 휘두르는 프레임까지 기다림
        yield return new WaitForSeconds(
            data.startupTime
        );

        // ==========================================
        // 4. 일섬 판정
        // 지금 이 순간 대시 궤적 전체 공격
        // ==========================================

        currentHitbox.Activate(data);

        currentHitbox.SweepFromTo(
            dashStartCenter,
            dashEndCenter
        );

        // 검이 실제로 베고 있는 짧은 시간
        yield return new WaitForSeconds(
            data.activeTime
        );

        currentHitbox.Deactivate();

        // ==========================================
        // 5. Recovery
        // ==========================================

        yield return new WaitForSeconds(
            data.recoveryTime
        );

        currentHitbox = null;
        isAttacking = false;
    }
    private IEnumerator JumpSlamRoutine(BossAttack attack)
    {
        isAttacking = true;

        AttackData data = attack.attackData;
        Hitbox hitbox = attack.hitbox;

        currentHitbox = hitbox;

        // 1. 점프 준비 + 점프 애니메이션
        // 1. 점프 애니메이션 + 실제 상승
        rb.linearVelocity = Vector2.zero;

        bossAnimator?.PlayJump();

        float jumpUpSpeed = 35f;
        float jumpUpTime = 0.03f;

        float elapsed = 0f;

        while (elapsed < jumpUpTime)
        {
            rb.linearVelocity = new Vector2(
                0f,
                jumpUpSpeed
            );

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = new Vector2(
            0f,
            jumpUpSpeed
        );

        yield return new WaitForSeconds(data.startupTime);

        // 2. 화면 밖으로 사라짐
        if (visual != null)
            visual.gameObject.SetActive(false);

        if (bodyCollider != null)
            bodyCollider.enabled = false;

        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(jumpSlamVanishTime);

        // 3. 플레이어 위쪽으로 위치 이동
        // 3. 플레이어 옆 위치를 착지 지점으로 설정
        float direction =
            Mathf.Sign(player.position.x - transform.position.x);

        float targetX =
            player.position.x - direction * jumpLandingOffset;

        transform.position = new Vector3(
            targetX,
            jumpSlamGroundY + jumpSlamSpawnHeight,
            transform.position.z
        );

        // 4. 재등장
        if (visual != null)
            visual.gameObject.SetActive(true);

        movement.FaceTarget();
        // 재등장하는 순간 JumpAttack 재생
        bossAnimator?.PlayJumpAttack();

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

        // 6. 착지 후 몸 충돌 복구
        if (bodyCollider != null)
            bodyCollider.enabled = true;

        // 7. 착지 공격 판정
        yield return new WaitForSeconds(0.3f);
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
        bossAnimator?.PlayBackDodge();
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
    private void RestoreBossState()
    {
        if (visual != null)
            visual.gameObject.SetActive(true);

        if (bodyCollider != null)
            bodyCollider.enabled = true;
    }
}