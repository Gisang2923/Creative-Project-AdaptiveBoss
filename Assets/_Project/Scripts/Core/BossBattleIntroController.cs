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

    private PlayerAnimator playerAnimator;

    [SerializeField]
    private CharacterSpeechBubble playerBubble;


    [Header("Fear")]
    [SerializeField]
    private Volume fearVolume;

    [SerializeField]
    private float fearFadeDuration = 0.15f;

    [SerializeField]
    private float[] fearWeightsByStage =
    {
        1.00f, 0.60f, 0.30f, 0.00f, 0.00f
    };

    [Header("World Binding Vision")]
    [SerializeField, Range(0f, 1f)] private float stage1VisionRadius = 0.20f;
    [SerializeField, Range(0f, 1f)] private float stage2VisionRadius = 0.38f;
    [SerializeField, Range(0f, 1f)] private float stage1VisionSoftness = 0.55f;
    [SerializeField, Range(0f, 1f)] private float stage2VisionSoftness = 0.65f;
    [SerializeField, Range(0f, 1f)] private float visionOutsideDarkness = 0.92f;
    [SerializeField, Min(0.01f)] private float visionEntryDuration = 0.5f;

    [Header("Injury Vignette")]
    [SerializeField] private Color injuryVignetteColor =
        new Color(0.35f, 0.02f, 0.04f, 1f);
    [SerializeField, Range(0f, 1f)] private float injuryClearRadius = 0.4f;
    [SerializeField, Range(0.01f, 1f)] private float injuryEdgeSoftness = 0.55f;
    [SerializeField, Range(0f, 1f)] private float stage3InjuryStrength = 0.55f;
    [SerializeField, Range(0f, 1f)] private float lowHealthThreshold = 0.3f;
    [SerializeField, Range(0f, 1f)] private float criticalHealthThreshold = 0.15f;
    [SerializeField, Range(0f, 1f)] private float lowHealthStrength = 0.45f;
    [SerializeField, Range(0f, 1f)] private float criticalHealthStrength = 0.68f;
    [SerializeField, Min(0f)] private float criticalPulseSpeed = 1.2f;
    [SerializeField, Range(0f, 0.2f)] private float criticalPulseAmount = 0.04f;
    [SerializeField, Min(0.01f)] private float injuryRecoveryFadeDuration = 0.25f;


    [Header("Normal Intro")]
    [SerializeField]
    private float initialHoldDuration = 1f;

    [SerializeField]
    private float readyDuration = 2.11f;

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
    private float breathLineDuration = 2.2f;

    [SerializeField]
    private float cannotBreatheLineDuration = 1.8f;
    [SerializeField]
    private float beforeExecutionDelay = 0.3f;

    

    private bool introStarted;
    private bool fearTriggered;
    private bool normalBindingApplied;
    private WorldBindingVision bindingVision;
    private AfterlifeWillMessage battleStatusMessage;
    private Coroutine normalFearFade;


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

        if (playerMovement != null)
        {
            playerAnimator =
                playerMovement.GetComponent<PlayerAnimator>();
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

    private void OnDisable()
    {
        playerAnimator?.ResumePresentationAnimation();
        bindingVision?.HideImmediately();
        if (normalFearFade != null)
            StopCoroutine(normalFearFade);
        if (fearVolume != null)
            fearVolume.weight = 0f;
    }

    private void OnDestroy()
    {
        if (bindingVision != null)
            Destroy(bindingVision.transform.parent.gameObject);
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
            FaceBoss();
            StartCoroutine(
                FirstEncounterRoutine()
            );

            return;
        }

        GameProgression progression = GameProgression.Instance;
        if (progression != null &&
            (progression.CurrentPhase == GamePhase.Act2Adaptive ||
             (progression.CurrentPhase == GamePhase.Act1 &&
              progression.FearRecoveryStage >=
                  GameProgression.MaxFearRecoveryStage)))
        {
            EnsureBindingVision()?.Begin(
                progression.FearRecoveryStage, progression.CurrentPhase);
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
        // Boss Ready
        // =====================================

        PlayReady();


        yield return new WaitForSeconds(
            readyDuration
        );

        // The Ready animation event is authoritative; retain a fallback if
        // that event is missing from a variant of the animation.
        if (!normalBindingApplied &&
            progression != null &&
            progression.CurrentPhase == GamePhase.Act1)
        {
            ApplyNormalBinding();
        }


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
    private void ApplyFearRecoveryWeight()
    {
        if (fearVolume == null)
            return;

        GameProgression progression =
            GameProgression.Instance;

        if (progression == null ||
            progression.CurrentPhase != GamePhase.Act1)
        {
            fearVolume.weight = 0f;
            return;
        }

        int stage = Mathf.Clamp(
            progression.FearRecoveryStage,
            0,
            GameProgression.MaxFearRecoveryStage
        );

        if (fearWeightsByStage == null ||
            fearWeightsByStage.Length <= stage)
        {
            Debug.LogWarning(
                "[Fear] Stage weight is not configured."
            );
            fearVolume.weight = stage ==
                GameProgression.MaxFearRecoveryStage ? 0f : 1f;
            return;
        }

        float targetWeight = Mathf.Clamp01(fearWeightsByStage[stage]);
        if (stage == 1 || stage == 2)
            normalFearFade = StartCoroutine(
                FadeNormalFearVolume(targetWeight));
        else
            fearVolume.weight = targetWeight;
    }

    private IEnumerator FadeNormalFearVolume(float targetWeight)
    {
        float startWeight = fearVolume.weight;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, visionEntryDuration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fearVolume.weight = Mathf.Lerp(
                startWeight, targetWeight,
                Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }

        fearVolume.weight = targetWeight;
        normalFearFade = null;
    }

    private void FaceBoss()
    {
        if (playerMovement == null ||
            bossController == null)
        {
            return;
        }

        playerMovement.ForceFacing(
            bossController.transform.position.x -
            playerMovement.transform.position.x
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
                    "생각보다… 평범한데?",
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


        // 4. 구속이 발동한 뒤의 독백
        if (playerBubble != null)
        {
            yield return playerBubble
                .ShowForSeconds(
                    "…어째서 몸이 움직이지 않는 거지?",
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
                    "움직여… 내 몸이잖아.",
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

        playerAnimator?.ResumePresentationAnimation();

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
        GameProgression progression = GameProgression.Instance;
        if (progression == null)
            return;

        if (!progression.IsFirstEncounter)
        {
            if (progression.CurrentPhase == GamePhase.Act1)
                ApplyNormalBinding();
            return;
        }

        if (fearTriggered)
            return;

        fearTriggered = true;
        playerAnimator?.FreezePresentationAnimation();

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

    private void ApplyNormalBinding()
    {
        if (normalBindingApplied)
            return;

        GameProgression progression = GameProgression.Instance;
        if (progression == null || progression.CurrentPhase != GamePhase.Act1)
            return;

        normalBindingApplied = true;
        ApplyFearRecoveryWeight();

        int stage = progression.FearRecoveryStage;
        EnsureBindingVision()?.Begin(stage, progression.CurrentPhase);

        string status = null;
        switch (stage)
        {
            case 1:
                status = "세계의 구속\n본래의 능력이 제한되고 있습니다.\n공격 불가 · 최대 체력 제한 · 시야 제한";
                break;
            case 2:
                status = "세계의 구속\n공격 능력이 회복되었습니다.\n최대 체력 제한 · 시야 제한";
                break;
            case 3:
                status = "세계의 구속\n시야에 대한 구속이 해제되었습니다.\n최대 체력 제한";
                break;
            case 4:
                status = "세계의 구속 해제\n본래의 능력이 모두 회복되었습니다.\n모든 구속 효과 해제";
                break;
        }

        if (status != null)
            GetBattleStatusMessage()?.TryShowText(stage, status);
    }

    private WorldBindingVision EnsureBindingVision()
    {
        if (bindingVision != null)
            return bindingVision;
        if (playerMovement == null)
            return null;

        Health playerHealth = playerMovement.GetComponent<Health>();
        if (playerHealth == null)
        {
            Debug.LogError("[World Binding] Player Health is missing.");
            return null;
        }

        bindingVision = WorldBindingVision.Create();
        if (bindingVision == null)
            return null;

        WorldBindingVision.Settings settings =
            new WorldBindingVision.Settings
            {
                Stage1Radius = stage1VisionRadius,
                Stage2Radius = stage2VisionRadius,
                Stage1Softness = stage1VisionSoftness,
                Stage2Softness = stage2VisionSoftness,
                BindingStrength = visionOutsideDarkness,
                EntryDuration = visionEntryDuration,
                InjuryColor = injuryVignetteColor,
                InjuryRadius = injuryClearRadius,
                InjurySoftness = injuryEdgeSoftness,
                Stage3Strength = stage3InjuryStrength,
                LowHealthThreshold = lowHealthThreshold,
                CriticalHealthThreshold = criticalHealthThreshold,
                LowHealthStrength = lowHealthStrength,
                CriticalHealthStrength = criticalHealthStrength,
                PulseSpeed = criticalPulseSpeed,
                PulseAmount = criticalPulseAmount,
                RecoveryFadeDuration = injuryRecoveryFadeDuration
            };

        bindingVision.Initialize(
            GameProgression.Instance,
            playerHealth,
            playerMovement.transform,
            Camera.main,
            settings);
        return bindingVision;
    }

    public void ShowBindingDefeatFeedback()
    {
        if (GameProgression.Instance == null ||
            GameProgression.Instance.CurrentPhase != GamePhase.Act1)
            return;

        GetBattleStatusMessage()?.TryShowText(100,
            "세계의 구속\n종결 앞에서 몸이 다시 멈춥니다.", 1.5f);
    }

    private AfterlifeWillMessage GetBattleStatusMessage()
    {
        if (battleStatusMessage == null && playerBubble != null)
            battleStatusMessage = AfterlifeWillMessage.GetOrCreate(
                playerBubble.DialogueText);
        return battleStatusMessage;
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
