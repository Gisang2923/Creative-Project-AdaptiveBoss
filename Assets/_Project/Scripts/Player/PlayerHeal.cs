using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHeal : MonoBehaviour
{
    [Header("Heal Settings")]
    [SerializeField] private int healAmount = 30;
    [SerializeField] private float healDuration = 1f;
    [SerializeField] private int maxHealCount = 3;
    [SerializeField] private float healEndDuration = 0.3f;
    private Health health;
    private PlayerDamageReceiver damageReceiver;
    private PlayerCombat playerCombat;
    private PlayerCounter playerCounter;

    private Coroutine healRoutine;
    private Coroutine healEndRoutine;
    private bool isHealing;
    private bool healButtonHeld;

    private int remainingHealCount;
    private bool healSucceeded;

    public bool HealSucceeded => healSucceeded;

    public bool IsHealing => isHealing;
    public int RemainingHealCount => remainingHealCount;

    private void Awake()
    {
        health = GetComponent<Health>();
        damageReceiver = GetComponent<PlayerDamageReceiver>();
        playerCombat = GetComponent<PlayerCombat>();
        playerCounter = GetComponent<PlayerCounter>();

        remainingHealCount = maxHealCount;
    }

    public void OnHeal(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            StartHeal();
        }

        if (context.canceled)
        {
            CancelHeal();
        }
    }

    private void StartHeal()
    {
        if (isHealing)
            return;

        if (health == null || health.IsDead)
            return;

        if (health.CurrentHealth >= health.MaxHealth)
            return;

        if (remainingHealCount <= 0)
            return;

        if (damageReceiver != null && damageReceiver.IsStunned)
            return;

        if (playerCombat != null &&
            (playerCombat.IsAttacking || playerCombat.IsCharging))
            return;

        if (playerCounter != null && playerCounter.IsCountering)
            return;

        healButtonHeld = true;
        CombatLogger.Instance?.RecordPlayerResponse(
            PlayerResponseType.Heal
        );
        healRoutine = StartCoroutine(HealRoutine());
    }

    private IEnumerator HealRoutine()
    {
        isHealing = true;

        yield return new WaitForSeconds(healDuration);

        if (!healButtonHeld)
        {
            isHealing = false;
            healRoutine = null;
            yield break;
        }

        health.Heal(healAmount);
        remainingHealCount--;

        isHealing = false;
        healRoutine = null;

        healEndRoutine = StartCoroutine(HealEndRoutine());

        Debug.Log($"Heal Success! Remaining: {remainingHealCount}");
    }
    private IEnumerator HealEndRoutine()
    {
        healSucceeded = true;

        yield return new WaitForSeconds(healEndDuration);

        healSucceeded = false;
        healEndRoutine = null;
    }
    private void CancelHeal()
    {
        EndHeal();
    }

    public void ForceCancelHeal()
    {
        EndHeal();
    }

    private void EndHeal()
    {
        healButtonHeld = false;

        if (healRoutine != null)
        {
            StopCoroutine(healRoutine);
            healRoutine = null;
        }

        if (healEndRoutine != null)
        {
            StopCoroutine(healEndRoutine);
            healEndRoutine = null;
        }

        isHealing = false;
        healSucceeded = false;
    }
}