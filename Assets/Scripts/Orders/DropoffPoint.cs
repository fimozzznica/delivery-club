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
            Debug.Log($"[DropoffPoint] {name} - Найден коллайдер: {collider.GetType()}, IsTrigger: {collider.isTrigger}");
            if (!collider.isTrigger)
            {
                Debug.LogWarning($"[DropoffPoint] {name} - ВНИМАНИЕ! IsTrigger = false. Триггеры не будут работать!");
            }
        }
        else
        {
            Debug.LogError($"[DropoffPoint] {name} - НЕТ КОЛЛАЙДЕРА! Компонент не будет работать!");
        }
    }

    void Start()
    {
        Debug.Log($"[DropoffPoint] {name} - Start(), Manager: {(manager ? manager.name : "NULL")}");
    }

    void Reset()
    {
        Debug.Log($"[DropoffPoint] {name} - Reset() вызван");
        var c = GetComponent<Collider>();
        if (c)
        {
            c.isTrigger = true;
            Debug.Log($"[DropoffPoint] {name} - Автоматически установлен IsTrigger = true");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[DropoffPoint] {name} - OnTriggerEnter с объектом: {other.name}");
        
        var box = other.GetComponentInParent<Box>();
        if (!box)
        {
            Debug.Log($"[DropoffPoint] {name} - Объект {other.name} не содержит компонент Box или его родители");
            return;
        }

        Debug.Log($"[DropoffPoint] {name} - Найдена коробка: {box.name}");
        Debug.Log($"[DropoffPoint] {name} - Коробка назначена на Dropoff: {(box.assignedDropoff ? box.assignedDropoff.name : "NULL")}");
        Debug.Log($"[DropoffPoint] {name} - Это правильный Dropoff: {box.assignedDropoff == this}");

        if (box.assignedDropoff == this)
        {
            Debug.Log($"[DropoffPoint] {name} - ? Правильная коробка! Отправляем в менеджер...");
            if (manager)
            {
                bool completed = manager.TryComplete(box, this);
                Debug.Log($"[DropoffPoint] {name} - Результат TryComplete: {completed}");
            }
            else
            {
                Debug.LogError($"[DropoffPoint] {name} - НЕТ МЕНЕДЖЕРА! Не могу завершить заказ.");
            }
        }
        else
        {
            Debug.Log($"[DropoffPoint] {name} - ? Неправильная коробка для этого Dropoff");
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"[DropoffPoint] {name} - OnTriggerExit с объектом: {other.name}");
    }

    void OnDestroy()
    {
        Debug.Log($"[DropoffPoint] {name} - OnDestroy()");
    }
}