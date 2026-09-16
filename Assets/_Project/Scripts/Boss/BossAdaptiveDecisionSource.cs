using UnityEngine;

public class BossAdaptiveDecisionSource : MonoBehaviour
{
    public virtual float GetAttackWeightMultiplier(
        BossAttack attack,
        float distance)
    {
        return 1f;
    }

    public virtual float GetPostActionWeightMultiplier(
        BossAttack attack,
        BossPostAction postAction,
        float distance)
    {
        return 1f;
    }

    public virtual float GetBackDodgeWeightMultiplier(
        float distance)
    {
        return 1f;
    }
}