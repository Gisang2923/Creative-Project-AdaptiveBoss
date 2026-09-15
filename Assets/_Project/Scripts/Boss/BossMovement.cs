using UnityEngine;

public class BossMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform facingRoot;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    
    [SerializeField] private BossAnimator bossAnimator;
    private Rigidbody2D rb;

    public float DistanceToTarget
    {
        get
        {
            if (target == null)
                return Mathf.Infinity;

            return Mathf.Abs(target.position.x - transform.position.x);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void MoveTowardTarget()
    {
        if (target == null)
        {
            Stop();
            return;
        }

        float direction =
            Mathf.Sign(target.position.x - transform.position.x);

        rb.linearVelocity =
            new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        UpdateFacing(direction);

        bossAnimator?.SetMoving(true);
    }

    public void MoveAwayFromTarget()
    {
        if (target == null)
        {
            Stop();
            return;
        }

        float direction =
            Mathf.Sign(transform.position.x - target.position.x);

        rb.linearVelocity =
            new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        FaceTarget();
        bossAnimator?.SetMoving(true);
    }
    public void Stop()
    {
        rb.linearVelocity =
            new Vector2(0f, rb.linearVelocity.y);

        bossAnimator?.SetMoving(false);
    }

    public void FaceTarget()
    {
        if (target == null)
            return;

        float direction =
            Mathf.Sign(target.position.x - transform.position.x);

        UpdateFacing(direction);
    }

    private void UpdateFacing(float direction)
    {
        if (direction == 0f || facingRoot == null)
            return;

        facingRoot.localScale =
            new Vector3(direction, 1f, 1f);
    }
}