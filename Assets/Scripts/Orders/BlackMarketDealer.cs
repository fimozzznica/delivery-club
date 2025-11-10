using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Скупщик краденых заказов
/// </summary>
public class BlackMarketDealer : MonoBehaviour
{
    [Header("UI")]
    public GameObject dealerPanel;
    public TextMeshProUGUI priceText;
    public Button sellButton;

    [Header("Settings")]
    [Range(1f, 3f)]
    public float priceMultiplier = 1.5f; // На сколько больше платит (+50% по умолчанию)

    [Tooltip("Радиус взаимодействия")]
    public float interactionRadius = 5f;

    [Header("References")]
    public OrderManager orderManager;

    private Transform playerTransform;
    private bool playerInRange = false;

    void Start()
    {
        if (orderManager == null)
            orderManager = FindObjectOfType<OrderManager>();

        if (sellButton != null)
            sellButton.onClick.AddListener(OnSellButtonClick);

        if (dealerPanel != null)
            dealerPanel.SetActive(false);

        // Находим игрока по тегу
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    void Update()
    {
        if (playerTransform == null)
            return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        bool inRange = distance <= interactionRadius;

        if (inRange != playerInRange)
        {
            playerInRange = inRange;

            if (inRange)
            {
                ShowUI();
            }
            else
            {
                HideUI();
            }
        }

        if (playerInRange)
        {
            UpdateUI();
        }
    }

    void ShowUI()
    {
        if (dealerPanel != null)
            dealerPanel.SetActive(true);

        UpdateUI();
    }

    void HideUI()
    {
        if (dealerPanel != null)
            dealerPanel.SetActive(false);
    }

    void UpdateUI()
    {
        if (orderManager == null || !orderManager.HasActiveOrder)
        {
            if (priceText) priceText.text = "Нет товара";
            if (sellButton) sellButton.interactable = false;
            return;
        }

        float price = CalculateBlackMarketPrice();
        if (priceText) priceText.text = $"Куплю за: ${price:F0}";
        if (sellButton) sellButton.interactable = orderManager.IsOrderStarted;
    }

    float CalculateBlackMarketPrice()
    {
        if (orderManager == null || !orderManager.HasActiveOrder)
            return 0f;

        float normalPrice = orderManager.GetCurrentOrderPrice();
        return normalPrice * priceMultiplier;
    }

    void OnSellButtonClick()
    {
        if (orderManager == null || !orderManager.HasActiveOrder || !orderManager.IsOrderStarted)
            return;

        float price = CalculateBlackMarketPrice();

        // Добавляем деньги
        orderManager.playerBalance += price;

        // Сильно понижаем рейтинг
        orderManager.playerRating = Mathf.Max(0f, orderManager.playerRating - 0.5f);

        // Переводим игрока в состояние "продажа скупщику"
        var gameState = FindObjectOfType<GameStateManager>();
        if (gameState != null)
        {
            gameState.StartBlackMarketDeal();
        }

        // Завершаем заказ
        var order = orderManager.CurrentOrder;
        if (order != null && order.box != null)
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

        Debug.Log($"[BlackMarketDealer] Товар продан за ${price:F0}! Рейтинг упал до {orderManager.playerRating:F1}");

        HideUI();
    }

    void OnDestroy()
    {
        if (sellButton != null)
            sellButton.onClick.RemoveListener(OnSellButtonClick);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
