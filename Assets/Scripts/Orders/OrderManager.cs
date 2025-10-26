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
        Debug.Log("[OrderManager] Awake() - Начинаем инициализацию (режим одного заказа)");

        if (dropoffs == null || dropoffs.Length == 0)
        {
            Debug.Log("[OrderManager] Dropoffs не заданы, ищем в сцене...");
            dropoffs = FindObjectsOfType<DropoffPoint>();
            Debug.Log($"[OrderManager] Найдено DropoffPoint в сцене: {dropoffs.Length}");
        }
        else
        {
            Debug.Log($"[OrderManager] Используем заданные Dropoffs: {dropoffs.Length}");
        }

        foreach (var d in dropoffs)
        {
            if (d != null)
            {
                d.manager = this;
                Debug.Log($"[OrderManager] Присвоен manager для DropoffPoint: {d.name} (адрес: '{d.deliveryAddress}')");
            }
            else
            {
                Debug.LogWarning("[OrderManager] Найден NULL DropoffPoint в массиве!");
            }
        }

        if (boxes == null || boxes.Length == 0)
        {
            Debug.Log("[OrderManager] Boxes не заданы, ищем в сцене (включая выключенные)...");
            boxes = FindObjectsOfType<Box>(true);
            Debug.Log($"[OrderManager] Найдено Box в сцене: {boxes.Length}");
        }
        else
        {
            Debug.Log($"[OrderManager] Используем заданные Boxes: {boxes.Length}");
        }

        for (int i = 0; i < boxes.Length; i++)
        {
            if (boxes[i] != null)
            {
                Debug.Log($"[OrderManager] Box[{i}]: {boxes[i].name}, Level: {boxes[i].level}, Content: '{boxes[i].contentName}', Price: {boxes[i].price}, PickupAddress: '{boxes[i].pickupAddress}', Active: {boxes[i].gameObject.activeInHierarchy}");
            }
            else
            {
                Debug.LogWarning($"[OrderManager] Box[{i}] is NULL!");
            }
        }

        Debug.Log("[OrderManager] Awake() завершён");
    }

    void Start()
    {
        Debug.Log($"[OrderManager] Start() - AutoGenerate: {autoGenerate}");
        if (autoGenerate)
        {
            Debug.Log($"[OrderManager] Запускаем автогенерацию с интервалом {spawnInterval} сек (один заказ за раз)");
            StartCoroutine(GenerateLoop());
        }
        else
        {
            Debug.Log("[OrderManager] Автогенерация отключена");
        }
    }

    IEnumerator GenerateLoop()
    {
        Debug.Log("[OrderManager] GenerateLoop() начат");
        var wait = new WaitForSeconds(spawnInterval);
        int loopCounter = 0;

        while (true)
        {
            loopCounter++;
            Debug.Log($"[OrderManager] GenerateLoop итерация #{loopCounter}, есть активный заказ: {HasActiveOrder}");

            if (!HasActiveOrder)
            {
                Debug.Log("[OrderManager] Нет активного заказа, пытаемся создать новый...");
                CreateOrder();
            }
            else
            {
                Debug.Log($"[OrderManager] Уже есть активный заказ ID: {_currentOrder.id}, ждём завершения...");
            }

            Debug.Log($"[OrderManager] Ждём {spawnInterval} секунд до следующей попытки...");
            yield return wait;
        }
    }

    public void CreateOrder()
    {
        Debug.Log("[OrderManager] CreateOrder() - Начинаем создание заказа");

        if (HasActiveOrder)
        {
            Debug.LogWarning($"[OrderManager] Уже есть активный заказ ID: {_currentOrder.id}! Новый заказ не создан.");
            return;
        }

        if (dropoffs == null || dropoffs.Length == 0)
        {
            Debug.LogError("[OrderManager] CreateOrder() - НЕТ DROPOFFS! Заказ не создан.");
            return;
        }

        Debug.Log("[OrderManager] Ищем подходящие коробки...");
        var candidates = boxes.Where(b =>
            b != null &&
            !b.gameObject.activeInHierarchy &&
            !b.IsAssigned &&
            IsLevelUnlocked(b.level)
        ).ToList();

        Debug.Log($"[OrderManager] Найдено кандидатов: {candidates.Count}");

        if (candidates.Count == 0)
        {
            Debug.LogWarning("[OrderManager] Нет подходящих коробок для заказа!");

            Debug.Log("[OrderManager] Диагностика коробок:");
            for (int i = 0; i < boxes.Length; i++)
            {
                if (boxes[i] != null)
                {
                    Debug.Log($"[OrderManager]   Box[{i}] {boxes[i].name}: Active={boxes[i].gameObject.activeInHierarchy}, Assigned={boxes[i].IsAssigned}, Level={boxes[i].level}, LevelUnlocked={IsLevelUnlocked(boxes[i].level)}");
                }
            }
            return;
        }

        int boxIndex = UnityEngine.Random.Range(0, candidates.Count);
        var box = candidates[boxIndex];
        Debug.Log($"[OrderManager] Выбрана коробка: {box.name} (индекс {boxIndex} из {candidates.Count} кандидатов)");

        int dropoffIndex = UnityEngine.Random.Range(0, dropoffs.Length);
        var dropoff = dropoffs[dropoffIndex];
        Debug.Log($"[OrderManager] Выбран dropoff: {dropoff.name} (адрес: '{dropoff.deliveryAddress}', индекс {dropoffIndex} из {dropoffs.Length} доступных)");

        _currentOrder = new Order
        {
            id = NewId(),
            box = box,
            dropoff = dropoff
        };

        Debug.Log($"[OrderManager] Создан заказ ID: {_currentOrder.id}");
        Debug.Log($"[OrderManager] Назначаем заказ коробке...");

        box.Assign(_currentOrder.id, dropoff);

        Debug.Log($"[OrderManager] Активируем коробку {box.name}...");
        box.gameObject.SetActive(true);

        Debug.Log($"[OrderManager] ✅ ЗАКАЗ СОЗДАН! ID: {_currentOrder.id} | Level: {box.level} | Item: '{box.contentName}' (${box.price}) | From: '{box.pickupAddress}' | To: '{dropoff.deliveryAddress}'");

        // Сбрасываем флаг "начат" для нового заказа
        _orderStarted = false;
        Debug.Log("[OrderManager] Флаг orderStarted сброшен для нового заказа");

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
            Debug.LogWarning("[OrderManager] StartOrder() - Нет активного заказа для начала!");
            return false;
        }

        if (_orderStarted)
        {
            Debug.LogWarning($"[OrderManager] StartOrder() - Заказ {_currentOrder.id} уже начат!");
            return false;
        }

        _orderStarted = true;
        Debug.Log($"[OrderManager] ✅ Заказ {_currentOrder.id} НАЧАТ! Теперь можно взять коробку.");

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
            Debug.LogWarning("[OrderManager] CanPickupBox() - Нет активного заказа!");
            return false;
        }

        if (_currentOrder.box != box)
        {
            Debug.LogWarning("[OrderManager] CanPickupBox() - Эта коробка не относится к текущему заказу!");
            return false;
        }

        if (!_orderStarted)
        {
            Debug.LogWarning("[OrderManager] CanPickupBox() - Заказ не начат! Нажмите 'Начать заказ' сначала.");
            return false;
        }

        return true;
    }

    public bool TryComplete(Box box, DropoffPoint atDropoff)
    {
        Debug.Log($"[OrderManager] TryComplete() - Коробка {box.name} попала в Dropoff {atDropoff.name}");

        if (!HasActiveOrder)
        {
            Debug.LogWarning($"[OrderManager] Нет активного заказа! Коробка {box.name} не может быть доставлена.");
            return false;
        }

        if (!_orderStarted)
        {
            Debug.LogWarning($"[OrderManager] Заказ {_currentOrder.id} не начат! Нельзя завершить.");
            return false;
        }

        if (_currentOrder.box != box)
        {
            Debug.LogWarning($"[OrderManager] Коробка {box.name} не является частью текущего заказа {_currentOrder.id}!");
            return false;
        }

        Debug.Log($"[OrderManager] Найден текущий заказ ID: {_currentOrder.id}, целевой dropoff: {_currentOrder.dropoff.name}");

        if (_currentOrder.dropoff != atDropoff)
        {
            Debug.LogWarning($"[OrderManager] ❌ НЕПРАВИЛЬНЫЙ DROPOFF! Коробка {box.name} попала в {atDropoff.name} ('{atDropoff.deliveryAddress}'), а нужно в {_currentOrder.dropoff.name} ('{_currentOrder.dropoff.deliveryAddress}')");
            return false;
        }

        Debug.Log($"[OrderManager] ✅ ПРАВИЛЬНЫЙ DROPOFF! Завершаем заказ {_currentOrder.id}");

        Debug.Log($"[OrderManager] Возвращаем коробку {box.name} в домашнюю позицию...");
        box.ReturnHome();

        Debug.Log($"[OrderManager] Очищаем назначение коробки...");
        box.ClearAssignment();

        Debug.Log($"[OrderManager] Выключаем коробку {box.name}...");
        box.gameObject.SetActive(false);

        Debug.Log($"[OrderManager] 🎉 ЗАКАЗ ДОСТАВЛЕН! ID: {_currentOrder.id} | Item: '{box.contentName}' (${box.price}) | From: '{box.pickupAddress}' | To: '{atDropoff.deliveryAddress}'");

        // Сохраняем заказ для события перед очисткой
        Order completedOrder = _currentOrder;

        // Очищаем текущий заказ и флаг
        _currentOrder = null;
        _orderStarted = false;
        Debug.Log("[OrderManager] Текущий заказ очищен, флаг orderStarted сброшен, готов к созданию нового");

        // Вызываем события
        OnOrderCompleted?.Invoke(completedOrder);
        OnOrderStateChanged?.Invoke();

        return true;
    }

    public bool IsLevelUnlocked(int level)
    {
        bool unlocked = true;
        Debug.Log($"[OrderManager] IsLevelUnlocked({level}) = {unlocked}");
        return unlocked;
    }

    string NewId()
    {
        _idCounter++;
        string id = _idCounter.ToString();
        Debug.Log($"[OrderManager] Сгенерирован новый ID: {id}");
        return id;
    }

    void OnDestroy()
    {
        Debug.Log("[OrderManager] OnDestroy() - Менеджер заказов уничтожается");
    }
}
