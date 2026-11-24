using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class TitleButtonHoverScale : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Min(1f)] private float hoverMultiplier = 1.05f;
    [SerializeField, Min(0.01f)] private float smoothTime = 0.12f;

    private Button button;
    private Vector3 originalScale;
    private Vector3 scaleVelocity;
    private bool pointerInside;

    private void Awake()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;
    }

    private void Update()
    {
        if (button == null || !button.IsInteractable())
            pointerInside = false;

        Vector3 targetScale = pointerInside
            ? originalScale * hoverMultiplier
            : originalScale;

        if ((transform.localScale - targetScale).sqrMagnitude < 0.000001f)
        {
            transform.localScale = targetScale;
            scaleVelocity = Vector3.zero;
            return;
        }

        transform.localScale = Vector3.SmoothDamp(
            transform.localScale,
            targetScale,
            ref scaleVelocity,
            smoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && button.IsInteractable())
            pointerInside = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
    }

    private void OnDisable()
    {
        pointerInside = false;
        scaleVelocity = Vector3.zero;
        transform.localScale = originalScale;
    }
}
