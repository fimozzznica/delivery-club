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

    private const float baseDeliveryPrice = 200f;
    private const float packageValuePercent = 0.03f;

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
        if (autoGenerate) { StartCoroutine(GenerateLoop()); }
    }

    void InitializeDropoffs()
    {
        if (dropoffs == null || dropoffs.Length == 0) { dropoffs = FindObjectsOfType<DropoffPoint>(); }
        dropoffs = dropoffs.Where(d => d != null).ToArray();
        foreach (var dropoff in dropoffs)
        {
            dropoff.manager = this;
        }
    }

    void InitializeBoxes()
    {
        if (boxes == null || boxes.Length == 0) { boxes = FindObjectsOfType<Box>(true); }
        boxes = boxes.Where(b => b != null).ToArray();
    }


    IEnumerator GenerateLoop()
    {
        var wait = new WaitForSeconds(spawnInterval);

        while (true)
        {
            if (!HasActiveOrder) { CreateOrder(); }
            yield return wait;
        }
    }


    public void CreateOrder()
    {
        if (HasActiveOrder || dropoffs.Length == 0) { return; }


        var candidates = boxes.Where(b =>
            b != null &&
            !b.gameObject.activeSelf &&
            !b.IsAssigned &&
            IsLevelUnlocked(b.level)
        ).ToList();

        if (candidates.Count == 0) { return; }

        var box = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        var dropoff = dropoffs[UnityEngine.Random.Range(0, dropoffs.Length)];

        bool parentWasInactive = box.transform.parent != null &&
                                 !box.transform.parent.gameObject.activeInHierarchy;
        
        _currentOrder = new Order
        {
            id = NewId(),
            box = box,
            dropoff = dropoff,
            parentWasInactive = parentWasInactive
        };

        box.Assign(_currentOrder.id, dropoff);

        if (parentWasInactive) { box.transform.parent.gameObject.SetActive(true); }

        box.gameObject.SetActive(true);
        _orderStarted = false;

        OnOrderCreated?.Invoke(_currentOrder);
        OnOrderStateChanged?.Invoke();
    }

    public float CalculateDeliveryPrice(Box box, DropoffPoint dropoff)
    {
        if (box == null || dropoff == null) { return baseDeliveryPrice; }

        float price = baseDeliveryPrice;
        price += box.price * packageValuePercent;
        float distance = Vector3.Distance(box.transform.position, dropoff.transform.position);
        price += distance * 0.03f;

        return Mathf.Round(price);
    }
    
    public float GetCurrentOrderPrice()
    {
        if (!HasActiveOrder) { return 0f; }
        return CalculateDeliveryPrice(_currentOrder.box, _currentOrder.dropoff);
    }

    public bool StartOrder()
    {
        if (!HasActiveOrder) { return false; }
        if (_orderStarted) { return false; }

        _orderStarted = true;
        OnOrderStateChanged?.Invoke();
        return true;
    }

    public bool CanPickupBox(Box box)
    {
        if (!HasActiveOrder || _currentOrder.box != box) { return false; }
        if (!_orderStarted) { return false; }

        return true;
    }

    public bool TryComplete(Box box, DropoffPoint atDropoff)
    {
        if (!HasActiveOrder) { return false; }
        if (!_orderStarted) { return false; }
        if (_currentOrder.box != box) { return false; }
        if (_currentOrder.dropoff == null) { return false; }
        if (_currentOrder.dropoff != atDropoff) { return false; }

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

        OnOrderCompleted?.Invoke(completedOrder);
        OnOrderStateChanged?.Invoke();

        return true;
    }

    public void AddBalance(float amount) { playerBalance += amount; }

    void UpdateRatingAfterDelivery(bool success)
    {
        if (success) { playerRating = Mathf.Min(5.0f, playerRating + 0.1f); }
        else { playerRating = Mathf.Max(0f, playerRating - 0.2f); }

        UpdateLevel();
    }


    void UpdateLevel()
    {
        if (playerRating >= 4.8f) { currentLevel = 4; }
        else if (playerRating >= 4.4f) { currentLevel = 3; }
        else if (playerRating >= 4.0f) { currentLevel = 2; }
        else { currentLevel = 1; }
    }


    public bool IsLevelUnlocked(int level)
    {
        if (level <= 1) { return true; }
        if (level == 2) { return playerRating >= 4.0f; }
        if (level == 3) { return playerRating >= 4.4f; }
        if (level == 4) { return playerRating >= 4.8f; }
        return false;
    }


    public void ClearCurrentOrder()
    {
        _currentOrder = null;
        _orderStarted = false;
        OnOrderStateChanged?.Invoke();

    }

    string NewId()
    {
        _idCounter++;
        return _idCounter.ToString();
    }
}
