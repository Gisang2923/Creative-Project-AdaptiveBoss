using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the existing boss Health and reveals its name in Act 2.
/// Add to a UI object under the BossBattle Canvas.
/// </summary>
public sealed class BossHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health bossHealth;
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Display Names")]
    [SerializeField] private string hiddenName = "???";
    [SerializeField] private string revealedName = "종결";

    private GameProgression progression;

    private void Start()
    {
        if (bossHealth == null)
        {
            BossDamageReceiver boss = FindFirstObjectByType<BossDamageReceiver>();
            if (boss != null)
                bossHealth = boss.GetComponent<Health>();
        }

        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged += UpdateHealthBar;
            bossHealth.OnDied += HandleBossDied;
            UpdateHealthBar(bossHealth.CurrentHealth, bossHealth.MaxHealth);
        }
        else
        {
            Debug.LogWarning("[BossHealthBarUI] Boss Health reference is missing.");
        }

        progression = GameProgression.Instance;
        if (progression != null)
        {
            progression.OnPhaseChanged += UpdateBossName;
            UpdateBossName(progression.CurrentPhase);
        }
        else if (bossNameText != null)
        {
            bossNameText.text = hiddenName;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = bossHealth != null && bossHealth.IsDead ? 0f : 1f;
    }

    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthFill == null)
            return;

        healthFill.fillAmount = maxHealth > 0
            ? Mathf.Clamp01((float)currentHealth / maxHealth)
            : 0f;
    }

    private void UpdateBossName(GamePhase phase)
    {
        if (bossNameText == null)
            return;

        bool reveal = phase == GamePhase.Act2Adaptive || phase == GamePhase.Completed;
        bossNameText.text = reveal ? revealedName : hiddenName;
    }

    private void HandleBossDied()
    {
        UpdateHealthBar(0, bossHealth != null ? bossHealth.MaxHealth : 1);
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged -= UpdateHealthBar;
            bossHealth.OnDied -= HandleBossDied;
        }

        if (progression != null)
            progression.OnPhaseChanged -= UpdateBossName;
    }
}
