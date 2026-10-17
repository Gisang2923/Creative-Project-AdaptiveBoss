using UnityEngine;

public class VFXDestroyOnEnd : MonoBehaviour
{
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}