using UnityEngine;

[DisallowMultipleComponent]
public class Box : MonoBehaviour
{
    [Header("Design")]
    [Range(1, 4)] public int level = 1;
    public bool shopStreet = false;

    [Header("Item")]
    public string contentName = "Unknown";
    public int price = 0;

    [Header("Address")]
    [Tooltip("Адрес откуда забрать эту коробку (строка для UI)")]
    public string pickupAddress = "Склад";

    [Header("Runtime")]
    public string orderId;
    public DropoffPoint assignedDropoff;

    [Header("Home Position (saved on first activation)")]
    [SerializeField] private bool _homePositionSaved = false;
    [SerializeField] private Vector3 _homePosition;
    [SerializeField] private Quaternion _homeRotation;

    public bool IsAssigned => !string.IsNullOrEmpty(orderId);
    public bool HasHomePosition => _homePositionSaved;

    void Awake()
    {
        //Debug.Log($"[Box] {name} - Awake() | Level: {level}, Content: '{contentName}', Price: {price}, PickupAddress: '{pickupAddress}', Active: {gameObject.activeInHierarchy}");
        //Debug.Log($"[Box] {name} - Home position saved: {_homePositionSaved}");
        
        var collider = GetComponent<Collider>();
        var rigidbody = GetComponent<Rigidbody>();
        
        //Debug.Log($"[Box] {name} - Компоненты: Collider={collider != null}, Rigidbody={rigidbody != null}");
    }

    void Start()
    {
        Debug.Log($"[Box] {name} - Start() | IsAssigned: {IsAssigned}, OrderID: '{orderId}', AssignedDropoff: {(assignedDropoff ? assignedDropoff.name : "NULL")}");
    }

    void OnEnable()
    {
        //Debug.Log($"[Box] {name} - OnEnable() | IsAssigned: {IsAssigned}");
        
        // Сохраняем домашнюю позицию при первой активации
        if (!_homePositionSaved)
        {
            SaveHomePosition();
        }
    }

    void OnDisable()
    {
        //Debug.Log($"[Box] {name} - OnDisable() | IsAssigned: {IsAssigned}");
    }

    public void SaveHomePosition()
    {
        if (_homePositionSaved)
        {
            //Debug.Log($"[Box] {name} - SaveHomePosition() - позиция уже сохранена, пропускаем");
            return;
        }

        _homePosition = transform.position;
        _homeRotation = transform.rotation;
        _homePositionSaved = true;
        
        //Debug.Log($"[Box] {name} - SaveHomePosition() - сохранена домашняя позиция: {_homePosition}, поворот: {_homeRotation}");
    }

    public void ReturnHome()
    {
        if (!_homePositionSaved)
        {
            Debug.LogWarning($"[Box] {name} - ReturnHome() - домашняя позиция не сохранена! Не могу вернуть коробку домой.");
            return;
        }

        //Debug.Log($"[Box] {name} - ReturnHome() - возвращаем с позиции {transform.position} в домашнюю {_homePosition}");
        
        Vector3 oldPos = transform.position;
        Quaternion oldRot = transform.rotation;
        
        transform.SetPositionAndRotation(_homePosition, _homeRotation);

        //Debug.Log($"[Box] {name} - Позиция изменена с {oldPos} на {transform.position}");
        //Debug.Log($"[Box] {name} - Поворот изменён с {oldRot} на {transform.rotation}");

        // Обнуляем физику
        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            //Debug.Log($"[Box] {name} - Обнуляем физику Rigidbody: velocity={rb.velocity}, angularVelocity={rb.angularVelocity}");
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            //Debug.LogWarning($"[Box] {name} - У коробки нет Rigidbody!");
        }
    }

    public void Assign(string id, DropoffPoint dropoff)
    {
        Debug.Log($"[Box] {name} - Assign() вызван с ID: '{id}', Dropoff: {(dropoff ? dropoff.name : "NULL")}");
        
        string oldOrderId = orderId;
        DropoffPoint oldDropoff = assignedDropoff;
        string oldName = name;
        
        orderId = id;
        assignedDropoff = dropoff;
        name = $"Box_{orderId}";
        
        //Debug.Log($"[Box] {name} - Назначение изменено:");
        //Debug.Log($"[Box] {name} -   OrderID: '{oldOrderId}' -> '{orderId}'");
        //Debug.Log($"[Box] {name} -   Dropoff: {(oldDropoff ? oldDropoff.name : "NULL")} -> {(assignedDropoff ? assignedDropoff.name : "NULL")}");
        //Debug.Log($"[Box] {name} -   Name: '{oldName}' -> '{name}'");
        //Debug.Log($"[Box] {name} -   IsAssigned: {IsAssigned}");
    }

    public void ClearAssignment()
    {
        //Debug.Log($"[Box] {name} - ClearAssignment() вызван");
        
        string oldOrderId = orderId;
        DropoffPoint oldDropoff = assignedDropoff;
        string oldName = name;
        
        orderId = null;
        assignedDropoff = null;
        name = "Box";
        
        //Debug.Log($"[Box] {name} - Назначение очищено:");
        //Debug.Log($"[Box] {name} -   OrderID: '{oldOrderId}' -> '{orderId}'");
        //Debug.Log($"[Box] {name} -   Dropoff: {(oldDropoff ? oldDropoff.name : "NULL")} -> NULL");
        //Debug.Log($"[Box] {name} -   Name: '{oldName}' -> '{name}'");
        //Debug.Log($"[Box] {name} -   IsAssigned: {IsAssigned}");
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"[Box] {name} - OnTriggerEnter с {other.name}");
    }

    void OnCollisionEnter(Collision collision)
    {
        //Debug.Log($"[Box] {name} - OnCollisionEnter с {collision.gameObject.name}");
    }

    void OnDestroy()
    {
        //Debug.Log($"[Box] {name} - OnDestroy() | IsAssigned: {IsAssigned}, OrderID: '{orderId}'");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (_homePositionSaved)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(_homePosition, Vector3.one * 0.5f);
            Gizmos.DrawLine(transform.position, _homePosition);
        }
    }
#endif
}