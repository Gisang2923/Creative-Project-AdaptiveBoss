using System;
using System.Collections;
using UnityEngine;

public class AfterlifeReturnAttemptController
    : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private CanvasGroup whiteFlash;


    [Header("Timing")]
    [SerializeField]
    private float chargeDuration =
        0.8f;

    [SerializeField]
    private float holdDuration =
        0.15f;

    [SerializeField]
    private float failureDuration =
        0.45f;


    private bool isPlaying;


    private void Awake()
    {
        if (whiteFlash != null)
        {
            whiteFlash.alpha = 0f;
        }
    }


    public void PlayAttempt(
        Action onFailed)
    {
        if (isPlaying)
            return;

        StartCoroutine(
            AttemptRoutine(
                onFailed
            )
        );
    }


    private IEnumerator AttemptRoutine(
        Action onFailed)
    {
        isPlaying = true;

        // 귀환이 될 것처럼
        // 화면이 점점 하얘짐
        yield return Fade(
            0f,
            0.9f,
            chargeDuration
        );


        yield return WaitRealtime(
            holdDuration
        );


        // 실패:
        // 갑자기 빛이 꺼짐
        if (whiteFlash != null)
        {
            whiteFlash.alpha =
                0.25f;
        }

        yield return WaitRealtime(
            0.08f
        );

        if (whiteFlash != null)
        {
            whiteFlash.alpha =
                0.75f;
        }

        yield return WaitRealtime(
            0.06f
        );


        yield return Fade(
            whiteFlash != null
                ? whiteFlash.alpha
                : 0f,
            0f,
            failureDuration
        );


        isPlaying = false;

        onFailed?
            .Invoke();
    }


    private IEnumerator Fade(
        float from,
        float to,
        float duration)
    {
        if (whiteFlash == null)
            yield break;

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

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            whiteFlash.alpha =
                Mathf.Lerp(
                    from,
                    to,
                    t
                );

            yield return null;
        }

        whiteFlash.alpha = to;
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
}