using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Default Test Settings")]
    [SerializeField] private float testDuration = 0.12f;
    [SerializeField] private float testStrength = 0.12f;

    private Vector3 originalLocalPosition;
    private Coroutine shakeRoutine;
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

    [ContextMenu("Test Shake")]
    private void TestShake()
    {
        Shake(
            testDuration,
            testStrength
        );
    }
}