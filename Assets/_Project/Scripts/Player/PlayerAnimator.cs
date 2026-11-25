using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

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
    private static readonly int LightAttack2 =
        Animator.StringToHash("LightAtk2");
    private static readonly int LightAttack3 =
        Animator.StringToHash("LightAtk3");

    private static readonly int ChargeHold =
    Animator.StringToHash("ChargeHold");
    private static readonly int ChargeAttack =
    Animator.StringToHash("ChargeAttack");
    private static readonly int Counter =
    Animator.StringToHash("Counter");
    private static readonly int CounterSuccess =
    Animator.StringToHash("CounterSuccess");
    private static readonly int PerfectCounterSuccess =
    Animator.StringToHash(
        "PerfectCounterSuccess"
    );
    private static readonly int Heal =
    Animator.StringToHash("Heal");
    private static readonly int HealEnd =
    Animator.StringToHash("HealEnd");
    private static readonly int Hurt =
    Animator.StringToHash("Hurt");
    private static readonly int Death =
    Animator.StringToHash("Death");
    private static readonly int FinalStrike =
        Animator.StringToHash("Base Layer.FinalStrike");
    private static readonly int DashAttack =
    Animator.StringToHash("DashAttack");
    private static readonly int EnhancedDashAttackPrepare =
        Animator.StringToHash(
            "EnhancedDashAttackPrepare"
        );
    private static readonly int EnhancedDashAttack =
    Animator.StringToHash(
        "EnhancedDashAttack"
    );
    private static readonly int Knockdown =
        Animator.StringToHash(
            "Knockdown"
        );

    private static readonly int GetUp =
        Animator.StringToHash(
            "GetUp"
        );
    private static readonly int PerfectDodgeHash =
    Animator.StringToHash("PerfectDodge");
    private bool isPerfectDodging;
    public bool IsPerfectDodging => isPerfectDodging;
    private int currentState = -1;
    private float presentationPreviousSpeed;
    private bool presentationFrozen;
    private bool finalStrikePresentation;
    private PlayableGraph upgradeGraph;
    private bool upgradePresentation;
    private float upgradeEndTime;

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
        if (upgradePresentation &&
            (!upgradeGraph.IsValid() || Time.unscaledTime >= upgradeEndTime))
        {
            StopUpgradeAnimation();
        }

        UpdateMovementAnimation();
    }

    private void OnDisable()
    {
        StopUpgradeAnimation();
        ResumePresentationAnimation();
        ClearFinalStrikePresentation();
    }

    private void UpdateMovementAnimation()
    {
        if (damageReceiver != null && damageReceiver.IsDead)
        {
            StopUpgradeAnimation();
            ResumePresentationAnimation();
            ClearFinalStrikePresentation();
            PlayState(Death);
            return;
        }
        if (presentationFrozen || finalStrikePresentation || upgradePresentation)
            return;
        if (damageReceiver != null &&
            damageReceiver.IsKnockedDown)
        {
            PlayState(
                Knockdown
            );

            return;
        }

        if (damageReceiver != null &&
            damageReceiver.IsGettingUp)
        {
            PlayState(
                GetUp
            );

            return;
        }
        if (damageReceiver != null && damageReceiver.IsStunned)
        {
            PlayState(Hurt);
            return;
        }
        if (isPerfectDodging)
            return;
        if (counter != null && counter.CounterSucceeded)
        {
            if (counter.PerfectParrySucceeded)
            {
                PlayState(
                    PerfectCounterSuccess
                );
            }
            else
            {
                PlayState(
                    CounterSuccess
                );
            }

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
            combat.CurrentAttackType ==
                PlayerCombat.AttackType.Normal)
        {
            PlayNormalAttack(
                combat.CurrentNormalComboStep
            );

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
        if (combat != null &&
            combat.IsAttacking &&
            combat.CurrentAttackType ==
                PlayerCombat.AttackType.EnhancedDashAttack)
        {
            if (combat.CurrentEnhancedDashPhase ==
                PlayerCombat.EnhancedDashPhase.Prepare)
            {
                PlayState(
                    EnhancedDashAttackPrepare
                );
            }
            else
            {
                PlayState(
                    EnhancedDashAttack
                );
            }

            return;
        }
        if (combat != null &&
            combat.IsAttacking &&
            combat.CurrentAttackType == PlayerCombat.AttackType.DashAttack)
        {
            PlayState(DashAttack);
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

    public void FreezePresentationAnimation()
    {
        if (animator == null || presentationFrozen)
            return;

        StopUpgradeAnimation();
        presentationPreviousSpeed = animator.speed;
        animator.speed = 0f;
        presentationFrozen = true;
    }

    public void ResumePresentationAnimation()
    {
        if (!presentationFrozen)
            return;

        if (animator != null)
            animator.speed = presentationPreviousSpeed;

        presentationFrozen = false;
    }

    public bool TryPlayFinalStrike()
    {
        if (animator == null)
            return false;

        StopUpgradeAnimation();
        ResumePresentationAnimation();

        bool hasFinalStrike =
            animator.HasState(0, FinalStrike);

        int stateHash = hasFinalStrike
            ? FinalStrike
            : LightAttack;

        finalStrikePresentation = true;
        currentState = stateHash;
        animator.Play(stateHash, 0, 0f);

        return hasFinalStrike;
    }

    public void ClearFinalStrikePresentation()
    {
        if (!finalStrikePresentation)
            return;

        finalStrikePresentation = false;
        currentState = -1;
    }

    public void PlayUpgradeAnimation(AnimationClip clip)
    {
        if (clip == null || clip.length <= 0f || animator == null ||
            !isActiveAndEnabled || !animator.isActiveAndEnabled ||
            presentationFrozen || finalStrikePresentation ||
            (damageReceiver != null && damageReceiver.IsDead))
        {
            return;
        }

        StopUpgradeAnimation();

        upgradeGraph = PlayableGraph.Create("Player Upgrade Animation");
        upgradeGraph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);

        AnimationClipPlayable clipPlayable =
            AnimationClipPlayable.Create(upgradeGraph, clip);
        AnimationPlayableOutput output =
            AnimationPlayableOutput.Create(upgradeGraph, "Upgrade", animator);
        output.SetSourcePlayable(clipPlayable);

        upgradePresentation = true;
        upgradeEndTime = Time.unscaledTime + clip.length;
        upgradeGraph.Play();
    }

    public void StopUpgradeAnimation()
    {
        if (!upgradePresentation && !upgradeGraph.IsValid())
            return;

        if (upgradeGraph.IsValid())
            upgradeGraph.Destroy();

        upgradePresentation = false;
        upgradeEndTime = 0f;
        currentState = -1;

        if (animator != null && animator.isActiveAndEnabled &&
            !presentationFrozen && !finalStrikePresentation &&
            (damageReceiver == null || !damageReceiver.IsDead))
        {
            animator.Play(Idle, 0, 0f);
        }
    }

    public void PlayPerfectDodge()
    {
        isPerfectDodging = true;

        currentState = PerfectDodgeHash;

        animator.Play(
            PerfectDodgeHash,
            0,
            0f
        );
    }
    public void OnPerfectDodgeEnd()
    {
        isPerfectDodging = false;
    }
    public void PlayCounterSuccess(
        bool isPerfect)
    {
        int targetState =
            isPerfect
                ? PerfectCounterSuccess
                : CounterSuccess;

        currentState = targetState;

        animator.Play(
            targetState,
            0,
            0f
        );
    }
    public void PlayNormalAttack(
        int comboStep)
    {
        int stateHash;

        switch (comboStep)
        {
            case 2:
                stateHash = LightAttack2;
                break;

            case 3:
                stateHash = LightAttack3;
                break;

            default:
                stateHash = LightAttack;
                break;
        }

        if (currentState == stateHash)
            return;

        currentState = stateHash;

        animator.Play(
            stateHash,
            0,
            0f
        );
    }
}
