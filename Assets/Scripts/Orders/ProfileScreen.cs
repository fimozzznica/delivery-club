using UnityEngine;
using TMPro;

/// <summary>
/// Простой экран профиля игрока
/// </summary>
public class ProfileScreen : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI ratingText;
    public TextMeshProUGUI completedOrdersText;

    [Header("References")]
    public OrderManager orderManager;

    private int completedOrders = 0;

    void Start()
    {
        if (orderManager == null)
            orderManager = FindObjectOfType<OrderManager>();

        if (orderManager != null)
            orderManager.OnOrderCompleted.AddListener(OnOrderCompleted);

        UpdateDisplay();
    }

    void OnDestroy()
    {
        if (orderManager != null)
            orderManager.OnOrderCompleted.RemoveListener(OnOrderCompleted);
    }

    void OnEnable()
    {
        UpdateDisplay();
    }

    void OnOrderCompleted(OrderManager.Order order)
    {
        completedOrders++;
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (orderManager == null)
            return;

        if (ratingText)
            ratingText.text = $"{orderManager.PlayerRating:F1} ★";

        if (completedOrdersText)
            completedOrdersText.text = completedOrders.ToString();
    }
}
