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
}