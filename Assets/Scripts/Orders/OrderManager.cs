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

    [Header("Игрок")]
    [Tooltip("Баланс игрока")]
    public float playerBalance = 0f;

    [Tooltip("Рейтинг игрока")]
    [Range(0f, 5f)]
    public float playerRating = 4.8f;

    [Tooltip("Текущий доступный уровень заказов")]
    public int currentLevel = 4;

    [Serializable]
    public class Order
    {
        public string id;
        public Box box;
        public DropoffPoint dropoff;
    }


    private Order _currentOrder;
    private int _idCounter = 0;
    private bool _orderStarted = false;

    // Константы для формулы оплаты
    private const float BASE_DELIVERY_PRICE = 50f;
    private const float PACKAGE_VALUE_PERCENT = 0.03f; // 3%

    public Order CurrentOrder => _currentOrder;
    public bool HasActiveOrder => _currentOrder != null;
    public bool IsOrderStarted => _orderStarted;

    // Свойства для UI
    public float PlayerBalance => playerBalance;
    public float PlayerRating => playerRating;
    public int CurrentLevel => currentLevel;

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
        // Инициализируем уровень на основе текущего рейтинга
        UpdateLevel();

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
    /// Рассчитать оплату за доставку
    /// </summary>
    public float CalculateDeliveryPrice(Box box, DropoffPoint dropoff)
    {
        if (box == null || dropoff == null)
            return BASE_DELIVERY_PRICE;

        // Базовая цена
        float price = BASE_DELIVERY_PRICE;

        // 3% от стоимости посылки
        price += box.price * PACKAGE_VALUE_PERCENT;

        // Процент за расстояние (если есть позиции)
        if (box.transform != null && dropoff.transform != null)
        {
            float distance = Vector3.Distance(box.transform.position, dropoff.transform.position);
            // Добавляем ~1$ за каждые 10 единиц расстояния
            price += distance * 0.1f;
        }

        return Mathf.Round(price);
    }

    /// <summary>
    /// Получить оплату за текущий заказ
    /// </summary>
    public float GetCurrentOrderPrice()
    {
        if (!HasActiveOrder)
            return 0f;

        return CalculateDeliveryPrice(_currentOrder.box, _currentOrder.dropoff);
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
Debug.Log($"[OrderManager] 🎉 ЗАКАЗ ДОСТАВЛЕН! ID: {_currentOrder.id} | Item: '{box.contentName}' (${box.price}) | From: '{box.pickupAddress}' | To: '{atDropoff.deliveryAddress}'");

// Сохраняем заказ для события перед очисткой
Order completedOrder = _currentOrder;

// Начисляем оплату
float payment = CalculateDeliveryPrice(box, atDropoff);
AddBalance(payment);
Debug.Log($"[OrderManager] Начислено за доставку: ${payment:F0}");

// Обновляем рейтинг
UpdateRatingAfterDelivery(true);

// Очищаем текущий заказ и флаг
_currentOrder = null;
_orderStarted = false;
Debug.Log("[OrderManager] Текущий заказ очищен, флаг orderStarted сброшен, готов к созданию нового");

// Обновляем рейтинг после успешной доставки
UpdateRatingAfterDelivery(true);

// Вызываем события
OnOrderCompleted?.Invoke(completedOrder);
OnOrderStateChanged?.Invoke();

return true;
}
</text>


/// <summary>
/// Добавить к балансу игрока
/// </summary>
public void AddBalance(float amount)
{
playerBalance += amount;
Debug.Log($"[OrderManager] Баланс пополнен на ${amount:F0}. Новый баланс: ${playerBalance:F0}");
}

/// <summary>
/// Обновить рейтинг после доставки
/// </summary>
    private void UpdateRatingAfterDelivery(bool success)
    {
        if (success)
        {
            // Плавное повышение рейтинга к 5.0
            if (playerRating < 5.0f)
            {
                playerRating = Mathf.Min(5.0f, playerRating + 0.1f);
            }
        }
        else
        {
            // Понижение при неудаче
            playerRating = Mathf.Max(0f, playerRating - 0.2f);
        }

        Debug.Log($"[OrderManager] Рейтинг обновлён: {playerRating:F1}");
        UpdateLevel();
    }

    /// <summary>
    /// Обновить доступный уровень на основе рейтинга
    /// </summary>
    private void UpdateLevel()
    {
        if (playerRating >= 4.8f)
            currentLevel = 4;
        else if (playerRating >= 4.4f)
            currentLevel = 3;
        else if (playerRating >= 4.0f)
            currentLevel = 2;
        else
            currentLevel = 1;

        Debug.Log($"[OrderManager] Доступный уровень заказов: {currentLevel} (рейтинг: {playerRating:F1})");
    }

    public bool IsLevelUnlocked(int level)
    {
        // Уровень 1 всегда доступен
        if (level <= 1)
            return true;

        // Уровень 2: рейтинг >= 4.0
        if (level == 2)
            return playerRating >= 4.0f;

        // Уровень 3: рейтинг >= 4.4
        if (level == 3)
            return playerRating >= 4.4f;

        // Уровень 4: рейтинг >= 4.8
        if (level == 4)
            return playerRating >= 4.8f;

        return false;
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
