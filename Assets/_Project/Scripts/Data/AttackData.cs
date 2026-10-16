using UnityEngine;

[CreateAssetMenu(
    fileName = "AttackData",
    menuName = "Combat/Attack Data"
)]
public class AttackData : ScriptableObject
{
    [Header("Identity")]
    public string attackId;

    [Header("Damage")]
    public int damage = 20;

    [Header("Timing")]
    public float startupTime = 0.05f;
    public float activeTime = 0.12f;
    public float recoveryTime = 0.15f;

    [Header("Combat")]
    public float knockbackForce = 2f;
    public bool parryable = true;

    [Header("Special")]
    public bool causesBossStun = false;
    [Header("Knockdown")]
    public bool causesKnockdown = false;

    public float knockdownX = 9f;
    public float knockdownY = 3.5f;

    public float knockdownDuration = 0.8f;
}