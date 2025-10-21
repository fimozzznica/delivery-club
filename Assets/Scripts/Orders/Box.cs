using UnityEngine;

[DisallowMultipleComponent]
public class Box : MonoBehaviour
{
    [Header("Design")]
    [Range(1, 4)] public int level = 1;

    [Header("Item")]
    public string contentName = "Unknown";
    public int price = 0;

    [Header("Runtime")]
    public string orderId;
    public DropoffPoint assignedDropoff;

    public bool IsAssigned => !string.IsNullOrEmpty(orderId);

    public void Assign(string id, DropoffPoint dropoff)
    {
        orderId = id;
        assignedDropoff = dropoff;
        name = $"Box_{orderId}";
    }

    public void ClearAssignment()
    {
        orderId = null;
        assignedDropoff = null;
        name = "Box";
    }
}