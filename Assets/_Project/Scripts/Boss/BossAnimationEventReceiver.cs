using UnityEngine;

public class BossAnimationEventReceiver : MonoBehaviour
{
    [SerializeField] private BossParry bossParry;
    [SerializeField] private BossAction bossAction;
    [SerializeField] private BossController bossController;
    public void BasicHit()
    {
        bossAction?.OnBasicHit();
    }
    public void BasicRelease()
    {
        bossAction?
            .OnBasicRelease();
    }
    public void HeavyHit()
    {
        bossAction?.OnHeavyHit();
    }
    public void CounterHitboxOn()
    {
        bossParry?.OnCounterHitboxOn();
    }

    public void CounterHitboxOff()
    {
        bossParry?.OnCounterHitboxOff();
    }

    public void CounterThrustEnd()
    {
        bossParry?.OnCounterThrustEnd();
    }
    public void PrecisionStunRecoverEnd()
    {
        bossController?
            .OnPrecisionStunRecoverEnd();
    }
    public void ChargeCueSFX()
    {
        bossAction?
            .OnChargeCueSFX();
    }

    public void ChargeHit()
    {
        bossAction?
            .OnChargeHit();
    }
}