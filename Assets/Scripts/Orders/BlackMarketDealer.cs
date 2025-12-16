using UnityEngine;

public class BlackMarketDealer : MonoBehaviour
{
    [Header("Settings")]
    [Range(1f, 3f)]
    [Tooltip("Множитель цены (скупщик платит больше чем обычная доставка)")]
    public float priceMultiplier = 1.5f;

    [Tooltip("Радиус взаимодействия с игроком")]
    public float interactionRadius = 5f;

    [Header("References")]
    [Tooltip("Менеджер заказов")]
    public OrderManager orderManager;

    [Tooltip("UI диалога скупщика")]
    public BlackMarketDialogUI dialogUI;

    [Tooltip("Точка размещения товара")]
    public BlackMarketDropoffPoint dropoffPoint;

    [Tooltip("Менеджер состояния игры")]
    public GameStateManager gameStateManager;

    private Transform playerTransform;
    private bool playerInRange = false;

    void Start()
    {
        if (orderManager == null)
            orderManager = FindObjectOfType<OrderManager>();

        if (gameStateManager == null)
            gameStateManager = FindObjectOfType<GameStateManager>();

        if (dialogUI == null)
            dialogUI = GetComponent<BlackMarketDialogUI>();

        if (dropoffPoint == null)
            dropoffPoint = GetComponentInChildren<BlackMarketDropoffPoint>();


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

            if (playerInRange)
            {
                OnPlayerEnterRange();
            }
            else
            {
                OnPlayerExitRange();
            }
        }
    }

    void OnPlayerEnterRange()
    {
        if (dialogUI != null)
        {
            dialogUI.UpdateDialogState();
        }
    }

    void OnPlayerExitRange()
    {
        if (dialogUI != null)
        {
            dialogUI.ForceHide();
        }
    }
    
    public float CalculateBlackMarketPrice()
    {
        if (orderManager == null || !orderManager.HasActiveOrder)
            return 0f;

        float normalPrice = orderManager.GetCurrentOrderPrice();
        return normalPrice * priceMultiplier;
    }
    
    public void SellToDealer()
    {
        if (orderManager == null || !orderManager.HasActiveOrder || !orderManager.IsOrderStarted)
        {
            return;
        }

        if (dropoffPoint != null && !dropoffPoint.IsBoxPlaced())
        {
            return;
        }
        
        var order = orderManager.CurrentOrder;
        float price = CalculateBlackMarketPrice();
        
        if (gameStateManager != null && gameStateManager.IsGameOver)
        {
            return;
        }
        
        orderManager.AddBalance(price);
        orderManager.playerRating = Mathf.Max(0f, orderManager.playerRating - 0.5f);
        
        if (order.box != null)
        {
            order.box.ReturnHome();
            order.box.ClearAssignment();
            order.box.gameObject.SetActive(false);

            if (order.parentWasInactive && order.box.transform.parent != null)
            {
                order.box.transform.parent.gameObject.SetActive(false);
            }
        }
        
        orderManager.ClearCurrentOrder();
        
        if (dropoffPoint != null)
        {
            dropoffPoint.ClearBox();
        }
        
        if (dialogUI != null)
        {
            dialogUI.ForceHide();
        }
    }

    public bool IsPlayerInRange()
    {
        return playerInRange;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
#endif
}
