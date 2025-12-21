using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public bool IsInBlackMarketDeal { get; private set; }
    public bool IsGameOver { get; private set; }
    
    [Tooltip("Радиус проверки полицейских")]
    public float policeDetectionRadius = 15f;
    
    [Tooltip("Экран Game Over")]
    public GameObject gameOverScreen;

    [Tooltip("UI экран заказов для отображения Game Over")]
    public OrderScreenUI orderScreenUI;

    [Tooltip("Transform игрока")]
    public Transform playerTransform;
    
    [Tooltip("Компонент движения игрока для блокировки при поимке")]
    public MonoBehaviour movementProvider;

    private List<PoliceOfficer> allPolice = new List<PoliceOfficer>();
    private bool movementDisabled = false;

    void Start()
    {
        IsInBlackMarketDeal = false;
        IsGameOver = false;

        if (gameOverScreen != null) { gameOverScreen.SetActive(false); }

        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) { playerTransform = player.transform; }
        }
        if (orderScreenUI == null) { orderScreenUI = FindAnyObjectByType<OrderScreenUI>(); }
        RefreshPoliceList();
    }

    public void RefreshPoliceList()
    {
        allPolice.Clear();
        allPolice.AddRange(FindObjectsOfType<PoliceOfficer>());
    }

    public void StartBlackMarketDeal()
    {
        if (IsGameOver) { return; }
        IsInBlackMarketDeal = true;
        CheckPoliceProximity();
    }

    void CheckPoliceProximity()
    {
        if (playerTransform == null) { return; }
        bool policeNearby = false;
        foreach (var police in allPolice)
        {
            if (police == null) { continue; }
            float distance = Vector3.Distance(playerTransform.position, police.transform.position);
            if (distance <= policeDetectionRadius) { policeNearby = true; }
        }
    }

    public void OnPlayerCaughtByPolice(string officerName)
    {
        if (IsGameOver) { return; }

        DisablePlayerMovement();
        GameOver("Вас поймали полицейские!");
    }
    
    public void EndBlackMarketDeal()
    {
        if (IsGameOver) { return; }
        IsInBlackMarketDeal = false;
    }

    void DisablePlayerMovement()
    {
        if (movementDisabled) { return; }
        if (movementProvider != null) { movementProvider.enabled = false; }
        movementDisabled = true;
    }
    
    void EnablePlayerMovement()
    {
        if (!movementDisabled) { return; }
        if (movementProvider != null) { movementProvider.enabled = true; }
        movementDisabled = false;
    }

    public void GameOver(string reason = "Game Over")
    {
        if (IsGameOver) { return; }
        IsGameOver = true;
        IsInBlackMarketDeal = false;
        DisablePlayerMovement();
        if (orderScreenUI != null) { orderScreenUI.ShowGameOver(reason); }
        if (gameOverScreen != null) { gameOverScreen.SetActive(true); }
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        EnablePlayerMovement();
    }
}
