using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDeathPresentationController
    : MonoBehaviour
{
    public static
        PlayerDeathPresentationController
        Instance
    {
        get;
        private set;
    }


    [Header("Canvas")]
    [SerializeField]
    private RectTransform canvasRect;


    [Header("Black")]
    [SerializeField]
    private CanvasGroup blackOverlay;


    [Header("Silhouette")]
    [SerializeField]
    private Image silhouetteImage;

    [SerializeField]
    private RectTransform silhouetteRect;

    [SerializeField]
    private CanvasGroup silhouetteGroup;


    [Header("Death Text")]
    [SerializeField]
    private TMP_Text deathText;

    [SerializeField]
    private CanvasGroup deathTextGroup;


    [Header("Timing")]
    [SerializeField]
    private float blackFadeDuration =
        0.15f;

    [SerializeField]
    private float silhouetteFadeDuration =
        0.08f;

    [SerializeField]
    private float moveDuration =
        0.35f;

    [SerializeField]
    private float stillDuration =
        0.20f;

    [SerializeField]
    private float textFadeDuration =
        0.20f;

    [SerializeField]
    private float finalHoldDuration =
        0.45f;


    [Header("Silhouette")]
    [SerializeField]
    private float startScale =
        0.75f;

    [SerializeField]
    private float finalScale =
        1.55f;
    [Header("Silhouette Size")]
    [SerializeField]
    private float silhouetteHeight = 220f;

    private Camera targetCamera;

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

        targetCamera =
            Camera.main;

        ResetPresentation();
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }


    public void Play(
        SpriteRenderer sourceRenderer)
    {
        if (isPlaying)
            return;

        if (sourceRenderer == null)
        {
            Debug.LogWarning(
                "[Death Presentation] " +
                "Source SpriteRenderer missing."
            );

            GameFlowController.Instance?
                .CompletePlayerDeathPresentation();

            return;
        }

        StartCoroutine(
            PresentationRoutine(
                sourceRenderer
            )
        );
    }


    private IEnumerator
        PresentationRoutine(
            SpriteRenderer sourceRenderer)
    {
        isPlaying = true;


        // =====================================
        // Copy current player sprite
        // =====================================

        Sprite currentSprite =
            sourceRenderer.sprite;

        if (currentSprite == null)
        {
            Debug.LogError(
                "[Death Presentation] " +
                "Player SpriteRenderer has no sprite."
            );

            GameFlowController.Instance?
                .CompletePlayerDeathPresentation();

            yield break;
        }

        if (silhouetteImage != null)
        {
            silhouetteImage.sprite =
                currentSprite;

            silhouetteImage.type =
                Image.Type.Simple;

            silhouetteImage
                .preserveAspect = true;
        }

        if (silhouetteRect != null)
        {
            float spriteWidth =
                currentSprite.rect.width;

            float spriteHeight =
                currentSprite.rect.height;

            float aspect =
                spriteHeight > 0f
                    ? spriteWidth /
                    spriteHeight
                    : 1f;

            silhouetteRect.sizeDelta =
                new Vector2(
                    silhouetteHeight * aspect,
                    silhouetteHeight
                );
        }


        if (deathText != null)
        {
            deathText.text =
                "사망하였습니다";
        }


        // =====================================
        // Convert Player World Position
        // → UI Position
        // =====================================

        Vector2 startPosition =
            Vector2.zero;

        if (targetCamera == null)
        {
            targetCamera =
                Camera.main;
        }

        if (targetCamera != null &&
            canvasRect != null)
        {
            Vector3 screenPosition =
                targetCamera
                    .WorldToScreenPoint(
                        sourceRenderer
                            .bounds
                            .center
                    );

            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPosition,
                    null,
                    out startPosition
                );
        }


        if (silhouetteRect != null)
        {
            silhouetteRect
                .anchoredPosition =
                startPosition;

            silhouetteRect
                .localScale =
                Vector3.one *
                startScale;
        }


        // 플레이어가 바라보던 방향 유지
        if (silhouetteRect != null)
        {
            float direction =
                Mathf.Sign(
                    sourceRenderer
                        .transform
                        .lossyScale
                        .x
                );

            if (direction == 0f)
                direction = 1f;

            Vector3 scale =
                silhouetteRect.localScale;

            scale.x =
                Mathf.Abs(scale.x) *
                direction;

            silhouetteRect.localScale =
                scale;
        }


        // =====================================
        // Black Fade
        // =====================================

        yield return FadeCanvasGroup(
            blackOverlay,
            0f,
            1f,
            blackFadeDuration
        );


        // 실제 월드 캐릭터는 이제 숨김
        sourceRenderer.enabled = false;


        // =====================================
        // White Silhouette Appears
        // =====================================

        yield return FadeCanvasGroup(
            silhouetteGroup,
            0f,
            1f,
            silhouetteFadeDuration
        );


        // =====================================
        // Move to Center + Zoom
        // =====================================

        Vector2 fromPosition =
            silhouetteRect != null
                ? silhouetteRect
                    .anchoredPosition
                : Vector2.zero;

        Vector3 fromScale =
            silhouetteRect != null
                ? silhouetteRect
                    .localScale
                : Vector3.one;


        float scaleDirection =
            Mathf.Sign(
                fromScale.x
            );

        if (scaleDirection == 0f)
            scaleDirection = 1f;


        Vector3 targetScale =
            new Vector3(
                finalScale *
                scaleDirection,
                finalScale,
                1f
            );
        Vector2 targetPosition =
            new Vector2(
                0f,
                -80f
            );

        float elapsed = 0f;

        while (elapsed <
               moveDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    moveDuration
                );

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            if (silhouetteRect != null)
            {
                silhouetteRect.anchoredPosition =
                    Vector2.Lerp(
                        fromPosition,
                        targetPosition,
                        t
                    );

                silhouetteRect
                    .localScale =
                    Vector3.Lerp(
                        fromScale,
                        targetScale,
                        t
                    );
            }

            yield return null;
        }


        // =====================================
        // Silence
        // =====================================

        yield return WaitRealtime(
            stillDuration
        );


        // =====================================
        // DEATH
        // =====================================

        yield return FadeCanvasGroup(
            deathTextGroup,
            0f,
            1f,
            textFadeDuration
        );


        yield return WaitRealtime(
            finalHoldDuration
        );


        // AfterLife에서 멈추는 상황 방지
        Time.timeScale = 1f;


        GameFlowController.Instance?
            .CompletePlayerDeathPresentation();
    }


    private IEnumerator FadeCanvasGroup(
        CanvasGroup group,
        float from,
        float to,
        float duration)
    {
        if (group == null)
            yield break;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        group.alpha = from;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );

            group.alpha =
                Mathf.Lerp(
                    from,
                    to,
                    t
                );

            yield return null;
        }

        group.alpha = to;
    }


    private IEnumerator WaitRealtime(
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            yield return null;
        }
    }


    private void ResetPresentation()
    {
        if (blackOverlay != null)
            blackOverlay.alpha = 0f;

        if (silhouetteGroup != null)
            silhouetteGroup.alpha = 0f;

        if (deathTextGroup != null)
            deathTextGroup.alpha = 0f;
    }
}