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

    public void Flash()
    {
        if (material == null)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        material.SetFloat(FlashAmount, 1f);

        yield return new WaitForSeconds(flashDuration);

        material.SetFloat(FlashAmount, 0f);

        flashRoutine = null;
    }
}