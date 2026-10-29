using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDash : MonoBehaviour
{
    [Header("Dash")]
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.4f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Perfect Dodge")]
    [SerializeField] private float perfectDodgeWindow = 0.10f;
    [SerializeField] private float perfectDodgeDistance = 2.0f;
    private Rigidbody2D rb;

    private bool isDashing;
    private bool canDash = true;
    private bool airDashAvailable = true;

    private float dashDirection = 1f;
    private float dashStartTime;
    private bool perfectDodgeTriggered;
    private float facingBeforeDash = 1f;
    public float FacingBeforeDash => facingBeforeDash; 
    private PlayerPrecisionStack precisionStack;

    public bool IsDashing => isDashing;
    
    private PlayerCombat playerCombat;

    private PlayerDamageReceiver damageReceiver;
    private Coroutine dashCoroutine;
    private float originalGravity;
    private PlayerMovement playerMovement;
    private PlayerCounter playerCounter;
    private PlayerHeal playerHeal;
    private PlayerAnimator playerAnimator;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCombat = GetComponent<PlayerCombat>();
        damageReceiver = GetComponent<PlayerDamageReceiver>();
        playerMovement = GetComponent<PlayerMovement>();
        playerCounter = GetComponent<PlayerCounter>();
        playerHeal = GetComponent<PlayerHeal>();
        precisionStack = GetComponent<PlayerPrecisionStack>();
        playerAnimator = GetComponent<PlayerAnimator>();
    }

    private void Update()
    {
        if (IsGrounded())
        {
            airDashAvailable = true;
        }
    }

    public void SetDirection(float direction)
    {
        if (direction != 0f)
        {
            dashDirection = Mathf.Sign(direction);
        }
    }
    public void OnDash(InputAction.CallbackContext context)
    {
        if (damageReceiver != null &&
            (damageReceiver.IsStunned ||
            damageReceiver.IsDead))
        {
            return;
        }

        if (playerCounter != null &&
            playerCounter.IsCountering)
        {
            return;
        }

        if (!context.performed)
            return;


        if (playerCombat != null &&
            !playerCombat.CanDashCancel)
        {
            return;
        }


        if (!canDash)
            return;


        if (!IsGrounded() &&
            !airDashAvailable)
        {
            return;
        }


        // Recovery 중 Dash Cancel
        if (playerCombat != null &&
            playerCombat.CurrentPhase ==
                PlayerCombat.AttackPhase.Recovery)
        {
            playerCombat.CancelAttack();
        }


        if (playerCombat != null &&
            playerCombat.IsCharging)
        {
            return;
        }


        if (playerHeal != null &&
            playerHeal.IsHealing)
        {
            return;
        }
        if (playerMovement != null)
        {
            facingBeforeDash =
                playerMovement.FacingDirection;
        }
        float actionDirection =
            playerMovement != null
            ? playerMovement.GetActionDirection()
            : dashDirection;

        playerMovement?
            .ForceFacing(
                actionDirection
            );

        dashDirection =
            actionDirection;
        // =====================================
        // Precision 5 / 5
        // Dash → Enhanced Dash Attack
        // =====================================

        if (playerCombat != null &&
            playerCombat.TryStartEnhancedDashAttack())
        {
            canDash = false;


            if (!IsGrounded())
            {
                airDashAvailable = false;
            }


            StartCoroutine(
                ResetDashCooldown()
            );


            return;
        }


        // =====================================
        // 일반 Dash
        // =====================================

        dashCoroutine =
            StartCoroutine(
                Dash()
            );
    }   

    private IEnumerator Dash()
    {
        isDashing = true;
        canDash = false;

        dashStartTime = Time.time;
        perfectDodgeTriggered = false;

        if (!IsGrounded())
            airDashAvailable = false;

        originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        rb.linearVelocity = new Vector2(
            dashDirection * dashSpeed,
            0f
        );

        CombatLogger.Instance?.RecordPlayerResponse(
            PlayerResponseType.Dash
        );

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);

        canDash = true;
        dashCoroutine = null;
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }
    public bool TryPerfectDodge()
    {
        if (!isDashing)
            return false;

        if (perfectDodgeTriggered)
            return false;

        float dodgeTiming =
            Time.time - dashStartTime;

        if (dodgeTiming > perfectDodgeWindow)
            return false;

        perfectDodgeTriggered = true;

        precisionStack?.GainStack();
        playerMovement?.ForceFacing(
            facingBeforeDash * -1
        );
        playerAnimator?.PlayPerfectDodge();
        Debug.Log(
            $"[Perfect Dodge] " +
            $"Timing={dodgeTiming:F3}"
        );
        CombatLogger.Instance?
            .RecordPlayerAction(
                "PerfectDodge"
            );

        return true;
    }
    public bool TryPerfectDodgeByDistance(Transform attacker)
    {
        if (!isDashing)
            return false;

        if (perfectDodgeTriggered)
            return false;

        if (attacker == null)
            return false;

        // 이미 피격 무적 상태면
        // 위험한 공격을 회피한 것이 아니므로 제외
        if (damageReceiver != null &&
            damageReceiver.IsInvincible)
        {
            return false;
        }

        float dodgeTiming =
            Time.time - dashStartTime;

        if (dodgeTiming > perfectDodgeWindow)
            return false;

        float distance =
            Mathf.Abs(
                transform.position.x -
                attacker.position.x
            );

        if (distance > perfectDodgeDistance)
            return false;

        perfectDodgeTriggered = true;

        precisionStack?.GainStack();
        playerMovement?.ForceFacing(
            facingBeforeDash *-1
        );
        playerAnimator?.PlayPerfectDodge();
        Debug.Log(
            $"[Perfect Dodge - Distance] " +
            $"Timing={dodgeTiming:F3} | " +
            $"Distance={distance:F2}"
        );
        CombatLogger.Instance?
            .RecordPlayerAction(
                "PerfectDodge"
            );
        return true;
    }
    public void ForceCancelDash()
    {
        if (!isDashing)
            return;

        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
            dashCoroutine = null;
        }

        rb.gravityScale = originalGravity;
        isDashing = false;

        // 피격 직후 다시 대시하지 못하도록
        canDash = false;

        StartCoroutine(ResetDashCooldown());
    }
    public void CancelDashForAttack()
    {
        if (!isDashing)
            return;

        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
            dashCoroutine = null;
        }

        rb.gravityScale = originalGravity;
        isDashing = false;

        // DashAttack으로 전환해도 기존 Dash 쿨타임은 소모
        canDash = false;

        StartCoroutine(ResetDashCooldown());
    }
    private IEnumerator ResetDashCooldown()
    {
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
    
}