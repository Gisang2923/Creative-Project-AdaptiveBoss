using System.Collections;
using TMPro;
using UnityEngine;

public class AdaptivePresentationController
    : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BossAdaptiveDecisionSource
        adaptiveSource;

    [SerializeField]
    private CameraBossFollow cameraFollow;

    [SerializeField]
    private Camera targetCamera;

    [SerializeField]
    private TMP_Text dialogueText;


    [Header("Slow Motion")]
    [SerializeField, Range(0.05f, 1f)]
    private float slowTimeScale = 0.3f;


    [Header("Camera")]
    [SerializeField, Range(0f, 1f)]
    private float bossFocusWeight = 0.7f;

    [SerializeField]
    private float zoomAmount = 0.5f;

    [SerializeField]
    private float zoomInDuration = 0.20f;

    [SerializeField]
    private float holdDuration = 0.50f;

    [SerializeField]
    private float returnDuration = 0.25f;


    [Header("Dialogue")]
    [SerializeField, TextArea]
    private string chaseDialogue =
        "You keep chasing me.";

    [SerializeField, TextArea]
    private string chaseCommitDialogue =
        "Still coming after me?";

    [SerializeField, TextArea]
    private string retreatDialogue =
        "You keep backing away.";

    [SerializeField, TextArea]
    private string attackDialogue =
        "So you always strike first.";

    [SerializeField, TextArea]
    private string counterDialogue =
        "Waiting for that timing again?";

    [SerializeField, TextArea]
    private string postPunishDialogue =
        "You never miss an opening.";


    private Coroutine presentationRoutine;


    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera =
                Camera.main;
        }

        if (dialogueText != null)
        {
            dialogueText
                .gameObject
                .SetActive(false);
        }
    }


    private void OnEnable()
    {
        if (adaptiveSource != null)
        {
            adaptiveSource
                .OnPolicyPresentationRequested
                += HandlePolicyPresentation;
        }
    }


    private void OnDisable()
    {
        if (adaptiveSource != null)
        {
            adaptiveSource
                .OnPolicyPresentationRequested
                -= HandlePolicyPresentation;
        }

        cameraFollow?
            .SetAdaptiveFocusWeight(0f);
    }


    private void HandlePolicyPresentation(
        AdaptiveHabitType habit,
        string reason)
    {
        // 이미 연출 중이면 중복 재생하지 않음
        if (presentationRoutine != null)
            return;

        string dialogue =
            GetDialogue(habit);

        presentationRoutine =
            StartCoroutine(
                PresentationRoutine(
                    dialogue
                )
            );
    }


    private IEnumerator PresentationRoutine(
        string dialogue)
    {
        // HitStop 중에 Policy가 발생했다면
        // HitStop을 강제로 풀지 않고 끝날 때까지 기다림
        while (Time.timeScale <= 0.01f)
        {
            yield return null;
        }


        float previousTimeScale =
            Time.timeScale;

        float startCameraSize =
            targetCamera != null
                ? targetCamera.orthographicSize
                : 6f;

        float targetCameraSize =
            Mathf.Max(
                0.1f,
                startCameraSize -
                zoomAmount
            );


        // =====================================
        // Dialogue ON
        // =====================================

        if (dialogueText != null)
        {
            dialogueText.text =
                dialogue;

            dialogueText
                .gameObject
                .SetActive(true);
        }


        // =====================================
        // Slow Motion
        // =====================================

        Time.timeScale =
            Mathf.Min(
                previousTimeScale,
                slowTimeScale
            );


        // =====================================
        // Zoom In + Boss Focus
        // =====================================

        float elapsed = 0f;

        while (elapsed <
               zoomInDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    zoomInDuration
                );

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            cameraFollow?
                .SetAdaptiveFocusWeight(
                    Mathf.Lerp(
                        0f,
                        bossFocusWeight,
                        t
                    )
                );

            if (targetCamera != null)
            {
                targetCamera
                    .orthographicSize =
                    Mathf.Lerp(
                        startCameraSize,
                        targetCameraSize,
                        t
                    );
            }

            yield return null;
        }


        // =====================================
        // Hold
        // =====================================

        elapsed = 0f;

        while (elapsed <
               holdDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            yield return null;
        }


        // =====================================
        // Return
        // =====================================

        elapsed = 0f;

        float returnStartSize =
            targetCamera != null
                ? targetCamera.orthographicSize
                : startCameraSize;

        while (elapsed <
               returnDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    returnDuration
                );

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            cameraFollow?
                .SetAdaptiveFocusWeight(
                    Mathf.Lerp(
                        bossFocusWeight,
                        0f,
                        t
                    )
                );

            if (targetCamera != null)
            {
                targetCamera
                    .orthographicSize =
                    Mathf.Lerp(
                        returnStartSize,
                        startCameraSize,
                        t
                    );
            }

            yield return null;
        }


        cameraFollow?
            .SetAdaptiveFocusWeight(0f);

        if (targetCamera != null)
        {
            targetCamera
                .orthographicSize =
                startCameraSize;
        }


        // =====================================
        // Dialogue OFF
        // =====================================

        if (dialogueText != null)
        {
            dialogueText
                .gameObject
                .SetActive(false);
        }


        // =====================================
        // Normal Speed
        // =====================================

        Time.timeScale =
            previousTimeScale;

        presentationRoutine = null;
    }


    private string GetDialogue(
        AdaptiveHabitType habit)
    {
        switch (habit)
        {
            case AdaptiveHabitType.Chase:
                return chaseDialogue;

            case AdaptiveHabitType.ChaseCommit:
                return chaseCommitDialogue;

            case AdaptiveHabitType.Retreat:
                return retreatDialogue;

            case AdaptiveHabitType.Attack:
                return attackDialogue;

            case AdaptiveHabitType.Counter:
                return counterDialogue;

            case AdaptiveHabitType.PostPunishCommit:
                return postPunishDialogue;

            default:
                return "...";
        }
    }
}