using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private PlayerMovement movement;
    private PlayerDash dash;
    private PlayerDamageReceiver damageReceiver;
    private PlayerCombat combat;
    private static readonly int Idle =
        Animator.StringToHash("Idle");

    private static readonly int Run =
        Animator.StringToHash("Run");

    private static readonly int Jump =
        Animator.StringToHash("Jump");

    private static readonly int Fall =
        Animator.StringToHash("Fall");

    private static readonly int Dash =
        Animator.StringToHash("Dash");
    private static readonly int LightAttack =
    Animator.StringToHash("LightAtk");

    private int currentState = -1;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        dash = GetComponent<PlayerDash>();
        damageReceiver = GetComponent<PlayerDamageReceiver>();
        combat = GetComponent<PlayerCombat>();
    }

    private void Update()
    {
        UpdateMovementAnimation();
    }

    private void UpdateMovementAnimation()
    {
        if (damageReceiver != null &&
            (damageReceiver.IsStunned || damageReceiver.IsDead))
            return;
        if (combat != null && combat.IsAttacking)
        {
            PlayState(LightAttack);
            return;
        }
        if (dash != null && dash.IsDashing)
        {
            PlayState(Dash);
            return;
        }

        if (!movement.IsGroundedState)
        {
            if (movement.VerticalVelocity > 0f)
                PlayState(Jump);
            else
                PlayState(Fall);

            return;
        }

        if (Mathf.Abs(movement.HorizontalVelocity) > 0.1f)
        {
            PlayState(Run);
            return;
        }

        PlayState(Idle);
    }

    private void PlayState(int stateHash)
    {
        if (currentState == stateHash)
            return;

        currentState = stateHash;

        animator.CrossFade(
            stateHash,
            0.05f,
            0,
            0f
        );
    }
}