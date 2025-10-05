using UnityEngine;

public class CloseTriggerController : MonoBehaviour
{
    public BoxLidController box;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hand"))
        {
            Debug.Log("Close trigger entered by: " + other.name);
            box.CloseBox();
        }
    }
}