using UnityEngine;

public class OpenTriggerController : MonoBehaviour
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


