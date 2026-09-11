using UnityEngine;

public struct DamageInfo
{
    public int Damage;
    public Vector2 HitDirection;
    public float KnockbackForce;
    public GameObject Attacker;
    public string AttackId;
    public bool Parryable;

    public DamageInfo(
        int damage,
        Vector2 hitDirection,
        float knockbackForce,
        GameObject attacker,
        string attackId,
        bool parryable)
    {
        Damage = damage;
        HitDirection = hitDirection;
        KnockbackForce = knockbackForce;
        Attacker = attacker;
        AttackId = attackId;
        Parryable = parryable;
    }
}