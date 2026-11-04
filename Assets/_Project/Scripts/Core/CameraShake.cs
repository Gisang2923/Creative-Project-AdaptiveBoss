using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Default Test Settings")]
    [SerializeField] private float testDuration = 0.12f;
    [SerializeField] private float testStrength = 0.12f;

    private Vector3 originalLocalPosition;
    private Coroutine shakeRoutine;
    private Coroutine verticalShakeRoutine;
    public static CameraShake Instance { get; private set; }
    private void Awake()
    {
        Instance = this;

        originalLocalPosition =
            transform.localPosition;
    }
    public void Shake(
        float duration,
        float strength)
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
        }

        transform.localPosition =
            originalLocalPosition;

        shakeRoutine =
            StartCoroutine(
                ShakeRoutine(
                    duration,
                    strength
                )
            );
    }
    public void ShakeVertical(
        float duration,
        float strength)
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(
                shakeRoutine
            );

            shakeRoutine = null;
        }

        if (verticalShakeRoutine != null)
        {
            StopCoroutine(
                verticalShakeRoutine
            );
        }

        transform.localPosition =
            originalLocalPosition;

        verticalShakeRoutine =
            StartCoroutine(
                VerticalShakeRoutine(
                    duration,
                    strength
                )
            );
    }
    private IEnumerator ShakeRoutine(
        float duration,
        float strength)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float progress =
                timer / duration;

            float currentStrength =
                Mathf.Lerp(
                    strength,
                    0f,
                    progress
                );

            Vector2 offset =
                Random.insideUnitCircle *
                currentStrength;

            transform.localPosition =
                originalLocalPosition +
                new Vector3(
                    offset.x,
                    offset.y,
                    0f
                );

            yield return null;
        }

        transform.localPosition =
            originalLocalPosition;

        shakeRoutine = null;
    }
    private IEnumerator VerticalShakeRoutine(
        float duration,
        float strength)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float progress =
                timer /
                duration;

            float currentStrength =
                Mathf.Lerp(
                    strength,
                    0f,
                    progress
                );

            float offsetY =
                Random.Range(
                    -currentStrength,
                    currentStrength
                );

            transform.localPosition =
                originalLocalPosition +
                new Vector3(
                    0f,
                    offsetY,
                    0f
                );

            yield return null;
        }

        transform.localPosition =
            originalLocalPosition;

        verticalShakeRoutine = null;
    }
}