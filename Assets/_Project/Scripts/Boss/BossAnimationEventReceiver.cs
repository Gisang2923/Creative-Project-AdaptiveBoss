using UnityEngine;

public class BossAnimationEventReceiver : MonoBehaviour
{
    [SerializeField] private BossParry bossParry;
    [SerializeField] private BossAction bossAction;

    public void BasicHit()
    {
        bossAction?.OnBasicHit();
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
}