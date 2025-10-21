using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DropoffPoint : MonoBehaviour
{
    [HideInInspector] public OrderManager manager;

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        var box = other.GetComponentInParent<Box>();
        if (!box) return;

        if (box.assignedDropoff == this)
        {
            manager?.TryComplete(box, this);
        }
    }
}