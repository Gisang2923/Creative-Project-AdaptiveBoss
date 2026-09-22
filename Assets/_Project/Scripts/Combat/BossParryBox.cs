using UnityEngine;

public class BossParryBox : MonoBehaviour
{
    private BossParry bossParry;

    private void Awake()
    {
        bossParry = GetComponentInParent<BossParry>();
    }

    public void ReceiveHit(DamageInfo damageInfo)
    {
        bossParry?.ResolveParry(damageInfo);
    }
}