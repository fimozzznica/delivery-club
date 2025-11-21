using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


public class OrderManager : MonoBehaviour
{

    [System.Serializable]
    public class OrderEvent : UnityEvent<Order> { }

    [System.Serializable]
    public class OrderStateEvent : UnityEvent { }

    [Header("События")]
    public OrderEvent OnOrderCreated = new OrderEvent();
    public OrderEvent OnOrderCompleted = new OrderEvent();
    public OrderStateEvent OnOrderStateChanged = new OrderStateEvent();

    [Header("Объекты сцены")]
    [Tooltip("Коробки для доставки")]
    public Box[] boxes;

    [Tooltip("Точки доставки (автопоиск если пусто)")]
    public DropoffPoint[] dropoffs;

    [Header("Генерация заказов")]
    public bool autoGenerate = true;
    public float spawnInterval = 8f;

    [Header("Игрок")]
    public float playerBalance = 0f;

    [Range(0f, 5f)]
    public float playerRating = 4.8f;

    public int currentLevel = 4;
    
    [Serializable]
    public class Order
    {
        public string id;
        public Box box;
        public DropoffPoint dropoff;
        public bool parentWasInactive;
    }
    
    private Order _currentOrder;
    private int _idCounter = 0;
    private bool _orderStarted = false;
    
    private const float BASE_DELIVERY_PRICE = 200f;
    private const float PACKAGE_VALUE_PERCENT = 0.03f;
    
    public Order CurrentOrder => _currentOrder;
    public bool HasActiveOrder => _currentOrder != null;
    public bool IsOrderStarted => _orderStarted;
    public float PlayerBalance => playerBalance;
    public float PlayerRating => playerRating;
    public int CurrentLevel => currentLevel;

    void Awake()
    {
        InitializeDropoffs();
        InitializeBoxes();
    }

    void Start()
    {
        UpdateLevel();

        if (autoGenerate)
        {
            StartCoroutine(GenerateLoop());
        }
    }
    
    void InitializeDropoffs()
    {
        if (dropoffs == null || dropoffs.Length == 0)
        {
            dropoffs = FindObjectsOfType<DropoffPoint>();
        }

        // Фильтруем NULL и привязываем к менеджеру
        dropoffs = dropoffs.Where(d => d != null).ToArray();

        foreach (var dropoff in dropoffs)
        {
            dropoff.manager = this;
        }

        if (dropoffs.Length == 0)
        {
            Debug.LogError("[OrderManager] Не найдено ни одной точки доставки!");
        }
        else
        {
            Debug.Log($"[OrderManager] Найдено точек доставки: {dropoffs.Length}");
        }
    }
    
    void InitializeBoxes()
    {
        if (boxes == null || boxes.Length == 0)
        {
            boxes = FindObjectsOfType<Box>(true);
        }

        // Фильтруем NULL
        boxes = boxes.Where(b => b != null).ToArray();

        if (boxes.Length == 0)
        {
            Debug.LogError("[OrderManager] Не найдено ни одной коробки!");
        }
        else
        {
            Debug.Log($"[OrderManager] Найдено коробок: {boxes.Length}");
        }
    }
    
    //автогенерация заказов
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
    
    // Создать новый заказ
    public void CreateOrder()
    {
        if (HasActiveOrder)
        {
            Debug.LogWarning($"[OrderManager] Уже есть активный заказ #{_currentOrder.id}");
            return;
        }

        if (dropoffs.Length == 0)
        {
            Debug.LogError("[OrderManager] Нет точек доставки!");
            return;
        }

        // Находим подходящие коробки
        var candidates = boxes.Where(b =>
            b != null &&
            !b.gameObject.activeSelf &&
            !b.IsAssigned &&
            IsLevelUnlocked(b.level)
        ).ToList();

        if (candidates.Count == 0)
        {
            Debug.LogWarning("[OrderManager] Нет доступных коробок для заказа");
            return;
        }
        
        var box = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        var dropoff = dropoffs[UnityEngine.Random.Range(0, dropoffs.Length)];
        
        bool parentWasInactive = box.transform.parent != null &&
                                 !box.transform.parent.gameObject.activeInHierarchy;

        // Создаём заказ
        _currentOrder = new Order
        {
            id = NewId(),
            box = box,
            dropoff = dropoff,
            parentWasInactive = parentWasInactive
        };

        box.Assign(_currentOrder.id, dropoff);
        
        if (parentWasInactive)
        {
            box.transform.parent.gameObject.SetActive(true);
        }

        // Активируем коробку
        box.gameObject.SetActive(true);

        _orderStarted = false;

        Debug.Log($"[OrderManager] Заказ #{_currentOrder.id}: {box.pickupAddress} → {dropoff.deliveryAddress}");
        
        OnOrderCreated?.Invoke(_currentOrder);
        OnOrderStateChanged?.Invoke();
    }
    
