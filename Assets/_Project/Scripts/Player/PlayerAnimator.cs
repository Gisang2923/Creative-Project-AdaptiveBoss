using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private PlayerMovement movement;
    private PlayerDash dash;
    private PlayerDamageReceiver damageReceiver;
    private PlayerCounter counter;
    private PlayerCombat combat;
    private PlayerHeal heal;
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
    private static readonly int ChargeHold =
    Animator.StringToHash("ChargeHold");
    private static readonly int ChargeAttack =
    Animator.StringToHash("ChargeAttack");
    private static readonly int Counter =
    Animator.StringToHash("Counter");
    private static readonly int CounterSuccess =
    Animator.StringToHash("CounterSuccess");
    private static readonly int Heal =
    Animator.StringToHash("Heal");
    private static readonly int HealEnd =
    Animator.StringToHash("HealEnd");
    private static readonly int Hurt =
    Animator.StringToHash("Hurt");
    private static readonly int Death =
    Animator.StringToHash("Death");
    private int currentState = -1;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        dash = GetComponent<PlayerDash>();
        damageReceiver = GetComponent<PlayerDamageReceiver>();
        combat = GetComponent<PlayerCombat>();
        counter = GetComponent<PlayerCounter>();
        heal = GetComponent<PlayerHeal>();
    }

    private void Update()
    {
        UpdateMovementAnimation();
    }

    private void UpdateMovementAnimation()
    {
        if (damageReceiver != null && damageReceiver.IsDead)
        {
            PlayState(Death);
            return;
        }
        if (damageReceiver != null && damageReceiver.IsStunned)
        {
            PlayState(Hurt);
            return;
        }
        if (counter != null && counter.CounterSucceeded)
        {
            PlayState(CounterSuccess);
            return;
        }    
        if (counter != null && counter.IsCountering)
        {
            PlayState(Counter);
            return;
        }    
        if (heal != null && heal.HealSucceeded)
        {
            PlayState(HealEnd);
            return;
        }

        if (heal != null && heal.IsHealing)
        {
            PlayState(Heal);
            return;
        }
        if (combat != null &&
            combat.IsAttacking &&
            combat.CurrentAttackType == PlayerCombat.AttackType.Normal)
        {
            PlayState(LightAttack);
            return;
        }
        if (combat != null &&
            combat.IsAttacking &&
            combat.CurrentAttackType == PlayerCombat.AttackType.Charge)
        {
            PlayState(ChargeAttack);
            return;
        }
        if (combat != null && combat.IsCharging)
        {
            PlayState(ChargeHold);
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