using System.Collections;
using UnityEngine;

public class CameraZoomEffect : MonoBehaviour
{
    public static CameraZoomEffect Instance
    {
        get;
        private set;
    }

    [Header("Reference")]
    [SerializeField]
    private Camera targetCamera;

    [Header("Heal Zoom")]
    [SerializeField]
    private float healZoomAmount = 0.35f;

    [SerializeField]
    private float zoomInDuration = 0.20f;

    [SerializeField]
    private float zoomOutDuration = 0.25f;

    private float defaultSize;

    private Coroutine zoomRoutine;


    private void Awake()
    {
        Instance = this;

        if (targetCamera == null)
        {
            targetCamera =
                GetComponent<Camera>();
        }

        if (targetCamera != null)
        {
            defaultSize =
                targetCamera.orthographicSize;
        }
    }


    public void StartHealZoom(
        float duration)
    {
        if (targetCamera == null)
            return;

        float targetSize =
            defaultSize -
            healZoomAmount;

        StartZoom(
            targetSize,
            duration
        );
    }


    public void StopHealZoom()
    {
        if (targetCamera == null)
            return;

        StartZoom(
            defaultSize,
            zoomOutDuration
        );
    }


    private void StartZoom(
        float targetSize,
        float duration)
    {
        if (zoomRoutine != null)
        {
            StopCoroutine(
                zoomRoutine
            );
        }

        zoomRoutine =
            StartCoroutine(
                ZoomRoutine(
                    targetSize,
                    duration
                )
            );
    }


    private IEnumerator ZoomRoutine(
        float targetSize,
        float duration)
    {
        float startSize =
            targetCamera
                .orthographicSize;

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

            targetCamera
                .orthographicSize =
                Mathf.Lerp(
                    startSize,
                    targetSize,
                    t
                );

            yield return null;
        }

        targetCamera
            .orthographicSize =
            targetSize;

        zoomRoutine = null;
    }
}