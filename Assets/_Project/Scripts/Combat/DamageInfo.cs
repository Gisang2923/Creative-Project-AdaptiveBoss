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
    public bool CausesKnockdown;

    public float KnockdownX;
    public float KnockdownY;
    public float KnockdownDuration;
    public DamageInfo(
        int damage,
        Vector2 hitDirection,
        float knockbackForce,
        GameObject attacker,
        string attackId,
        bool parryable,
        bool causesBossStun = false,
        bool causesKnockdown = false,
        float knockdownX = 0f,
        float knockdownY = 0f,
        float knockdownDuration = 0f)
    {
        Damage = damage;
        HitDirection = hitDirection;
        KnockbackForce = knockbackForce;
        Attacker = attacker;
        AttackId = attackId;
        Parryable = parryable;

        CausesBossStun =
            causesBossStun;

        CausesKnockdown =
            causesKnockdown;

        KnockdownX =
            knockdownX;

        KnockdownY =
            knockdownY;

        KnockdownDuration =
            knockdownDuration;
    }
}