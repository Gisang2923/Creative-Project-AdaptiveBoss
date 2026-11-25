using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class AfterlifeDialogueController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private GameObject dialoguePanel;

    [SerializeField]
    private TMP_Text speakerText;

    [SerializeField]
    private TMP_Text dialogueText;

    [Header("Typing")]
    [SerializeField]
    private float charactersPerSecond = 45f;

    private string[] currentLines;
    private int currentLineIndex;

    private Coroutine typingCoroutine;

    private bool isTyping;
    private bool isOpen;

    private int openedFrame;

    public bool IsOpen => isOpen;
    public TMP_Text DialogueText => dialogueText;

    public event Action OnDialogueEnded;

    private void Awake()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (Time.frameCount <= openedFrame)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Advance();
        }
    }

    public void StartDialogue(
        string speakerName,
        string[] lines)
    {
        if (isOpen)
            return;

        if (lines == null ||
            lines.Length == 0)
        {
            return;
        }

        isOpen = true;

        openedFrame =
            Time.frameCount;

        currentLines =
            lines;

        currentLineIndex = 0;

        if (speakerText != null)
        {
            speakerText.text =
                speakerName;
        }

        dialoguePanel?
            .SetActive(true);

        ShowCurrentLine();
    }

    private void Advance()
    {
        // 출력 중 E:
        // 현재 문장 즉시 완성
        if (isTyping)
        {
            CompleteCurrentLine();
            return;
        }

        // 출력 완료 후 E:
        // 다음 문장
        currentLineIndex++;

        if (currentLineIndex >=
            currentLines.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(
                typingCoroutine
            );
        }

        typingCoroutine =
            StartCoroutine(
                TypeLine(
                    currentLines[
                        currentLineIndex
                    ]
                )
            );
    }

    private IEnumerator TypeLine(
        string line)
    {
        isTyping = true;

        dialogueText.text = "";

        float delay =
            1f /
            Mathf.Max(
                1f,
                charactersPerSecond
            );

        foreach (char character in line)
        {
            dialogueText.text +=
                character;

            yield return
                new WaitForSeconds(delay);
        }

        isTyping = false;

        typingCoroutine = null;
    }

    private void CompleteCurrentLine()
    {
        if (!isTyping)
            return;

        if (typingCoroutine != null)
        {
            StopCoroutine(
                typingCoroutine
            );

            typingCoroutine = null;
        }

        dialogueText.text =
            currentLines[
                currentLineIndex
            ];

        isTyping = false;
    }

    private void EndDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(
                typingCoroutine
            );

            typingCoroutine = null;
        }

        isTyping = false;
        isOpen = false;

        dialoguePanel?
            .SetActive(false);

        OnDialogueEnded?
            .Invoke();

        Debug.Log(
            "[Afterlife Dialogue] End"
        );
    }
}