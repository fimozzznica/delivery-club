using UnityEngine;

/// <summary>
/// Управляет состоянием игры
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public bool IsInBlackMarketDeal { get; private set; }
    public bool IsGameOver { get; private set; }

    [Header("References")]
    public GameObject gameOverScreen;

    void Start()
    {
        IsInBlackMarketDeal = false;
        IsGameOver = false;

        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);
    }

    public void StartBlackMarketDeal()
    {
        if (IsGameOver)
            return;

        IsInBlackMarketDeal = true;
        Debug.Log("[GameStateManager] Начата сделка на чёрном рынке!");

        // Через 5 секунд выходим из состояния если не пойманы
        Invoke(nameof(EndBlackMarketDeal), 5f);
    }

    void EndBlackMarketDeal()
    {
        if (!IsGameOver)
        {
            IsInBlackMarketDeal = false;
            Debug.Log("[GameStateManager] Сделка завершена успешно!");
        }
    }

    public void GameOver()
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
        IsInBlackMarketDeal = false;

        Debug.Log("[GameStateManager] GAME OVER!");

        // Останавливаем время
        Time.timeScale = 0f;

        // Показываем экран Game Over
        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);

        // Блокируем управление игроком
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.isKinematic = true;
            }

            var playerController = player.GetComponent<MonoBehaviour>();
            if (playerController != null)
            {
                playerController.enabled = false;
            }
        }
    }

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
    }
}
