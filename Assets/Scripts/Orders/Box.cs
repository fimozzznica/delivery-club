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

    [Header("References")]
    [Tooltip("Ссылка на OrderManager для проверки разрешений")]
    public OrderManager orderManager;

    [Header("Home Position (saved on first activation)")]
    [SerializeField] private bool _homePositionSaved = false;
    [SerializeField] private Vector3 _homePosition;
    [SerializeField] private Quaternion _homeRotation;

    public bool IsAssigned => !string.IsNullOrEmpty(orderId);
    public bool HasHomePosition => _homePositionSaved;

    private Rigidbody _rigidbody;
    private bool _canBePickedUp = false;

    void Awake()
    {
        var collider = GetComponent<Collider>();
        _rigidbody = GetComponent<Rigidbody>();

        // Ищем OrderManager если не назначен
        if (orderManager == null)
        {
            orderManager = FindObjectOfType<OrderManager>();
        }
    }

    void OnEnable()
    {
        // Сохраняем домашнюю позицию при первой активации
        if (!_homePositionSaved)
        {
            SaveHomePosition();
        }

        // Делаем коробку кинематической до разрешения на взятие
        _canBePickedUp = false;
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
        }
    }

    public void SaveHomePosition()
    {
        if (_homePositionSaved)
        {
            return;
        }

        _homePosition = transform.position;
        _homeRotation = transform.rotation;
        _homePositionSaved = true;
    }

    public void ReturnHome()
    {
        if (!_homePositionSaved)
        {
            Debug.LogWarning($"[Box] {name} - домашняя позиция не сохранена!");
            return;
        }

        transform.SetPositionAndRotation(_homePosition, _homeRotation);

        // Обнуляем физику
        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void Assign(string id, DropoffPoint dropoff)
    {
        orderId = id;
        assignedDropoff = dropoff;
        name = $"Box_{orderId}";

        // Блокируем взятие до начала заказа
        _canBePickedUp = false;
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
        }
    }

    public void ClearAssignment()
    {
        orderId = null;
        assignedDropoff = null;
        name = "Box";
    }

    /// <summary>
    /// Попытка взять коробку (вызывается из системы взаимодействия)
    /// </summary>
    public bool TryPickup()
    {
        if (!IsAssigned || orderManager == null)
        {
            return false;
        }

        // Проверяем разрешение через OrderManager
        if (!orderManager.CanPickupBox(this))
        {
            return false;
        }

        // Разрешаем взятие
        _canBePickedUp = true;
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false;
        }

        return true;
    }

    /// <summary>
    /// Проверить, можно ли взять коробку
    /// </summary>
    public bool CanPickup()
    {
        if (!IsAssigned || orderManager == null)
            return false;

        return orderManager.CanPickupBox(this);
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
