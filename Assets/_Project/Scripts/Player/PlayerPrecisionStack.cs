using UnityEngine;

public class PlayerPrecisionStack : MonoBehaviour
{
    [Header("Precision Stack")]
    [SerializeField] private int maxStacks = 1;

    private int currentStacks;

    public int CurrentStacks => currentStacks;
    public bool HasStack => currentStacks > 0;

    public void GainStack()
    {
        int previousStacks = currentStacks;

        currentStacks =
            Mathf.Min(
                currentStacks + 1,
                maxStacks
            );

        if (currentStacks > previousStacks)
        {
            Debug.Log(
                $"[Precision Stack] Gain | " +
                $"{currentStacks}/{maxStacks}"
            );
        }
    }

    public bool TryConsumeStack()
    {
        if (currentStacks <= 0)
            return false;

        currentStacks--;

        Debug.Log(
            $"[Precision Stack] Consume | " +
            $"{currentStacks}/{maxStacks}"
        );

        return true;
    }
}