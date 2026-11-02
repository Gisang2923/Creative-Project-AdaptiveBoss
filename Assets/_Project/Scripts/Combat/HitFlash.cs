using System.Collections;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float flashDuration = 0.08f;

    private Material material;
    private Coroutine flashRoutine;

    private static readonly int FlashAmount =
        Shader.PropertyToID("_FlashAmount");

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            material = spriteRenderer.material;
    }

    // 기존 피격용
    public void Flash()
    {
        Flash(
            1f,
            flashDuration,
            false
        );
    }

    // 패링 등 강도 조절용
    public void Flash(
        float amount,
        float duration,
        bool useUnscaledTime)
    {
        if (material == null)
            return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            material.SetFloat(
                FlashAmount,
                0f
            );
        }

        flashRoutine =
            StartCoroutine(
                FlashRoutine(
                    amount,
                    duration,
                    useUnscaledTime
                )
            );
    }

    private IEnumerator FlashRoutine(
        float amount,
        float duration,
        bool useUnscaledTime)
    {
        material.SetFloat(
            FlashAmount,
            Mathf.Clamp01(amount)
        );

        if (useUnscaledTime)
        {
            yield return
                new WaitForSecondsRealtime(
                    duration
                );
        }
        else
        {
            yield return
                new WaitForSeconds(
                    duration
                );
        }

        material.SetFloat(
            FlashAmount,
            0f
        );

        flashRoutine = null;
    }
}