using UnityEngine;
using UnityEngine.InputSystem;

public class BossDebugInput : MonoBehaviour
{
    [SerializeField] private BossAction bossAction;

#if UNITY_EDITOR
    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            bossAction.ExecuteAttack();
        }
    }
#endif
}