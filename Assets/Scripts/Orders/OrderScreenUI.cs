using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class OrderScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject orderInfoPanel;
    public GameObject noOrderPanel;
    public GameObject gameOverPanel;
    public TextMeshProUGUI orderIdText;
    public TextMeshProUGUI pickupText;
    public TextMeshProUGUI deliveryText;
    public TextMeshProUGUI deliveryPriceText;
    public TextMeshProUGUI balanceText;
    public TextMeshProUGUI ratingText;
    public TextMeshProUGUI noOrderText;
    public TextMeshProUGUI gameOverText;
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;

    [Header("References")]
    public OrderManager orderManager;

    private bool isGameOver = false;

    void Start()
    {
        if (orderManager == null) { orderManager = FindObjectOfType<OrderManager>(); }
        if (actionButton != null) { actionButton.onClick.AddListener(OnActionButtonClick); }
        if (orderManager != null) { orderManager.OnOrderStateChanged.AddListener(UpdateDisplay); }
        if (gameOverPanel != null) { gameOverPanel.SetActive(false); }

        UpdateDisplay();
    }

    void OnDestroy()
    {
        if (actionButton != null) { actionButton.onClick.RemoveListener(OnActionButtonClick); }
        if (orderManager != null) { orderManager.OnOrderStateChanged.RemoveListener(UpdateDisplay); }
    }

    void UpdateDisplay()
    {
        if (isGameOver) { return; }
        UpdatePlayerStats();
        if (orderManager == null || !orderManager.HasActiveOrder)
        {
            ShowNoOrder();
            return;
        }

        var order = orderManager.CurrentOrder;
        if (order?.box == null || order?.dropoff == null)
        {
            ShowNoOrder();
            return;
        }

        ShowOrder(order);
    }

    void ShowOrder(OrderManager.Order order)
    {
        if (orderInfoPanel) orderInfoPanel.SetActive(true);
        if (noOrderPanel) noOrderPanel.SetActive(false);

        if (orderIdText) orderIdText.text = $"Заказ #{order.id}";
        if (pickupText) pickupText.text = $"Забрать: {order.box.pickupAddress}";
        if (deliveryText) deliveryText.text = $"Доставить: {order.dropoff.deliveryAddress}";
        float deliveryPrice = orderManager != null ? orderManager.CalculateDeliveryPrice(order.box, order.dropoff) : 0f;
        if (deliveryPriceText) deliveryPriceText.text = $"Оплата: ${deliveryPrice:F0}";

        UpdateButton(orderManager.IsOrderStarted);
    }

    void ShowNoOrder()
    {
        if (orderInfoPanel) orderInfoPanel.SetActive(false);
        if (noOrderPanel) noOrderPanel.SetActive(true);
        if (noOrderText) noOrderText.text = "ОЖИДАНИЕ ЗАКАЗА...";

        if (actionButton) actionButton.interactable = false;
        if (actionButtonText) actionButtonText.text = "Нет заказа";
    }

    void UpdateButton(bool orderStarted)
    {
        if (actionButton) actionButton.interactable = true;
        if (actionButtonText) { actionButtonText.text = orderStarted ? "Завершить заказ" : "Начать заказ"; }
    }

    void UpdatePlayerStats()
    {
        if (orderManager == null) { return; }
        if (balanceText) { balanceText.text = $"${orderManager.PlayerBalance:F0}"; }
        if (ratingText) { ratingText.text = $"{orderManager.PlayerRating:F1} ★"; }
    }

    void OnActionButtonClick()
    {
        if (orderManager == null || !orderManager.HasActiveOrder || isGameOver) { return; }

        if (!orderManager.IsOrderStarted) { orderManager.StartOrder(); }
        else
        {
            var order = orderManager.CurrentOrder;
            if (orderManager.TryComplete(order.box, order.dropoff))
            {
                UpdatePlayerStats();
            }
        }
    }


    public void ShowGameOver(string reason)
    {
        isGameOver = true;
        if (orderInfoPanel) orderInfoPanel.SetActive(false);
        if (noOrderPanel) noOrderPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (gameOverText) gameOverText.text = reason;
        if (actionButton) actionButton.interactable = false;
    }
}
