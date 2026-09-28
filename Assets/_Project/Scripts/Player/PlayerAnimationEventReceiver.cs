using UnityEngine;

public class PlayerAnimationEventReceiver : MonoBehaviour
{
    public void OnResetVelocity(){}
    public void OnDash(){}
    public void OnBreak(){}
    public void OnCheckNPCCombo(){}
    public void OnCheckCharge(){}
    public void OnCastAtkBuff(){}
    public void CheckCombo(){}
    public void RecoverState(){}
    public void DoCast(){}

    public void OnCounter(){}
    [SerializeField] private PlayerCombat playerCombat;
    public void DashAttackHitboxOn()
    {
        playerCombat?.OnDashAttackHitboxOn();
    }

    public void DashAttackHitboxOff()
    {
        playerCombat?.OnDashAttackHitboxOff();
    }

    public void DashAttackEnd()
    {
        playerCombat?.OnDashAttackEnd();
    }
}