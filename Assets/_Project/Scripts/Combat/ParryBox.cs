using UnityEngine;

public class ParryBox : MonoBehaviour
{
    private PlayerCounter playerCounter;

    private void Awake()
    {
        playerCounter = GetComponentInParent<PlayerCounter>();
    }

    public void ReceiveHit(DamageInfo damageInfo)
    {
        playerCounter?.ResolveCounter(damageInfo);
    }
}