using UnityEngine;

public class BossAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private int currentState = -1;

    private static readonly int Ready =
    Animator.StringToHash("Ready");
    private static readonly int BattleIdle =
        Animator.StringToHash("BattleIdle");

    private static readonly int Walk =
        Animator.StringToHash("Walk");

    private static readonly int IsMoving =
        Animator.StringToHash("IsMoving");

    private static readonly int BackDash =
        Animator.StringToHash("BackDash");

    private static readonly int Dash =
        Animator.StringToHash("Dash");

    private static readonly int LightAttack =
        Animator.StringToHash("LightAtk1");

    private static readonly int DashAttack =
        Animator.StringToHash("DashAtk");

    private static readonly int HeavyAttack =
        Animator.StringToHash("FrontHeavyAtk");

    private static readonly int Jump =
        Animator.StringToHash("Jump");

    private static readonly int JumpAttack =
        Animator.StringToHash("JumpAtk");

    private static readonly int Hited =
    Animator.StringToHash("Hited");
    private static readonly int CounterHit =
    Animator.StringToHash("BossCounterHit");
    private static readonly int Death =
    Animator.StringToHash("Death");

    private static readonly int Parry =
        Animator.StringToHash("Parry");
    private static readonly int ParrySuccess =
        Animator.StringToHash("ParrySuccess");
    public void PlayReady()
    {
        animator.Play(Ready, 0, 0f);
    }
    public void PlayDeath()
    {
        PlayAction(Death);
    }
    public void PlayHit()
    {
        PlayAction(Hited);
    }
    public void PlayCounterHit()
    {
        Debug.Log(
            $"CounterHit State Exists: {animator.HasState(0, CounterHit)}"
        );

        PlayAction(CounterHit);
    }
    public void SetMoving(bool isMoving)
    {
        animator.SetBool(IsMoving, isMoving);

        if (isMoving)
        {
            PlayState(Walk, 0.05f);
        }
        else
        {
            PlayState(BattleIdle, 0.05f);
        }
    }

    private void PlayState(int stateHash, float transitionTime)
    {
        // 이미 같은 애니메이션이면 다시 처음부터 재생하지 않음
        if (currentState == stateHash)
            return;

        currentState = stateHash;

        animator.CrossFade(
            stateHash,
            transitionTime,
            0,
            0f
        );
    }

    private void PlayAction(int stateHash)
    {
        animator.SetBool(IsMoving, false);

        PlayState(stateHash, 0.02f);
    }

    public void PlayBackDodge()
    {
        PlayAction(BackDash);
    }

    public void PlayDash()
    {
        PlayAction(Dash);
    }

    public void PlayDashAttack()
    {
        PlayAction(DashAttack);
    }

    public void PlayLightAttack()
    {
        PlayAction(LightAttack);
    }

    public void PlayHeavyAttack()
    {
        PlayAction(HeavyAttack);
    }

    public void PlayJump()
    {
        PlayAction(Jump);
    }

    public void PlayJumpAttack()
    {
        PlayAction(JumpAttack);
    }
    public void PlayParry()
    {
        animator.Play(Parry, 0, 0f);
    }

    public void PlayParrySuccess()
    {
        animator.Play(ParrySuccess, 0, 0f);
    }
}