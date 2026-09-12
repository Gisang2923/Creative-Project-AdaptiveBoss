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
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerDash = GetComponent<PlayerDash>();
        damageReceiver = GetComponent<PlayerDamageReceiver>();
        playerCounter = GetComponent<PlayerCounter>();
    }

    private void Update()
    {
        UpdateGroundState();
        UpdateJumpBuffer();
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

        Move();
        HandleJump();
        ApplyBetterGravity();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        if (moveInput.x != 0f)
        {
            facingDirection = Mathf.Sign(moveInput.x);

            facingRoot.localScale = new Vector3(
                facingDirection,
                1f,
                1f
            );

            if (playerDash != null)
            {
                playerDash.SetDirection(facingDirection);
            }
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (damageReceiver != null && (damageReceiver.IsStunned || damageReceiver.IsDead))
            return;
        if (playerCounter != null && playerCounter.IsCountering)
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
        rb.linearVelocity = new Vector2(
            moveInput.x * moveSpeed,
            rb.linearVelocity.y
        );
    }

    private void UpdateGroundState()
    {
        if (IsGrounded())
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
        jumpBufferCounter -= Time.deltaTime;
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