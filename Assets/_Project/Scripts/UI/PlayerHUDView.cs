using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presentation-only player HUD. Connects to existing Health, PlayerHeal,
/// and PlayerPrecisionStack without changing any gameplay rules.
/// </summary>
public sealed class PlayerHUDView : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private PlayerHeal playerHeal;
    [SerializeField] private PlayerPrecisionStack precisionStack;

    [Header("UI References")]
    [SerializeField] private Image healthFill;
    [Tooltip("Order from left to right. Create one slot per max heal charge (currently 3).")]
    [SerializeField] private Image[] healSlots;
    [Tooltip("Order from left to right. Create five slots for Precision Stack.")]
    [SerializeField] private Image[] precisionSlots;

    [Header("Colors")]
    [SerializeField] private Color healAvailable = new Color(0.741f, 0.678f, 0.522f, 1f); // #BDAD85
    [SerializeField] private Color healUsed = new Color(0.196f, 0.231f, 0.263f, 1f); // #323B43
    [SerializeField] private Color precisionFilled = new Color(0.631f, 0.541f, 0.357f, 1f); // #A18A5B
    [SerializeField] private Color precisionEmpty = new Color(0.094f, 0.137f, 0.173f, 1f); // #18232C
    [SerializeField] private Color precisionReady = new Color(0.843f, 0.753f, 0.549f, 1f); // #D7C08C
    [SerializeField] private Color precisionLocked = new Color(0.15f, 0.16f, 0.18f, 1f);

    private int lastHealCount = int.MinValue;
    private int lastPrecisionCount = int.MinValue;
    private bool lastPrecisionEnabled;

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged += UpdateHealth;
        if (precisionStack != null)
            precisionStack.OnPrecisionChanged += OnPrecisionChanged;

        RefreshAll();
    }

    private void Start()
    {
        // Re-read after PlayerHeal.Awake and Health.Awake initialized runtime values.
        RefreshAll();
    }

    private void Update()
    {
        // PlayerHeal exposes RemainingHealCount but no changed event.
        // Update the visual only when the count actually changes.
        if (playerHeal != null && playerHeal.RemainingHealCount != lastHealCount)
            RefreshHeal();

        // PrecisionEnabled can change without an OnPrecisionChanged event.
        if (precisionStack != null &&
            (precisionStack.CurrentPoints != lastPrecisionCount ||
             precisionStack.PrecisionEnabled != lastPrecisionEnabled))
        {
            RefreshPrecision();
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateHealth;
        if (precisionStack != null)
            precisionStack.OnPrecisionChanged -= OnPrecisionChanged;
    }

    private void RefreshAll()
    {
        if (playerHealth != null)
            UpdateHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        RefreshHeal();
        RefreshPrecision();
    }

    private void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (healthFill == null)
            return;
        healthFill.fillAmount = maxHealth > 0
            ? Mathf.Clamp01((float)currentHealth / maxHealth)
            : 0f;
    }

    private void RefreshHeal()
    {
        if (playerHeal == null)
            return;

        int remaining = Mathf.Max(0, playerHeal.RemainingHealCount);
        lastHealCount = remaining;
        if (healSlots == null)
            return;

        for (int i = 0; i < healSlots.Length; i++)
        {
            if (healSlots[i] != null)
                healSlots[i].color = i < remaining ? healAvailable : healUsed;
        }
    }

    private void OnPrecisionChanged(int current, int max)
    {
        RefreshPrecision();
    }

    private void RefreshPrecision()
    {
        if (precisionStack == null)
            return;

        int current = Mathf.Clamp(precisionStack.CurrentPoints, 0, precisionStack.MaxPoints);
        bool enabled = precisionStack.PrecisionEnabled;
        bool ready = enabled && current >= precisionStack.MaxPoints;
        lastPrecisionCount = current;
        lastPrecisionEnabled = enabled;
        if (precisionSlots == null)
            return;

        for (int i = 0; i < precisionSlots.Length; i++)
        {
            Image slot = precisionSlots[i];
            if (slot == null)
                continue;
            slot.color = !enabled ? precisionLocked
                : i >= precisionStack.MaxPoints ? precisionLocked
                : i < current ? (ready ? precisionReady : precisionFilled)
                : precisionEmpty;
        }
    }
}
