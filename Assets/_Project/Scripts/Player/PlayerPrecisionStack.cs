using UnityEngine;

public class PlayerPrecisionStack : MonoBehaviour
{
    [Header("Precision Point")]
    [SerializeField] private int maxPoints = 5;

    private int currentPoints;

    public int CurrentPoints => currentPoints;
    public int MaxPoints => maxPoints;

    public bool IsFull =>
        currentPoints >= maxPoints;

    public bool CanUseEnhancedAttack =>
        IsFull;


    // =====================================
    // Perfect Dodge / Perfect Parry 성공
    // =====================================

    public void GainStack()
    {
        if (currentPoints >= maxPoints)
            return;

        currentPoints++;

        currentPoints =
            Mathf.Clamp(
                currentPoints,
                0,
                maxPoints
            );

        Debug.Log(
            $"[Precision Point] Gain | " +
            $"{currentPoints}/{maxPoints}"
        );


        if (currentPoints >= maxPoints)
        {
            Debug.Log(
                "[Precision Point] " +
                "Enhanced Attack Ready"
            );
        }
    }


    // =====================================
    // 강화공격 사용
    // =====================================

    public bool TryConsumeEnhancedAttack()
    {
        if (!CanUseEnhancedAttack)
            return false;

        currentPoints = 0;

        Debug.Log(
            $"[Precision Point] " +
            $"Enhanced Attack Consume | " +
            $"{currentPoints}/{maxPoints}"
        );

        return true;
    }
}