using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет состоянием игры и проверяет полицейских при сделках на чёрном рынке
/// </summary>
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

        // Автопоиск компонентов
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        if (orderScreenUI == null)
            orderScreenUI = FindObjectOfType<OrderScreenUI>();

        // Собираем всех полицейских на сцене
        RefreshPoliceList();
    }

    /// <summary>
    /// Обновить список полицейских (вызывать если полицейские спавнятся динамически)
    /// </summary>
    public void RefreshPoliceList()
    {
        allPolice.Clear();
        allPolice.AddRange(FindObjectsOfType<PoliceOfficer>());
        Debug.Log($"[GameStateManager] Найдено полицейских: {allPolice.Count}");
    }

    /// <summary>
    /// Начать сделку на чёрном рынке с проверкой полицейских
    /// </summary>
    public void StartBlackMarketDeal()
    {
        if (IsGameOver)
            return;

        IsInBlackMarketDeal = true;
        Debug.Log("[GameStateManager] Начата сделка на чёрном рынке!");

        // Проверяем расстояние до всех полицейских
        CheckPoliceProximity();
    }

    /// <summary>
    /// Проверить близость полицейских (вызывается при начале сделки)
    /// </summary>
    void CheckPoliceProximity()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("[GameStateManager] Не найден Transform игрока!");
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
                Debug.Log($"[GameStateManager] ⚠️ Полицейский '{police.name}' обнаружен на расстоянии {distance:F1}м!");
                policeNearby = true;
            }
        }

        if (!policeNearby)
        {
            Debug.Log("[GameStateManager] ✅ Полицейских поблизости нет, сделка безопасна");
        }
        // Если полицейские рядом, они начнут погоню и вызовут OnPlayerCaughtByPolice()
    }

    /// <summary>
    /// Вызывается полицейским когда он ловит игрока
    /// </summary>
    public void OnPlayerCaughtByPolice(string officerName)
    {
        if (IsGameOver)
            return;

        Debug.Log($"[GameStateManager] Игрок пойман полицейским '{officerName}'!");

        DisablePlayerMovement();
        GameOver("Вас поймали полицейские!");
    }

    /// <summary>
    /// Завершить сделку на чёрном рынке (вызывается при продаже)
    /// </summary>
    public void EndBlackMarketDeal()
    {
        if (IsGameOver)
            return;

        IsInBlackMarketDeal = false;
        Debug.Log("[GameStateManager] Сделка завершена!");
    }

    /// <summary>
    /// Отключить передвижение игрока (оставить вращение камеры)
    /// </summary>
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

    /// <summary>
    /// Включить передвижение игрока обратно
    /// </summary>
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

    /// <summary>
    /// Game Over с кастомным сообщением
    /// </summary>
    public void GameOver(string reason = "Game Over")
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
        IsInBlackMarketDeal = false;

        Debug.Log($"[GameStateManager] GAME OVER! Причина: {reason}");

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

    /// <summary>
    /// Перезапустить игру
    /// </summary>
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
