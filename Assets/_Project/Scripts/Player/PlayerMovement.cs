using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;

    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform facingRoot;
    [Header("Hit Recovery")]
    [SerializeField] private float hitMoveRecoveryTime = 0.15f;

    private float hitMoveRecoveryTimer;
    private bool wasStunned;
    private Rigidbody2D rb;

    private Vector2 moveInput;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    private bool jumpReleased;

    private PlayerDash playerDash;

    private float facingDirection = 1f;
    public float FacingDirection => facingDirection;

    private PlayerDamageReceiver damageReceiver;

    private PlayerCounter playerCounter;
    private PlayerCombat playerCombat;
    private PlayerHeal playerHeal;
    private bool isGrounded;

    public bool IsGroundedState => isGrounded;
    public float VerticalVelocity => rb.linearVelocity.y;
    public float HorizontalVelocity => rb.linearVelocity.x;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerDash = GetComponent<PlayerDash>();
        damageReceiver = GetComponent<PlayerDamageReceiver>();
        playerCounter = GetComponent<PlayerCounter>();
        playerCombat = GetComponent<PlayerCombat>();
        playerHeal = GetComponent<PlayerHeal>();
    }

    private void Update()
    {
        UpdateGroundState();
        UpdateJumpBuffer();

        bool isStunned =
            damageReceiver != null && damageReceiver.IsStunned;

        if (wasStunned && !isStunned)
        {
            hitMoveRecoveryTimer = hitMoveRecoveryTime;
        }

        // 현재 입력과 Facing을 지속적으로 동기화
        UpdateFacing();

        if (hitMoveRecoveryTimer > 0f)
        {
            hitMoveRecoveryTimer -= Time.deltaTime;
        }

        wasStunned = isStunned;
    }
    private void FixedUpdate()
    {
        if (damageReceiver != null && (damageReceiver.IsStunned || damageReceiver.IsDead))
        {
            return;
        }
        if (playerCounter != null && playerCounter.IsCountering)
        {
            rb.linearVelocity =
                new Vector2(0f, rb.linearVelocity.y);

            return;
        }
        if (playerDash != null && playerDash.IsDashing)
            return;
        if (playerCombat != null && playerCombat.IsCharging)
        {
            rb.linearVelocity =
                new Vector2(0f, rb.linearVelocity.y);

            return;
        }
        if (playerHeal != null && playerHeal.IsHealing)
        {
            rb.linearVelocity =
                new Vector2(0f, rb.linearVelocity.y);

            return;
        }
        Move();
        HandleJump();
        ApplyBetterGravity();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    private bool CanChangeFacing()
    {
        if (damageReceiver != null &&
            (damageReceiver.IsStunned || damageReceiver.IsDead))
            return false;

        if (playerDash != null && playerDash.IsDashing)
            return false;

        if (playerCounter != null && playerCounter.IsCountering)
            return false;

        if (playerHeal != null && playerHeal.IsHealing)
            return false;

        if (playerCombat != null &&
            (playerCombat.IsAttacking || playerCombat.IsCharging))
            return false;

        return true;
    }
    private void UpdateFacing()
    {
        if (Mathf.Abs(moveInput.x) <= 0.01f)
            return;

        if (!CanChangeFacing())
            return;

        float newDirection = Mathf.Sign(moveInput.x);

        if (newDirection == facingDirection)
            return;

        facingDirection = newDirection;

        facingRoot.localScale = new Vector3(
            facingDirection,
            1f,
            1f
        );

        playerDash?.SetDirection(facingDirection);
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        if (damageReceiver != null && (damageReceiver.IsStunned || damageReceiver.IsDead))
            return;
        if (playerCounter != null && playerCounter.IsCountering)
            return;  

        if (playerCombat != null && playerCombat.IsCharging)
            return;      
        if (playerHeal != null && playerHeal.IsHealing)
            return;
        if (context.performed)
        {
            jumpBufferCounter = jumpBufferTime;
            jumpReleased = false;
        }

        if (context.canceled)
        {
            jumpReleased = true;
        }
    }

    private void Move()
    {
        if (playerCombat != null &&
            (playerCombat.IsAttacking || playerCombat.IsCharging))
        {
            rb.linearVelocity = new Vector2(
                0f,
                rb.linearVelocity.y
            );

            return;
        }

        float moveMultiplier = 1f;

        if (hitMoveRecoveryTimer > 0f)
        {
            moveMultiplier =
                1f - (hitMoveRecoveryTimer / hitMoveRecoveryTime);
        }

        rb.linearVelocity = new Vector2(
            moveInput.x * moveSpeed * moveMultiplier,
            rb.linearVelocity.y
        );
    }

    private void UpdateGroundState()
    {
        isGrounded = IsGrounded();

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void UpdateJumpBuffer()
    {
        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void HandleJump()
    {
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );

            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }

        // 버튼을 일찍 놓으면 상승 속도를 잘라서 낮은 점프
        if (jumpReleased && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * jumpCutMultiplier
            );

            jumpReleased = false;
        }
    }

    private void ApplyBetterGravity()
    {
        // 내려갈 때 더 빠르게
        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up
                * Physics2D.gravity.y
                * (fallMultiplier - 1f)
                * Time.fixedDeltaTime;
        }
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );
    }
}