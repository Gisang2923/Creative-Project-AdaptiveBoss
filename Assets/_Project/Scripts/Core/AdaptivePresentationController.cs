using System.Collections;
using UnityEngine;

public class AdaptivePresentationController
    : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BossAdaptiveDecisionSource adaptiveSource;

    [SerializeField]
    private CameraBossFollow cameraFollow;

    [SerializeField]
    private Camera targetCamera;

    [SerializeField]
    private CharacterSpeechBubble bossBubble;


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
        "또 따라오는군.";

    [SerializeField, TextArea]
    private string chaseCommitDialogue =
        "끝까지 오는군.";

    [SerializeField, TextArea]
    private string retreatDialogue =
        "또 물러나는군.";

    [SerializeField, TextArea]
    private string attackDialogue =
        "또 먼저 움직이는군.";

    [SerializeField, TextArea]
    private string counterDialogue =
        "또 그 타이밍인가.";

    [SerializeField, TextArea]
    private string postPunishDialogue =
        "역시 빈틈을 노리는군.";

    [SerializeField, TextArea]
    private string genericDialogue =
        "역시.";


    private Coroutine presentationRoutine;


    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera =
                Camera.main;
        }

        bossBubble?
            .HideImmediate();
    }


    private void OnEnable()
    {
        if (adaptiveSource != null)
        {
            adaptiveSource
                .OnAdaptiveSuccessPresentationRequested
                += HandleAdaptiveSuccess;
        }
    }


    private void OnDisable()
    {
        if (adaptiveSource != null)
        {
            adaptiveSource
                .OnAdaptiveSuccessPresentationRequested
                -= HandleAdaptiveSuccess;
        }

        cameraFollow?
            .SetAdaptiveFocusWeight(0f);

        bossBubble?
            .Hide();
    }


    private void HandleAdaptiveSuccess(
        AdaptiveHabitType habit)
    {
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
        // 기존 HitStop이 끝날 때까지 기다림
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
        // Boss Speech Bubble
        // =====================================

        bossBubble?
            .Show(dialogue);


        // =====================================
        // Slow Motion
        // =====================================

        Time.timeScale =
            Mathf.Min(
                previousTimeScale,
                slowTimeScale
            );


        // =====================================
        // Zoom + Boss Focus
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


        bossBubble?
            .Hide();


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
                return genericDialogue;
        }
    }
}