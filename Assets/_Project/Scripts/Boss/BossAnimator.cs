using UnityEngine;

public class BossAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private static readonly int IsMoving =
        Animator.StringToHash("IsMoving");

    private static readonly int BackDash =
        Animator.StringToHash("BackDash");

    public void SetMoving(bool isMoving)
    {
        animator.SetBool(IsMoving, isMoving);
    }

    public void PlayBackDodge()
    {
        // 이동 애니메이션 먼저 종료
        animator.SetBool(IsMoving, false);

        // 현재 Transition 여부와 관계없이 BackDash를 처음부터 재생
        animator.CrossFade(BackDash, 0.02f, 0, 0f);
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