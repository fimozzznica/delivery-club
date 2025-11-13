using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Скупщик краденых заказов
/// </summary>
public class BlackMarketDealer : MonoBehaviour
{
    [Header("UI (старый вариант - можно оставить для совместимости)")]
    public GameObject dealerPanel;
    public TextMeshProUGUI priceText;
    public Button sellButton;

    [Header("Dialog UI (новый вариант)")]
    [Tooltip("Компонент управления диалоговым окном")]
    public BlackMarketDialogUI dialogUI;

    [Header("Settings")]
    [Range(1f, 3f)]
    public float priceMultiplier = 1.5f; // На сколько больше платит (+50% по умолчанию)

    [Tooltip("Радиус взаимодействия")]
    public float interactionRadius = 5f;

    [Header("References")]
    public OrderManager orderManager;

    private Transform playerTransform;
    private bool playerInRange = false;
    private bool wasPlayerInRange = false;

    void Start()
    {
        if (orderManager == null)
            orderManager = FindObjectOfType<OrderManager>();

        if (dialogUI == null)
            dialogUI = GetComponent<BlackMarketDialogUI>();

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
        playerInRange = distance <= interactionRadius;

        // Обрабатываем вход/выход из зоны
        if (playerInRange != wasPlayerInRange)
        {
            if (playerInRange)
            {
                OnPlayerEnterRange();
            }
            else
            {
                OnPlayerExitRange();
            }

            wasPlayerInRange = playerInRange;
        }

        // Обновляем UI пока игрок в зоне
        if (playerInRange)
        {
            UpdateUI();
        }
    }

    /// <summary>
    /// Вызывается когда игрок входит в зону взаимодействия
    /// </summary>
    void OnPlayerEnterRange()
    {
        ShowUI();

        // Обновляем диалоговое окно если оно есть
        if (dialogUI != null)
        {
            dialogUI.UpdateDialogState();
        }
    }

    /// <summary>
    /// Вызывается когда игрок выходит из зоны взаимодействия
    /// </summary>
    void OnPlayerExitRange()
    {
        HideUI();

        // Скрываем диалоговое окно если оно есть
        if (dialogUI != null)
        {
            dialogUI.ForceHide();
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

    public float CalculateBlackMarketPrice()
    {
        if (orderManager == null || !orderManager.HasActiveOrder)
            return 0f;

        float normalPrice = orderManager.GetCurrentOrderPrice();
        return normalPrice * priceMultiplier;
    }

    void OnSellButtonClick()
    {
        SellToDealer();
    }

    /// <summary>
    /// Публичный метод для продажи товара скупщику (вызывается из диалога или кнопки)
    /// </summary>
    public void SellToDealer()
    {
        if (orderManager == null || !orderManager.HasActiveOrder || !orderManager.IsOrderStarted)
        {
            Debug.LogWarning("[BlackMarketDealer] Невозможно продать: нет активного начатого заказа!");
            return;
        }

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

        // Завершаем состояние сделки
        if (gameState != null)
        {
            gameState.EndBlackMarketDeal();
        }

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
