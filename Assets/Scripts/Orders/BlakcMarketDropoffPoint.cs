using UnityEngine;

/// <summary>
/// Точка размещения товара у скупщика на чёрном рынке
/// Активирует кнопку продажи когда коробка размещена на столе
/// </summary>
[RequireComponent(typeof(Collider))]
public class BlackMarketDropoffPoint : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Ссылка на UI диалога скупщика")]
    public BlackMarketDialogUI dialogUI;

    [Tooltip("Ссылка на менеджер заказов")]
    public OrderManager orderManager;

    [Header("Debug")]
    [Tooltip("Показывать отладочные сообщения")]
    public bool showDebugMessages = true;

    private Box currentBox = null;
    private bool isBoxOnTable = false;

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
        // Автопоиск компонентов если не назначены
        if (dialogUI == null)
        {
            dialogUI = GetComponentInParent<BlackMarketDialogUI>();
            if (dialogUI == null)
            {
                dialogUI = FindObjectOfType<BlackMarketDialogUI>();
            }
        }

        if (orderManager == null)
        {
            orderManager = FindObjectOfType<OrderManager>();
        }
    }

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c)
        {
            c.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Проверяем есть ли Box компонент
        var box = other.GetComponentInParent<Box>();
        if (box == null)
            return;

        // Проверяем что это коробка из активного заказа
        if (orderManager == null || !orderManager.HasActiveOrder)
        {
            if (showDebugMessages)
                Debug.Log($"[BlackMarketDropoffPoint] Коробка положена, но нет активного заказа");
            return;
        }

        if (orderManager.CurrentOrder.box != box)
        {
            if (showDebugMessages)
                Debug.Log($"[BlackMarketDropoffPoint] Это не та коробка (нужна коробка текущего заказа)");
            return;
        }

        // Коробка правильная и на столе!
        currentBox = box;
        isBoxOnTable = true;

        if (showDebugMessages)
            Debug.Log($"[BlackMarketDropoffPoint] ✅ Коробка '{box.contentName}' размещена на столе скупщика!");

        // Активируем кнопку продажи в UI
        if (dialogUI != null)
        {
            dialogUI.SetSellButtonEnabled(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        var box = other.GetComponentInParent<Box>();
        if (box == null)
            return;

        // Если убрали коробку со стола
        if (box == currentBox)
        {
            currentBox = null;
            isBoxOnTable = false;

            if (showDebugMessages)
                Debug.Log($"[BlackMarketDropoffPoint] Коробка убрана со стола");

            // Деактивируем кнопку продажи
            if (dialogUI != null)
            {
                dialogUI.SetSellButtonEnabled(false);
            }
        }
    }

    /// <summary>
    /// Проверить, лежит ли коробка на столе
    /// </summary>
    public bool IsBoxOnTable()
    {
        return isBoxOnTable && currentBox != null;
    }

    /// <summary>
    /// Получить коробку, которая лежит на столе
    /// </summary>
    public Box GetBoxOnTable()
    {
        return currentBox;
    }

    /// <summary>
    /// Очистить ссылку на коробку (вызывается после продажи)
    /// </summary>
    public void ClearBox()
    {
        currentBox = null;
        isBoxOnTable = false;

        if (dialogUI != null)
        {
            dialogUI.SetSellButtonEnabled(false);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = isBoxOnTable ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
#endif
}
