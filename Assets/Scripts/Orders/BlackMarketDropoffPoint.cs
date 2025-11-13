using UnityEngine;

/// <summary>
/// Точка размещения коробки у скупщика (стол, полка и т.д.)
/// Отслеживает когда коробка размещена и активирует кнопку продажи
/// </summary>
[RequireComponent(typeof(Collider))]
public class BlackMarketDropoffPoint : MonoBehaviour
{
    [Header("References")]
    [Tooltip("UI диалога скупщика для активации кнопки")]
    public BlackMarketDialogUI dialogUI;

    [Tooltip("Менеджер заказов")]
    public OrderManager orderManager;

    [Tooltip("Менеджер состояния игры")]
    public GameStateManager gameStateManager;

    private Box currentBox = null;

    void Awake()
    {
        var collider = GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogError($"[BlackMarketDropoffPoint] {name} - НЕТ КОЛЛАЙДЕРА!");
        }
        else
        {
            collider.isTrigger = true;
        }
    }

    void Start()
    {
        // Автопоиск компонентов
        if (dialogUI == null)
            dialogUI = GetComponentInParent<BlackMarketDialogUI>();

        if (orderManager == null)
            orderManager = FindObjectOfType<OrderManager>();

        if (gameStateManager == null)
            gameStateManager = FindObjectOfType<GameStateManager>();
    }

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c)
            c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        var box = other.GetComponentInParent<Box>();
        if (box == null)
            return;

        // Проверяем что это коробка из текущего заказа
        if (orderManager == null || !orderManager.HasActiveOrder)
            return;

        if (orderManager.CurrentOrder.box != box)
            return;

        // Коробка размещена!
        currentBox = box;
        Debug.Log($"[BlackMarketDropoffPoint] ✅ Коробка размещена на столе");

        // НАЧИНАЕМ СДЕЛКУ (проверка полицейских)


        // Активируем кнопку продажи
        if (dialogUI != null)
        {
            dialogUI.SetSellButtonEnabled(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        var box = other.GetComponentInParent<Box>();
        if (box == null || box != currentBox)
            return;

        // Коробка убрана
        currentBox = null;
        Debug.Log($"[BlackMarketDropoffPoint] Коробка убрана со стола");

        // Завершаем сделку если она была начата
        if (gameStateManager != null && gameStateManager.IsInBlackMarketDeal)
        {
            gameStateManager.EndBlackMarketDeal();
        }

        // Деактивируем кнопку продажи
        if (dialogUI != null)
        {
            dialogUI.SetSellButtonEnabled(false);
        }
    }

    /// <summary>
    /// Проверить, размещена ли коробка
    /// </summary>
    public bool IsBoxPlaced()
    {
        return currentBox != null;
    }

    /// <summary>
    /// Очистить ссылку на коробку (после продажи)
    /// </summary>
    public void ClearBox()
    {
        currentBox = null;

        // Завершаем сделку
        if (gameStateManager != null && gameStateManager.IsInBlackMarketDeal)
        {
            gameStateManager.EndBlackMarketDeal();
        }

        if (dialogUI != null)
        {
            dialogUI.SetSellButtonEnabled(false);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = currentBox != null ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
#endif
}
