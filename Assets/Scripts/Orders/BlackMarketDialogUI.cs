using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Управление диалоговым окном скупщика с черного рынка
/// </summary>
public class BlackMarketDialogUI : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Главная панель диалога")]
    public GameObject dialogPanel;

    [Tooltip("Панель с предложением")]
    public GameObject offerPanel;

    [Tooltip("Панель с прощанием")]
    public GameObject farewellPanel;

    [Header("Offer Panel Elements")]
    [Tooltip("Текст предложения (например: 'Куплю у вас товар за...')")]
    public TextMeshProUGUI offerText;

    [Tooltip("Кнопка 'Продать'")]
    public Button sellButton;

    [Tooltip("Кнопка 'Отказаться'")]
    public Button declineButton;

    [Header("Farewell Panel Elements")]
    [Tooltip("Текст прощания")]
    public TextMeshProUGUI farewellText;

    [Header("Settings")]
    [Tooltip("Время отображения прощания перед исчезновением (секунды)")]
    public float farewellDuration = 3f;

    [Tooltip("Текст прощания")]
    public string farewellMessage = "Ну ладно, ещё увидимся...";

    [Tooltip("Шаблон текста предложения (используйте {0} для цены)")]
    public string offerTemplate = "Куплю у вас товар за ${0}!";

    [Header("References")]
    [Tooltip("Ссылка на BlackMarketDealer")]
    public BlackMarketDealer dealer;

    [Tooltip("Ссылка на OrderManager")]
    public OrderManager orderManager;

    private bool isShowingFarewell = false;
    private Coroutine farewellCoroutine;

    void Start()
    {
        // Находим компоненты если не назначены
        if (dealer == null)
            dealer = GetComponent<BlackMarketDealer>();

        if (orderManager == null)
            orderManager = FindObjectOfType<OrderManager>();

        // Подписываемся на кнопки
        if (sellButton != null)
            sellButton.onClick.AddListener(OnSellButtonClick);

        if (declineButton != null)
            declineButton.onClick.AddListener(OnDeclineButtonClick);

        // Подписываемся на события изменения состояния заказа
        if (orderManager != null)
            orderManager.OnOrderStateChanged.AddListener(UpdateDialogState);

        // Скрываем диалог при старте
        HideDialog();
    }

    void OnDestroy()
    {
        if (sellButton != null)
            sellButton.onClick.RemoveListener(OnSellButtonClick);

        if (declineButton != null)
            declineButton.onClick.RemoveListener(OnDeclineButtonClick);

        if (orderManager != null)
            orderManager.OnOrderStateChanged.RemoveListener(UpdateDialogState);
    }

    /// <summary>
    /// Обновить состояние диалога на основе состояния заказа
    /// </summary>
    public void UpdateDialogState()
    {
        // Если показываем прощание, не обновляем
        if (isShowingFarewell)
            return;

        // Проверяем есть ли активный НАЧАТЫЙ заказ
        bool hasActiveStartedOrder = orderManager != null &&
                                     orderManager.HasActiveOrder &&
                                     orderManager.IsOrderStarted;

        if (hasActiveStartedOrder)
        {
            ShowOffer();
        }
        else
        {
            HideDialog();
        }
    }

    /// <summary>
    /// Показать предложение о покупке
    /// </summary>
    void ShowOffer()
    {
        if (dialogPanel != null)
            dialogPanel.SetActive(true);

        if (offerPanel != null)
            offerPanel.SetActive(true);

        if (farewellPanel != null)
            farewellPanel.SetActive(false);

        // Обновляем текст с ценой
        UpdateOfferText();

        isShowingFarewell = false;
    }

    /// <summary>
    /// Обновить текст предложения с актуальной ценой
    /// </summary>
    void UpdateOfferText()
    {
        if (offerText == null)
            return;

        float price = 0f;

        // Получаем цену от дилера если возможно
        if (dealer != null && orderManager != null && orderManager.HasActiveOrder)
        {
            float normalPrice = orderManager.GetCurrentOrderPrice();
            price = normalPrice * dealer.priceMultiplier;
        }

        offerText.text = string.Format(offerTemplate, price.ToString("F0"));
    }

    /// <summary>
    /// Показать прощание
    /// </summary>
    void ShowFarewell()
    {
        if (offerPanel != null)
            offerPanel.SetActive(false);

        if (farewellPanel != null)
            farewellPanel.SetActive(true);

        if (farewellText != null)
            farewellText.text = farewellMessage;

        isShowingFarewell = true;

        // Запускаем таймер скрытия
        if (farewellCoroutine != null)
            StopCoroutine(farewellCoroutine);

        farewellCoroutine = StartCoroutine(HideAfterDelay());
    }

    /// <summary>
    /// Скрыть диалог полностью
    /// </summary>
    void HideDialog()
    {
        if (dialogPanel != null)
            dialogPanel.SetActive(false);

        if (offerPanel != null)
            offerPanel.SetActive(false);

        if (farewellPanel != null)
            farewellPanel.SetActive(false);

        isShowingFarewell = false;

        // Останавливаем таймер если был запущен
        if (farewellCoroutine != null)
        {
            StopCoroutine(farewellCoroutine);
            farewellCoroutine = null;
        }
    }

    /// <summary>
    /// Корутина для скрытия диалога после задержки
    /// </summary>
    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(farewellDuration);
        HideDialog();
    }

    /// <summary>
    /// Обработчик нажатия кнопки "Продать"
    /// </summary>
    void OnSellButtonClick()
    {
        if (dealer == null)
        {
            Debug.LogError("[BlackMarketDialogUI] Dealer не назначен!");
            return;
        }

        // Вызываем метод продажи у дилера
        // (логика продажи остается в BlackMarketDealer)
        dealer.SellToDealer();

        // Скрываем диалог
        HideDialog();

        Debug.Log("[BlackMarketDialogUI] Игрок продал товар!");
    }

    /// <summary>
    /// Обработчик нажатия кнопки "Отказаться"
    /// </summary>
    void OnDeclineButtonClick()
    {
        ShowFarewell();
        Debug.Log("[BlackMarketDialogUI] Игрок отказался от продажи");
    }

    /// <summary>
    /// Принудительно обновить диалог (для вызова извне)
    /// </summary>
    public void ForceUpdate()
    {
        UpdateDialogState();
    }

    /// <summary>
    /// Принудительно скрыть диалог (для вызова извне)
    /// </summary>
    public void ForceHide()
    {
        HideDialog();
    }
}
