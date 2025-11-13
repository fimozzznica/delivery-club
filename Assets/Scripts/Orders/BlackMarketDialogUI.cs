using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Управление диалоговым окном скупщика с черного рынка
/// Три состояния: вопрос -> предложение -> информация (опционально)
/// </summary>
public class BlackMarketDialogUI : MonoBehaviour
{
    [Header("Main Panel")]
    [Tooltip("Главная панель диалога")]
    public GameObject dialogPanel;

    [Header("Question Panel (Состояние 1)")]
    [Tooltip("Панель с вопросом 'Что везёшь?'")]
    public GameObject questionPanel;

    [Tooltip("Текст вопроса")]
    public TextMeshProUGUI questionText;

    [Tooltip("Кнопка с названием товара")]
    public Button itemNameButton;

    [Tooltip("Текст на кнопке с названием товара")]
    public TextMeshProUGUI itemNameButtonText;

    [Header("Offer Panel (Состояние 2)")]
    [Tooltip("Панель с предложением о покупке")]
    public GameObject offerPanel;

    [Tooltip("Текст предложения")]
    public TextMeshProUGUI offerText;

    [Tooltip("Кнопка 'Продать'")]
    public Button sellButton;

    [Tooltip("Кнопка 'Отказаться'")]
    public Button declineButton;

    [Tooltip("Кнопка информации (i)")]
    public Button infoButton;

    [Header("Info Panel (Состояние 3)")]
    [Tooltip("Информационная панель")]
    public GameObject infoPanel;

    [Tooltip("Текст информации")]
    public TextMeshProUGUI infoText;

    [Tooltip("Кнопка 'Назад' из информации")]
    public Button backFromInfoButton;

    [Header("Farewell Panel (После отказа)")]
    [Tooltip("Панель с прощанием")]
    public GameObject farewellPanel;

    [Tooltip("Текст прощания")]
    public TextMeshProUGUI farewellText;

    [Header("Settings")]
    [Tooltip("Время отображения прощания перед исчезновением (секунды)")]
    public float farewellDuration = 3f;

    [Header("Dialog Texts")]
    [Tooltip("Текст вопроса")]
    public string questionMessage = "Эй, ты на доставке? Что везёшь?";

    [Tooltip("Шаблон текста предложения (используйте {0} для цены)")]
    public string offerTemplate = "Беру за ${0}, по рукам?";

    [Tooltip("Текст информации")]
    public string infoMessage = "Сделки со скупщиком приносят больше денег, но вы теряете рейтинг";

    [Tooltip("Текст прощания")]
    public string farewellMessage = "Ну ладно, ещё увидимся...";

    [Header("References")]
    [Tooltip("Ссылка на BlackMarketDealer")]
    public BlackMarketDealer dealer;

    [Tooltip("Ссылка на OrderManager")]
    public OrderManager orderManager;

    // Состояния диалога
    private enum DialogState
    {
        Hidden,
        Question,
        Offer,
        Info,
        Farewell
    }

    private DialogState currentState = DialogState.Hidden;
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
        if (itemNameButton != null)
            itemNameButton.onClick.AddListener(OnItemNameButtonClick);

        if (sellButton != null)
            sellButton.onClick.AddListener(OnSellButtonClick);

        if (declineButton != null)
            declineButton.onClick.AddListener(OnDeclineButtonClick);

        if (infoButton != null)
            infoButton.onClick.AddListener(OnInfoButtonClick);

        if (backFromInfoButton != null)
            backFromInfoButton.onClick.AddListener(OnBackFromInfoClick);

        // Подписываемся на события изменения состояния заказа
        if (orderManager != null)
            orderManager.OnOrderStateChanged.AddListener(UpdateDialogState);

