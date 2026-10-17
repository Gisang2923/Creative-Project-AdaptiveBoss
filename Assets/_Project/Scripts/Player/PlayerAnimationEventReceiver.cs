using UnityEngine;

public class PlayerAnimationEventReceiver : MonoBehaviour
{
    [SerializeField] private PlayerDamageReceiver playerDamageReceiver;
    [SerializeField] private PlayerAnimator playerAnimator;
    [SerializeField] private PerfectDodgeAfterimage perfectDodgeAfterimage;
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
    public void EnhancedDashPrepareEnd()
    {
        playerCombat?
            .OnEnhancedDashPrepareEnd();
    }

    public void EnhancedDashAttackStart()
    {
        playerCombat?
            .OnEnhancedDashAttackStart();
    }

    public void EnhancedDashAttackEnd()
    {
        playerCombat?
            .OnEnhancedDashAttackEnd();
    }
    public void GetUpEnd()
    {
        playerDamageReceiver?
            .OnGetUpEnd();
    }
    public void PerfectDodgeEnd()
    {
        playerAnimator?.OnPerfectDodgeEnd();
    }
    public void AfterimageStart()
    {
        perfectDodgeAfterimage?
            .StartAfterimage();
    }

    public void AfterimageEnd()
    {
        perfectDodgeAfterimage?
            .StopAfterimage();
    }
}  