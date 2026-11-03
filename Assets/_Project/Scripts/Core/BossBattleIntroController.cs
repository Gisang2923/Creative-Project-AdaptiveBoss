using System.Collections;
using UnityEngine;

public class BossBattleIntroController : MonoBehaviour
{
    [SerializeField] private BossSFX bossSFX;
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
        if (bossSFX == null &&
            bossController != null)
        {
            bossSFX =
                bossController
                    .GetComponent<BossSFX>();
        }    
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
        yield return new WaitForSeconds(initialHoldDuration);

        Debug.Log("[INTRO] Start");

        // Ready 시작
        if (animator != null)
        {
            animator.enabled = true;
            animator.speed = 1f;
        }
        bossSFX?.PlayStart();
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