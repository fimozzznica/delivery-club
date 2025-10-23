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
    [Tooltip("Как часто обновлять информацию (в секундах)")]
    public float updateInterval = 0.5f;
    
    [Tooltip("Показывать подробные логи в консоли")]
    public bool debugLogs = true;

    private OrderManager orderManager;
    private bool isInitialized = false;

    void Start()
    {
        if (debugLogs) Debug.Log("[OrderDisplayCanvas] Start() - Инициализация канваса заказов");
        
        InitializeOrderManager();
        
        // Сразу обновляем дисплей
        UpdateOrderDisplay();
        
        // Запускаем регулярное обновление
        InvokeRepeating(nameof(UpdateOrderDisplay), updateInterval, updateInterval);
        
        if (debugLogs) Debug.Log($"[OrderDisplayCanvas] Запущено автообновление каждые {updateInterval} сек");
    }

    void InitializeOrderManager()
    {
        if (debugLogs) Debug.Log("[OrderDisplayCanvas] Поиск OrderManager в сцене...");
        
        orderManager = FindObjectOfType<OrderManager>();
        
        if (orderManager != null)
        {
            isInitialized = true;
            if (debugLogs) Debug.Log($"[OrderDisplayCanvas] ✅ OrderManager найден: {orderManager.name}");
        }
        else
        {
            isInitialized = false;
            Debug.LogError("[OrderDisplayCanvas] ❌ OrderManager НЕ НАЙДЕН в сцене!");
        }
    }

    void UpdateOrderDisplay()
    {
        if (debugLogs) Debug.Log("[OrderDisplayCanvas] UpdateOrderDisplay() вызван");

        // Проверяем инициализацию
        if (!isInitialized)
        {
            if (debugLogs) Debug.Log("[OrderDisplayCanvas] Не инициализирован, пытаемся найти OrderManager заново...");
            InitializeOrderManager();
            
            if (!isInitialized)
            {
                ShowNoOrder("OrderManager не найден");
                return;
            }
        }

        // Проверяем наличие активного заказа
        if (!orderManager.HasActiveOrder)
        {
            if (debugLogs) Debug.Log("[OrderDisplayCanvas] Нет активного заказа");
            ShowNoOrder("Ожидание заказа...");
            return;
        }

        // Показываем информацию о заказе
        var currentOrder = orderManager.CurrentOrder;
        if (debugLogs) Debug.Log($"[OrderDisplayCanvas] Отображаем заказ ID: {currentOrder.id}");
        
        ShowOrderInfo(currentOrder);
    }

    void ShowOrderInfo(OrderManager.Order order)
    {
        if (debugLogs) Debug.Log($"[OrderDisplayCanvas] ShowOrderInfo() для заказа {order.id}");

        // Показываем панель заказа, скрываем панель "нет заказов"
        if (orderPanel) orderPanel.SetActive(true);
        if (noOrderPanel) noOrderPanel.SetActive(false);

        // Заполняем информацию о заказе
        SetTextSafe(orderIdText, $"Заказ #{order.id}");
        SetTextSafe(itemText, $"Груз: {order.box.contentName}");
        SetTextSafe(pickupText, $"Забрать из: {order.box.pickupAddress}");
        SetTextSafe(deliveryText, $"Доставить в: {order.dropoff.deliveryAddress}");
        SetTextSafe(priceText, $"Оплата: ${order.box.price}");

        if (debugLogs)
        {
            Debug.Log($"[OrderDisplayCanvas] ✅ Отображено:");
            Debug.Log($"[OrderDisplayCanvas]   Заказ: #{order.id}");
            Debug.Log($"[OrderDisplayCanvas]   Груз: {order.box.contentName}");
            Debug.Log($"[OrderDisplayCanvas]   Забрать: {order.box.pickupAddress}");
            Debug.Log($"[OrderDisplayCanvas]   Доставить: {order.dropoff.deliveryAddress}");
            Debug.Log($"[OrderDisplayCanvas]   Оплата: ${order.box.price}");
        }
    }

    void ShowNoOrder(string message)
    {
        if (debugLogs) Debug.Log($"[OrderDisplayCanvas] ShowNoOrder(): {message}");

        // Скрываем панель заказа, показываем панель "нет заказов"
        if (orderPanel) orderPanel.SetActive(false);
        if (noOrderPanel) noOrderPanel.SetActive(true);

        // Устанавливаем сообщение
        SetTextSafe(noOrderText, message.ToUpper());

        if (debugLogs) Debug.Log($"[OrderDisplayCanvas] Показано сообщение: {message}");
    }

    void SetTextSafe(Text textComponent, string value)
    {
        if (textComponent != null)
        {
            textComponent.text = value;
        }
        else if (debugLogs)
        {
            Debug.LogWarning($"[OrderDisplayCanvas] Text компонент не назначен для значения: {value}");
        }
    }

    void OnValidate()
    {
        // Автоматические проверки в редакторе
        if (updateInterval <= 0)
        {
            updateInterval = 0.5f;
            Debug.LogWarning("[OrderDisplayCanvas] Update Interval не может быть <= 0, установлено 0.5");
        }
    }

    void OnDestroy()
    {
        if (debugLogs) Debug.Log("[OrderDisplayCanvas] OnDestroy() - Отменяем все Invoke");
        CancelInvoke();
    }

    // Методы для ручного управления (опционально)
    [ContextMenu("Force Update Display")]
    public void ForceUpdateDisplay()
    {
        Debug.Log("[OrderDisplayCanvas] Принудительное обновление дисплея");
        UpdateOrderDisplay();
    }

    [ContextMenu("Test No Order Message")]
    public void TestNoOrderMessage()
    {
        Debug.Log("[OrderDisplayCanvas] Тест сообщения 'нет заказов'");
        ShowNoOrder("ТЕСТОВОЕ СООБЩЕНИЕ");
    }

    public void SetDebugLogs(bool enabled)
    {
        debugLogs = enabled;
        Debug.Log($"[OrderDisplayCanvas] Debug логи {(enabled ? "включены" : "отключены")}");
    }
}