        // Скрываем диалог при старте
        HideDialog();
    }

    void OnDestroy()
    {
        if (itemNameButton != null)
            itemNameButton.onClick.RemoveListener(OnItemNameButtonClick);

        if (sellButton != null)
            sellButton.onClick.RemoveListener(OnSellButtonClick);

        if (declineButton != null)
            declineButton.onClick.RemoveListener(OnDeclineButtonClick);

        if (infoButton != null)
            infoButton.onClick.RemoveListener(OnInfoButtonClick);

        if (backFromInfoButton != null)
            backFromInfoButton.onClick.RemoveListener(OnBackFromInfoClick);

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
            ShowQuestion();
        }
        else
        {
            HideDialog();
        }
    }

    /// <summary>
    /// Показать начальный вопрос (Состояние 1)
    /// </summary>
    void ShowQuestion()
    {
        currentState = DialogState.Question;

        if (dialogPanel != null)
            dialogPanel.SetActive(true);

        if (questionPanel != null)
            questionPanel.SetActive(true);

        if (offerPanel != null)
            offerPanel.SetActive(false);

        if (infoPanel != null)
            infoPanel.SetActive(false);

        if (farewellPanel != null)
            farewellPanel.SetActive(false);

        // Устанавливаем текст вопроса
        if (questionText != null)
            questionText.text = questionMessage;

        // Устанавливаем название товара на кнопку
        if (itemNameButtonText != null && orderManager != null && orderManager.HasActiveOrder)
        {
            var box = orderManager.CurrentOrder.box;
            if (box != null)
            {
                itemNameButtonText.text = box.contentName;
            }
            else
            {
                itemNameButtonText.text = "Товар";
            }
        }

        isShowingFarewell = false;
    }

    /// <summary>
    /// Показать предложение о покупке (Состояние 2)
    /// </summary>
    void ShowOffer()
    {
        currentState = DialogState.Offer;

        if (questionPanel != null)
            questionPanel.SetActive(false);

        if (offerPanel != null)
            offerPanel.SetActive(true);

        if (infoPanel != null)
            infoPanel.SetActive(false);

        if (farewellPanel != null)
            farewellPanel.SetActive(false);

        // Обновляем текст с ценой
        UpdateOfferText();
    }

    /// <summary>
    /// Показать информационное окно (Состояние 3)
    /// </summary>
    void ShowInfo()
    {
        currentState = DialogState.Info;

        if (questionPanel != null)
            questionPanel.SetActive(false);

        if (offerPanel != null)
            offerPanel.SetActive(false);

        if (infoPanel != null)
            infoPanel.SetActive(true);

        if (farewellPanel != null)
            farewellPanel.SetActive(false);

        // Устанавливаем информационный текст
        if (infoText != null)
            infoText.text = infoMessage;
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
    /// Показать прощание (После отказа)
    /// </summary>
    void ShowFarewell()
    {
        currentState = DialogState.Farewell;

        if (questionPanel != null)
            questionPanel.SetActive(false);

        if (offerPanel != null)
            offerPanel.SetActive(false);

        if (infoPanel != null)
            infoPanel.SetActive(false);

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
        currentState = DialogState.Hidden;

        if (dialogPanel != null)
            dialogPanel.SetActive(false);

        if (questionPanel != null)
            questionPanel.SetActive(false);

        if (offerPanel != null)
            offerPanel.SetActive(false);

        if (infoPanel != null)
            infoPanel.SetActive(false);

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

    // === ОБРАБОТЧИКИ КНОПОК ===

    /// <summary>
    /// Обработчик нажатия на кнопку с названием товара
    /// </summary>
    void OnItemNameButtonClick()
    {
        ShowOffer();
        Debug.Log("[BlackMarketDialogUI] Игрок показал товар скупщику");
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
    /// Обработчик нажатия кнопки информации (i)
    /// </summary>
    void OnInfoButtonClick()
    {
        ShowInfo();
        Debug.Log("[BlackMarketDialogUI] Показана информация о сделках");
    }

    /// <summary>
    /// Обработчик нажатия кнопки "Назад" из информации
    /// </summary>
    void OnBackFromInfoClick()
    {
        ShowOffer();
        Debug.Log("[BlackMarketDialogUI] Возврат к предложению");
    }

    // === ПУБЛИЧНЫЕ МЕТОДЫ ===

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

    /// <summary>
    /// Установить активность кнопки "Продать"
    /// </summary>
    public void SetSellButtonEnabled(bool enabled)
    {
        if (sellButton != null)
        {
            sellButton.interactable = enabled;
        }
    }
}
