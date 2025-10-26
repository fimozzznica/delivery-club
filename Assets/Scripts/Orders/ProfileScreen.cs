using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Экран профиля игрока с отображением статистики
/// </summary>
public class ProfileScreen : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Текст с рейтингом игрока")]
    public Text ratingText;

    [Tooltip("Текст с количеством выполненных заказов")]
    public Text completedOrdersText;

    [Header("References")]
    [Tooltip("Ссылка на OrderManager для получения статистики")]
    public OrderManager orderManager;

    [Header("Settings")]
    [Tooltip("Показывать подробные логи")]
    public bool debugLogs = false;

    // Временные данные (заглушки)
    private float playerRating = 4.8f;
    private int completedOrders = 0;

    void OnEnable()
    {
        if (debugLogs) Debug.Log("[ProfileScreen] OnEnable() - Экран профиля открыт");

        // Ищем OrderManager если не назначен
        if (orderManager == null)
        {
            orderManager = FindObjectOfType<OrderManager>();
            if (orderManager == null)
            {
                Debug.LogWarning("[ProfileScreen] OrderManager не найден!");
            }
        }

        // Обновляем отображение при открытии
        UpdateDisplay();
    }

    void Start()
    {
        if (debugLogs) Debug.Log("[ProfileScreen] Start() - Инициализация экрана профиля");

        // Подписываемся на события завершения заказов
        if (orderManager != null)
        {
            orderManager.OnOrderCompleted.AddListener(OnOrderCompletedHandler);
            if (debugLogs) Debug.Log("[ProfileScreen] Подписка на OnOrderCompleted");
        }

        UpdateDisplay();
    }

    void OnDisable()
    {
        if (debugLogs) Debug.Log("[ProfileScreen] OnDisable() - Экран профиля закрыт");
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (orderManager != null)
        {
            orderManager.OnOrderCompleted.RemoveListener(OnOrderCompletedHandler);
            if (debugLogs) Debug.Log("[ProfileScreen] Отписка от OnOrderCompleted");
        }
    }

    /// <summary>
    /// Обработчик события завершения заказа
    /// </summary>
    private void OnOrderCompletedHandler(OrderManager.Order order)
    {
        if (debugLogs) Debug.Log($"[ProfileScreen] Заказ {order.id} завершён, обновляем статистику");

        // Увеличиваем счётчик заказов
        completedOrders++;

        // Обновляем рейтинг (пока просто заглушка)
        // TODO: Реализовать реальную систему рейтинга
        UpdateRating();

        // Обновляем отображение
        UpdateDisplay();
    }

    /// <summary>
    /// Обновить отображение статистики
    /// </summary>
    public void UpdateDisplay()
    {
        if (debugLogs) Debug.Log("[ProfileScreen] UpdateDisplay()");

        // Обновляем рейтинг
        if (ratingText != null)
        {
            ratingText.text = playerRating.ToString("F1") + " ★";
        }
        else if (debugLogs)
        {
            Debug.LogWarning("[ProfileScreen] RatingText не назначен!");
        }

        // Обновляем количество заказов
        if (completedOrdersText != null)
        {
            completedOrdersText.text = completedOrders.ToString();
        }
        else if (debugLogs)
        {
            Debug.LogWarning("[ProfileScreen] CompletedOrdersText не назначен!");
        }

        if (debugLogs)
        {
            Debug.Log($"[ProfileScreen] Отображено - Рейтинг: {playerRating:F1}, Заказов: {completedOrders}");
        }
    }

    /// <summary>
    /// Обновить рейтинг (заглушка)
    /// </summary>
    private void UpdateRating()
    {
        // TODO: Реализовать реальную логику расчёта рейтинга
        // Пока просто держим постоянное значение
        playerRating = 4.8f;

        if (debugLogs) Debug.Log($"[ProfileScreen] Рейтинг обновлён: {playerRating:F1}");
    }

    /// <summary>
    /// Получить текущий рейтинг
    /// </summary>
    public float GetRating()
    {
        return playerRating;
    }

    /// <summary>
    /// Получить количество выполненных заказов
    /// </summary>
    public int GetCompletedOrders()
    {
        return completedOrders;
    }

    /// <summary>
    /// Установить рейтинг (для тестирования)
    /// </summary>
    public void SetRating(float rating)
    {
        playerRating = Mathf.Clamp(rating, 0f, 5f);
        UpdateDisplay();
        if (debugLogs) Debug.Log($"[ProfileScreen] Рейтинг установлен: {playerRating:F1}");
    }

    /// <summary>
    /// Установить количество заказов (для тестирования)
    /// </summary>
    public void SetCompletedOrders(int count)
    {
        completedOrders = Mathf.Max(0, count);
        UpdateDisplay();
        if (debugLogs) Debug.Log($"[ProfileScreen] Количество заказов установлено: {completedOrders}");
    }

    // Методы для тестирования через Context Menu
    [ContextMenu("Add Test Order")]
    private void AddTestOrder()
    {
        completedOrders++;
        UpdateDisplay();
        Debug.Log($"[ProfileScreen] Тестовый заказ добавлен. Всего: {completedOrders}");
    }

    [ContextMenu("Reset Stats")]
    private void ResetStats()
    {
        completedOrders = 0;
        playerRating = 4.8f;
        UpdateDisplay();
        Debug.Log("[ProfileScreen] Статистика сброшена");
    }
}
