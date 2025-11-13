using UnityEngine;

/// <summary>
/// Скупщик краденых заказов
/// Управляет зоной взаимодействия и логикой продажи
/// </summary>
public class BlackMarketDealer : MonoBehaviour
{
    [Header("Settings")]
    [Range(1f, 3f)]
    [Tooltip("Множитель цены (скупщик платит больше чем обычная доставка)")]
    public float priceMultiplier = 1.5f;

    [Tooltip("Радиус взаимодействия с игроком")]
    public float interactionRadius = 5f;

    [Header("References")]
    [Tooltip("Менеджер заказов")]
    public OrderManager orderManager;

    [Tooltip("UI диалога скупщика")]
    public BlackMarketDialogUI dialogUI;

    [Tooltip("Точка размещения товара")]
    public BlackMarketDropoffPoint dropoffPoint;

    [Tooltip("Менеджер состояния игры")]
    public GameStateManager gameStateManager;

    private Transform playerTransform;
    private bool playerInRange = false;

    void Start()
    {
        // Автопоиск компонентов
        if (orderManager == null)
            orderManager = FindObjectOfType<OrderManager>();

        if (gameStateManager == null)
            gameStateManager = FindObjectOfType<GameStateManager>();

        if (dialogUI == null)
            dialogUI = GetComponent<BlackMarketDialogUI>();

        if (dropoffPoint == null)
            dropoffPoint = GetComponentInChildren<BlackMarketDropoffPoint>();

        // Находим игрока
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    void Update()
    {
        if (playerTransform == null)
            return;

        // Проверяем расстояние до игрока
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        bool inRange = distance <= interactionRadius;

        // Обрабатываем вход/выход из зоны
        if (inRange != playerInRange)
        {
            playerInRange = inRange;

            if (playerInRange)
            {
                OnPlayerEnterRange();
            }
            else
            {
                OnPlayerExitRange();
            }
        }
    }

    void OnPlayerEnterRange()
    {
        if (dialogUI != null)
        {
            dialogUI.UpdateDialogState();
        }
    }

    void OnPlayerExitRange()
    {
        if (dialogUI != null)
        {
            dialogUI.ForceHide();
        }
    }

    /// <summary>
    /// Рассчитать цену на чёрном рынке
    /// </summary>
    public float CalculateBlackMarketPrice()
    {
        if (orderManager == null || !orderManager.HasActiveOrder)
            return 0f;

        float normalPrice = orderManager.GetCurrentOrderPrice();
        return normalPrice * priceMultiplier;
    }

    /// <summary>
    /// Продать товар скупщику (вызывается из UI)
    /// </summary>
    public void SellToDealer()
    {
        // Проверки
        if (orderManager == null || !orderManager.HasActiveOrder || !orderManager.IsOrderStarted)
        {
            Debug.LogWarning("[BlackMarketDealer] Невозможно продать: нет активного начатого заказа!");
            return;
        }

        if (dropoffPoint != null && !dropoffPoint.IsBoxPlaced())
        {
            Debug.LogWarning("[BlackMarketDealer] Коробка не размещена на столе!");
            return;
        }

        // Получаем данные заказа
        var order = orderManager.CurrentOrder;
        float price = CalculateBlackMarketPrice();

        // Начинаем сделку (проверка полицейских)
        if (gameStateManager != null)
        {
            gameStateManager.StartBlackMarketDeal();

            // Если игра закончилась (поймали полицейские), прерываем продажу
            if (gameStateManager.IsGameOver)
            {
                Debug.Log("[BlackMarketDealer] Продажа прервана - игрок пойман!");
                return;
            }
        }

        // Добавляем деньги
        orderManager.AddBalance(price);

        // Понижаем рейтинг
        orderManager.playerRating = Mathf.Max(0f, orderManager.playerRating - 0.5f);

        // Убираем коробку
        if (order.box != null)
        {
            order.box.ReturnHome();
            order.box.ClearAssignment();
            order.box.gameObject.SetActive(false);

            if (order.parentWasInactive && order.box.transform.parent != null)
            {
                order.box.transform.parent.gameObject.SetActive(false);
            }
        }

        // Очищаем заказ
        orderManager.ClearCurrentOrder();

        // Завершаем сделку
        if (gameStateManager != null)
        {
            gameStateManager.EndBlackMarketDeal();
        }

        // Очищаем размещение коробки
        if (dropoffPoint != null)
        {
            dropoffPoint.ClearBox();
        }

        Debug.Log($"[BlackMarketDealer] ✅ Товар продан за ${price:F0}! Рейтинг упал до {orderManager.playerRating:F1}");

        // Скрываем диалог
        if (dialogUI != null)
        {
            dialogUI.ForceHide();
        }
    }

    /// <summary>
    /// Проверить находится ли игрок в зоне взаимодействия
    /// </summary>
    public bool IsPlayerInRange()
    {
        return playerInRange;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
#endif
}
