using UnityEngine;
using UnityEngine.InputSystem;

public class AfterlifeNPC : MonoBehaviour
{
    private enum NpcState
    {
        FirstDialogue,
        Growth,
        Farewell,

        ClearConfusion,
        ClearNpcResponse,
        ReturnAttempt,
        ReturnFailed,

        Finished
    }
    [Header("Systems")]
    [SerializeField]
    private AfterlifeDialogueController
        dialogueController;

    [SerializeField]
    private AfterlifeGrowthUI
        growthUI;
    [SerializeField]
    private AfterlifeReturnAttemptController
        returnAttemptController;
    [Header("Dialogue")]
    [SerializeField]
    private string speakerName = "???";

    [SerializeField, TextArea(2, 4)]
    private string[] firstDialogueLines;

    [SerializeField, TextArea(2, 4)]
    private string[] farewellDialogueLines;

    [Header("Act1 Clear Event")]
    [SerializeField]
    private string playerSpeakerName =
        "나";

    [SerializeField, TextArea(2, 4)]
    private string[] clearPlayerLines =
    {
        "왜... 다시 여기로 온 거죠?",
        "분명 그자를 쓰러뜨렸는데."
    };

    [SerializeField, TextArea(2, 4)]
    private string[] clearNpcLines =
    {
        "이런 적은 없었는데...",
        "어째서 이러는 거지?",
        "잠시 기다리게.",
        "내가 정상적으로 돌아갈 수 있도록 해보겠네."
    };

    [SerializeField, TextArea(2, 4)]
    private string[] returnFailedLines =
    {
        "...안 되는군.",
        "아무래도 다시 한 번 그놈을 처리해야 할 것 같아.",
        "그대에게는 어려운 일은 아니겠지?"
    };

    [Header("UI")]
    [SerializeField]
    private GameObject interactPrompt;

    private PlayerMovement
        currentPlayerMovement;

    private bool playerInRange;

    private NpcState state =
        NpcState.FirstDialogue;

    private void Start()
    {
        GameProgression progression =
            GameProgression.Instance;

        if (progression != null &&
            progression.IsAct1Cleared)
        {
            state =
                NpcState.ClearConfusion;
        }
        SetPrompt(false);

        if (dialogueController != null)
        {
            dialogueController
                .OnDialogueEnded +=
                HandleDialogueEnded;
        }

        if (growthUI != null)
        {
            growthUI.OnGrowthClosed +=
                HandleGrowthClosed;
        }
    }

    private void OnDestroy()
    {
        if (dialogueController != null)
        {
            dialogueController
                .OnDialogueEnded -=
                HandleDialogueEnded;
        }

        if (growthUI != null)
        {
            growthUI.OnGrowthClosed -=
                HandleGrowthClosed;
        }
    }

    private void Update()
    {
        if (!playerInRange)
            return;

        if (state ==
                NpcState.Growth ||
            state ==
                NpcState.ReturnAttempt ||
            state ==
                NpcState.Finished)
        {
            return;
        }

        if (state ==
            NpcState.Finished)
        {
            return;
        }

        if (dialogueController == null)
            return;

        if (dialogueController.IsOpen)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current
            .eKey.wasPressedThisFrame)
        {
            StartCurrentDialogue();
        }
    }

    private void StartCurrentDialogue()
    {
        SetPrompt(false);

        currentPlayerMovement?
            .SetInputLocked(true);

        switch (state)
        {
            case NpcState.FirstDialogue:

                dialogueController
                    .StartDialogue(
                        speakerName,
                        firstDialogueLines
                    );

                break;


            case NpcState.Farewell:

                dialogueController
                    .StartDialogue(
                        speakerName,
                        farewellDialogueLines
                    );

                break;


            case NpcState.ClearConfusion:

                dialogueController
                    .StartDialogue(
                        playerSpeakerName,
                        clearPlayerLines
                    );

                break;


            case NpcState.ClearNpcResponse:

                dialogueController
                    .StartDialogue(
                        speakerName,
                        clearNpcLines
                    );

                break;


            case NpcState.ReturnFailed:

                dialogueController
                    .StartDialogue(
                        speakerName,
                        returnFailedLines
                    );

                break;
        }
    }

    private void HandleDialogueEnded()
    {
        // =====================================
        // Act1 Cleared Special Route
        // =====================================

        if (state ==
            NpcState.ClearConfusion)
        {
            state =
                NpcState.ClearNpcResponse;

            StartCurrentDialogue();

            return;
        }


        if (state ==
            NpcState.ClearNpcResponse)
        {
            state =
                NpcState.ReturnAttempt;

            returnAttemptController?
                .PlayAttempt(
                    HandleReturnAttemptFailed
                );

            return;
        }


        if (state ==
            NpcState.ReturnFailed)
        {
            state =
                NpcState.Finished;

            SetPrompt(false);

            GameProgression.Instance?
                .BeginAct2Adaptive();

            GameFlowController.Instance?
                .ReturnToBattle();

            return;
        }


        // =====================================
        // Normal Death Route
        // =====================================

        if (state ==
            NpcState.FirstDialogue)
        {
            GameProgression progression =
                GameProgression.Instance;

            progression?
                .ClaimGrowthPoint();

            state =
                NpcState.Growth;

            growthUI?.Open(
                currentPlayerMovement
            );

            return;
        }


        if (state ==
            NpcState.Farewell)
        {
            state =
                NpcState.Finished;

            SetPrompt(false);

            GameFlowController.Instance?
                .ReturnToBattle();
        }
    }
    private void HandleReturnAttemptFailed()
    {
        if (state !=
            NpcState.ReturnAttempt)
        {
            return;
        }

        Debug.Log(
            "[Afterlife] " +
            "Return Attempt Failed"
        );

        state =
            NpcState.ReturnFailed;

        StartCurrentDialogue();
    }
    private void HandleGrowthClosed()
    {
        state =
            NpcState.Farewell;

        if (playerInRange)
        {
            SetPrompt(true);
        }
    }

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        PlayerMovement movement =
            other.GetComponentInParent<
                PlayerMovement>();

        if (movement == null)
            return;

        currentPlayerMovement =
            movement;

        playerInRange = true;

        if (state ==
                NpcState.FirstDialogue ||
            state ==
                NpcState.Farewell ||
            state ==
                NpcState.ClearConfusion)
        {
            SetPrompt(true);
        }
    }

    private void OnTriggerExit2D(
        Collider2D other)
    {
        PlayerMovement movement =
            other.GetComponentInParent<
                PlayerMovement>();

        if (movement == null)
            return;

        if (movement !=
            currentPlayerMovement)
        {
            return;
        }

        playerInRange = false;

        SetPrompt(false);
    }

    private void SetPrompt(
        bool visible)
    {
        interactPrompt?
            .SetActive(visible);
    }
}