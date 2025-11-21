using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


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

    [Tooltip("Ссылка на GameStateManager")]
    public GameStateManager gameStateManager;
    
    private enum DialogState
    {
        Hidden,
        Question,
        Offer,
        Info,
        Farewell
    }

    private DialogState currentState = DialogState.Hidden;
    private Coroutine farewellCoroutine;

    void Start()
    {
        // Находим компоненты если не назначены
        if (dealer == null)
            dealer = GetComponent<BlackMarketDealer>();

        if (orderManager == null)
            orderManager = FindObjectOfType<OrderManager>();

        if (gameStateManager == null)
            gameStateManager = FindObjectOfType<GameStateManager>();

        // подписываемся на кнопки
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


    public void UpdateDialogState()
    {
        // Если показываем прощание, не обновляем
        if (currentState == DialogState.Farewell)
            return;

        // Проверяем есть ли активный заказ
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


    // Показать начальный вопрос 
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

        // устанавливаем текст 
        if (questionText != null)
            questionText.text = questionMessage;

        // устанавливаем название товара на кнопку
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
    }
    
    // показать предложение о покупке 
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


    // Показать информационное окно 
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


    //обновить текст предложения с актуальной ценой

    void UpdateOfferText()
    {
        if (offerText == null)
            return;

        float price = 0f;

        // Получаем цену от дилера 
        if (dealer != null && orderManager != null && orderManager.HasActiveOrder)
        {
            price = dealer.CalculateBlackMarketPrice();
        }

        offerText.text = string.Format(offerTemplate, price.ToString("F0"));
    }


    // Показать прощание (После отказа)
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

        // Запускаем таймер скрытия
        if (farewellCoroutine != null)
            StopCoroutine(farewellCoroutine);

        farewellCoroutine = StartCoroutine(HideAfterDelay());
    }


    // Скрыть диалог полностью
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

        // Останавливаем таймер если был запущен
        if (farewellCoroutine != null)
        {
            StopCoroutine(farewellCoroutine);
            farewellCoroutine = null;
        }
    }
    
    // для скрытия диалога после задержки
    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(farewellDuration);
        HideDialog();
    }
    
    
    // обработчик нажатия на кнопку с названием товара
    void OnItemNameButtonClick()
    {
        // начало сделки когда игрок показывает товар
        if (gameStateManager != null)
        {
            gameStateManager.StartBlackMarketDeal();

            // Если игра закончилась, прерываем
            if (gameStateManager.IsGameOver)
            {
                Debug.Log("[BlackMarketDialogUI] Показ товара прерван - игрок пойман!");
                HideDialog();
                return;
            }
        }

        ShowOffer();
        Debug.Log("[BlackMarketDialogUI] Игрок показал товар скупщику - сделка началась!");
    }
    
    // Обработчик нажатия кнопки "Продать"
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
    
    // обработчик нажатия кнопки "Отказаться"
    void OnDeclineButtonClick()
    {
        // Завершаем сделку при отказе
        if (gameStateManager != null && gameStateManager.IsInBlackMarketDeal)
        {
            gameStateManager.EndBlackMarketDeal();
            Debug.Log("[BlackMarketDialogUI] Сделка завершена - игрок отказался");
        }

        ShowFarewell();
        Debug.Log("[BlackMarketDialogUI] Игрок отказался от продажи");
    }
    
    // Обработчик нажатия кнопки информации
    void OnInfoButtonClick()
    {
        ShowInfo();
        Debug.Log("[BlackMarketDialogUI] Показана информация о сделках");
    }
    
    // Обработчик нажатия кнопки "Назад" из информации
    void OnBackFromInfoClick()
    {
        ShowOffer();
        Debug.Log("[BlackMarketDialogUI] Возврат к предложению");
    }

    //методы
    // Принудительно обновить диалог
    public void ForceUpdate()
    {
        UpdateDialogState();
    }
    
    // принудительно скрыть диалог (для вызова извне)
    public void ForceHide()
    {
        HideDialog();
    }
    
    // Установить активность кнопки "Продать"
    // Вызывается из BlackMarketDropoffPoint когда коробка размещена/убрана
    public void SetSellButtonEnabled(bool enabled)
    {
        if (sellButton != null)
        {
            sellButton.interactable = enabled;
            Debug.Log($"[BlackMarketDialogUI] Кнопка 'Продать' {(enabled ? "активна" : "неактивна")}");
        }
    }
}
