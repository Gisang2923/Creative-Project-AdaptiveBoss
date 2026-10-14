using UnityEngine;

public struct DamageInfo
{
    public int Damage;
    public Vector2 HitDirection;
    public float KnockbackForce;
    public GameObject Attacker;
    public string AttackId;
    public bool Parryable;
    public bool CausesBossStun;

    public DamageInfo(
        int damage,
        Vector2 hitDirection,
        float knockbackForce,
        GameObject attacker,
        string attackId,
        bool parryable,
        bool causesBossStun = false)
    {
        Damage = damage;
        HitDirection = hitDirection;
        KnockbackForce = knockbackForce;
        Attacker = attacker;
        AttackId = attackId;
        Parryable = parryable;
        CausesBossStun = causesBossStun;
    }
}