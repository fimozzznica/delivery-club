using UnityEngine;

public class CloseTriggerController1 : MonoBehaviour
{
    public BoxLidController1 box;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hand"))
        {
            box.CloseBox1();
        }
    }
}
