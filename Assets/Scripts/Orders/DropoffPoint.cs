using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DropoffPoint : MonoBehaviour
{
    [Header("Address")]
    [Tooltip("Адрес этой точки доставки (строка для UI)")]
    public string deliveryAddress;

    [HideInInspector] public OrderManager manager;

    void Awake() { var collider = GetComponent<Collider>(); }

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c) { c.isTrigger = true; }
    }

    void OnTriggerEnter(Collider other)
    {
        var box = other.GetComponentInParent<Box>();
        if (!box) { return; }
        if (box.assignedDropoff == this)
        {
            if (manager) { manager.TryComplete(box, this); }
        }
    }
}
