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

    private static readonly int DashAttack =
        Animator.StringToHash("DashAtk");

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
        animator.SetTrigger("LightAttack");
    }

    public void PlayHeavyAttack()
    {
        animator.SetTrigger("HeavyAttack");
    }

    public void PlayRushAttack()
    {
        animator.SetTrigger("RushAttack");
    }
}