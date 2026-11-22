using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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
    {
        new FinalDialogueBeat("보스", 1f, "마침내…"),
        new FinalDialogueBeat("나", 0f, "…뭐가 말이지?"),
        new FinalDialogueBeat("보스", 0f, "끝까지 왔군."),
        new FinalDialogueBeat("나", 0f, "끝?", "당신을 쓰러뜨리면 끝나는 거 아닌가."),
        new FinalDialogueBeat("보스", 0f, "나도 그렇게 생각했어."),
        new FinalDialogueBeat("나", 0f, "…무슨 뜻이지?"),
        new FinalDialogueBeat("보스", 0f, "나도 자네와 같았으니까."),
        new FinalDialogueBeat("나", 0f, "나와 같았다고?"),
        new FinalDialogueBeat("보스", 0f,
            "이곳에 도착했고.", "끝에 있는 자와 싸웠지.",
            "죽고.", "돌아오고.", "또 싸웠어."),
        new FinalDialogueBeat("나", 0f, "…당신도 도전자였다는 건가?"),
        new FinalDialogueBeat("보스", 0f, "그래."),
        new FinalDialogueBeat("나", 0f, "그럼 당신이 쓰러뜨린 자는?"),
        new FinalDialogueBeat("보스", 0f,
            "지금의 나와 같은 사람이었지.",
            "아마 그 사람 전에도 누군가 있었을 거다."),
        new FinalDialogueBeat("나", 0f,
            "그렇다면…", "내 움직임을 알고 있었던 것도 그 때문인가?"),
        new FinalDialogueBeat("보스", 0f, "그건 자네 때문이지."),
        new FinalDialogueBeat("나", 0f, "나?"),
        new FinalDialogueBeat("보스", 0f,
            "죽어서 돌아올 때마다.",
            "모든 것이 자네와 함께 돌아갔다고 생각했나?",
            "아니.", "나를 끝내겠다는 의지.",
            "다시 일어서게 한 그 의지는 자네에게 돌아갔다.",
            "하지만 자네가 어떻게 싸웠는지는…", "이곳에 남았어."),
        new FinalDialogueBeat("나", 0f, "그래서 내 공격을…"),
        new FinalDialogueBeat("보스", 0f,
            "자네가 나를 배운 만큼.", "나도 자네를 배웠다."),
        new FinalDialogueBeat("나", 0f,
            "그럼 나를 계속 이곳으로 돌려보내 준 사람은?"),
        new FinalDialogueBeat("보스", 0f, "…아직 그곳에 있나."),
        new FinalDialogueBeat("나", 0f, "알고 있는 사람이야?"),
        new FinalDialogueBeat("보스", 0f,
            "오래전에 나와 싸웠던 사람이지.",
            "몇 번을 쓰러뜨렸는지는 기억나지 않아."),
        new FinalDialogueBeat("나", 0f, "그런데 왜 지금은 돌아오지 못하지?"),
        new FinalDialogueBeat("보스", 0f, "너무 많이 남겼으니까."),
        new FinalDialogueBeat("나", 0f, "…뭘?"),
        new FinalDialogueBeat("보스", 0f,
            "자기 자신을.", "죽을 때마다 싸움의 흔적만 이곳에 남는 건 아니야.",
            "돌아오는 쪽에서도 조금씩 잃는다.", "처음에는 이름을 잊고.",
            "그다음에는 왜 싸우고 있었는지를 잊지.",
            "그래도 맹세를 기억하는 동안에는 다시 돌아온다.",
            "하지만 죽음을 반복하다 보면 그 맹세마저 닳아가지.",
            "전장으로 돌아갈 만큼의 이유는 잃었지만.",
            "완전히 놓아버리지도 못한 자는… 그곳에 남게 된다."),
        new FinalDialogueBeat("나", 0f, "맹세?"),
        new FinalDialogueBeat("보스", 0f,
            "끝을 보겠다는 것.", "쓰러져도 다시 이곳에 오겠다는 것.",
            "그게 무엇이든 상관없어.", "자네는 죽었는데도 다시 왔어.",
            "또 죽었는데도 돌아왔고.", "누가 강요한 것도 아닌데 말이야.",
            "매번 자네가 선택한 거다.", "다시 돌아오는 것을.",
            "끝을 보는 것을."),
        new FinalDialogueBeat("나", 0f, "……"),
        new FinalDialogueBeat("보스", 0f, "그게 자네의 맹세였겠지."),
        new FinalDialogueBeat("보스", 0f,
            "나도 그랬어.", "쓰러지면 다시 오면 된다고 생각했지.",
            "실수하면 다음에는 다르게 하면 됐고.",
            "그자를 쓰러뜨리면 돌아갈 수 있을 거라고 생각했어."),
        new FinalDialogueBeat("나", 0f, "그런데?"),
        new FinalDialogueBeat("보스", 0f,
            "쓰러뜨렸지.", "마침내.", "그런데 아무것도 끝나지 않았어."),
        new FinalDialogueBeat("나", 0f, "왜?"),
        new FinalDialogueBeat("보스", 0f,
            "끝까지 가져온 맹세는…", "끝에 도달했다고 사라지는 게 아니었거든.",
            "끝낼 대상이 사라지자.", "그 맹세가 끝 그 자체를 붙들었어.",
            "그리고 그 맹세를 가진 나도 함께 남았지."),
        new FinalDialogueBeat("나", 0f, "…그래서 당신이 종결인가?"),
        new FinalDialogueBeat("보스", 0f,
            "지금은.", "종결은 사람의 이름이 아니야.",
            "하나의 역할일 뿐이지.", "이야기의 끝에 도달했을 때.",
            "그 끝에 남는 자.", "우리는 그걸 종결이라 불렀다."),
        new FinalDialogueBeat("나", 0f,
            "잠깐.", "당신도 이전의 종결을 쓰러뜨렸다고 했지."),
        new FinalDialogueBeat("보스", 0f, "그래."),
        new FinalDialogueBeat("나", 0.6f,
            "그리고 그 순간…", "당신이 이곳에 남게 됐고.",
            "…그럼 내가 당신을 쓰러뜨리면."),
        new FinalDialogueBeat("나", 0f, "아니.", "나는 돌아갈 거야."),
        new FinalDialogueBeat("보스", 1.5f, "나도 그렇게 말했다."),
        new FinalDialogueBeat("나", 0f, "…그럼 이게 어떻게 끝이라는 거지?"),
        new FinalDialogueBeat("보스", 0f,
            "끝은 비어 있을 수 없나 보더군.",
            "나도 오랫동안 그 이유를 생각했어.", "그러다 알게 됐지.",
            "이야기는 스스로 끝나는 게 아니야.", "누군가가 끝을 맡는 거야."),
        new FinalDialogueBeat("나", 0f, "…그래서 아까 마침내라고 한 건가."),
        new FinalDialogueBeat("보스", 0f, "그래."),
        new FinalDialogueBeat("나", 0f, "나를 기다렸던 건가?"),
        new FinalDialogueBeat("보스", 0f,
            "아니.", "끝까지 오는 자를 기다렸다."),
        new FinalDialogueBeat("보스", 0f,
            "이제 선택하게.", "여기서 멈추든, 끝을 보든.")
    };

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

    [Header("Epilogue Pause")]
    [SerializeField] private GameObject combatUiRoot;
    [SerializeField, Min(0f)] private float controlReturnDelay = 0.8f;
    [SerializeField, Min(0f)] private float epilogueFreeMoveDuration = 12f;
    [SerializeField] private CanvasGroup endingFade;
    [SerializeField, Min(0f)] private float endingFadeDuration = 1.5f;

    private PlayerAnimator playerAnimator;
    private PlayerCombat playerCombat;
    private PlayerDash playerDash;
    private PlayerHeal playerHeal;
    private bool finaleStarted;
    private bool dialogueEnded;
    private bool dialogueSubscribed;
    private bool finalBlowStarted;
    private int promptShownFrame;

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

        yield return WaitUnscaled(controlReturnDelay);

        playerAnimator?.ClearFinalStrikePresentation();
        playerMovement?.SetInputLocked(false);
        playerInput?.ActivateInput();

        yield return WaitUnscaled(epilogueFreeMoveDuration);

        playerMovement?.SetInputLocked(true);
        playerInput?.DeactivateInput();

        yield return FadeToBlack();
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
