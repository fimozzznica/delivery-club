using UnityEngine;

public class CloseTriggerController : MonoBehaviour
{
    public BoxLidController box;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hand"))
        {
            box.CloseBox();
        }
    }
}