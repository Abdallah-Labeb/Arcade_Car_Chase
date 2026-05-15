using UnityEngine;

public class FinishLine : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        CarController cc = other.GetComponent<CarController>();
        if (cc != null && cc.ct == cartype.player)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayerReachedFinish();
        }
    }
}
