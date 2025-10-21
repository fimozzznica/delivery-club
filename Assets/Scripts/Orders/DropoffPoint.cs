using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DropoffPoint : MonoBehaviour
{
    [HideInInspector] public OrderManager manager;

    void Awake()
    {
        Debug.Log($"[DropoffPoint] {name} - Awake()");
        var collider = GetComponent<Collider>();
        if (collider)
        {
            Debug.Log($"[DropoffPoint] {name} - ������ ���������: {collider.GetType()}, IsTrigger: {collider.isTrigger}");
            if (!collider.isTrigger)
            {
                Debug.LogWarning($"[DropoffPoint] {name} - ��������! IsTrigger = false. �������� �� ����� ��������!");
            }
        }
        else
        {
            Debug.LogError($"[DropoffPoint] {name} - ��� ����������! ��������� �� ����� ��������!");
        }
    }

    void Start()
    {
        Debug.Log($"[DropoffPoint] {name} - Start(), Manager: {(manager ? manager.name : "NULL")}");
    }

    void Reset()
    {
        Debug.Log($"[DropoffPoint] {name} - Reset() ������");
        var c = GetComponent<Collider>();
        if (c)
        {
            c.isTrigger = true;
            Debug.Log($"[DropoffPoint] {name} - ������������� ���������� IsTrigger = true");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[DropoffPoint] {name} - OnTriggerEnter � ��������: {other.name}");
        
        var box = other.GetComponentInParent<Box>();
        if (!box)
        {
            Debug.Log($"[DropoffPoint] {name} - ������ {other.name} �� �������� ��������� Box ��� ��� ��������");
            return;
        }

        Debug.Log($"[DropoffPoint] {name} - ������� �������: {box.name}");
        Debug.Log($"[DropoffPoint] {name} - ������� ��������� �� Dropoff: {(box.assignedDropoff ? box.assignedDropoff.name : "NULL")}");
        Debug.Log($"[DropoffPoint] {name} - ��� ���������� Dropoff: {box.assignedDropoff == this}");

        if (box.assignedDropoff == this)
        {
            Debug.Log($"[DropoffPoint] {name} - ? ���������� �������! ���������� � ��������...");
            if (manager)
            {
                bool completed = manager.TryComplete(box, this);
                Debug.Log($"[DropoffPoint] {name} - ��������� TryComplete: {completed}");
            }
            else
            {
                Debug.LogError($"[DropoffPoint] {name} - ��� ���������! �� ���� ��������� �����.");
            }
        }
        else
        {
            Debug.Log($"[DropoffPoint] {name} - ? ������������ ������� ��� ����� Dropoff");
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"[DropoffPoint] {name} - OnTriggerExit � ��������: {other.name}");
    }

    void OnDestroy()
    {
        Debug.Log($"[DropoffPoint] {name} - OnDestroy()");
    }
}