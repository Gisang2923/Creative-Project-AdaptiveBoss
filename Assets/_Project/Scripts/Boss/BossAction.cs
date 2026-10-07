using System.Collections;
using UnityEngine;

public class BossAction : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [Header("Heavy Slash")]

    // 빤짝 Prepare를 보여주는 고정 시간
    [SerializeField] private float heavyPrepareCueTime = 0.2f;

    // 찌르기 직전 자세에서 기본적으로 기다리는 시간
    [SerializeField] private float heavyBaseHoldTime = 0.3f;

    // HeavyAttack 시작 후
    // '찌르기 직전 자세'까지 도달하는 시간
    [SerializeField] private float heavyPreHoldTime = 0.18f;

    private float nextHeavyHoldTime = -1f;

    public float HeavyPrepareCueTime =>
        heavyPrepareCueTime;

    public float HeavyBaseHoldTime =>
        heavyBaseHoldTime;

    public void SetNextHeavyHoldTime(float holdTime)
    {
        nextHeavyHoldTime =
            Mathf.Max(0f, holdTime);
    }
    
    [Header("Charge Slash")]
    [SerializeField] private float chargePostPassDelay = 0.15f;
    [SerializeField] private float dashSpeed = 12f;
    private float nextChargePostPassDelay = -1f;

    public float ChargePostPassDelay =>
        chargePostPassDelay;

    public void SetNextChargePostPassDelay(
        float delay)
    {
        nextChargePostPassDelay =
            Mathf.Max(0f, delay);
    }
    // 공격을 결정한 순간 플레이어 위치 기준,
    // 플레이어 뒤로 얼마나 넘어갈지
    [SerializeField] private float dashBehindDistance = 1.5f;

    // 벽 등에 막혔을 때 무한 Dash 방지
    [SerializeField] private float maxDashDuration = 0.6f;
    [SerializeField] private Transform player;
    [SerializeField] private Transform visual;
    [SerializeField] private Collider2D bodyCollider;

    [SerializeField] private float jumpSlamVanishTime = 0.35f;

    [SerializeField] private float jumpSlamHoverTime = 0.4f;

    [SerializeField] private float jumpSlamSpawnHeight = 8f;

    [SerializeField]
    private float jumpSlamFallSpeed = 30f;

    [SerializeField]
    private float jumpSlamSpawnSideOffset = 4.0f;

    [SerializeField]
    private float jumpSlamLandingHitDelay = 0.03f;
    [SerializeField] private float jumpSlamGroundY = 0f;
    [SerializeField] private float jumpLandingOffset = 0.6f;
    [Header("Jump Slam Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 10f;
    [SerializeField] private float groundOffset = 0.7f;
    [SerializeField] private float jumpSlamAttackLeadTime = 0.12f;
    [Header("Back Dodge")]
    [SerializeField] private float backDodgeDistance = 5f;
    [SerializeField] private float backDodgeDuration = 0.6f;
    [SerializeField] private BossAnimator bossAnimator;
    private bool isDodging;

    public bool IsDodging => isDodging;

    [Header("Front Step")]
    [SerializeField] private float frontStepDistance = 2f;
    [SerializeField] private float frontStepDuration = 0.2f;
    [SerializeField] private float frontStepStopDistance = 0.8f;

    private bool isFrontStepping;

    public bool IsFrontStepping => isFrontStepping;
    
    public bool IsBusy =>
        isAttacking ||
        isDodging ||
        isFrontStepping;
    private bool isAttacking;
    private Hitbox currentHitbox;

    [SerializeField] private LayerMask playerLayer;
    public bool IsAttacking => isAttacking;
    [SerializeField] private BossMovement movement;
    private float defaultGravityScale;
    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb != null)
            defaultGravityScale = rb.gravityScale;
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
                StartCoroutine(HeavySlashRoutine(attack));
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
    private IEnumerator HeavySlashRoutine(BossAttack attack)
    {
        isAttacking = true;

        AttackData data = attack.attackData;
        Hitbox hitbox = attack.hitbox;

        currentHitbox = hitbox;

        // =====================================
        // 1. Heavy 예고
        // 검을 들고 빤짝하는 고정 모션
        // =====================================

        bossAnimator?.PlayHeavyPrepare();

        yield return new WaitForSeconds(
            heavyPrepareCueTime
        );


        // =====================================
        // 2. 실제 HeavyAttack 시작
        // =====================================

        bossAnimator?.PlayHeavyAttack();


        // 찌르기 직전 자세까지 이동
        yield return new WaitForSeconds(
            heavyPreHoldTime
        );


        // =====================================
        // 3. 찌르기 직전 자세에서 Adaptive Hold
        // =====================================

        float holdTime =
            nextHeavyHoldTime >= 0f
                ? nextHeavyHoldTime
                : heavyBaseHoldTime;

        nextHeavyHoldTime = -1f;


        // 현재 공격 자세에서 애니메이션 정지
        bossAnimator?.SetPlaybackSpeed(0f);

        yield return new WaitForSeconds(
            holdTime
        );

        // 다시 재생
        bossAnimator?.SetPlaybackSpeed(1f);


        // =====================================
        // 4. Hold 이후 실제 타격까지 재생
        //
        // startupTime = HeavyAttack 시작부터
        // Hit까지 전체 시간
        // =====================================

        float releaseTime =
            Mathf.Max(
                0f,
                data.startupTime -
                heavyPreHoldTime
            );

        yield return new WaitForSeconds(
            releaseTime
        );


        // =====================================
        // 5. Active
        // =====================================

        hitbox.Activate(data);

        yield return new WaitForSeconds(
            data.activeTime
        );

        hitbox.Deactivate();


        // =====================================
        // 6. Recovery
        // =====================================

        yield return new WaitForSeconds(
            data.recoveryTime
        );

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
        isFrontStepping = false;
    }
    private IEnumerator ChargeSlashRoutine(BossAttack attack)
    {
        isAttacking = true;

        AttackData data = attack.attackData;
        Hitbox hitbox = attack.hitbox;

        currentHitbox = hitbox;
        float postPassDelay =
            nextChargePostPassDelay >= 0f
                ? nextChargePostPassDelay
                : chargePostPassDelay;

        // 이번 Charge에만 적용
        nextChargePostPassDelay = -1f;
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
            hitbox.GetWorldCenter();

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
            hitbox.GetWorldCenter();
        CombatLogger.Instance?.MarkChargePassEnd();
        // ==========================================
        // 3. 제자리에서 DashAttack 모션
        // ==========================================
        yield return new WaitForSeconds(postPassDelay);
        bossAnimator?.PlayDashAttack();

        // 실제 검을 휘두르는 프레임까지 기다림
        yield return new WaitForSeconds(
            data.startupTime
        );

        // ==========================================
        // 4. 일섬 판정
        // 지금 이 순간 대시 궤적 전체 공격
        // ==========================================

        hitbox.Activate(data);

        hitbox.SweepFromTo(
            dashStartCenter,
            dashEndCenter
        );

        // 검이 실제로 베고 있는 짧은 시간
        yield return new WaitForSeconds(
            data.activeTime
        );

        hitbox.Deactivate();

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

        float jumpUpSpeed = 40f;
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
        // 왼쪽 / 오른쪽 상공 랜덤 등장
        // ==========================================

        // -1 = 플레이어 왼쪽에서 등장
        //  1 = 플레이어 오른쪽에서 등장
        float spawnSide =
            Random.value < 0.5f ? -1f : 1f;

        // 등장한 쪽 반대 방향으로 내려찍음
        float slamDirection =
            -spawnSide;


        // ==========================================
        // 착지 목표
        // 플레이어를 살짝 지나가는 위치
        // ==========================================

        float landingX =
            player.position.x -
            slamDirection * jumpLandingOffset;


        // ==========================================
        // 등장 위치
        // 반드시 플레이어의 왼쪽 / 오른쪽으로 분리
        // ==========================================

        float spawnX =
            player.position.x +
            spawnSide * jumpSlamSpawnSideOffset;


        // ==========================================
        // 빠른 낙하에서도 정확히 landingX까지
        // 도달하도록 수평 속도를 역산
        // ==========================================

        float fallDistance =
            Mathf.Max(
                0.1f,
                jumpSlamSpawnHeight - groundOffset
            );

        float estimatedFallTime =
            fallDistance /
            jumpSlamFallSpeed;

        float horizontalDistance =
            landingX - spawnX;

        float slamHorizontalVelocity =
            horizontalDistance /
            estimatedFallTime;

        transform.position = new Vector3(
            spawnX,
            jumpSlamGroundY + jumpSlamSpawnHeight,
            transform.position.z
        );
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        // ==========================================
        // 4. 재등장 + 회피 경고 시간
        // ==========================================

        if (visual != null)
            visual.gameObject.SetActive(true);

        movement.FaceTarget();

        rb.linearVelocity = Vector2.zero;
        bossAnimator?.PlayJumpHover();
        // 이 시점에서 이미 landingX / 궤적은 확정되어 있음.
        // 플레이어는 이 시간 안에 위험 지점에서 벗어나야 한다.
        yield return new WaitForSeconds(
            jumpSlamHoverTime
        );

        // ==========================================
        // 5. Commit 아래로 낙하
        // 여기부터는 플레이어 위치를 다시 추적하지 않음
        // ==========================================
        bool jumpAttackStarted = false;
        while (true)
        {
            rb.linearVelocity = new Vector2(
                slamHorizontalVelocity,
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

                // =====================================
                // 착지 직전에 공격 모션 선행 재생
                // =====================================

                float remainingFallDistance =
                    Mathf.Max(
                        0f,
                        distanceToGround - groundOffset
                    );

                float timeToGround =
                    remainingFallDistance /
                    jumpSlamFallSpeed;

                if (!jumpAttackStarted &&
                    timeToGround <=
                        jumpSlamAttackLeadTime)
                {
                    bossAnimator?.PlayJumpAttack();

                    jumpAttackStarted = true;
                }


                // =====================================
                // 착지
                // =====================================

                if (distanceToGround <=
                    groundOffset)
                {
                    rb.linearVelocity =
                        Vector2.zero;

                    transform.position =
                        new Vector3(
                            transform.position.x,
                            hit.point.y +
                                groundOffset,
                            transform.position.z
                        );

                    break;
                }
            }

            yield return new WaitForFixedUpdate();
        }
        rb.gravityScale = defaultGravityScale;

        // 6. 착지 후 몸 충돌 복구
        if (bodyCollider != null)
            bodyCollider.enabled = true;

        // ==========================================
        // 7. 착지 공격 판정
        // 회피 판단은 공중 Hover 동안 끝났으므로
        // 착지 후 추가 회피시간은 거의 주지 않음
        // ==========================================

        yield return new WaitForSeconds(
            jumpSlamLandingHitDelay
        );

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
    public void ExecuteFrontStep()
    {
        if (IsBusy)
            return;

        StartCoroutine(FrontStepRoutine());
    }
    private IEnumerator FrontStepRoutine()
    {
        if (player == null)
            yield break;

        isFrontStepping = true;

        movement?.FaceTarget();
        bossAnimator?.PlayFrontStep();

        float deltaX =
            player.position.x -
            transform.position.x;

        float currentDistance =
            Mathf.Abs(deltaX);

        // 이미 충분히 가까우면 FrontStep하지 않음
        if (currentDistance <= frontStepStopDistance)
        {
            isFrontStepping = false;
            yield break;
        }

        float direction =
            Mathf.Sign(deltaX);

        // 플레이어를 뚫고 지나가지 않도록
        float moveDistance =
            Mathf.Min(
                frontStepDistance,
                currentDistance -
                frontStepStopDistance
            );

        float speed =
            moveDistance /
            frontStepDuration;

        float movedDistance = 0f;

        while (movedDistance < moveDistance)
        {
            float moveThisFrame =
                speed * Time.fixedDeltaTime;

            moveThisFrame =
                Mathf.Min(
                    moveThisFrame,
                    moveDistance - movedDistance
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
            new Vector2(
                0f,
                rb.linearVelocity.y
            );

        isFrontStepping = false;
    }
    private void RestoreBossState()
    {
        bossAnimator?.SetPlaybackSpeed(1f);
        if (visual != null)
            visual.gameObject.SetActive(true);

        if (bodyCollider != null)
            bodyCollider.enabled = true;

        if (rb != null)
            rb.gravityScale = defaultGravityScale;
    }
}