    public float CalculateDeliveryPrice(Box box, DropoffPoint dropoff)
    {
        if (box == null || dropoff == null)
            return BASE_DELIVERY_PRICE;

        float price = BASE_DELIVERY_PRICE;
        price += box.price * PACKAGE_VALUE_PERCENT;
        
        float distance = Vector3.Distance(box.transform.position, dropoff.transform.position);
        price += distance * 0.03f;

        return Mathf.Round(price);
    }
    
    // Получить оплату 
    public float GetCurrentOrderPrice()
    {
        if (!HasActiveOrder)
            return 0f;

        return CalculateDeliveryPrice(_currentOrder.box, _currentOrder.dropoff);
    }
    
    public bool StartOrder()
    {
        if (!HasActiveOrder)
        {
            Debug.LogWarning("[OrderManager] Нет заказа для начала!");
            return false;
        }

        if (_orderStarted)
        {
            Debug.LogWarning("[OrderManager] Заказ уже начат!");
            return false;
        }

        _orderStarted = true;
        Debug.Log($"[OrderManager] Заказ #{_currentOrder.id} начат");

        OnOrderStateChanged?.Invoke();
        return true;
    }
    
    public bool CanPickupBox(Box box)
    {
        if (!HasActiveOrder || _currentOrder.box != box)
            return false;

        if (!_orderStarted)
        {
            Debug.LogWarning("[OrderManager] Сначала начните заказ!");
            return false;
        }

        return true;
    }
    
    public bool TryComplete(Box box, DropoffPoint atDropoff)
    {
        if (!HasActiveOrder)
        {
            Debug.LogWarning("[OrderManager] Нет активного заказа!");
            return false;
        }

        if (!_orderStarted)
        {
            Debug.LogWarning("[OrderManager] Заказ не начат!");
            return false;
        }

        if (_currentOrder.box != box)
        {
            Debug.LogWarning("[OrderManager] Это не та коробка!");
            return false;
        }

        if (_currentOrder.dropoff == null)
        {
            Debug.LogError("[OrderManager] Точка доставки отсутствует!");
            return false;
        }

        if (_currentOrder.dropoff != atDropoff)
        {
            Debug.LogWarning($"[OrderManager] Неверный адрес! Нужно: {_currentOrder.dropoff.deliveryAddress}");
            return false;
        }
        
        Order completedOrder = _currentOrder;
        float payment = CalculateDeliveryPrice(box, atDropoff);
        
        box.ReturnHome();
        box.ClearAssignment();
        box.gameObject.SetActive(false);
        
        if (completedOrder.parentWasInactive && box.transform.parent != null)
        {
            box.transform.parent.gameObject.SetActive(false);
        }
        
        AddBalance(payment);
        UpdateRatingAfterDelivery(true);
        
        _currentOrder = null;
        _orderStarted = false;

        Debug.Log($"[OrderManager] Заказ #{completedOrder.id} выполнен! +${payment:F0}, рейтинг: {playerRating:F1}");
        
        OnOrderCompleted?.Invoke(completedOrder);
        OnOrderStateChanged?.Invoke();

        return true;
    }
    
    public void AddBalance(float amount)
    {
        playerBalance += amount;
        Debug.Log($"[OrderManager] Баланс: ${playerBalance:F0} (+${amount:F0})");
    }
    
    // Обновить рейтинг 
    void UpdateRatingAfterDelivery(bool success)
    {
        if (success)
        {
            playerRating = Mathf.Min(5.0f, playerRating + 0.1f);
        }
        else
        {
            playerRating = Mathf.Max(0f, playerRating - 0.2f);
        }

        Debug.Log($"[OrderManager] Рейтинг: {playerRating:F1}");
        UpdateLevel();
    }
    

    void UpdateLevel()    // Обновить доступный уровень на основе рейтинга
    {
        if (playerRating >= 4.8f)
            currentLevel = 4;
        else if (playerRating >= 4.4f)
            currentLevel = 3;
        else if (playerRating >= 4.0f)
            currentLevel = 2;
        else
            currentLevel = 1;

        Debug.Log($"[OrderManager] Доступный уровень: {currentLevel}");
    }
    
    // Проверить доступен ли уровень заказов
    public bool IsLevelUnlocked(int level)
    {
        if (level <= 1)
            return true;

        if (level == 2)
            return playerRating >= 4.0f;

        if (level == 3)
            return playerRating >= 4.4f;

        if (level == 4)
            return playerRating >= 4.8f;

        return false;
    }
    
    // Очистить заказ 
    public void ClearCurrentOrder()
    {
        _currentOrder = null;
        _orderStarted = false;
        OnOrderStateChanged?.Invoke();
        Debug.Log("[OrderManager] Заказ очищен");
    }


    string NewId()
    {
        _idCounter++;
        return _idCounter.ToString();
    }
}
