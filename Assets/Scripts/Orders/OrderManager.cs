using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OrderManager : MonoBehaviour
{

    [Header("Scene (можно оставить пустым для авто-поиска)")]
    public Box[] boxes;
    public DropoffPoint[] dropoffs;

    [Header("Генерация заказов")]
    public bool autoGenerate = true;
    public float spawnInterval = 8f;
    public int maxActiveOrders = 3;

    [Serializable]
    public class Order
    {
        public string id;
        public Box box;
        public DropoffPoint dropoff;
    }

    private readonly List<Order> _active = new();
    private int _idCounter = 0;

    void Awake()
    {
        Debug.Log("[OrderManager] Awake() - Начинаем инициализацию");
        
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

        // Присваиваем себя каждому dropoff
        foreach (var d in dropoffs)
        {
            if (d != null)
            {
                d.manager = this;
                Debug.Log($"[OrderManager] Присвоен manager для DropoffPoint: {d.name}");
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

        // Проверяем каждую коробку
        for (int i = 0; i < boxes.Length; i++)
        {
            if (boxes[i] != null)
            {
                Debug.Log($"[OrderManager] Box[{i}]: {boxes[i].name}, Level: {boxes[i].level}, Active: {boxes[i].gameObject.activeInHierarchy}, Content: '{boxes[i].contentName}', Price: {boxes[i].price}, HasHome: {boxes[i].HasHomePosition}");
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
            Debug.Log($"[OrderManager] Запускаем автогенерацию с интервалом {spawnInterval} сек, макс заказов: {maxActiveOrders}");
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
            Debug.Log($"[OrderManager] GenerateLoop итерация #{loopCounter}, активных заказов: {_active.Count}/{maxActiveOrders}");
            
            if (_active.Count < maxActiveOrders)
            {
                Debug.Log("[OrderManager] Пытаемся создать новый заказ...");
                CreateOrder();
            }
            else
            {
                Debug.Log("[OrderManager] Достигнуто максимальное количество активных заказов, ждём...");
            }
            
            Debug.Log($"[OrderManager] Ждём {spawnInterval} секунд до следующей попытки...");
            yield return wait;
        }
    }

    public void CreateOrder()
    {
        Debug.Log("[OrderManager] CreateOrder() - Начинаем создание заказа");
        
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
            
            // Дополнительная диагностика
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

        // Выбираем случайную коробку
        int boxIndex = UnityEngine.Random.Range(0, candidates.Count);
        var box = candidates[boxIndex];
        Debug.Log($"[OrderManager] Выбрана коробка: {box.name} (индекс {boxIndex} из {candidates.Count} кандидатов)");

        // Выбираем случайный dropoff
        int dropoffIndex = UnityEngine.Random.Range(0, dropoffs.Length);
        var dropoff = dropoffs[dropoffIndex];
        Debug.Log($"[OrderManager] Выбран dropoff: {dropoff.name} (индекс {dropoffIndex} из {dropoffs.Length} доступных)");

        // Создаём заказ
        var order = new Order
        {
            id = NewId(),
            box = box,
            dropoff = dropoff
        };

        Debug.Log($"[OrderManager] Создан заказ ID: {order.id}");
        Debug.Log($"[OrderManager] Назначаем заказ коробке...");
        
        box.Assign(order.id, dropoff);
        
        Debug.Log($"[OrderManager] Активируем коробку {box.name}...");
        box.gameObject.SetActive(true);
        // При активации коробка автоматически сохранит домашнюю позицию в OnEnable()
        
        _active.Add(order);
        
        Debug.Log($"[OrderManager] ✅ ЗАКАЗ СОЗДАН! ID: {order.id} | Level: {box.level} | Item: '{box.contentName}' (${box.price}) | From: {box.transform.position} | To: {dropoff.name}");
        Debug.Log($"[OrderManager] Всего активных заказов теперь: {_active.Count}");
    }

    public bool TryComplete(Box box, DropoffPoint atDropoff)
    {
        Debug.Log($"[OrderManager] TryComplete() - Коробка {box.name} попала в Dropoff {atDropoff.name}");
        
        int idx = _active.FindIndex(o => o.box == box);
        Debug.Log($"[OrderManager] Ищем заказ для коробки {box.name}... найден индекс: {idx}");
        
        if (idx < 0)
        {
            Debug.LogWarning($"[OrderManager] Коробка {box.name} не найдена в активных заказах! Возможно, заказ уже завершён или коробка не назначена.");
            return false;
        }

        var order = _active[idx];
        Debug.Log($"[OrderManager] Найден заказ ID: {order.id}, целевой dropoff: {order.dropoff.name}");
        
        if (order.dropoff != atDropoff)
        {
            Debug.LogWarning($"[OrderManager] ❌ НЕПРАВИЛЬНЫЙ DROPOFF! Коробка {box.name} попала в {atDropoff.name}, а нужно в {order.dropoff.name}");
            return false;
        }

        Debug.Log($"[OrderManager] ✅ ПРАВИЛЬНЫЙ DROPOFF! Завершаем заказ {order.id}");
        
        _active.RemoveAt(idx);
        Debug.Log($"[OrderManager] Заказ удалён из активных. Осталось активных: {_active.Count}");

        // Возвращаем коробку домой (в сохранённую позицию)
        Debug.Log($"[OrderManager] Возвращаем коробку {box.name} в домашнюю позицию...");
        box.ReturnHome();

        Debug.Log($"[OrderManager] Очищаем назначение коробки...");
        box.ClearAssignment();
        
        Debug.Log($"[OrderManager] Выключаем коробку {box.name}...");
        box.gameObject.SetActive(false);

        Debug.Log($"[OrderManager] 🎉 ЗАКАЗ ДОСТАВЛЕН! ID: {order.id} | Item: '{box.contentName}' (${box.price}) | Dropoff: {atDropoff.name}");
        return true;
    }

    public bool IsLevelUnlocked(int level)
    {
        bool unlocked = true; // Заглушка - все уровни доступны
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