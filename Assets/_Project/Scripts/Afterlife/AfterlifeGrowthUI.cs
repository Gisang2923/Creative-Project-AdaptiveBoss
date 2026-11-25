using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AfterlifeGrowthUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField]
    private GameObject statusPanel;

    [SerializeField]
    private RectTransform statusPanelRect;

    [Header("Follow")]
    [SerializeField]
    private Vector3 worldOffset =
        new Vector3(0f, 2.5f, 0f);

    [Header("Texts")]
    [SerializeField]
    private TMP_Text pointText;

    [SerializeField]
    private TMP_Text attackValueText;

    [SerializeField]
    private TMP_Text healthValueText;

    [Header("Attack")]
    [SerializeField]
    private Button attackMinusButton;

    [SerializeField]
    private Button attackPlusButton;

    [Header("Player Upgrade Animation Clips")]
    [SerializeField] private AnimationClip attackUpgradeAnimationClip;
    [SerializeField] private AnimationClip healthUpgradeAnimationClip;

    [Header("Health")]
    [SerializeField]
    private Button healthMinusButton;

    [SerializeField]
    private Button healthPlusButton;

    [Header("Close")]
    [SerializeField]
    private Button closeButton;

    private GameProgression progression;

    private PlayerMovement playerMovement;
    private PlayerAnimator playerAnimator;

    private Transform playerTransform;

    // 이번 UI에서만 임시로 찍은 값
    private int pendingAttack;
    private int pendingHealth;

    public bool IsOpen =>
        statusPanel != null &&
        statusPanel.activeSelf;

    public event Action OnGrowthClosed;

    private void Awake()
    {
        statusPanel?.SetActive(false);

        attackPlusButton?.onClick.AddListener(
            AddAttack
        );

        attackMinusButton?.onClick.AddListener(
            RemoveAttack
        );

        healthPlusButton?.onClick.AddListener(
            AddHealth
        );

        healthMinusButton?.onClick.AddListener(
            RemoveHealth
        );

        closeButton?.onClick.AddListener(
            ConfirmAndClose
        );
    }

    private void Start()
    {
        progression =
            GameProgression.Instance;
    }

    private void OnDisable()
    {
        playerAnimator?.StopUpgradeAnimation();

        if (IsOpen)
        {
            statusPanel.SetActive(false);
            playerMovement?.SetInputLocked(false);
        }
    }

    private void LateUpdate()
    {
        if (!IsOpen)
            return;

        if (playerTransform == null)
            return;

        if (statusPanelRect == null)
            return;

        Camera cam = Camera.main;

        if (cam == null)
            return;

        Vector3 screenPosition =
            cam.WorldToScreenPoint(
                playerTransform.position +
                worldOffset
            );

        statusPanelRect.position =
            screenPosition;
    }

    public void Open(
        PlayerMovement movement)
    {
        progression =
            GameProgression.Instance;

        if (progression == null)
        {
            Debug.LogError(
                "[Afterlife Growth] " +
                "GameProgression not found."
            );

            return;
        }

        if (!progression.StatsUpgradeUnlocked)
            return;

        playerMovement =
            movement;
        playerAnimator = movement != null
            ? movement.GetComponent<PlayerAnimator>()
            : null;

        playerTransform =
            movement != null
                ? movement.transform
                : null;

        pendingAttack = 0;
        pendingHealth = 0;

        playerMovement?
            .SetInputLocked(true);

        statusPanel?.SetActive(true);

        RefreshUI();

        Debug.Log(
            "[Afterlife Growth] Open"
        );
    }

    private int AvailablePoints()
    {
        if (progression == null)
            return 0;

        return Mathf.Max(
            0,
            progression.GrowthPoints -
            pendingAttack -
            pendingHealth
        );
    }

    private void AddAttack()
    {
        if (progression == null)
            return;

        int previewLevel =
            progression.AttackLevel +
            pendingAttack;

        if (previewLevel >=
            GameProgression.MaxAttackLevel)
        {
            return;
        }

        if (AvailablePoints() <= 0)
            return;

        pendingAttack++;

        RefreshUI();
        playerAnimator?.PlayUpgradeAnimation(attackUpgradeAnimationClip);
    }

    private void RemoveAttack()
    {
        if (pendingAttack <= 0)
            return;

        pendingAttack--;

        RefreshUI();
    }

    private void AddHealth()
    {
        if (progression == null)
            return;

        int previewLevel =
            progression.HealthLevel +
            pendingHealth;

        if (previewLevel >=
            GameProgression.MaxHealthLevel)
        {
            return;
        }

        if (AvailablePoints() <= 0)
            return;

        pendingHealth++;

        RefreshUI();
        playerAnimator?.PlayUpgradeAnimation(healthUpgradeAnimationClip);
    }

    private void RemoveHealth()
    {
        if (pendingHealth <= 0)
            return;

        pendingHealth--;

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (progression == null)
            return;

        int attackPreview =
            progression.AttackLevel +
            pendingAttack;

        int healthPreview =
            progression.HealthLevel +
            pendingHealth;

        if (pointText != null)
        {
            pointText.text =
                $"스테이터스 포인트  {AvailablePoints()}";
        }

        if (attackValueText != null)
        {
            attackValueText.text =
                $"{attackPreview}";
        }

        if (healthValueText != null)
        {
            healthValueText.text =
                $"{healthPreview}";
        }

        bool hasPoint =
            AvailablePoints() > 0;

        if (attackPlusButton != null)
        {
            attackPlusButton.interactable =
                hasPoint &&
                attackPreview <
                GameProgression.MaxAttackLevel;
        }

        if (healthPlusButton != null)
        {
            healthPlusButton.interactable =
                hasPoint &&
                healthPreview <
                GameProgression.MaxHealthLevel;
        }

        // 이전에 확정한 레벨은 감소 불가.
        // 이번 UI에서 올린 것만 되돌릴 수 있다.
        if (attackMinusButton != null)
        {
            attackMinusButton.interactable =
                pendingAttack > 0;
        }

        if (healthMinusButton != null)
        {
            healthMinusButton.interactable =
                pendingHealth > 0;
        }

    }

    private void ConfirmAndClose()
    {
        if (progression == null)
            return;

        playerAnimator?.StopUpgradeAnimation();

        for (int i = 0;
             i < pendingAttack;
             i++)
        {
            progression.TryUpgradeAttack();
        }

        for (int i = 0;
             i < pendingHealth;
             i++)
        {
            progression.TryUpgradeHealth();
        }

        pendingAttack = 0;
        pendingHealth = 0;

        statusPanel?.SetActive(false);

        playerMovement?
            .SetInputLocked(false);

        Debug.Log(
            "[Afterlife Growth] Closed"
        );

        OnGrowthClosed?.Invoke();
    }
}
