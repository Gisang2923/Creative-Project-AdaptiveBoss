using UnityEngine;
using UnityEngine.InputSystem;

public class AfterlifeNPC : MonoBehaviour
{
    private enum NpcState
    {
        FirstDialogue,
        Growth,
        Farewell,
        Finished
    }

    [Header("Systems")]
    [SerializeField]
    private AfterlifeDialogueController
        dialogueController;

    [SerializeField]
    private AfterlifeGrowthUI
        growthUI;

    [Header("Dialogue")]
    [SerializeField]
    private string speakerName = "???";

    [SerializeField, TextArea(2, 4)]
    private string[] firstDialogueLines;

    [SerializeField, TextArea(2, 4)]
    private string[] farewellDialogueLines;

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
            NpcState.Growth)
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

        if (state ==
            NpcState.FirstDialogue)
        {
            dialogueController.StartDialogue(
                speakerName,
                firstDialogueLines
            );

            return;
        }

        if (state ==
            NpcState.Farewell)
        {
            dialogueController.StartDialogue(
                speakerName,
                farewellDialogueLines
            );
        }
    }

    private void HandleDialogueEnded()
    {
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
            NpcState.Farewell)
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