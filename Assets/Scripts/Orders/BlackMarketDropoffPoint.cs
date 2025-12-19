using UnityEngine;

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
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    void Start()
    {
        if (dialogUI == null) { dialogUI = GetComponentInParent<BlackMarketDialogUI>(); }
        if (orderManager == null) { orderManager = FindObjectOfType<OrderManager>(); }
        if (gameStateManager == null) { gameStateManager = FindObjectOfType<GameStateManager>(); }
    }
    
    void OnTriggerEnter(Collider other)
    {
        var box = other.GetComponentInParent<Box>();
        if (box == null) { return; }
        if (orderManager == null || !orderManager.HasActiveOrder) { return; }
        if (orderManager.CurrentOrder.box != box) { return; }
        currentBox = box;

        if (dialogUI != null) { dialogUI.SetSellButtonEnabled(true); }
    }

    void OnTriggerExit(Collider other)
    {
        var box = other.GetComponentInParent<Box>();
        if (box == null || box != currentBox)
            return;

        currentBox = null;
        if (gameStateManager != null && gameStateManager.IsInBlackMarketDeal) { gameStateManager.EndBlackMarketDeal(); }
        if (dialogUI != null) { dialogUI.SetSellButtonEnabled(false); }
    }

    public bool IsBoxPlaced() { return currentBox != null; }
    public void ClearBox()
    {
        currentBox = null;
        if (gameStateManager != null && gameStateManager.IsInBlackMarketDeal) { gameStateManager.EndBlackMarketDeal(); }
        if (dialogUI != null) { dialogUI.SetSellButtonEnabled(false); }
    }
}
