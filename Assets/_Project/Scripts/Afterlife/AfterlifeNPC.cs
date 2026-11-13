using UnityEngine;
using UnityEngine.InputSystem;

public class AfterlifeNPC : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField]
    private AfterlifeDialogueController dialogueController;

    [SerializeField]
    private string speakerName = "???";

    [SerializeField, TextArea(2, 4)]
    private string[] dialogueLines;

    [Header("UI")]
    [SerializeField]
    private GameObject interactPrompt;

    private bool playerInRange;

    private void Start()
    {
        SetPrompt(false);

        if (dialogueController != null)
        {
            dialogueController.OnDialogueEnded +=
                HandleDialogueEnded;
        }
    }

    private void OnDestroy()
    {
        if (dialogueController != null)
        {
            dialogueController.OnDialogueEnded -=
                HandleDialogueEnded;
        }
    }

    private void Update()
    {
        if (!playerInRange)
            return;

        if (dialogueController == null)
            return;

        if (dialogueController.IsOpen)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            StartDialogue();
        }
    }

    private void StartDialogue()
    {
        SetPrompt(false);

        dialogueController.StartDialogue(
            speakerName,
            dialogueLines
        );

        Debug.Log(
            "[Afterlife NPC] Dialogue Start"
        );
    }

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        PlayerMovement playerMovement =
            other.GetComponentInParent<PlayerMovement>();

        if (playerMovement == null)
            return;

        playerInRange = true;

        if (dialogueController == null ||
            !dialogueController.IsOpen)
        {
            SetPrompt(true);
        }
    }

    private void OnTriggerExit2D(
        Collider2D other)
    {
        PlayerMovement playerMovement =
            other.GetComponentInParent<PlayerMovement>();

        if (playerMovement == null)
            return;

        playerInRange = false;

        SetPrompt(false);
    }

    private void HandleDialogueEnded()
    {
        if (playerInRange)
        {
            SetPrompt(true);
        }
    }

    private void SetPrompt(bool visible)
    {
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(
                visible
            );
        }
    }
}