using System.Collections;
using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    private Coroutine hitStopRoutine;

    private float restoreTimeScale = 1f;
    private float stopUntil;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Play(float duration)
    {
        if (duration <= 0f)
            return;

        float requestedEnd =
            Time.unscaledTime + duration;

        // 이미 Hit Stop 중이면
        // 더 긴 쪽까지만 연장
        if (hitStopRoutine != null)
        {
            stopUntil =
                Mathf.Max(
                    stopUntil,
                    requestedEnd
                );

            return;
        }

        restoreTimeScale =
            Time.timeScale;

        stopUntil =
            requestedEnd;

        hitStopRoutine =
            StartCoroutine(
                HitStopRoutine()
            );
    }

    private IEnumerator HitStopRoutine()
    {
        Time.timeScale = 0f;

        while (Time.unscaledTime <
               stopUntil)
        {
            yield return null;
        }

        Time.timeScale =
            restoreTimeScale;

        hitStopRoutine = null;
    }

    private void OnDisable()
    {
        if (hitStopRoutine == null)
            return;

        Time.timeScale =
            restoreTimeScale;

        hitStopRoutine = null;
    }
}