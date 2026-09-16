using UnityEngine;

[System.Serializable]
public class BossAttack
{
    public BossActionIntent intent;
    public BossAttackType attackType;
    public AttackData attackData;
    public Hitbox hitbox;

    [Header("Range")]
    public float minRange;
    public float maxRange;

    [Header("Decision")]
    [Min(0f)]
    public float baseWeight = 1f;

    [Range(0f, 1f)]
    public float repeatWeightMultiplier = 0.35f;

    [Min(0f)]
    public float decisionCooldown = 0f;

    [Header("Post Action")]
    [Min(0f)]
    public float holdWeight = 1f;

    [Min(0f)]
    public float approachWeight = 0f;

    [Min(0f)]
    public float retreatWeight = 0f;
}