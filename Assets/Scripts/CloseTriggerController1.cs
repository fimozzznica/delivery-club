using UnityEngine;

public class CloseTriggerController1 : MonoBehaviour
{
    public BoxLidController1 box;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hand"))
        {
            Debug.Log("Close trigger entered by: " + other.name);
            box.CloseBox1();
        }
    }
}
