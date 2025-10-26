using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Простой UI экран заказов
/// </summary>
public class OrderScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject orderInfoPanel;
    public GameObject noOrderPanel;
    public TextMeshProUGUI orderIdText;
    public TextMeshProUGUI pickupText;
    public TextMeshProUGUI deliveryText;
    public TextMeshProUGUI deliveryPriceText;
    public TextMeshProUGUI balanceText;
    public TextMeshProUGUI noOrderText;
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;

    [Header("References")]
    public OrderManager orderManager;

    private float playerBalance = 0f;
    private const float DELIVERY_PRICE = 50f;

    void Start()
    {
        if (orderManager == null)
            orderManager = FindObjectOfType<OrderManager>();

        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionButtonClick);

        if (orderManager != null)
            orderManager.OnOrderStateChanged.AddListener(UpdateDisplay);

        UpdateDisplay();
    }

    void OnDestroy()
    {
        if (actionButton != null)
            actionButton.onClick.RemoveListener(OnActionButtonClick);

        if (orderManager != null)
            orderManager.OnOrderStateChanged.RemoveListener(UpdateDisplay);
    }

    void UpdateDisplay()
    {
        UpdateBalance();

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
        if (deliveryPriceText) deliveryPriceText.text = $"Оплата: ${DELIVERY_PRICE:F0}";

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
        if (actionButtonText)
            actionButtonText.text = orderStarted ? "Завершить заказ" : "Начать заказ";
    }

    void UpdateBalance()
    {
        if (balanceText)
            balanceText.text = $"${playerBalance:F0}";
    }

    void OnActionButtonClick()
    {
        if (orderManager == null || !orderManager.HasActiveOrder)
            return;

        if (!orderManager.IsOrderStarted)
        {
            orderManager.StartOrder();
        }
        else
        {
            var order = orderManager.CurrentOrder;
            if (orderManager.TryComplete(order.box, order.dropoff))
            {
                playerBalance += DELIVERY_PRICE;
                UpdateBalance();
            }
            else
            {
                Debug.Log("Доставьте груз в правильное место!");
            }
        }
    }
}
