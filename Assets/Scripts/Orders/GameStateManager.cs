using System.Collections.Generic;
using UnityEngine;



public class GameStateManager : MonoBehaviour
{
    public bool IsInBlackMarketDeal { get; private set; }
    public bool IsGameOver { get; private set; }

    [Header("Police Detection")]
    [Tooltip("Радиус проверки полицейских при начале сделки")]
    public float policeDetectionRadius = 15f;

    [Header("References")]
    [Tooltip("Экран Game Over (опционально)")]
    public GameObject gameOverScreen;

    [Tooltip("UI экран заказов для отображения Game Over")]
    public OrderScreenUI orderScreenUI;

    [Tooltip("Transform игрока (обычно Main Camera или XR Origin)")]
    public Transform playerTransform;

    [Header("Player Movement (VR)")]
    [Tooltip("Компонент движения игрока для блокировки при поимке")]
    public MonoBehaviour movementProvider;

    private List<PoliceOfficer> allPolice = new List<PoliceOfficer>();
    private bool movementDisabled = false;

    void Start()
    {
        IsInBlackMarketDeal = false;
        IsGameOver = false;

        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);
        
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        if (orderScreenUI == null)
            orderScreenUI = FindAnyObjectByType<OrderScreenUI>();
        
        RefreshPoliceList();
    }
    
    public void RefreshPoliceList()
    {
        allPolice.Clear();
        allPolice.AddRange(FindObjectsOfType<PoliceOfficer>());
    }
    
    // Начать сделку  с проверкой полицейских
    public void StartBlackMarketDeal()
    {
        if (IsGameOver)
            return;

        IsInBlackMarketDeal = true;
        
        CheckPoliceProximity();
    }

    void CheckPoliceProximity()
    {
        if (playerTransform == null)
        {
            return;
        }

        bool policeNearby = false;

        foreach (var police in allPolice)
        {
            if (police == null)
                continue;

            float distance = Vector3.Distance(playerTransform.position, police.transform.position);

            if (distance <= policeDetectionRadius)
            {
                policeNearby = true;
            }
        }

        if (!policeNearby)
        {
            Debug.Log("[GameStateManager] Полицейских поблизости нет");
        }
    }
    
    // Вызывается полицейским когда он ловит игрока
    public void OnPlayerCaughtByPolice(string officerName)
    {
        if (IsGameOver)
            return;

        Debug.Log($"[GameStateManager] Игрок пойман полицейским '{officerName}'!");

        DisablePlayerMovement();
        GameOver("Вас поймали полицейские!");
    }
    
    // Завершить сделку на чёрном рынке (при продаже)
    public void EndBlackMarketDeal()
    {
        if (IsGameOver)
            return;

        IsInBlackMarketDeal = false;
        Debug.Log("[GameStateManager] Сделка завершена!");
    }
    
    // Отключить передвижение игрока 
    void DisablePlayerMovement()
    {
        if (movementDisabled)
            return;

        if (movementProvider != null)
        {
            movementProvider.enabled = false;
            Debug.Log($"[GameStateManager] Передвижение отключено");
        }

        movementDisabled = true;
    }
    
    // Включить передвижение игрока
    void EnablePlayerMovement()
    {
        if (!movementDisabled)
            return;

        if (movementProvider != null)
        {
            movementProvider.enabled = true;
            Debug.Log($"[GameStateManager] Передвижение включено");
        }

        movementDisabled = false;
    }
    
    public void GameOver(string reason = "Game Over")
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
        IsInBlackMarketDeal = false;

        Debug.Log($"[GameStateManager] GAME OVER!");

        DisablePlayerMovement();

        // Показываем Game Over на экране заказов
        if (orderScreenUI != null)
        {
            orderScreenUI.ShowGameOver(reason);
        }

        // Показываем отдельный экран Game Over если есть
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
        }
    }
    
    // Перезапустить игру
    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        EnablePlayerMovement();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, policeDetectionRadius);
        }
    }
#endif
}
