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

    [Header("Мини-база товаров (без ScriptableObject)")]
    [Serializable]
    public struct Item
    {
        public string name;
        public int price;
        public int minLevel; // включительно
        public int maxLevel; // включительно
    }
    public Item[] items;

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
        public string itemName;
        public int price;
    }

    private readonly List<Order> _active = new();
    private int _idCounter = 0;

    void Awake()
    {
        if (dropoffs == null || dropoffs.Length == 0)
            dropoffs = FindObjectsOfType<DropoffPoint>();
        foreach (var d in dropoffs) d.manager = this;

        if (boxes == null || boxes.Length == 0)
            boxes = FindObjectsOfType<Box>(true); // найдёт и выключенные
    }

    void Start()
    {
        if (autoGenerate)
            StartCoroutine(GenerateLoop());
    }

    IEnumerator GenerateLoop()
    {
        var wait = new WaitForSeconds(spawnInterval);
        while (true)
        {
            if (_active.Count < maxActiveOrders)
                CreateOrder();
            yield return wait;
        }
    }

    public void CreateOrder()
    {
        if (dropoffs == null || dropoffs.Length == 0) return;

        // Берём выключенные коробки нужных уровней (пока все уровни доступны)
        var candidates = boxes.Where(b =>
            b != null &&
            !b.gameObject.activeInHierarchy &&
            !b.IsAssigned &&
            IsLevelUnlocked(b.level)
        ).ToList();

        if (candidates.Count == 0) return;

        var box = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        var dropoff = dropoffs[UnityEngine.Random.Range(0, dropoffs.Length)];

        // Выбираем товар под уровень коробки (если задан список items)
        string itemName = box.contentName;
        int price = box.price;
        if (items != null && items.Length > 0)
        {
            var pool = items.Where(i => i.minLevel <= box.level && box.level <= i.maxLevel).ToList();
            if (pool.Count == 0) pool = items.ToList();
            var it = pool[UnityEngine.Random.Range(0, pool.Count)];
            itemName = it.name;
            price = it.price;
        }

        box.contentName = itemName;
        box.price = price;

        // Активируем коробку, назначаем заказ
        var order = new Order
        {
            id = NewId(),
            box = box,
            dropoff = dropoff,
            itemName = itemName,
            price = price
        };

        box.Assign(order.id, dropoff);

        box.gameObject.SetActive(true);
        _active.Add(order);
        // Debug.Log($"Order created: {order.id} | L{box.level} | {itemName} (${price}) -> {dropoff.name}");
    }

    public bool TryComplete(Box box, DropoffPoint atDropoff)
    {
        int idx = _active.FindIndex(o => o.box == box);
        if (idx < 0) return false;

        var order = _active[idx];
        if (order.dropoff != atDropoff) return false;

        _active.RemoveAt(idx);

        // Возвращаем коробку “домой” без кешей: локально к родителю (его позиция — старт)
        if (box.transform.parent != null)
        {
            box.transform.localPosition = Vector3.zero;
            box.transform.localRotation = Quaternion.identity;
        }
        var rb = box.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        box.ClearAssignment();
        box.gameObject.SetActive(false);

        // Debug.Log($"Order delivered: {order.id} | {order.itemName} (${order.price}) -> {atDropoff.name}");
        return true;
    }

    // Заглушка: все уровни доступны
    public bool IsLevelUnlocked(int level) => true;

    string NewId() => (++_idCounter).ToString();
}