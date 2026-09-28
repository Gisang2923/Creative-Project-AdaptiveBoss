using UnityEngine;

public class BossAnimationEventReceiver : MonoBehaviour
{
    [SerializeField] private BossParry bossParry;

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