using UnityEngine;

public class OpenTriggerController1 : MonoBehaviour
{
    public BoxLidController box;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hand"))
        {
            box.OpenBox();
        }
    }
}
