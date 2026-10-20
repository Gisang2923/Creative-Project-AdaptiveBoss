using UnityEngine;

public class PlayerAnimationEventReceiver : MonoBehaviour
{
    [SerializeField]
    private PlayerDamageReceiver playerDamageReceiver;

    [SerializeField]
    private PlayerAnimator playerAnimator;

    [SerializeField]
    private PerfectDodgeAfterimage perfectDodgeAfterimage;

    [SerializeField]
    private PlayerCombat playerCombat;

    // Imported animation에서 들어오는 기존 이벤트 더미
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

    // =====================================
    // Dash Attack
    // =====================================

    public void DashAttackHitboxOn()
    {
        playerCombat?
            .OnDashAttackHitboxOn();
    }

    public void DashAttackHitboxOff()
    {
        playerCombat?
            .OnDashAttackHitboxOff();
    }

    public void DashAttackEnd()
    {
        playerCombat?
            .OnDashAttackEnd();
    }

    // =====================================
    // Enhanced Dash
    // =====================================

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

    // =====================================
    // Enhanced Heavy
    //
    // ChargeAttack 애니메이션에 아래 이벤트 3개를 배치한다.
    // 일반 ChargeAttack에서도 이벤트 자체는 호출될 수 있지만
    // PlayerCombat 내부에서 Enhanced Heavy일 때만 반응한다.
    // =====================================

    public void EnhancedHeavyHitboxOn()
    {
        playerCombat?
            .OnEnhancedHeavyHitboxOn();
    }

    public void EnhancedHeavyHitboxOff()
    {
        playerCombat?
            .OnEnhancedHeavyHitboxOff();
    }

    public void EnhancedHeavyAttackEnd()
    {
        playerCombat?
            .OnEnhancedHeavyAttackEnd();
    }

    // =====================================
    // Other Player Animation Events
    // =====================================

    public void GetUpEnd()
    {
        playerDamageReceiver?
            .OnGetUpEnd();
    }

    public void PerfectDodgeEnd()
    {
        playerAnimator?
            .OnPerfectDodgeEnd();
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
