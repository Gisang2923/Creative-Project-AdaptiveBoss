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
    [SerializeField]
    private CharacterSpeechBubble bossBubble;

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

    [SerializeField]
    private float mentalReactionDuration = 1.2f;

    [Header("Act2 First Intro")]
    [SerializeField]
    private float act2IntroHoldDuration = 0.6f;

    [SerializeField]
    private float act2BossLineDuration = 1.4f;

    [SerializeField, TextArea]
    private string act2BossLine =
        "또 왔군.";
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
        GameProgression progression =
            GameProgression.Instance;


        // =====================================
        // Act2 First Encounter Check
        // =====================================

        bool isFirstAct2Intro =
            progression != null &&
            progression.TryConsumeAct2Intro();


        if (isFirstAct2Intro)
        {
            LockPlayer();

            Debug.Log(
                "[ACT2 INTRO] First Adaptive Encounter"
            );
        }


        // =====================================
        // Initial Hold
        // =====================================

        yield return new WaitForSeconds(
            initialHoldDuration
        );


        // =====================================
        // Act2 First Intro
        // =====================================

        if (isFirstAct2Intro)
        {
            yield return new WaitForSeconds(
                act2IntroHoldDuration
            );


            if (bossBubble != null)
            {
                yield return bossBubble
                    .ShowForSeconds(
                        act2BossLine,
                        act2BossLineDuration
                    );
            }


            yield return new WaitForSeconds(
                0.2f
            );
        }


        // =====================================
        // Mental Growth Reaction
        //
        // Act2 첫 등장에서는 생략
        // =====================================

        if (!isFirstAct2Intro &&
            progression != null &&
            playerBubble != null &&
            progression
                .TryConsumeMentalBattleReaction(
                    out int mentalLevel,
                    out bool isFirstRecovery
                ))
        {
            string reactionLine =
                GetMentalReactionLine(
                    mentalLevel,
                    isFirstRecovery
                );


            if (!string.IsNullOrEmpty(
                    reactionLine))
            {
                yield return playerBubble
                    .ShowForSeconds(
                        reactionLine,
                        mentalReactionDuration
                    );
            }
        }


        // =====================================
        // Boss Ready
        // =====================================

        PlayReady();


        yield return new WaitForSeconds(
            readyDuration
        );


        // =====================================
        // Battle Start
        // =====================================

        if (bossHurtbox != null)
        {
            bossHurtbox.SetActive(true);
        }


        if (isFirstAct2Intro)
        {
            UnlockPlayer();
        }


        bossController?
            .StartBattle();


        Debug.Log(
            "[INTRO] Normal Battle Start"
        );
    }
    private string GetMentalReactionLine(
        int mentalLevel,
        bool isFirstRecovery)
    {
        // 첫 강제 사망 이후
        // 두 번째 조우 전용 독백
        if (isFirstRecovery)
        {
            return
                "움직여... 이번엔 움직일 수 있어.";
        }


        switch (mentalLevel)
        {
            case 2:
                return
                    "호흡이... 조금씩 돌아오고 있어.";

            case 3:
                return
                    "몸이... 더는 굳지 않아.";

            case 4:
                return
                    "선명해... 이제 두렵지 않아.";

            default:
                return null;
        }
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
    private void UnlockPlayer()
    {
        playerMovement?
            .SetInputLocked(false);

        playerInput?
            .ActivateInput();

        Debug.Log(
            "[INTRO] Player Input Unlocked"
        );
    }
}