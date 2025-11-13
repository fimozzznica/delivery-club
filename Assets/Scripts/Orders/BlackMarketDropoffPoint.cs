using UnityEngine;

/// <summary>
/// Точка размещения коробки у скупщика (стол, полка и т.д.)
/// Отслеживает когда коробка размещена на объекте
/// </summary>
[RequireComponent(typeof(Collider))]
public class BlackMarketDropoffPoint : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Требуется ли чтобы коробка была из текущего заказа")]
    public bool requireActiveOrder = true;

    [Header("References")]
    [Tooltip("Ссылка на OrderManager для проверки заказа")]
    public OrderManager orderManager;

    [Tooltip("Ссылка на диалоговое окно для обновления кнопки")]
    public BlackMarketDialogUI dialogUI;

    // Публичное свойство для проверки размещения
    public bool IsBoxPlaced { get; private set; }

    private Box placedBox;

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
        if (orderManager == null)
            orderManager = FindObjectOfType<OrderManager>();

        if (dialogUI == null)
            dialogUI = GetComponentInParent<BlackMarketDialogUI>();

        IsBoxPlaced = false;
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
        // Проверяем что это коробка
        var box = other.GetComponentInParent<Box>();
        if (box == null)
            return;

        // Если требуется активный заказ
        if (requireActiveOrder)
        {
            if (orderManager == null || !orderManager.HasActiveOrder || !orderManager.IsOrderStarted)
            {
                Debug.Log("[BlackMarketDropoffPoint] Нет активного начатого заказа");
                return;
            }

            // Проверяем что это коробка из текущего заказа
            if (orderManager.CurrentOrder.box != box)
            {
                Debug.Log("[BlackMarketDropoffPoint] Это не коробка из текущего заказа");
                return;
            }
        }

        // Коробка размещена!
        placedBox = box;
        IsBoxPlaced = true;

        Debug.Log($"[BlackMarketDropoffPoint] Коробка '{box.contentName}' размещена на столе скупщика!");

        // Уведомляем диалоговое окно
        if (dialogUI != null)
        {
            dialogUI.OnBoxPlacementChanged();
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Проверяем что это наша размещённая коробка
        var box = other.GetComponentInParent<Box>();
        if (box == null || box != placedBox)
            return;

        // Коробка убрана
        placedBox = null;
        IsBoxPlaced = false;

        Debug.Log($"[BlackMarketDropoffPoint] Коробка '{box.contentName}' убрана со стола скупщика");

        // Уведомляем диалоговое окно
        if (dialogUI != null)
        {
            dialogUI.OnBoxPlacementChanged();
        }
    }

    /// <summary>
    /// Очистить размещение (вызывается при продаже)
    /// </summary>
    public void ClearPlacement()
    {
        placedBox = null;
        IsBoxPlaced = false;

        Debug.Log("[BlackMarketDropoffPoint] Размещение очищено");

        // Уведомляем диалоговое окно
        if (dialogUI != null)
        {
            dialogUI.OnBoxPlacementChanged();
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = IsBoxPlaced ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
