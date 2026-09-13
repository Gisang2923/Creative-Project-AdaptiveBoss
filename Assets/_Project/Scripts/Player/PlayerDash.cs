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

    private Rigidbody2D rb;

    private bool isDashing;
    private bool canDash = true;
    private bool airDashAvailable = true;

    private float dashDirection = 1f;

    public bool IsDashing => isDashing;

    private PlayerCombat playerCombat;

    private PlayerDamageReceiver damageReceiver;
    private Coroutine dashCoroutine;
    private float originalGravity;
    private PlayerCounter playerCounter;
    private PlayerHeal playerHeal;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCombat = GetComponent<PlayerCombat>();
        damageReceiver = GetComponent<PlayerDamageReceiver>();
        playerCounter = GetComponent<PlayerCounter>();
        playerHeal = GetComponent<PlayerHeal>();
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
        if (damageReceiver != null && (damageReceiver.IsStunned || damageReceiver.IsDead))
            return;
        if (playerCounter != null && playerCounter.IsCountering)
            return;
        if (!context.performed)
            return;

        if (playerCombat != null && !playerCombat.CanDashCancel)
            return;

        if (!canDash)
            return;

        if (playerCombat != null &&
            playerCombat.CurrentPhase == PlayerCombat.AttackPhase.Recovery)
        {
            playerCombat.CancelAttack();
        }
        if (playerCombat != null && playerCombat.IsCharging)
            return;

        if (playerHeal != null && playerHeal.IsHealing)
            return;
        dashCoroutine = StartCoroutine(Dash());
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        canDash = false;

        if (!IsGrounded())
            airDashAvailable = false;

        originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        rb.linearVelocity = new Vector2(
            dashDirection * dashSpeed,
            0f
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

    private IEnumerator ResetDashCooldown()
    {
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
    }