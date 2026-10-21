using UnityEngine;
using UnityEngine.UI;

public class PrecisionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerPrecisionStack precisionStack;

    [SerializeField]
    private Image fillImage;

    [SerializeField]
    private Image readyImage;

    private void OnEnable()
    {
        if (precisionStack == null)
            return;

        precisionStack.OnPrecisionChanged +=
            UpdatePrecision;
    }

    private void Start()
    {
        if (precisionStack == null)
            return;

        UpdatePrecision(
            precisionStack.CurrentPoints,
            precisionStack.MaxPoints
        );
    }

    private void OnDisable()
    {
        if (precisionStack == null)
            return;

        precisionStack.OnPrecisionChanged -=
            UpdatePrecision;
    }

    private void UpdatePrecision(
        int current,
        int max)
    {
        if (fillImage == null ||
            max <= 0)
        {
            return;
        }

        fillImage.fillAmount =
            (float)current / max;

        if (readyImage != null)
        {
            readyImage.fillAmount =
                current >= max
                    ? 1f
                    : 0f;
        }
    }
}