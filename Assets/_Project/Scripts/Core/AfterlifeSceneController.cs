using UnityEngine;

public class AfterlifeSceneController : MonoBehaviour
{
    public void ReturnToBattle()
    {
        GameFlowController.Instance?
            .ReturnToBattle();
    }
}