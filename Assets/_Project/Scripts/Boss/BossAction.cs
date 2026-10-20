using System.Collections;
using UnityEngine;

public class BossAction : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;

    [Header("Basic Slash")]
    [SerializeField] private float basicPreHoldTime = 0.06f;
    [SerializeField] private float basicPrepareHoldTime = 0.18f;

    // Animation Event가 누락됐을 때 무한 대기를 막는 Safety Timeout
    [SerializeField] private float basicAttack1Duration = 1.10f;
    [SerializeField] private float basicAttack2Duration = 1.05f;
    [SerializeField] private float basicAttack3Duration = 1.40f;

    // 타격 직전 플레이어 쪽으로 살짝 파고드는 움직임
    [SerializeField] private float basicForwardSpeed = 1.5f;
    [SerializeField] private float basicForwardStopDistance = 0.7f;
    [SerializeField] private float basicForwardMaxDistance = 0.40f;
    [SerializeField] private float basicThirdForwardMultiplier = 1.25f;

    // 타격 직후 공격 방향의 반대로 짧게 회수
    [SerializeField] private float basicRecoilDistance = 0.20f;
    [SerializeField] private float basicThirdRecoilMultiplier = 1.35f;
    [SerializeField] private float basicRecoilDuration = 0.10f;

    // 연타 사이 판단 텀
    [SerializeField] private float basicComboGap = 0.08f;

    // 플레이어가 이 거리 밖으로 빠지면 남은 연타를 중단
    [SerializeField] private float basicComboContinueDistance = 2.0f;

    // 3타 이후 최소 회수 시간
    [SerializeField] private float basicFinalRecoveryTime = 0.20f;

    private AttackData currentBasicAttackData;
    private bool basicHitTriggered;
    private bool basicHitTimedOut;

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
        currentBasicAttackData = data;

        // =====================================
        // 1타
        // =====================================
        yield return StartCoroutine(
            ExecuteBasicStrike(
                1,
                data,
                hitbox
            )
        );

        if (basicHitTimedOut)
        {
            FinishBasicCombo();
            yield break;
        }

        if (!CanContinueBasicCombo())
        {
            rb.linearVelocity =
                new Vector2(
                    0f,
                    rb.linearVelocity.y
                );

            yield return new WaitForSeconds(
                basicPrepareHoldTime
            );

            FinishBasicCombo();
            yield break;
        }

        yield return StartCoroutine(
            BasicComboGapRoutine()
        );

        if (!CanContinueBasicCombo())
        {
            FinishBasicCombo();
            yield break;
        }

        // =====================================
        // 2타
        // =====================================
        yield return StartCoroutine(
            ExecuteBasicStrike(
                2,
                data,
                hitbox
            )
        );

        if (basicHitTimedOut)
        {
            FinishBasicCombo();
            yield break;
        }

        if (!CanContinueBasicCombo())
        {
            rb.linearVelocity =
                new Vector2(
                    0f,
                    rb.linearVelocity.y
                );

            yield return new WaitForSeconds(
                basicPrepareHoldTime
            );

            FinishBasicCombo();
            yield break;
        }

        yield return StartCoroutine(
            BasicComboGapRoutine()
        );

        if (!CanContinueBasicCombo())
        {
            FinishBasicCombo();
            yield break;
        }

        // =====================================
        // 3타
        // =====================================
        yield return StartCoroutine(
            ExecuteBasicStrike(
                3,
                data,
                hitbox
            )
        );

        if (basicHitTimedOut)
        {
            FinishBasicCombo();
            yield break;
        }

        rb.linearVelocity =
            new Vector2(
                0f,
                rb.linearVelocity.y
            );

        yield return new WaitForSeconds(
            basicFinalRecoveryTime
        );

        FinishBasicCombo();
    }

    private IEnumerator ExecuteBasicStrike(
        int strikeIndex,
        AttackData data,
        Hitbox hitbox)
    {
        basicHitTriggered = false;
        basicHitTimedOut = false;

        movement?.FaceTarget();

        float deltaX =
            player.position.x -
            transform.position.x;

        float direction =
            Mathf.Sign(deltaX);

        if (direction == 0f)
            direction = 1f;

        float forwardMaxDistance =
            basicForwardMaxDistance;

        float recoilDistance =
            basicRecoilDistance;

        float eventTimeout =
            basicAttack1Duration;

        switch (strikeIndex)
        {
            case 1:
                bossAnimator?
                    .PlayLightAttack1();

                eventTimeout =
                    basicAttack1Duration;

                // 첫 타의 기존 예고 Hold는 유지
                yield return new WaitForSeconds(
                    basicPreHoldTime
                );

                rb.linearVelocity =
                    new Vector2(
                        0f,
                        rb.linearVelocity.y
                    );

                bossAnimator?
                    .SetPlaybackSpeed(0f);

                yield return new WaitForSeconds(
                    basicPrepareHoldTime
                );

                bossAnimator?
                    .SetPlaybackSpeed(1f);
                break;

            case 2:
                bossAnimator?
                    .PlayLightAttack2();

                eventTimeout =
                    basicAttack2Duration;
                break;

            case 3:
                bossAnimator?
                    .PlayLightAttack3();

                eventTimeout =
                    basicAttack3Duration;

                forwardMaxDistance *=
                    basicThirdForwardMultiplier;

                recoilDistance *=
                    basicThirdRecoilMultiplier;
                break;
        }

        // 실제 BasicHit Animation Event가 발생할 때까지
        // 플레이어 쪽으로 짧게 파고든다.
        yield return StartCoroutine(
            WaitForBasicHitWithLunge(
                direction,
                forwardMaxDistance,
                eventTimeout
            )
        );

        if (basicHitTimedOut)
        {
            hitbox?.Deactivate();
            yield break;
        }

        // 타격 순간부터는 앞으로 미는 이동을 끊는다.
        rb.linearVelocity =
            new Vector2(
                0f,
                rb.linearVelocity.y
            );

        yield return new WaitForSeconds(
            data.activeTime
        );

        hitbox.Deactivate();

        // 타격 후 검을 회수하는 느낌으로
        // 공격 방향 반대로 짧게 이동한다.
        yield return StartCoroutine(
            BasicRecoilRoutine(
                -direction,
                recoilDistance,
                basicRecoilDuration
            )
        );
    }

    private IEnumerator WaitForBasicHitWithLunge(
        float direction,
        float maxDistance,
        float timeout)
    {
        float startX =
            transform.position.x;

        float elapsed = 0f;

        while (!basicHitTriggered &&
               elapsed < timeout)
        {
            float movedDistance =
                Mathf.Abs(
                    transform.position.x -
                    startX
                );

            float distanceToPlayer =
                player != null
                    ? Mathf.Abs(
                        player.position.x -
                        transform.position.x
                    )
                    : Mathf.Infinity;

            bool canMoveForward =
                movedDistance <
                    maxDistance &&
                distanceToPlayer >
                    basicForwardStopDistance;

            rb.linearVelocity =
                new Vector2(
                    canMoveForward
                        ? direction *
                          basicForwardSpeed
                        : 0f,
                    rb.linearVelocity.y
                );

            elapsed +=
                Time.deltaTime;

            yield return null;
        }

        rb.linearVelocity =
            new Vector2(
                0f,
                rb.linearVelocity.y
            );

        if (!basicHitTriggered)
        {
            basicHitTimedOut = true;

            Debug.LogWarning(
                "[Boss Basic] " +
                "BasicHit Animation Event timeout."
            );
        }
    }

    private IEnumerator BasicRecoilRoutine(
        float direction,
        float distance,
        float duration)
    {
        if (distance <= 0f ||
            duration <= 0f)
        {
            yield break;
        }

        float speed =
            distance / duration;

        float movedDistance = 0f;

        while (movedDistance < distance)
        {
            float moveThisFrame =
                speed *
                Time.fixedDeltaTime;

            moveThisFrame =
                Mathf.Min(
                    moveThisFrame,
                    distance -
                    movedDistance
                );

            rb.linearVelocity =
                new Vector2(
                    direction * speed,
                    rb.linearVelocity.y
                );

            movedDistance +=
                moveThisFrame;

            yield return
                new WaitForFixedUpdate();
        }

        rb.linearVelocity =
            new Vector2(
                0f,
                rb.linearVelocity.y
            );
    }

    private IEnumerator BasicComboGapRoutine()
    {
        rb.linearVelocity =
            new Vector2(
                0f,
                rb.linearVelocity.y
            );

        float elapsed = 0f;

        while (elapsed <
               basicComboGap)
        {
            if (!CanContinueBasicCombo())
            {
                rb.linearVelocity =
                    new Vector2(
                        0f,
                        rb.linearVelocity.y
                    );

                yield return new WaitForSeconds(
                    basicPrepareHoldTime
                );

                FinishBasicCombo();
                yield break;
            }

            elapsed +=
                Time.deltaTime;

            yield return null;
        }
    }

    private bool CanContinueBasicCombo()
    {
        if (player == null)
            return false;

        float distance =
            Mathf.Abs(
                player.position.x -
                transform.position.x
            );

        return distance <=
            basicComboContinueDistance;
    }

    private void FinishBasicCombo()
    {
        currentHitbox?
            .Deactivate();

        if (rb != null)
        {
            rb.linearVelocity =
                new Vector2(
                    0f,
                    rb.linearVelocity.y
                );
        }

        bossAnimator?
            .SetPlaybackSpeed(1f);

        currentBasicAttackData = null;
        currentHitbox = null;

        basicHitTriggered = false;
        basicHitTimedOut = false;

        isAttacking = false;
    }

    private void TryPerfectDodgeDistanceGrace()
    {
        if (player == null)
            return;

        PlayerDash playerDash =
            player.GetComponent<PlayerDash>();

        playerDash?
            .TryPerfectDodgeByDistance(
                transform
            );
    }

    public void OnBasicHit()
    {
        if (!isAttacking ||
            currentHitbox == null ||
            currentBasicAttackData == null ||
            basicHitTriggered)
        {
            return;
        }

        basicHitTriggered = true;

        currentHitbox.Activate(
            currentBasicAttackData
        );

        TryPerfectDodgeDistanceGrace();
    }

    private IEnumerator HeavySlashRoutine(BossAttack attack)
    {
        isAttacking = true;

        AttackData data = attack.attackData;
        Hitbox hitbox = attack.hitbox;

        currentHitbox = hitbox;

        bossAnimator?.PlayHeavyPrepare();

        yield return new WaitForSeconds(
            heavyPrepareCueTime
        );

        bossAnimator?.PlayHeavyAttack();

        yield return new WaitForSeconds(
            heavyPreHoldTime
        );

        float holdTime =
            nextHeavyHoldTime >= 0f
                ? nextHeavyHoldTime
                : heavyBaseHoldTime;

        nextHeavyHoldTime = -1f;

        bossAnimator?.SetPlaybackSpeed(0f);

        yield return new WaitForSeconds(
            holdTime
        );

        bossAnimator?.SetPlaybackSpeed(1f);

        float releaseTime =
            Mathf.Max(
                0f,
                data.startupTime -
                heavyPreHoldTime
            );

        yield return new WaitForSeconds(
            releaseTime
        );

        hitbox.Activate(data);

        float activeElapsed = 0f;

        while (activeElapsed < data.activeTime)
        {
            TryPerfectDodgeDistanceGrace();

            activeElapsed += Time.deltaTime;
            yield return null;
        }

        hitbox.Deactivate();

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
                new Vector2(
                    0f,
                    rb.linearVelocity.y
                );
        }

        RestoreBossState();

        isAttacking = false;
        isDodging = false;
        isFrontStepping = false;
        currentBasicAttackData = null;
        basicHitTriggered = false;
        basicHitTimedOut = false;
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

        nextChargePostPassDelay = -1f;

        float direction =
            Mathf.Sign(
                player.position.x -
                transform.position.x
            );

        float playerXAtStart =
            player.position.x;

        float targetX =
            playerXAtStart +
            direction *
            dashBehindDistance;

        Vector2 dashStartCenter =
            hitbox.GetWorldCenter();

        bossAnimator?.PlayDash();

        float elapsed = 0f;

        while (
            (targetX - transform.position.x) *
                direction > 0f &&
            elapsed < maxDashDuration)
        {
            rb.linearVelocity =
                new Vector2(
                    direction * dashSpeed,
                    rb.linearVelocity.y
                );

            elapsed +=
                Time.fixedDeltaTime;

            yield return
                new WaitForFixedUpdate();
        }

        rb.linearVelocity =
            new Vector2(
                0f,
                rb.linearVelocity.y
            );

        Vector2 dashEndCenter =
            hitbox.GetWorldCenter();

        CombatLogger.Instance?
            .MarkChargePassEnd();

        yield return new WaitForSeconds(
            postPassDelay
        );

        bossAnimator?.PlayDashAttack();

        yield return new WaitForSeconds(
            data.startupTime
        );

        hitbox.Activate(data);

        hitbox.SweepFromTo(
            dashStartCenter,
            dashEndCenter
        );

        yield return new WaitForSeconds(
            data.activeTime
        );

        hitbox.Deactivate();

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

        rb.linearVelocity =
            Vector2.zero;

        bossAnimator?.PlayJump();

        float jumpUpSpeed = 40f;
        float jumpUpTime = 0.03f;

        float elapsed = 0f;

        while (elapsed < jumpUpTime)
        {
            rb.linearVelocity =
                new Vector2(
                    0f,
                    jumpUpSpeed
                );

            elapsed +=
                Time.fixedDeltaTime;

            yield return
                new WaitForFixedUpdate();
        }

        rb.linearVelocity =
            new Vector2(
                0f,
                jumpUpSpeed
            );

        yield return new WaitForSeconds(
            data.startupTime
        );

        if (visual != null)
            visual.gameObject.SetActive(false);

        if (bodyCollider != null)
            bodyCollider.enabled = false;

        rb.linearVelocity =
            Vector2.zero;

        yield return new WaitForSeconds(
            jumpSlamVanishTime
        );

        float spawnSide =
            Random.value < 0.5f
                ? -1f
                : 1f;

        float slamDirection =
            -spawnSide;

        float landingX =
            player.position.x -
            slamDirection *
            jumpLandingOffset;

        float spawnX =
            player.position.x +
            spawnSide *
            jumpSlamSpawnSideOffset;

        float fallDistance =
            Mathf.Max(
                0.1f,
                jumpSlamSpawnHeight -
                groundOffset
            );

        float estimatedFallTime =
            fallDistance /
            jumpSlamFallSpeed;

        float horizontalDistance =
            landingX - spawnX;

        float slamHorizontalVelocity =
            horizontalDistance /
            estimatedFallTime;

        transform.position =
            new Vector3(
                spawnX,
                jumpSlamGroundY +
                    jumpSlamSpawnHeight,
                transform.position.z
            );

        rb.linearVelocity =
            Vector2.zero;

        rb.gravityScale = 0f;

        if (visual != null)
            visual.gameObject.SetActive(true);

        movement.FaceTarget();

        rb.linearVelocity =
            Vector2.zero;

        bossAnimator?
            .PlayJumpHover();

        yield return new WaitForSeconds(
            jumpSlamHoverTime
        );

        bool jumpAttackStarted =
            false;

        while (true)
        {
            rb.linearVelocity =
                new Vector2(
                    slamHorizontalVelocity,
                    -jumpSlamFallSpeed
                );

            RaycastHit2D hit =
                Physics2D.Raycast(
                    transform.position,
                    Vector2.down,
                    groundCheckDistance,
                    groundLayer
                );

            if (hit.collider != null)
            {
                float distanceToGround =
                    transform.position.y -
                    hit.point.y;

                float remainingFallDistance =
                    Mathf.Max(
                        0f,
                        distanceToGround -
                            groundOffset
                    );

                float timeToGround =
                    remainingFallDistance /
                    jumpSlamFallSpeed;

                if (!jumpAttackStarted &&
                    timeToGround <=
                        jumpSlamAttackLeadTime)
                {
                    bossAnimator?
                        .PlayJumpAttack();

                    jumpAttackStarted = true;
                }

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

            yield return
                new WaitForFixedUpdate();
        }

        rb.gravityScale =
            defaultGravityScale;

        if (bodyCollider != null)
            bodyCollider.enabled = true;

        hitbox.Activate(data);

        float activeElapsed = 0f;

        while (activeElapsed <
               data.activeTime)
        {
            TryPerfectDodgeDistanceGrace();

            activeElapsed +=
                Time.deltaTime;

            yield return null;
        }

        hitbox.Deactivate();
        currentHitbox = null;

        yield return new WaitForSeconds(
            data.recoveryTime
        );

        isAttacking = false;
    }

    public void ExecuteBackDodge()
    {
        if (IsBusy)
            return;

        StartCoroutine(
            BackDodgeRoutine()
        );
    }

    private IEnumerator BackDodgeRoutine()
    {
        isDodging = true;

        bossAnimator?
            .PlayBackDodge();

        float direction =
            Mathf.Sign(
                transform.position.x -
                player.position.x
            );

        float speed =
            backDodgeDistance /
            backDodgeDuration;

        float movedDistance = 0f;

        while (movedDistance <
               backDodgeDistance)
        {
            float moveThisFrame =
                speed *
                Time.fixedDeltaTime;

            moveThisFrame =
                Mathf.Min(
                    moveThisFrame,
                    backDodgeDistance -
                        movedDistance
                );

            rb.linearVelocity =
                new Vector2(
                    direction * speed,
                    rb.linearVelocity.y
                );

            movedDistance +=
                moveThisFrame;

            yield return
                new WaitForFixedUpdate();
        }

        rb.linearVelocity =
            new Vector2(
                0f,
                rb.linearVelocity.y
            );

        isDodging = false;
    }

    public void ExecuteFrontStep()
    {
        if (IsBusy)
            return;

        StartCoroutine(
            FrontStepRoutine()
        );
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

        if (currentDistance <=
            frontStepStopDistance)
        {
            isFrontStepping = false;
            yield break;
        }

        float direction =
            Mathf.Sign(deltaX);

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

        while (movedDistance <
               moveDistance)
        {
            float moveThisFrame =
                speed *
                Time.fixedDeltaTime;

            moveThisFrame =
                Mathf.Min(
                    moveThisFrame,
                    moveDistance -
                        movedDistance
                );

            rb.linearVelocity =
                new Vector2(
                    direction * speed,
                    rb.linearVelocity.y
                );

            movedDistance +=
                moveThisFrame;

            yield return
                new WaitForFixedUpdate();
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
        bossAnimator?
            .SetPlaybackSpeed(1f);

        if (visual != null)
            visual.gameObject.SetActive(true);

        if (bodyCollider != null)
            bodyCollider.enabled = true;

        if (rb != null)
            rb.gravityScale =
                defaultGravityScale;
    }
}
