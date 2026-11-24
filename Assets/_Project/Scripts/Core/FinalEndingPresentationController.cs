using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class FinalEndingPresentationController : MonoBehaviour
{
    [Serializable]
    private class FinalDialogueBeat
    {
        [SerializeField] private string speakerName;
        [SerializeField, TextArea(2, 5)] private string[] lines;
        [SerializeField, Min(0f)] private float pauseBefore;
        [SerializeField, Min(0f)] private float pauseAfter;

        public string SpeakerName => speakerName;
        public string[] Lines => lines;
        public float PauseBefore => pauseBefore;
        public float PauseAfter => pauseAfter;

        public FinalDialogueBeat(
            string speakerName,
            float pauseAfter,
            params string[] lines)
        {
            this.speakerName = speakerName;
            this.lines = lines;
            this.pauseAfter = pauseAfter;
        }
    }

    [Header("Battle References")]
    [SerializeField] private BossAnimator bossAnimator;
    [SerializeField] private BossDamageReceiver bossDamageReceiver;
    [SerializeField] private AdaptivePresentationController adaptivePresentation;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private AudioSource battleBgmSource;

    [Header("Dialogue")]
    [SerializeField] private AfterlifeDialogueController dialogueController;
    [SerializeField, Min(0f)] private float finalKneelDelay = 0.2f;
    [SerializeField] private FinalDialogueBeat[] finalDialogueBeats =
        CreateDefaultFinalDialogue();

    private static FinalDialogueBeat[] CreateDefaultFinalDialogue()
    {
        return new[]
        {
            new FinalDialogueBeat("보스", 1f, "마침내…"),
            new FinalDialogueBeat("나", 0f, "……"),
            new FinalDialogueBeat("보스", 0f, "끝까지 왔군."),
            new FinalDialogueBeat("나", 0f, "당신을 쓰러뜨리면 끝나는 거 아닌가."),
            new FinalDialogueBeat("보스", 0f, "나도 그렇게 생각했다."),
            new FinalDialogueBeat("나", 0f, "…무슨 뜻이지?"),
            new FinalDialogueBeat("보스", 0f,
                "나도 처음부터 이곳에 있던 건 아니야. 자네처럼 이곳에 들어왔고, 앞을 막는 자와 싸웠다. 쓰러지고 다시 돌아오기를 몇 번이고 반복했지."),
            new FinalDialogueBeat("나", 0f, "당신도… 도전자였다는 건가?"),
            new FinalDialogueBeat("보스", 0f,
                "그래. 나도 종결을 앞두고 있었다. 그 앞을 막고 있던 자를 쓰러뜨리면 이 모든 것이 끝날 거라 생각했지."),
            new FinalDialogueBeat("나", 0f, "그런데 왜 당신이 여기 있지?"),
            new FinalDialogueBeat("보스", 0f,
                "그 전에 자네도 느끼지 않았나. 싸울수록 내가 자네의 움직임을 알고 있다는 걸."),
            new FinalDialogueBeat("나", 0f, "……"),
            new FinalDialogueBeat("보스", 0f,
                "죽는다고 모든 것이 처음으로 돌아가는 건 아니야. 몸은 돌아오지만, 그곳에서 있었던 일까지 사라지는 것은 아니지. 자네가 어떻게 싸웠는지, 어디서 피했고 어떤 순간에 검을 휘둘렀는지. 그 흔적은 이곳에 남는다."),
            new FinalDialogueBeat("나", 0f, "그래서 내 움직임을…"),
            new FinalDialogueBeat("보스", 0f, "자네가 나를 배운 만큼, 나도 자네를 배웠다."),
            new FinalDialogueBeat("나", 0f, "죽어도 다시 돌아오는 것이 이 세계의 법칙인가?"),
            new FinalDialogueBeat("보스", 0f, "그래. 이 세계에 들어온 자에게는 하나의 맹세가 주어진다."),
            new FinalDialogueBeat("나", 0f, "맹세?"),
            new FinalDialogueBeat("보스", 0f,
                "끝에 도달할 때까지 다시 돌아오게 만드는 것. 쓰러져도 돌아오고, 길을 잃어도 다시 끝을 향하게 하지. 자네도 그 맹세로 인해 지금까지 이곳으로 돌아온 거다."),
            new FinalDialogueBeat("나", 0f, "……"),
            new FinalDialogueBeat("보스", 0f,
                "나도 그랬다. 몇 번을 죽어도 돌아왔고, 결국 내 앞의 자를 쓰러뜨려 종결을 마주했지."),
            new FinalDialogueBeat("나", 0f, "그럼 끝난 거 아닌가?"),
            new FinalDialogueBeat("보스", 0f,
                "나도 그렇게 믿었다. 하지만 종결을 보려던 순간… 돌아갈 수 없었다."),
            new FinalDialogueBeat("나", 0f, "왜?"),
            new FinalDialogueBeat("보스", 0f,
                "그제야 알았지. 맹세의 목적은 이곳에서 나가는 것이 아니었다. 끝에 도달하는 것. 그게 전부였던 거다. 그리고 내가 마침내 끝에 도달했을 때, 더 이상 향해야 할 곳은 없었다."),
            new FinalDialogueBeat("나", 0f, "……"),
            new FinalDialogueBeat("보스", 0f,
                "그래서 맹세는 나를 끝에 남겼다. 내가 쓰러뜨린 자가 있던 자리에."),
            new FinalDialogueBeat("나", 0f, "…그래서 당신이 종결인가?"),
            new FinalDialogueBeat("보스", 0f,
                "지금은. 종결은 누군가의 이름이 아니다. 이 세계의 끝에 도달한 자가 맡게 되는 자리일 뿐이지."),
            new FinalDialogueBeat("나", 0.6f,
                "잠깐. 당신이 그 자리에 남았다면… 내가 당신을 쓰러뜨리면…"),
            new FinalDialogueBeat("보스", 0f, "……"),
            new FinalDialogueBeat("나", 0f, "아니. 나는 돌아갈 거야."),
            new FinalDialogueBeat("보스", 1.5f, "나도 그러고 싶었다."),
            new FinalDialogueBeat("나", 0f, "……"),
            new FinalDialogueBeat("보스", 0f,
                "하지만 맹세는 끝에 도달한 자를 돌려보내지 않았어. 끝에 도달하기 위해 존재하던 맹세가 그 순간 나를 이곳에 남겼지. 다음으로 끝까지 오는 자가 나타날 때까지."),
            new FinalDialogueBeat("나", 0f, "그래서… 처음에 말한 「마침내」가…"),
            new FinalDialogueBeat("보스", 0f,
                "그래. 끝까지 오는 자를 기다렸다. 그리고 자네가 여기까지 왔지."),
            new FinalDialogueBeat("나", 0f, "……"),
            new FinalDialogueBeat("보스", 0f, "이제 선택하게. 여기서 멈추든, 끝을 보든.")
        };
    }

#if UNITY_EDITOR
    [ContextMenu("Apply Default Final Dialogue")]
    private void ApplyDefaultFinalDialogue()
    {
        UnityEditor.Undo.RecordObject(this, "Apply Default Final Dialogue");
        finalDialogueBeats = CreateDefaultFinalDialogue();

        UnityEditor.EditorUtility.SetDirty(this);

        if (!Application.isPlaying && gameObject.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager
                .MarkSceneDirty(gameObject.scene);
        }
    }
#endif

    [Header("Final Blow Prompt")]
    [SerializeField] private GameObject finalBlowPromptRoot;
    [SerializeField] private TMP_Text finalBlowPromptLabel;
    [SerializeField] private string finalBlowPromptText =
        "[E] 최후의 일격";

    [Header("Final Blow")]
    [SerializeField, Min(0f)] private float finalStrikeImpactDelay = 0.35f;
    [SerializeField, Min(0f)] private float bossDeathDelayAfterStrike = 0.4f;
    [SerializeField, Min(0f)] private float bossDeathAnimationFallbackDuration = 3f;
    [SerializeField] private string finalDeathSpeakerName = "보스";
    [SerializeField, TextArea(2, 5)] private string[] finalDeathLines =
    {
        "그 맹세를…",
        "끝까지 포기하지 않는가.",
        "참으로 가련하군.",
        "그럼에도 끝을 선택한 젊은 대적자여.",
        "경의를 표하지."
    };

    [Header("Combat UI")]
    [SerializeField] private GameObject combatUiRoot;
    [SerializeField, HideInInspector, Min(0f)] private float controlReturnDelay = 0.8f;
    [SerializeField, HideInInspector, Min(0f)] private float epilogueFreeMoveDuration = 12f;

    [Header("Player Silhouette")]
    [SerializeField] private SpriteRenderer playerVisualRenderer;
    [SerializeField, Min(0f)] private float playerSilhouetteDelay = 3f;
    [SerializeField, Min(0f)] private float playerSilhouetteDuration = 1.5f;
    [SerializeField, Min(0f)] private float silhouetteHoldDuration = 1f;

    [Header("Ending Fade")]
    [SerializeField] private CanvasGroup endingFade;
    [SerializeField, Min(0f)] private float endingFadeDuration = 1.5f;
    [SerializeField, Min(0f)] private float blackScreenHoldDuration = 1.5f;

    private PlayerAnimator playerAnimator;
    private PlayerCombat playerCombat;
    private PlayerDash playerDash;
    private PlayerHeal playerHeal;
    private bool finaleStarted;
    private bool dialogueEnded;
    private bool dialogueSubscribed;
    private bool finalBlowStarted;
    private bool titleTransitionStarted;
    private int promptShownFrame;
    private Color playerOriginalColor;

    public bool AwaitingFinalBlow { get; private set; }

    private void Awake()
    {
        if (bossDamageReceiver == null && bossAnimator != null)
            bossDamageReceiver = bossAnimator.GetComponent<BossDamageReceiver>();

        if (playerMovement != null)
        {
            if (playerInput == null)
                playerInput = playerMovement.GetComponent<PlayerInput>();

            playerCombat = playerMovement.GetComponent<PlayerCombat>();
            playerDash = playerMovement.GetComponent<PlayerDash>();
            playerHeal = playerMovement.GetComponent<PlayerHeal>();
            playerAnimator = playerMovement.GetComponent<PlayerAnimator>();

            if (playerVisualRenderer == null)
            {
                Transform visual = playerMovement.transform
                    .Find("FacingRoot/Visual");

                if (visual != null)
                    playerVisualRenderer = visual.GetComponent<SpriteRenderer>();
            }
        }

        if (finalBlowPromptRoot != null)
            finalBlowPromptRoot.SetActive(false);

        if (endingFade != null)
        {
            endingFade.alpha = 0f;
            endingFade.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (!AwaitingFinalBlow || finalBlowStarted ||
            Time.frameCount <= promptShownFrame ||
            Keyboard.current == null ||
            !Keyboard.current.eKey.wasPressedThisFrame)
            return;

        finalBlowStarted = true;
        AwaitingFinalBlow = false;

        if (finalBlowPromptRoot != null)
            finalBlowPromptRoot.SetActive(false);

        StartCoroutine(FinalBlowRoutine());
    }

    private void OnDisable()
    {
        UnsubscribeDialogue();
        playerAnimator?.ClearFinalStrikePresentation();

        if (finaleStarted)
        {
            if (HitStopManager.Instance != null)
                HitStopManager.Instance.RestoreNormalSpeedAfterCurrentStop();
            else
                Time.timeScale = 1f;
        }
    }

    public void BeginFinale()
    {
        if (finaleStarted ||
            GameProgression.Instance == null ||
            GameProgression.Instance.CurrentPhase != GamePhase.Act2Adaptive)
            return;

        finaleStarted = true;
        AwaitingFinalBlow = false;

        adaptivePresentation?.StopForFinale();
        playerCombat?.ForceCancelAttack();
        playerDash?.ForceCancelDash();
        playerHeal?.ForceCancelHeal();
        playerMovement?.SetInputLocked(true);
        playerInput?.DeactivateInput();

        if (playerMovement != null && bossAnimator != null)
        {
            playerMovement.ForceFacing(
                bossAnimator.transform.position.x -
                playerMovement.transform.position.x
            );
        }

        if (battleBgmSource != null)
        {
            battleBgmSource.Stop();
        }

        if (finalBlowPromptRoot != null)
            finalBlowPromptRoot.SetActive(false);

        StartCoroutine(FinaleRoutine());
    }

    private IEnumerator FinaleRoutine()
    {
        // The lethal hit's existing HitStop starts after Health.OnDied returns.
        yield return null;

        while (Time.timeScale <= 0.01f)
            yield return null;

        // An interrupted adaptive presentation may have left slow motion active.
        if (HitStopManager.Instance != null)
            HitStopManager.Instance.RestoreNormalSpeedAfterCurrentStop();
        else
            Time.timeScale = 1f;

        yield return WaitUnscaled(finalKneelDelay);

        if (bossAnimator != null && !bossAnimator.TryPlayFinalKneel())
            Debug.LogWarning("[Final Ending] FinalKneel state is missing.");

        if (dialogueController == null ||
            finalDialogueBeats == null ||
            finalDialogueBeats.Length == 0)
        {
            Debug.LogError("[Final Ending] Dialogue is not configured.");
            yield break;
        }

        foreach (FinalDialogueBeat beat in finalDialogueBeats)
        {
            if (beat == null)
                continue;

            yield return WaitUnscaled(beat.PauseBefore);

            if (beat.Lines != null && beat.Lines.Length > 0)
            {
                if (dialogueController.IsOpen)
                {
                    Debug.LogError("[Final Ending] Dialogue is already open.");
                    yield break;
                }

                dialogueEnded = false;
                dialogueController.OnDialogueEnded += HandleDialogueEnded;
                dialogueSubscribed = true;
                dialogueController.StartDialogue(beat.SpeakerName, beat.Lines);

                while (!dialogueEnded)
                    yield return null;

                UnsubscribeDialogue();
            }

            yield return WaitUnscaled(beat.PauseAfter);
        }

        if (finalBlowPromptLabel != null)
            finalBlowPromptLabel.text = finalBlowPromptText;

        if (finalBlowPromptRoot != null)
            finalBlowPromptRoot.SetActive(true);
        else
            Debug.LogError("[Final Ending] Final Blow prompt is missing.");

        AwaitingFinalBlow = true;
        promptShownFrame = Time.frameCount;
    }

    private IEnumerator FinalBlowRoutine()
    {
        if (playerMovement != null && bossAnimator != null)
        {
            playerMovement.ForceFacing(
                bossAnimator.transform.position.x -
                playerMovement.transform.position.x
            );
        }

        if (playerAnimator == null)
            Debug.LogWarning("[Final Ending] PlayerAnimator is missing.");
        else if (!playerAnimator.TryPlayFinalStrike())
            Debug.LogWarning("[Final Ending] FinalStrike missing; using LightAtk.");

        yield return WaitUnscaled(finalStrikeImpactDelay);

        if (dialogueController != null &&
            finalDeathLines != null &&
            finalDeathLines.Length > 0)
        {
            if (dialogueController.IsOpen)
            {
                Debug.LogError("[Final Ending] Final death dialogue is already open.");
                yield break;
            }

            dialogueEnded = false;
            dialogueController.OnDialogueEnded += HandleDialogueEnded;
            dialogueSubscribed = true;
            dialogueController.StartDialogue(
                finalDeathSpeakerName,
                finalDeathLines
            );

            while (!dialogueEnded)
                yield return null;

            UnsubscribeDialogue();
        }

        yield return WaitUnscaled(bossDeathDelayAfterStrike);

        if (bossDamageReceiver == null ||
            !bossDamageReceiver.CompleteFinalDeath())
        {
            Debug.LogError("[Final Ending] Boss final death could not complete.");
            yield break;
        }

        float elapsed = 0f;
        while (!bossDamageReceiver.FinalDeathAnimationEnded &&
               elapsed < bossDeathAnimationFallbackDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!bossDamageReceiver.FinalDeathAnimationEnded)
            Debug.LogWarning("[Final Ending] Death animation event was missing.");

        if (combatUiRoot != null)
            combatUiRoot.SetActive(false);

        playerAnimator?.ClearFinalStrikePresentation();

        yield return WaitUnscaled(playerSilhouetteDelay);
        yield return FadePlayerToSilhouette();
        yield return WaitUnscaled(silhouetteHoldDuration);

        yield return FadeToBlack();
        yield return WaitUnscaled(blackScreenHoldDuration);

        if (titleTransitionStarted)
            yield break;

        titleTransitionStarted = true;
        SceneManager.LoadScene("TitleScreen");
    }

    private IEnumerator FadePlayerToSilhouette()
    {
        if (playerVisualRenderer == null)
        {
            Debug.LogWarning("[Final Ending] Player Visual SpriteRenderer is missing.");
            yield break;
        }

        playerOriginalColor = playerVisualRenderer.color;
        Color targetColor = new Color(0f, 0f, 0f, playerOriginalColor.a);

        float elapsed = 0f;
        while (playerVisualRenderer != null &&
               elapsed < playerSilhouetteDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / playerSilhouetteDuration)
            );

            playerVisualRenderer.color = Color.Lerp(
                playerOriginalColor,
                targetColor,
                t
            );

            yield return null;
        }

        if (playerVisualRenderer != null)
            playerVisualRenderer.color = targetColor;
    }

    private IEnumerator FadeToBlack()
    {
        if (endingFade == null)
        {
            Debug.LogWarning("[Final Ending] Ending fade is missing.");
            yield break;
        }

        endingFade.alpha = 0f;
        endingFade.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < endingFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            endingFade.alpha = Mathf.Clamp01(
                elapsed / endingFadeDuration
            );
            yield return null;
        }

        endingFade.alpha = 1f;
    }

    private IEnumerator WaitUnscaled(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void HandleDialogueEnded()
    {
        dialogueEnded = true;
    }

    private void UnsubscribeDialogue()
    {
        if (!dialogueSubscribed || dialogueController == null)
            return;

        dialogueController.OnDialogueEnded -= HandleDialogueEnded;
        dialogueSubscribed = false;
    }
}
