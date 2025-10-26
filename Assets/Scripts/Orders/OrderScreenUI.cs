using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI экран заказов с кнопками управления и отображением информации
/// </summary>
public class OrderScreenUI : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Панель с информацией о заказе")]
    public GameObject orderInfoPanel;

    [Tooltip("Панель когда нет заказов")]
    public GameObject noOrderPanel;

    [Header("Order Info Texts")]
    [Tooltip("Текст с номером заказа")]
    public Text orderIdText;

    [Tooltip("Текст с адресом забора")]
    public Text pickupText;

    [Tooltip("Текст с адресом доставки")]
    public Text deliveryText;

    [Tooltip("Текст с ценой доставки")]
    public Text deliveryPriceText;

    [Header("Balance")]
    [Tooltip("Текст с балансом игрока")]
    public Text balanceText;

    [Header("No Order")]
    [Tooltip("Текст когда нет заказов")]
    public Text noOrderText;

    [Header("Buttons")]
    [Tooltip("Кнопка Начать/Завершить заказ")]
    public Button actionButton;

    [Tooltip("Текст на кнопке действия")]
    public Text actionButtonText;

    [Header("References")]
    [Tooltip("Ссылка на OrderManager")]
    public OrderManager orderManager;

    [Header("Settings")]
    [Tooltip("Показывать подробные логи")]
    public bool debugLogs = false;

    [Tooltip("Использовать систему событий")]
    public bool useEvents = true;

    // Временные данные (заглушки)
    private float playerBalance = 0f;
    private const float DELIVERY_PRICE_STUB = 50f; // Заглушка для цены доставки

    private bool isInitialized = false;
    private bool subscribedToEvents = false;

    void Start()
    {
        if (debugLogs) Debug.Log("[OrderScreenUI] Start() - Инициализация экрана заказов");

        InitializeOrderManager();

        // Подписываемся на кнопку действия
        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnActionButtonClick);
            if (debugLogs) Debug.Log("[OrderScreenUI] Подписка на кнопку действия");
        }
        else
        {
            Debug.LogError("[OrderScreenUI] ActionButton не назначена!");
        }

        // Сразу обновляем дисплей
        UpdateDisplay();

        if (useEvents && isInitialized)
        {
            SubscribeToEvents();
        }
    }

    void InitializeOrderManager()
    {
        if (debugLogs) Debug.Log("[OrderScreenUI] Поиск OrderManager в сцене...");

        if (orderManager == null)
        {
            orderManager = FindObjectOfType<OrderManager>();
        }

        if (orderManager != null)
        {
            isInitialized = true;
            if (debugLogs) Debug.Log($"[OrderScreenUI] ✅ OrderManager найден: {orderManager.name}");
        }
        else
        {
            isInitialized = false;
            Debug.LogError("[OrderScreenUI] ❌ OrderManager НЕ НАЙДЕН в сцене!");
        }
    }

    void SubscribeToEvents()
    {
        if (orderManager == null || subscribedToEvents)
            return;

        orderManager.OnOrderStateChanged.AddListener(OnOrderStateChangedHandler);
        subscribedToEvents = true;

        if (debugLogs) Debug.Log("[OrderScreenUI] ✅ Подписка на события OrderManager выполнена");
    }

    void UnsubscribeFromEvents()
    {
        if (orderManager == null || !subscribedToEvents)
            return;

        orderManager.OnOrderStateChanged.RemoveListener(OnOrderStateChangedHandler);
        subscribedToEvents = false;

        if (debugLogs) Debug.Log("[OrderScreenUI] Отписка от событий OrderManager выполнена");
    }

    void OnOrderStateChangedHandler()
    {
        if (debugLogs) Debug.Log("[OrderScreenUI] OnOrderStateChanged event получен, обновляем UI");
        UpdateDisplay();
    }

    /// <summary>
    /// Обновить весь UI
    /// </summary>
    public void UpdateDisplay()
    {
        if (debugLogs) Debug.Log("[OrderScreenUI] UpdateDisplay() вызван");

        if (!isInitialized || orderManager == null)
        {
            if (debugLogs) Debug.Log("[OrderScreenUI] OrderManager не инициализирован");
            ShowNoOrder("OrderManager не найден");
            UpdateActionButton(false, false);
            return;
        }

        // Обновляем баланс
        UpdateBalance();

        // Проверяем наличие активного заказа
        if (!orderManager.HasActiveOrder)
        {
            if (debugLogs) Debug.Log("[OrderScreenUI] Нет активного заказа");
            ShowNoOrder("Ожидание заказа...");
            UpdateActionButton(false, false);
            return;
        }

        // Получаем текущий заказ
        var currentOrder = orderManager.CurrentOrder;
        if (currentOrder == null || currentOrder.box == null || currentOrder.dropoff == null)
        {
            if (debugLogs) Debug.LogWarning("[OrderScreenUI] Заказ или его компоненты равны null!");
            ShowNoOrder("Ошибка загрузки заказа");
            UpdateActionButton(false, false);
            return;
        }

        if (debugLogs) Debug.Log($"[OrderScreenUI] Отображаем заказ ID: {currentOrder.id}");

        // Показываем информацию о заказе
        ShowOrderInfo(currentOrder);

        // Обновляем кнопку в зависимости от состояния заказа
        bool orderStarted = orderManager.IsOrderStarted;
        UpdateActionButton(true, orderStarted);
    }

    /// <summary>
    /// Показать информацию о заказе
    /// </summary>
    void ShowOrderInfo(OrderManager.Order order)
    {
        if (debugLogs) Debug.Log($"[OrderScreenUI] ShowOrderInfo() для заказа {order.id}");

        // Показываем панель заказа, скрываем панель "нет заказов"
        if (orderInfoPanel) orderInfoPanel.SetActive(true);
        if (noOrderPanel) noOrderPanel.SetActive(false);

        // Заполняем информацию о заказе
        SetTextSafe(orderIdText, $"Заказ #{order.id}");
        SetTextSafe(pickupText, $"Забрать: {order.box.pickupAddress ?? "Неизвестно"}");
        SetTextSafe(deliveryText, $"Доставить: {order.dropoff.deliveryAddress ?? "Неизвестно"}");
        SetTextSafe(deliveryPriceText, $"Оплата: ${DELIVERY_PRICE_STUB:F0}");

        if (debugLogs)
        {
            Debug.Log($"[OrderScreenUI] ✅ Отображено:");
            Debug.Log($"[OrderScreenUI]   Заказ: #{order.id}");
            Debug.Log($"[OrderScreenUI]   Забрать: {order.box.pickupAddress}");
            Debug.Log($"[OrderScreenUI]   Доставить: {order.dropoff.deliveryAddress}");
            Debug.Log($"[OrderScreenUI]   Оплата: ${DELIVERY_PRICE_STUB:F0}");
        }
    }

    /// <summary>
    /// Показать сообщение "нет заказа"
    /// </summary>
    void ShowNoOrder(string message)
    {
        if (debugLogs) Debug.Log($"[OrderScreenUI] ShowNoOrder(): {message}");

        // Скрываем панель заказа, показываем панель "нет заказов"
        if (orderInfoPanel) orderInfoPanel.SetActive(false);
        if (noOrderPanel) noOrderPanel.SetActive(true);

        // Устанавливаем сообщение
        SetTextSafe(noOrderText, message.ToUpper());

        if (debugLogs) Debug.Log($"[OrderScreenUI] Показано сообщение: {message}");
    }

    /// <summary>
    /// Обновить отображение баланса
    /// </summary>
    void UpdateBalance()
    {
        if (balanceText != null)
        {
            balanceText.text = $"${playerBalance:F0}";
        }
        else if (debugLogs)
        {
            Debug.LogWarning("[OrderScreenUI] BalanceText не назначен!");
        }
    }

    /// <summary>
    /// Обновить состояние кнопки действия
    /// </summary>
    void UpdateActionButton(bool hasOrder, bool orderStarted)
    {
        if (actionButton == null)
            return;

        // Кнопка активна только если есть заказ
        actionButton.interactable = hasOrder;

        if (actionButtonText != null)
        {
            if (!hasOrder)
            {
                actionButtonText.text = "Нет заказа";
            }
            else if (!orderStarted)
            {
                actionButtonText.text = "Начать заказ";
            }
            else
            {
                actionButtonText.text = "Завершить заказ";
            }
        }

        if (debugLogs)
        {
            Debug.Log($"[OrderScreenUI] Кнопка обновлена: hasOrder={hasOrder}, orderStarted={orderStarted}");
        }
    }

    /// <summary>
    /// Обработчик нажатия на кнопку действия
    /// </summary>
    void OnActionButtonClick()
    {
        if (debugLogs) Debug.Log("[OrderScreenUI] OnActionButtonClick()");

        if (!isInitialized || orderManager == null)
        {
            Debug.LogError("[OrderScreenUI] OrderManager не инициализирован!");
            return;
        }

        if (!orderManager.HasActiveOrder)
        {
            Debug.LogWarning("[OrderScreenUI] Нет активного заказа для действия!");
            return;
        }

        if (!orderManager.IsOrderStarted)
        {
            // Начать заказ
            if (debugLogs) Debug.Log("[OrderScreenUI] Начинаем заказ...");

            bool started = orderManager.StartOrder();
            if (started)
            {
                if (debugLogs) Debug.Log("[OrderScreenUI] ✅ Заказ успешно начат!");
                // UI обновится автоматически через событие
            }
            else
            {
                Debug.LogWarning("[OrderScreenUI] Не удалось начать заказ!");
            }
        }
        else
        {
            // Завершить заказ
            if (debugLogs) Debug.Log("[OrderScreenUI] Пытаемся завершить заказ...");

            var currentOrder = orderManager.CurrentOrder;
            if (currentOrder != null && currentOrder.box != null && currentOrder.dropoff != null)
            {
                // Проверяем, находится ли коробка в правильном dropoff
                bool completed = orderManager.TryComplete(currentOrder.box, currentOrder.dropoff);

                if (completed)
                {
                    if (debugLogs) Debug.Log("[OrderScreenUI] ✅ Заказ завершён успешно!");

                    // Добавляем награду
                    AddBalance(DELIVERY_PRICE_STUB);

                    // UI обновится автоматически через событие
                }
                else
                {
                    Debug.LogWarning("[OrderScreenUI] ❌ Не удалось завершить заказ! Доставьте груз в правильное место.");
                    ShowTemporaryMessage("Доставьте груз в правильное место!");
                }
            }
            else
            {
                Debug.LogError("[OrderScreenUI] Заказ или его компоненты равны null!");
            }
        }
    }

    /// <summary>
    /// Добавить к балансу
    /// </summary>
    public void AddBalance(float amount)
    {
        playerBalance += amount;
        UpdateBalance();

        if (debugLogs) Debug.Log($"[OrderScreenUI] Баланс пополнен на ${amount:F0}. Новый баланс: ${playerBalance:F0}");
    }

    /// <summary>
    /// Получить текущий баланс
    /// </summary>
    public float GetBalance()
    {
        return playerBalance;
    }

    /// <summary>
    /// Установить баланс
    /// </summary>
    public void SetBalance(float amount)
    {
        playerBalance = Mathf.Max(0, amount);
        UpdateBalance();

        if (debugLogs) Debug.Log($"[OrderScreenUI] Баланс установлен: ${playerBalance:F0}");
    }

    /// <summary>
    /// Показать временное сообщение (заглушка)
    /// </summary>
    void ShowTemporaryMessage(string message)
    {
        // TODO: Реализовать всплывающее уведомление
        Debug.Log($"[OrderScreenUI] Сообщение: {message}");
    }

    /// <summary>
    /// Безопасная установка текста
    /// </summary>
    void SetTextSafe(Text textComponent, string value)
    {
        if (textComponent != null)
        {
            textComponent.text = value;
        }
        else if (debugLogs)
        {
            Debug.LogWarning($"[OrderScreenUI] Text компонент не назначен для значения: {value}");
        }
    }

    void OnEnable()
    {
        // Подписываемся при включении
        if (useEvents && isInitialized && !subscribedToEvents)
        {
            SubscribeToEvents();
        }

        // Обновляем при включении
        UpdateDisplay();
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
        if (debugLogs) Debug.Log("[OrderScreenUI] OnDestroy() - Очистка ресурсов");

        // Отписываемся от событий
        UnsubscribeFromEvents();

        // Отписываемся от кнопки
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(OnActionButtonClick);
        }
    }

    // Методы для ручного управления (опционально)
    [ContextMenu("Force Update Display")]
    public void ForceUpdateDisplay()
    {
        Debug.Log("[OrderScreenUI] Принудительное обновление дисплея");
        UpdateDisplay();
    }

    [ContextMenu("Add Test Balance")]
    public void AddTestBalance()
    {
        AddBalance(100f);
        Debug.Log($"[OrderScreenUI] Тестовое пополнение баланса. Текущий: ${playerBalance:F0}");
    }

    [ContextMenu("Reset Balance")]
    public void ResetBalance()
    {
        SetBalance(0f);
        Debug.Log("[OrderScreenUI] Баланс сброшен");
    }
}
