using System.Collections;
using UnityEngine;

public class BossBattleIntroController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossController bossController;
    [SerializeField] private BossAnimator bossAnimator;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject bossHurtbox;

    [Header("Timing")]
    [SerializeField] private float initialHoldDuration = 1f;
    [SerializeField] private float readyDuration = 2.11f;

    private bool introStarted;

    private void Awake()
    {
        if (animator != null)
            animator.enabled = false;
        if (bossHurtbox != null)
            bossHurtbox.SetActive(false);
    }

    private void Start()
    {
        // 지금은 테스트용
        StartIntro();
    }

    public void StartIntro()
    {
        if (introStarted)
            return;

        introStarted = true;

        StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        Debug.Log("[INTRO] Initial Hold");

        // ★ 여기서 1초 동안 아무것도 하지 않음
        yield return new WaitForSeconds(initialHoldDuration);

        Debug.Log("[INTRO] Ready Start");

        // Ready 시작
        if (animator != null)
        {
            animator.enabled = true;
            animator.speed = 1f;
        }

        bossAnimator?.PlayReady();

        // Ready 애니메이션 2.11초
        yield return new WaitForSeconds(readyDuration);

        Debug.Log("[INTRO] Battle Start");
        if (bossHurtbox != null)
            bossHurtbox.SetActive(true);
        // 전투 시작
        bossController?.StartBattle();
    }
}