using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DropoffPoint : MonoBehaviour
{
    [Header("Address")]
    [Tooltip("Адрес этой точки доставки (строка для UI)")]
    public string deliveryAddress = "Пункт выдачи";

    [HideInInspector] public OrderManager manager;

    void Awake()
    {
        var collider = GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogError($"[DropoffPoint] {name} - НЕТ КОЛЛАЙДЕРА! Компонент не будет работать!");
        }
    }

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c)
        {
            c.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var box = other.GetComponentInParent<Box>();
        if (!box)
        {
            return;
        }

        if (box.assignedDropoff == this)
        {
            if (manager)
            {
                manager.TryComplete(box, this);
            }
            else
            {
                Debug.LogError($"[DropoffPoint] {name} - НЕТ МЕНЕДЖЕРА! Не могу завершить заказ.");
            }
        }
    }
}
