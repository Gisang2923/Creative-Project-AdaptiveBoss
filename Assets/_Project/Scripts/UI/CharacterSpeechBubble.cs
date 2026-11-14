using System.Collections;
using TMPro;
using UnityEngine;

public class CharacterSpeechBubble : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private RectTransform bubbleRoot;

    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private TMP_Text dialogueText;

    [SerializeField]
    private Transform target;

    [SerializeField]
    private Camera targetCamera;


    [Header("Position")]
    [SerializeField]
    private Vector3 worldOffset =
        new Vector3(0f, 2.2f, 0f);


    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        HideImmediate();
    }


    private void LateUpdate()
    {
        if (target == null)
            return;

        if (targetCamera == null)
        {
            targetCamera = Camera.main;

            if (targetCamera == null)
                return;
        }

        if (bubbleRoot == null)
            return;

        Vector3 worldPosition =
            target.position +
            worldOffset;

        Vector3 screenPosition =
            targetCamera.WorldToScreenPoint(
                worldPosition
            );

        bubbleRoot.position =
            screenPosition;
    }


    public void SetTarget(
        Transform newTarget)
    {
        target = newTarget;
    }


    public void Show(
        string line)
    {
        if (dialogueText != null)
        {
            dialogueText.text = line;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }


    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }


    public void HideImmediate()
    {
        if (dialogueText != null)
        {
            dialogueText.text = "";
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }


    public IEnumerator ShowForSeconds(
        string line,
        float duration)
    {
        Show(line);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            yield return null;
        }

        Hide();
    }
}