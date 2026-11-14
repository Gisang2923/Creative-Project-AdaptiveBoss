using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class BossBattleIntroController : MonoBehaviour
{
    [Header("Boss")]
    [SerializeField]
    private BossSFX bossSFX;

    [SerializeField]
    private BossController bossController;

    [SerializeField]
    private BossAnimator bossAnimator;

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private GameObject bossHurtbox;
    [SerializeField]
    private BossAction bossAction;

    [SerializeField]
    private BossAttackSelector attackSelector;

    [Header("Player")]
    [SerializeField]
    private PlayerInput playerInput;

    [SerializeField]
    private PlayerMovement playerMovement;

    [SerializeField]
    private CharacterSpeechBubble playerBubble;


    [Header("Fear")]
    [SerializeField]
    private Volume fearVolume;

    [SerializeField]
    private float fearFadeDuration = 0.15f;


    [Header("Normal Intro")]
    [SerializeField]
    private float initialHoldDuration = 1f;

    [SerializeField]
    private float readyDuration = 2.11f;


    [Header("First Encounter")]
    [SerializeField]
    private float firstLineDuration = 1.5f;

    [SerializeField]
    private float beforeReadyDelay = 0.4f;

    [SerializeField]
    private float afterFearDelay = 0.2f;

    [SerializeField]
    private float breathLineDuration = 0.9f;

    [SerializeField]
    private float cannotBreatheLineDuration = 1.1f;
    [SerializeField]
    private float beforeExecutionDelay = 0.3f;

    

    private bool introStarted;
    private bool fearTriggered;


    private void Awake()
    {
        if (animator != null)
        {
            animator.enabled = false;
        }

        if (bossHurtbox != null)
        {
            bossHurtbox.SetActive(false);
        }

        if (fearVolume != null)
        {
            fearVolume.weight = 0f;
        }

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
        StartIntro();
    }


    public void StartIntro()
    {
        if (introStarted)
            return;

        introStarted = true;

        bool firstEncounter =
            GameProgression.Instance != null &&
            GameProgression.Instance.IsFirstEncounter;

        if (firstEncounter)
        {
            LockPlayer();

            StartCoroutine(
                FirstEncounterRoutine()
            );

            return;
        }

        StartCoroutine(
            NormalIntroRoutine()
        );
    }


    // =====================================
    // Normal Battle
    // =====================================

    private IEnumerator NormalIntroRoutine()
    {
        yield return new WaitForSeconds(
            initialHoldDuration
        );

        PlayReady();

        yield return new WaitForSeconds(
            readyDuration
        );

        if (bossHurtbox != null)
        {
            bossHurtbox.SetActive(true);
        }

        bossController?
            .StartBattle();

        Debug.Log(
            "[INTRO] Normal Battle Start"
        );
    }


    // =====================================
    // First Encounter
    // =====================================

    private IEnumerator FirstEncounterRoutine()
    {
        Debug.Log(
            "[FIRST ENCOUNTER] Start"
        );

        yield return new WaitForSeconds(
            initialHoldDuration
        );


        // 1. 처음에는 별것 아닌 것처럼 보임
        if (playerBubble != null)
        {
            yield return playerBubble
                .ShowForSeconds(
                    "생각보다... 평범한데?",
                    firstLineDuration
                );
        }


        yield return new WaitForSeconds(
            beforeReadyDelay
        );


        // 2. 보스가 칼을 뽑음
        PlayReady();


        // 3. Animation Event로
        // Fear가 실행될 때까지 대기
        float elapsed = 0f;

        while (!fearTriggered &&
               elapsed <
               readyDuration + 0.5f)
        {
            elapsed +=
                Time.deltaTime;

            yield return null;
        }


        // Animation Event 누락 방지
        if (!fearTriggered)
        {
            Debug.LogWarning(
                "[FIRST ENCOUNTER] " +
                "Fear Animation Event missing."
            );

            TriggerFearMoment();
        }


        yield return new WaitForSeconds(
            afterFearDelay
        );


        // 4. 공포 상태의 독백
        if (playerBubble != null)
        {
            yield return playerBubble
                .ShowForSeconds(
                    "숨이...",
                    breathLineDuration
                );
        }


        yield return new WaitForSeconds(
            0.1f
        );


        if (playerBubble != null)
        {
            yield return playerBubble
                .ShowForSeconds(
                    "안 쉬어져...",
                    cannotBreatheLineDuration
                );
        }


        Debug.Log(
            "[FIRST ENCOUNTER] " +
            "Fear Dialogue Complete"
        );

        yield return new WaitForSeconds(
            beforeExecutionDelay
        );

        // =====================================
        // Execution
        // =====================================

        BossAttack executionAttack =
            attackSelector?
                .GetAttackByType(
                    BossAttackType.ChargeSlash
                );

        if (executionAttack == null)
        {
            Debug.LogError(
                "[FIRST ENCOUNTER] " +
                "ChargeSlash attack not found."
            );

            yield break;
        }

        Debug.Log(
            "[FIRST ENCOUNTER] Execute ChargeSlash"
        );

        bossAction?
            .ExecuteAttack(
                executionAttack
            );
    }


    private void PlayReady()
    {
        if (animator != null)
        {
            animator.enabled = true;
            animator.speed = 1f;
        }

        bossSFX?
            .PlayStart();

        bossAnimator?
            .PlayReady();
    }


    // =====================================
    // Ready Animation Event
    // =====================================

    public void TriggerFearMoment()
    {
        if (fearTriggered)
            return;

        if (GameProgression.Instance == null ||
            !GameProgression.Instance
                .IsFirstEncounter)
        {
            return;
        }

        fearTriggered = true;

        Debug.Log(
            "[FIRST ENCOUNTER] Fear"
        );

        if (fearVolume != null)
        {
            StartCoroutine(
                FadeFearVolume()
            );
        }
    }


    private IEnumerator FadeFearVolume()
    {
        float startWeight =
            fearVolume.weight;

        float elapsed = 0f;

        while (elapsed <
               fearFadeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    fearFadeDuration
                );

            fearVolume.weight =
                Mathf.Lerp(
                    startWeight,
                    1f,
                    t
                );

            yield return null;
        }

        fearVolume.weight = 1f;
    }


    private void LockPlayer()
    {
        playerMovement?
            .SetInputLocked(true);

        playerInput?
            .DeactivateInput();

        Debug.Log(
            "[FIRST ENCOUNTER] " +
            "Player Input Locked"
        );
    }
}