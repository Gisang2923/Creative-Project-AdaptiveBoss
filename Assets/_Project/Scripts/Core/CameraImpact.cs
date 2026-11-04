using System.Collections;
using UnityEngine;

public class CameraImpact : MonoBehaviour
{
    public static CameraImpact Instance
    {
        get;
        private set;
    }

    private Vector3 originalLocalPosition;
    private Coroutine punchRoutine;

    private void Awake()
    {
        Instance = this;

        originalLocalPosition =
            transform.localPosition;
    }

    public void PunchHorizontal(
        float direction,
        float distance,
        float returnDuration)
    {
        if (punchRoutine != null)
        {
            StopCoroutine(
                punchRoutine
            );
        }

        transform.localPosition =
            originalLocalPosition;

        punchRoutine =
            StartCoroutine(
                PunchRoutine(
                    Mathf.Sign(direction),
                    distance,
                    returnDuration
                )
            );
    }

    private IEnumerator PunchRoutine(
        float direction,
        float distance,
        float returnDuration)
    {
        // 공격 방향으로 즉시 카메라를 밀어줌
        Vector3 punchPosition =
            originalLocalPosition +
            new Vector3(
                direction * distance,
                0f,
                0f
            );

        transform.localPosition =
            punchPosition;

        float elapsed = 0f;

        while (elapsed < returnDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    returnDuration
                );

            // 처음에는 조금 유지되고
            // 뒤에서 부드럽게 복귀
            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            transform.localPosition =
                Vector3.Lerp(
                    punchPosition,
                    originalLocalPosition,
                    t
                );

            yield return null;
        }

        transform.localPosition =
            originalLocalPosition;

        punchRoutine = null;
    }
}