using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class OrderManager : MonoBehaviour
{
    // События для обновления UI
    [System.Serializable]
    public class OrderEvent : UnityEvent<Order> { }

    [System.Serializable]
    public class OrderStateEvent : UnityEvent { }

    [Header("События")]
    [Tooltip("Вызывается при создании нового заказа")]
    public OrderEvent OnOrderCreated = new OrderEvent();

    [Tooltip("Вызывается при успешном завершении заказа")]
    public OrderEvent OnOrderCompleted = new OrderEvent();

    [Tooltip("Вызывается при изменении состояния заказа")]
    public OrderStateEvent OnOrderStateChanged = new OrderStateEvent();
    [Header("Scene (можно оставить пустым для авто-поиска)")]
    public Box[] boxes;
    public DropoffPoint[] dropoffs;

    [Header("Генерация заказов")]
    public bool autoGenerate = true;
    public float spawnInterval = 8f;
    // Убираем maxActiveOrders, так как максимум один заказ

    [Serializable]
    public class Order
    {
        public string id;
        public Box box;
        public DropoffPoint dropoff;
    }


    private Order _currentOrder; // Вместо списка - один текущий заказ
    private int _idCounter = 0;
    private bool _orderStarted = false; // Флаг "заказ начат"

    // Публичное свойство для доступа к текущему заказу из UI
    public Order CurrentOrder => _currentOrder;
    public bool HasActiveOrder => _currentOrder != null;
    public bool IsOrderStarted => _orderStarted;

    void Awake()
    {
        if (dropoffs == null || dropoffs.Length == 0)
        {
            dropoffs = FindObjectsOfType<DropoffPoint>();
        }

        foreach (var d in dropoffs)
        {
            if (d != null)
            {
                d.manager = this;
            }
            else
            {
                Debug.LogWarning("[OrderManager] Найден NULL DropoffPoint в массиве!");
            }
        }

        // Фильтруем NULL элементы из массива dropoffs
        dropoffs = dropoffs.Where(d => d != null).ToArray();

        if (dropoffs.Length == 0)
        {
            Debug.LogError("[OrderManager] НЕТ ВАЛИДНЫХ DROPOFFS после фильтрации!");
        }

        if (boxes == null || boxes.Length == 0)
        {
            boxes = FindObjectsOfType<Box>(true);
        }

        for (int i = 0; i < boxes.Length; i++)
        {
            if (boxes[i] != null)
            {
                // Проверяем родителей коробки
                if (!IsParentHierarchyActive(boxes[i].transform))
                {
                    string inactiveParent = GetFirstInactiveParent(boxes[i].transform);
                    Debug.LogWarning($"[OrderManager] ⚠️ Box '{boxes[i].name}' имеет неактивного родителя '{inactiveParent}' и НЕ БУДЕТ участвовать в заказах!");
                }
            }
            else
            {
                Debug.LogWarning($"[OrderManager] Box[{i}] is NULL!");
            }
        }

        // Фильтруем NULL элементы из массива boxes
        boxes = boxes.Where(b => b != null).ToArray();

        if (boxes.Length == 0)
        {
            Debug.LogError("[OrderManager] НЕТ ВАЛИДНЫХ BOXES после фильтрации!");
        }
    }

    void Start()
    {
        if (autoGenerate)
        {
            StartCoroutine(GenerateLoop());
        }
    }

    IEnumerator GenerateLoop()
    {
        var wait = new WaitForSeconds(spawnInterval);

        while (true)
        {
            if (!HasActiveOrder)
            {
                CreateOrder();
            }

            yield return wait;
        }
    }

    public void CreateOrder()
    {
        if (HasActiveOrder)
        {
            Debug.LogWarning($"[OrderManager] Уже есть активный заказ ID: {_currentOrder.id}!");
            return;
        }

        if (dropoffs == null || dropoffs.Length == 0)
        {
            Debug.LogError("[OrderManager] НЕТ DROPOFFS! Заказ не создан.");
            return;
        }

        var candidates = boxes.Where(b =>
            b != null &&
            !b.gameObject.activeSelf &&
            !b.IsAssigned &&
            IsLevelUnlocked(b.level) &&
            IsParentHierarchyActive(b.transform)
        ).ToList();

        if (candidates.Count == 0)
        {
            Debug.LogWarning("[OrderManager] Нет подходящих коробок для заказа!");
            return;
        }

        int boxIndex = UnityEngine.Random.Range(0, candidates.Count);
        var box = candidates[boxIndex];

        // Фильтруем валидные dropoffs перед случайным выбором
        var validDropoffs = dropoffs.Where(d => d != null).ToArray();
        if (validDropoffs.Length == 0)
        {
            Debug.LogError("[OrderManager] НЕТ ВАЛИДНЫХ DROPOFFS! Заказ не создан.");
            return;
        }

        int dropoffIndex = UnityEngine.Random.Range(0, validDropoffs.Length);
        var dropoff = validDropoffs[dropoffIndex];

        // Дополнительная проверка безопасности
        if (dropoff == null)
        {
            Debug.LogError("[OrderManager] Выбранный dropoff оказался NULL! Заказ не создан.");
            return;
        }

        _currentOrder = new Order
        {
            id = NewId(),
            box = box,
            dropoff = dropoff
        };

        box.Assign(_currentOrder.id, dropoff);
        box.gameObject.SetActive(true);

        // Дополнительная проверка на случай, если что-то пошло не так
        if (!box.gameObject.activeInHierarchy)
        {
            Debug.LogError($"[OrderManager] ❌ Коробка {box.name} не стала активной в иерархии! Проверьте родительские объекты!");
        }

        Debug.Log($"[OrderManager] ✅ Заказ #{_currentOrder.id} создан: '{box.pickupAddress}' ({box.contentName}) → '{dropoff.deliveryAddress}'");

        // Сбрасываем флаг "начат" для нового заказа
        _orderStarted = false;

        // Вызываем события
        OnOrderCreated?.Invoke(_currentOrder);
        OnOrderStateChanged?.Invoke();
    }

    /// <summary>
    /// Начать выполнение текущего заказа
    /// </summary>
    public bool StartOrder()
    {
        if (!HasActiveOrder)
        {
            Debug.LogWarning("[OrderManager] Нет активного заказа для начала!");
            return false;
        }

        if (_orderStarted)
        {
            return false;
        }

        _orderStarted = true;
        Debug.Log($"[OrderManager] ✅ Заказ #{_currentOrder.id} начат");

        // Вызываем событие изменения состояния
        OnOrderStateChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// Проверить, можно ли взять коробку
    /// </summary>
    public bool CanPickupBox(Box box)
    {
        if (!HasActiveOrder)
        {
            return false;
        }

        if (_currentOrder.box != box)
        {
            return false;
        }

        if (!_orderStarted)
        {
            Debug.LogWarning("[OrderManager] Заказ не начат! Нажмите 'Начать заказ' сначала.");
            return false;
        }

        return true;
    }

    public bool TryComplete(Box box, DropoffPoint atDropoff)
    {
        if (!HasActiveOrder)
        {
            Debug.LogWarning($"[OrderManager] Нет активного заказа!");
            return false;
        }

        if (!_orderStarted)
        {
            Debug.LogWarning($"[OrderManager] Заказ не начат! Нельзя завершить.");
            return false;
        }

        if (_currentOrder.box != box)
        {
            Debug.LogWarning($"[OrderManager] Коробка не является частью текущего заказа!");
            return false;
        }

        // Проверяем, что целевой dropoff не NULL
        if (_currentOrder.dropoff == null)
        {
            Debug.LogError($"[OrderManager] ❌ У заказа #{_currentOrder.id} целевой dropoff равен NULL!");
            return false;
        }

        if (_currentOrder.dropoff != atDropoff)
        {
            Debug.LogWarning($"[OrderManager] ❌ Неправильный адрес! Нужно: '{_currentOrder.dropoff.deliveryAddress}'");
            return false;
        }

        box.ReturnHome();
        box.ClearAssignment();
        box.gameObject.SetActive(false);

        Debug.Log($"[OrderManager] 🎉 Заказ #{_currentOrder.id} доставлен: '{box.contentName}' → '{atDropoff.deliveryAddress}'");

        // Сохраняем заказ для события перед очисткой
        Order completedOrder = _currentOrder;

        // Очищаем текущий заказ и флаг
        _currentOrder = null;
        _orderStarted = false;

        // Вызываем события
        OnOrderCompleted?.Invoke(completedOrder);
        OnOrderStateChanged?.Invoke();

        return true;
    }

    public bool IsLevelUnlocked(int level)
    {
        return true;
    }

    string NewId()
    {
        _idCounter++;
        return _idCounter.ToString();
    }

    /// <summary>
    /// Проверяет, активны ли все родители объекта
    /// </summary>
    bool IsParentHierarchyActive(Transform transform)
    {
        Transform parent = transform.parent;
        while (parent != null)
        {
            if (!parent.gameObject.activeSelf)
            {
                return false;
            }
            parent = parent.parent;
        }
        return true;
    }

    /// <summary>
    /// Возвращает имя первого неактивного родителя в иерархии
    /// </summary>
    string GetFirstInactiveParent(Transform transform)
    {
        Transform parent = transform.parent;
        while (parent != null)
        {
            if (!parent.gameObject.activeSelf)
            {
                return parent.name;
            }
            parent = parent.parent;
        }
        return "Unknown";
    }

    void OnDestroy()
    {
    }
}
