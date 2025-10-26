using UnityEngine;
using UnityEngine.UI;

public class OrderScreen : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Панель с информацией о заказе")]
    public GameObject orderPanel;

    [Tooltip("Панель когда нет заказов")]
    public GameObject noOrderPanel;

    [Header("Order Info Texts")]
    [Tooltip("Текст с номером заказа")]
    public Text orderIdText;

    [Tooltip("Текст с названием груза")]
    public Text itemText;

    [Tooltip("Текст с адресом забора")]
    public Text pickupText;

    [Tooltip("Текст с адресом доставки")]
    public Text deliveryText;

    [Tooltip("Текст с ценой")]
    public Text priceText;

    [Header("No Order Text")]
    [Tooltip("Текст когда нет заказов")]
    public Text noOrderText;

    [Header("Settings")]
    [Tooltip("Показывать подробные логи в консоли")]
    public bool debugLogs = false;

    [Tooltip("Использовать систему событий (рекомендуется) или polling")]
    public bool useEvents = true;

    [Tooltip("Интервал обновления при polling (если события отключены)")]
    public float pollingInterval = 0.5f;

    private OrderManager orderManager;
    private bool isInitialized = false;
    private int failedSearchAttempts = 0;
    private const int MAX_SEARCH_ATTEMPTS = 5;
    private bool subscribedToEvents = false;

    void Start()
    {
        if (debugLogs) Debug.Log("[OrderScreen] Start() - Инициализация экрана заказов");

        InitializeOrderManager();

        // Сразу обновляем дисплей
        UpdateOrderDisplay();

        if (useEvents && isInitialized)
        {
            // Подписываемся на события
            SubscribeToEvents();
            if (debugLogs) Debug.Log("[OrderScreen] Использование системы событий для обновления UI");
        }
        else
        {
            // Запускаем регулярное обновление (polling)
            InvokeRepeating(nameof(UpdateOrderDisplay), pollingInterval, pollingInterval);
            if (debugLogs) Debug.Log($"[OrderScreen] Запущено polling обновление каждые {pollingInterval} сек");
        }
    }

    void InitializeOrderManager()
    {
        if (debugLogs) Debug.Log("[OrderScreen] Поиск OrderManager в сцене...");

        orderManager = FindObjectOfType<OrderManager>();

        if (orderManager != null)
        {
            isInitialized = true;
            failedSearchAttempts = 0;
            if (debugLogs) Debug.Log($"[OrderScreen] ✅ OrderManager найден: {orderManager.name}");

            // Подписываемся на события если используем событийную систему
            if (useEvents && !subscribedToEvents)
            {
                SubscribeToEvents();
            }
        }
        else
        {
            isInitialized = false;
            failedSearchAttempts++;
            Debug.LogError("[OrderScreen] ❌ OrderManager НЕ НАЙДЕН в сцене!");
        }
    }

    void SubscribeToEvents()
    {
        if (orderManager == null || subscribedToEvents)
            return;

        orderManager.OnOrderStateChanged.AddListener(OnOrderStateChangedHandler);
        subscribedToEvents = true;

        if (debugLogs) Debug.Log("[OrderScreen] ✅ Подписка на события OrderManager выполнена");
    }

    void UnsubscribeFromEvents()
    {
        if (orderManager == null || !subscribedToEvents)
            return;

        orderManager.OnOrderStateChanged.RemoveListener(OnOrderStateChangedHandler);
        subscribedToEvents = false;

        if (debugLogs) Debug.Log("[OrderScreen] Отписка от событий OrderManager выполнена");
    }

    void OnOrderStateChangedHandler()
    {
        if (debugLogs) Debug.Log("[OrderScreen] OnOrderStateChanged event получен, обновляем UI");
        UpdateOrderDisplay();
    }

    void UpdateOrderDisplay()
    {
        if (debugLogs) Debug.Log("[OrderScreen] UpdateOrderDisplay() вызван");

        // Проверяем инициализацию
        if (!isInitialized || orderManager == null)
        {
            // Ограничиваем количество попыток поиска, чтобы не спамить FindObjectOfType
            if (failedSearchAttempts < MAX_SEARCH_ATTEMPTS)
            {
                if (debugLogs) Debug.Log("[OrderScreen] Не инициализирован, пытаемся найти OrderManager заново...");
                InitializeOrderManager();
            }

            if (!isInitialized || orderManager == null)
            {
                ShowNoOrder("OrderManager не найден");
                return;
            }
        }

        // Проверяем наличие активного заказа
        if (!orderManager.HasActiveOrder)
        {
            if (debugLogs) Debug.Log("[OrderScreen] Нет активного заказа");
            ShowNoOrder("Ожидание заказа...");
            return;
        }

        // Получаем текущий заказ и проверяем на null
        var currentOrder = orderManager.CurrentOrder;
        if (currentOrder == null)
        {
            if (debugLogs) Debug.LogWarning("[OrderScreen] CurrentOrder вернул null!");
            ShowNoOrder("Ошибка загрузки заказа");
            return;
        }

        if (debugLogs) Debug.Log($"[OrderScreen] Отображаем заказ ID: {currentOrder.id}");

        ShowOrderInfo(currentOrder);
    }

    void ShowOrderInfo(OrderManager.Order order)
    {
        // Проверяем заказ на null
        if (order == null)
        {
            Debug.LogError("[OrderScreen] ShowOrderInfo() - order is null!");
            ShowNoOrder("Ошибка данных заказа");
            return;
        }

        // Проверяем компоненты заказа
        if (order.box == null)
        {
            Debug.LogError("[OrderScreen] ShowOrderInfo() - order.box is null!");
            ShowNoOrder("Ошибка данных коробки");
            return;
        }

        if (order.dropoff == null)
        {
            Debug.LogError("[OrderScreen] ShowOrderInfo() - order.dropoff is null!");
            ShowNoOrder("Ошибка данных точки доставки");
            return;
        }

        if (debugLogs) Debug.Log($"[OrderScreen] ShowOrderInfo() для заказа {order.id}");

        // Показываем панель заказа, скрываем панель "нет заказов"
        if (orderPanel) orderPanel.SetActive(true);
        if (noOrderPanel) noOrderPanel.SetActive(false);

        // Заполняем информацию о заказе
        SetTextSafe(orderIdText, $"Заказ #{order.id}");
        SetTextSafe(itemText, $"Груз: {order.box.contentName ?? "Неизвестно"}");
        SetTextSafe(pickupText, $"Забрать из: {order.box.pickupAddress ?? "Неизвестно"}");
        SetTextSafe(deliveryText, $"Доставить в: {order.dropoff.deliveryAddress ?? "Неизвестно"}");
        SetTextSafe(priceText, $"Оплата: ${order.box.price}");

        if (debugLogs)
        {
            Debug.Log($"[OrderScreen] ✅ Отображено:");
            Debug.Log($"[OrderScreen]   Заказ: #{order.id}");
            Debug.Log($"[OrderScreen]   Груз: {order.box.contentName}");
            Debug.Log($"[OrderScreen]   Забрать: {order.box.pickupAddress}");
            Debug.Log($"[OrderScreen]   Доставить: {order.dropoff.deliveryAddress}");
            Debug.Log($"[OrderScreen]   Оплата: ${order.box.price}");
        }
    }

    void ShowNoOrder(string message)
    {
        if (debugLogs) Debug.Log($"[OrderScreen] ShowNoOrder(): {message}");

        // Скрываем панель заказа, показываем панель "нет заказов"
        if (orderPanel) orderPanel.SetActive(false);
        if (noOrderPanel) noOrderPanel.SetActive(true);

        // Устанавливаем сообщение
        SetTextSafe(noOrderText, message.ToUpper());

        if (debugLogs) Debug.Log($"[OrderScreen] Показано сообщение: {message}");
    }

    void SetTextSafe(Text textComponent, string value)
    {
        if (textComponent != null)
        {
            textComponent.text = value;
        }
        else if (debugLogs)
        {
            Debug.LogWarning($"[OrderScreen] Text компонент не назначен для значения: {value}");
        }
    }

    void OnValidate()
    {
        // Автоматические проверки в редакторе
        if (pollingInterval <= 0)
        {
            pollingInterval = 0.5f;
            Debug.LogWarning("[OrderScreen] Polling Interval не может быть <= 0, установлено 0.5");
        }
    }

    void OnEnable()
    {
        // Подписываемся при включении, если уже инициализированы
        if (useEvents && isInitialized && !subscribedToEvents)
        {
            SubscribeToEvents();
        }
    }

    void OnDisable()
    {
        // Отписываемся при выключении
        if (subscribedToEvents)
        {
            UnsubscribeFromEvents();
        }
    }

    void OnDestroy()
    {
        if (debugLogs) Debug.Log("[OrderScreen] OnDestroy() - Очистка ресурсов");

        // Отписываемся от событий
        UnsubscribeFromEvents();

        // Отменяем polling если был активен
        CancelInvoke();
    }

    // Методы для ручного управления (опционально)
    [ContextMenu("Force Update Display")]
    public void ForceUpdateDisplay()
    {
        Debug.Log("[OrderScreen] Принудительное обновление дисплея");
        UpdateOrderDisplay();
    }

    [ContextMenu("Test No Order Message")]
    public void TestNoOrderMessage()
    {
        Debug.Log("[OrderScreen] Тест сообщения 'нет заказов'");
        ShowNoOrder("ТЕСТОВОЕ СООБЩЕНИЕ");
    }

    public void SetDebugLogs(bool enabled)
    {
        debugLogs = enabled;
        Debug.Log($"[OrderScreen] Debug логи {(enabled ? "включены" : "отключены")}");
    }

    // Публичный метод для принудительного обновления (может вызываться из OrderManager)
    public void RefreshDisplay()
    {
        UpdateOrderDisplay();
    }
}
