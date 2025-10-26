using UnityEngine;

/// <summary>
/// Управляет переключением между различными UI экранами
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Панель экрана заказов")]
    public GameObject orderScreenPanel;

    [Tooltip("Панель экрана профиля")]
    public GameObject profileScreenPanel;

    [Header("Settings")]
    [Tooltip("Показывать подробные логи")]
    public bool debugLogs = false;

    private enum UIScreen
    {
        OrderScreen,
        ProfileScreen
    }

    private UIScreen currentScreen = UIScreen.OrderScreen;

    void Awake()
    {
        if (debugLogs) Debug.Log("[UIManager] Awake() - Инициализация UI Manager");

        // Проверяем наличие панелей
        if (orderScreenPanel == null)
        {
            Debug.LogError("[UIManager] OrderScreenPanel не назначена!");
        }

        if (profileScreenPanel == null)
        {
            Debug.LogError("[UIManager] ProfileScreenPanel не назначена!");
        }
    }

    void Start()
    {
        if (debugLogs) Debug.Log("[UIManager] Start() - Показываем начальный экран");

        // По умолчанию показываем экран заказов
        ShowOrderScreen();
    }

    /// <summary>
    /// Показать экран заказов
    /// </summary>
    public void ShowOrderScreen()
    {
        if (debugLogs) Debug.Log("[UIManager] ShowOrderScreen()");

        if (orderScreenPanel != null)
            orderScreenPanel.SetActive(true);

        if (profileScreenPanel != null)
            profileScreenPanel.SetActive(false);

        currentScreen = UIScreen.OrderScreen;
    }

    /// <summary>
    /// Показать экран профиля
    /// </summary>
    public void ShowProfileScreen()
    {
        if (debugLogs) Debug.Log("[UIManager] ShowProfileScreen()");

        if (orderScreenPanel != null)
            orderScreenPanel.SetActive(false);

        if (profileScreenPanel != null)
            profileScreenPanel.SetActive(true);

        currentScreen = UIScreen.ProfileScreen;
    }

    /// <summary>
    /// Переключить между экранами
    /// </summary>
    public void ToggleScreen()
    {
        if (currentScreen == UIScreen.OrderScreen)
        {
            ShowProfileScreen();
        }
        else
        {
            ShowOrderScreen();
        }
    }

    /// <summary>
    /// Проверка, какой экран сейчас активен
    /// </summary>
    public bool IsOrderScreenActive()
    {
        return currentScreen == UIScreen.OrderScreen;
    }

    /// <summary>
    /// Проверка, какой экран сейчас активен
    /// </summary>
    public bool IsProfileScreenActive()
    {
        return currentScreen == UIScreen.ProfileScreen;
    }

    // Методы для вызова из UI кнопок
    [ContextMenu("Show Order Screen")]
    public void OnOrderScreenButtonClick()
    {
        ShowOrderScreen();
    }

    [ContextMenu("Show Profile Screen")]
    public void OnProfileScreenButtonClick()
    {
        ShowProfileScreen();
    }
}
