using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Act1ClearPresentationController
    : MonoBehaviour
{
    public static
        Act1ClearPresentationController
        Instance
    {
        get;
        private set;
    }


    // =====================================
    // UI
    // =====================================

    [Header("Clear UI")]
    [SerializeField]
    private GameObject clearPanel;

    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text messageText;

    [SerializeField]
    private Button confirmButton;


    // =====================================
    // Player
    // =====================================

    [Header("Player")]
    [SerializeField]
    private PlayerMovement playerMovement;

    [SerializeField]
    private PlayerInput playerInput;

    [SerializeField]
    private CharacterSpeechBubble
        playerBubble;


    // =====================================
    // Boss Reveal
    // =====================================

    [Header("Boss Reveal")]
    [SerializeField]
    private BossIdentityPresentationController
        bossIdentity;


    // =====================================
    // Camera
    // =====================================

    [Header("Camera")]
    [SerializeField]
    private CameraBossFollow cameraFollow;

    [SerializeField]
    private Camera targetCamera;

    [SerializeField, Range(0f, 1f)]
    private float bossFocusWeight =
        0.9f;

    [SerializeField]
    private float zoomAmount =
        1.0f;

    [SerializeField]
    private float zoomInDuration =
        0.35f;

    [SerializeField]
    private float bossRevealHoldDuration =
        0.65f;

    [SerializeField]
    private float returnDuration =
        0.35f;


    // =====================================
    // Dialogue
    // =====================================

    [Header("Reveal Dialogue")]
    [SerializeField, TextArea]
    private string revealDialogue =
        "...인간인 건가?";

    [SerializeField]
    private float revealDialogueDuration =
        1.5f;


    // =====================================
    // Clear
    // =====================================

    [Header("Clear Text")]
    [SerializeField]
    private string clearTitle =
        "클리어했습니다.";

    [SerializeField, TextArea]
    private string clearMessage =
        "잠시 후 메인 화면으로 돌아갑니다...";

    [SerializeField]
    private float beforeClearDelay =
        0.25f;


    private bool isPlaying;


    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;


        if (targetCamera == null)
        {
            targetCamera =
                Camera.main;
        }


        clearPanel?
            .SetActive(false);


        confirmButton?
            .onClick
            .AddListener(
                Confirm
            );


        playerBubble?
            .HideImmediate();
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }


        confirmButton?
            .onClick
            .RemoveListener(
                Confirm
            );


        cameraFollow?
            .SetAdaptiveFocusWeight(0f);
    }


    // =====================================
    // Start
    // =====================================

    public void Play()
    {
        if (isPlaying)
            return;


        StartCoroutine(
            PresentationRoutine()
        );
    }


    // =====================================
    // Presentation
    // =====================================

    private IEnumerator
        PresentationRoutine()
    {
        isPlaying = true;


        // 플레이어 조작 완전 잠금
        playerMovement?
            .SetInputLocked(true);

        playerInput?
            .DeactivateInput();


        playerBubble?
            .HideImmediate();


        // =====================================
        // 1. Boss Identity Reveal
        // =====================================

        bossIdentity?
            .ApplyNormal();


        Debug.Log(
            "[Act1 Clear] " +
            "Boss identity revealed."
        );


        float startCameraSize =
            targetCamera != null
                ? targetCamera
                    .orthographicSize
                : 6f;


        float targetCameraSize =
            Mathf.Max(
                0.1f,
                startCameraSize -
                zoomAmount
            );


        // =====================================
        // 2. Camera → Boss
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


        cameraFollow?
            .SetAdaptiveFocusWeight(
                bossFocusWeight
            );


        if (targetCamera != null)
        {
            targetCamera.orthographicSize =
                targetCameraSize;
        }


        // =====================================
        // 3. Silence
        // =====================================

        yield return
            WaitRealtime(
                bossRevealHoldDuration
            );


        // =====================================
        // 4. Player Reaction
        // =====================================

        if (playerBubble != null)
        {
            yield return
                playerBubble
                    .ShowForSeconds(
                        revealDialogue,
                        revealDialogueDuration
                    );
        }
        else
        {
            yield return
                WaitRealtime(
                    revealDialogueDuration
                );
        }


        // =====================================
        // 5. Camera Return
        // =====================================

        elapsed = 0f;


        float returnStartSize =
            targetCamera != null
                ? targetCamera
                    .orthographicSize
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
            targetCamera.orthographicSize =
                startCameraSize;
        }


        // =====================================
        // 6. Fake Ending
        // =====================================

        yield return
            WaitRealtime(
                beforeClearDelay
            );


        ShowFakeClear();
    }


    // =====================================
    // Fake Clear
    // =====================================

    private void ShowFakeClear()
    {
        if (titleText != null)
        {
            titleText.text =
                clearTitle;
        }


        if (messageText != null)
        {
            messageText.text =
                clearMessage;
        }


        if (confirmButton != null)
        {
            confirmButton.interactable =
                true;
        }


        clearPanel?
            .SetActive(true);


        Debug.Log(
            "[Act1 Clear] " +
            "Fake Ending Start"
        );
    }


    private void Confirm()
    {
        if (!isPlaying)
            return;


        if (confirmButton != null)
        {
            confirmButton.interactable =
                false;
        }


        Debug.Log(
            "[Act1 Clear] " +
            "Fake Ending Confirmed"
        );


        GameFlowController.Instance?
            .CompleteAct1ClearPresentation();
    }


    private IEnumerator WaitRealtime(
        float duration)
    {
        float elapsed = 0f;


        while (elapsed <
               duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            yield return null;
        }
    }
}