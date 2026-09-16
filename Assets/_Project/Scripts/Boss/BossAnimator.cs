using UnityEngine;

public class BossAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

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

    public void SetMoving(bool isMoving)
    {
        animator.SetBool(IsMoving, isMoving);
    }

    public void PlayBackDodge()
    {
        animator.SetBool(IsMoving, false);
        animator.CrossFade(BackDash, 0.02f, 0, 0f);
    }

    public void PlayDash()
    {
        animator.SetBool(IsMoving, false);
        animator.CrossFade(Dash, 0.02f, 0, 0f);
    }

    public void PlayDashAttack()
    {
        animator.SetBool(IsMoving, false);
        animator.CrossFade(DashAttack, 0.02f, 0, 0f);
    }

    public void PlayLightAttack()
    {
        animator.SetBool(IsMoving, false);
        animator.CrossFade(LightAttack, 0.02f, 0, 0f);
    }

    public void PlayHeavyAttack()
    {
        animator.SetBool(IsMoving, false);
        animator.CrossFade(HeavyAttack, 0.02f, 0, 0f);
    }

    public void PlayRushAttack()
    {
        animator.SetTrigger("RushAttack");
    }
    
    public void PlayJump()
    {
        animator.SetBool(IsMoving, false);
        animator.CrossFade(Jump, 0.02f, 0, 0f);
    }
    public void PlayJumpAttack()
    {
        animator.SetBool(IsMoving, false);
        animator.CrossFade(JumpAttack, 0.02f, 0, 0f);
    }
}