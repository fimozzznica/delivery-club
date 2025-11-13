using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

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
    [Tooltip("Экран Game Over (можно оставить пустым, будет использоваться UI Manager)")]
    public GameObject gameOverScreen;

    [Tooltip("Ссылка на OrderScreenUI для отображения Game Over")]
    public OrderScreenUI orderScreenUI;

    [Header("Player Movement")]
    [Tooltip("XR Origin для блокировки передвижения (обычно XR Origin)")]
    public GameObject xrOrigin;

    [Tooltip("Компонент движения для отключения (например, Continuous Move Provider)")]
    public MonoBehaviour movementProvider;

    private Transform playerTransform;
    private List<PoliceOfficer> allPolice = new List<PoliceOfficer>();
    private bool movementDisabled = false;

    void Start()
    {
        IsInBlackMarketDeal = false;
        IsGameOver = false;

        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);

        // Находим игрока
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        // Находим XR Origin если не назначен
        if (xrOrigin == null)
        {
            xrOrigin = GameObject.Find("XR Origin");
            if (xrOrigin == null)
                xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        }

        // Находим movement provider автоматически
        if (movementProvider == null && xrOrigin != null)
        {
            movementProvider = xrOrigin.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider>();

            if (movementProvider == null)
            {
                // Пробуем найти другие типы движения
                var allMovement = xrOrigin.GetComponentsInChildren<MonoBehaviour>();
                foreach (var comp in allMovement)
                {
                    if (comp.GetType().Name.Contains("Move") || comp.GetType().Name.Contains("Locomotion"))
                    {
                        movementProvider = comp;
                        break;
                    }
                }
            }
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
    /// Проверить близость полицейских и заблокировать игрока если они рядом
    /// </summary>
    void CheckPoliceProximity()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("[GameStateManager] Не найден Transform игрока!");
            return;
        }

        bool policeCaught = false;

        foreach (var police in allPolice)
        {
            if (police == null)
                continue;

            float distance = Vector3.Distance(playerTransform.position, police.transform.position);

            if (distance <= policeDetectionRadius)
            {
                Debug.Log($"[GameStateManager] ⚠️ Полицейский {police.name} обнаружен на расстоянии {distance:F1}м!");
                policeCaught = true;
                break;
            }
        }

        if (policeCaught)
        {
            // Блокируем передвижение игрока
            DisablePlayerMovement();

            // Запускаем Game Over
            GameOver("Вас поймали полицейские!");
        }
        else
        {
            Debug.Log("[GameStateManager] ✅ Полицейских поблизости нет, сделка безопасна");
        }
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
    /// Отключить передвижение игрока (оставить только вращение камеры)
    /// </summary>
    void DisablePlayerMovement()
    {
        if (movementDisabled)
            return;

        if (movementProvider != null)
        {
            movementProvider.enabled = false;
            Debug.Log($"[GameStateManager] Передвижение отключено: {movementProvider.GetType().Name}");
        }

        // Дополнительно можем заблокировать Rigidbody если есть
        if (xrOrigin != null)
        {
            var rb = xrOrigin.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
                Debug.Log("[GameStateManager] Rigidbody игрока заморожен");
            }
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
            Debug.Log($"[GameStateManager] Передвижение включено: {movementProvider.GetType().Name}");
        }

        if (xrOrigin != null)
        {
            var rb = xrOrigin.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
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

        // Отключаем передвижение
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

    void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, policeDetectionRadius);
        }
    }
}
