using System.Collections;
using UnityEngine;

public class BossAction : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;

    [Header("Basic Slash Timing")]
    [SerializeField] private float basicPrepareHoldTime = 0.18f;

    [SerializeField] private float basicAnticipationSpeed1 = 0.80f;
    [SerializeField] private float basicAnticipationSpeed2 = 0.80f;
    [SerializeField] private float basicAnticipationSpeed3 = 0.70f;

    [SerializeField] private float basicReleaseSpeed1 = 1.65f;
    [SerializeField] private float basicReleaseSpeed2 = 1.80f;
    [SerializeField] private float basicReleaseSpeed3 = 1.90f;

    private int currentBasicStrikeIndex;

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
    [SerializeField] private float heavyPrepareCueTime = 0.2f;
    [SerializeField] private float heavyBaseHoldTime = 0.22f;
    [SerializeField] private float heavyPreHoldTime = 0.18f;

    // 준비 중 뒤로 빠지는 거리
    [SerializeField] private float heavyBackStepDistance = 0.6f;
    [SerializeField] private float heavyBackStepDuration = 0.12f;

    // Release 순간 전진
    [SerializeField] private float heavyForwardSpeed = 7f;
    [SerializeField] private float heavyForwardMaxDistance = 1.4f;
    [SerializeField] private float heavyForwardStopDistance = 0.75f;

    private float nextHeavyHoldTime = -1f;
    private AttackData currentHeavyAttackData;
    private bool heavyHitTriggered;
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

    [Header("Charge Slash VFX")]
    [SerializeField]
    private ChargeTrailVFX chargeTrailPrefab;

    private ChargeTrailVFX activeChargeTrail;

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
    private BossSFX bossSFX;
    private bool chargeHitTriggered;
    private Vector2 currentChargeSweepStart;
    private Vector2 currentChargeSweepEnd;
    private AttackData currentChargeAttackData;
    private float defaultGravityScale;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb != null)
            defaultGravityScale = rb.gravityScale;
        bossSFX = GetComponent<BossSFX>();    
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

        currentBasicStrikeIndex =
            strikeIndex;

        switch (strikeIndex)
        {
            case 1:
                bossAnimator?
                    .SetPlaybackSpeed(
                        basicAnticipationSpeed1
                    );

                bossAnimator?
                    .PlayLightAttack1();

                eventTimeout =
                    basicAttack1Duration;
                break;

            case 2:
                bossAnimator?
                    .SetPlaybackSpeed(
                        basicAnticipationSpeed2
                    );

                bossAnimator?
                    .PlayLightAttack2();

                eventTimeout =
                    basicAttack2Duration;
                break;

            case 3:
                bossAnimator?
                    .SetPlaybackSpeed(
                        basicAnticipationSpeed3
                    );

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
    public void OnBasicRelease()
    {
        if (!isAttacking)
            return;

        float releaseSpeed;

        switch (currentBasicStrikeIndex)
        {
            case 2:
                releaseSpeed =
                    basicReleaseSpeed2;
                break;

            case 3:
                releaseSpeed =
                    basicReleaseSpeed3;
                break;

            default:
                releaseSpeed =
                    basicReleaseSpeed1;
                break;
        }

        bossAnimator?
            .SetPlaybackSpeed(
                releaseSpeed
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
        bossAnimator?.SetPlaybackSpeed(1f);

        currentHitbox.Activate(
            currentBasicAttackData
        );

        TryPerfectDodgeDistanceGrace();
    }
    public void OnHeavyHit()
    {
        if (!isAttacking ||
            heavyHitTriggered ||
            currentHitbox == null ||
            currentHeavyAttackData == null)
        {
            return;
        }

        heavyHitTriggered = true;
        bossSFX?.PlayHeavyHit();

        currentHitbox.Activate(
            currentHeavyAttackData
        );

        TryPerfectDodgeDistanceGrace();
    }
    private IEnumerator HeavySlashRoutine(BossAttack attack)
    {
        isAttacking = true;

        AttackData data = attack.attackData;
        Hitbox hitbox = attack.hitbox;

        currentHitbox = hitbox;
        currentHeavyAttackData = data;
        heavyHitTriggered = false;

        // 1. 공격 예고
        bossAnimator?.PlayHeavyPrepare();

        yield return StartCoroutine(
            HeavyTrackTargetForDuration(
                heavyPrepareCueTime
            )
        );

        // 2. 플레이어 반대 방향으로 짧게 Back Step
        yield return StartCoroutine(
            HeavyBackStep()
        );

        // 3. 실제 Heavy Attack 모션 시작
        bossAnimator?.PlayHeavyAttack();

        yield return StartCoroutine(
            HeavyTrackTargetForDuration(
                heavyPreHoldTime
            )
        );

        float holdTime =
            nextHeavyHoldTime >= 0f
                ? nextHeavyHoldTime
                : heavyBaseHoldTime;

        nextHeavyHoldTime = -1f;

        bossAnimator?.SetPlaybackSpeed(0f);

        yield return StartCoroutine(
            HeavyTrackTargetForDuration(
                holdTime
            )
        );

        bossAnimator?.SetPlaybackSpeed(1f);

        // 4. 여기서 방향 고정
        float direction =
            player != null
                ? Mathf.Sign(
                    player.position.x -
                    transform.position.x
                )
                : 1f;

        if (direction == 0f)
            direction = 1f;

        float releaseTimeout =
            Mathf.Max(
                0.05f,
                data.startupTime -
                heavyPreHoldTime
            );

        // Animation Event가 발생할 때까지
        // 앞으로 파고들며 대기
        yield return StartCoroutine(
            HeavyReleaseUntilHit(
                direction,
                releaseTimeout
            )
        );

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
        currentHeavyAttackData = null;
        heavyHitTriggered = false;

        isAttacking = false;
    }
    private IEnumerator HeavyBackStep()
    {
        if (rb == null ||
            player == null ||
            heavyBackStepDistance <= 0f ||
            heavyBackStepDuration <= 0f)
        {
            yield break;
        }

        float directionAway =
            Mathf.Sign(
                transform.position.x -
                player.position.x
            );

        if (directionAway == 0f)
            directionAway = 1f;

        float speed =
            heavyBackStepDistance /
            heavyBackStepDuration;

        float movedDistance = 0f;

        while (movedDistance <
            heavyBackStepDistance)
        {
            float moveThisFrame =
                speed *
                Time.fixedDeltaTime;

            moveThisFrame =
                Mathf.Min(
                    moveThisFrame,
                    heavyBackStepDistance -
                        movedDistance
                );

            rb.linearVelocity =
                new Vector2(
                    directionAway * speed,
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
    private IEnumerator HeavyTrackTargetForDuration(
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            movement?.FaceTarget();

            if (rb != null)
            {
                rb.linearVelocity =
                    new Vector2(
                        0f,
                        rb.linearVelocity.y
                    );
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator HeavyReleaseUntilHit(
        float direction,
        float timeout)
    {
        if (rb == null)
            yield break;

        float elapsed = 0f;
        float movedDistance = 0f;

        while (!heavyHitTriggered &&
            elapsed < timeout)
        {
            float distanceToPlayer =
                player != null
                    ? Mathf.Abs(
                        player.position.x -
                        transform.position.x
                    )
                    : Mathf.Infinity;

            bool canMove =
                movedDistance <
                    heavyForwardMaxDistance &&
                distanceToPlayer >
                    heavyForwardStopDistance;

            if (canMove)
            {
                rb.linearVelocity =
                    new Vector2(
                        direction *
                        heavyForwardSpeed,
                        rb.linearVelocity.y
                    );

                movedDistance +=
                    heavyForwardSpeed *
                    Time.deltaTime;
            }
            else
            {
                rb.linearVelocity =
                    new Vector2(
                        0f,
                        rb.linearVelocity.y
                    );
            }

            elapsed += Time.deltaTime;

            yield return null;
        }

        rb.linearVelocity =
            new Vector2(
                0f,
                rb.linearVelocity.y
            );

        // Animation Event 누락 방지
        if (!heavyHitTriggered)
        {
            Debug.LogWarning(
                "[Boss Heavy] " +
                "HeavyHit Animation Event timeout. " +
                "Fallback hit triggered."
            );

            OnHeavyHit();
        }
    }
    public void ForceCancelAttack()
    {
        StopAllCoroutines();

        if (currentHitbox != null)
        {
            currentHitbox.Deactivate();
            currentHitbox = null;
        }

        // Charge Trail 강제 정리
        if (activeChargeTrail != null)
        {
            Destroy(
                activeChargeTrail.gameObject
            );

            activeChargeTrail = null;
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

        currentHeavyAttackData = null;
        heavyHitTriggered = false;
        currentChargeAttackData = null;
        chargeHitTriggered = false;
    }

    private IEnumerator ChargeSlashRoutine(
        BossAttack attack)
    {
        isAttacking = true;

        AttackData data =
            attack.attackData;

        Hitbox hitbox =
            attack.hitbox;

        currentHitbox =
            hitbox;

        currentChargeAttackData =
            data;

        chargeHitTriggered =
            false;


        // =====================================
        // Adaptive Charge Timing
        // =====================================

        float postPassDelay =
            nextChargePostPassDelay >= 0f
                ? nextChargePostPassDelay
                : chargePostPassDelay;

        nextChargePostPassDelay = -1f;


        // =====================================
        // Dash 방향 / 목표 위치
        // =====================================

        float direction =
            Mathf.Sign(
                player.position.x -
                transform.position.x
            );

        if (direction == 0f)
            direction = 1f;

        float playerXAtStart =
            player.position.x;

        float targetX =
            playerXAtStart +
            direction *
            dashBehindDistance;


        // =====================================
        // Dash Start
        // =====================================

        Vector2 dashStartCenter =
            hitbox.GetWorldCenter();

        bossAnimator?
            .PlayDash();

        float elapsed = 0f;

        while (
            (targetX - transform.position.x) *
                direction > 0f &&
            elapsed <
                maxDashDuration)
        {
            rb.linearVelocity =
                new Vector2(
                    direction *
                    dashSpeed,
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


        // =====================================
        // Dash End
        // =====================================

        Vector2 dashEndCenter =
            hitbox.GetWorldCenter();


        // Charge Trail 생성
        if (chargeTrailPrefab != null)
        {
            activeChargeTrail =
                Instantiate(
                    chargeTrailPrefab
                );

            activeChargeTrail.Setup(
                dashStartCenter,
                dashEndCenter
            );
        }


        CombatLogger.Instance?
            .MarkChargePassEnd();


        // Animation Event에서 사용할
        // Sweep 위치 저장
        currentChargeSweepStart =
            dashStartCenter;

        currentChargeSweepEnd =
            dashEndCenter;


        // =====================================
        // Adaptive Post Pass Delay
        // =====================================

        yield return
            new WaitForSeconds(
                postPassDelay
            );


        // Trail 제거
        if (activeChargeTrail != null)
        {
            Destroy(
                activeChargeTrail.gameObject
            );

            activeChargeTrail = null;
        }


        // =====================================
        // Charge Slash Attack
        //
        // 여기부터는 Animation Event가 담당
        //
        // ChargeCueSFX
        //      ↓
        // ChargeHit
        // =====================================

        bossAnimator?
            .PlayDashAttack();


        // ChargeHit Animation Event 기다림
        float hitWaitElapsed = 0f;

        float hitEventTimeout =
            Mathf.Max(
                data.startupTime + 0.5f,
                0.8f
            );


        while (
            !chargeHitTriggered &&
            hitWaitElapsed <
                hitEventTimeout)
        {
            hitWaitElapsed +=
                Time.deltaTime;

            yield return null;
        }


        // Animation Event 누락 방지
        if (!chargeHitTriggered)
        {
            Debug.LogWarning(
                "[Boss Charge] " +
                "ChargeHit Animation Event timeout. " +
                "Fallback hit triggered."
            );

            OnChargeHit();
        }


        // =====================================
        // Active
        // =====================================

        yield return
            new WaitForSeconds(
                data.activeTime
            );

        hitbox.Deactivate();


        // =====================================
        // Recovery
        // =====================================

        yield return
            new WaitForSeconds(
                data.recoveryTime
            );


        // =====================================
        // Cleanup
        // =====================================

        currentHitbox = null;

        currentChargeAttackData = null;

        chargeHitTriggered = false;

        isAttacking = false;
    }
    public void OnChargeCueSFX()
    {
        if (!isAttacking)
            return;

        bossSFX?
            .PlayCharge();
    }
    public void OnChargeHit()
    {
        if (!isAttacking ||
            chargeHitTriggered ||
            currentHitbox == null)
        {
            return;
        }

        chargeHitTriggered = true;

        currentHitbox.Activate(
            currentChargeAttackData
        );

        currentHitbox.SweepFromTo(
            currentChargeSweepStart,
            currentChargeSweepEnd
        );

        TryPerfectDodgeDistanceGrace();
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
