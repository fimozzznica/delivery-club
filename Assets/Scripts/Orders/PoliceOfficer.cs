using UnityEngine;

/// <summary>
/// Полицейский, который ловит игрока при продаже скупщику
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PoliceOfficer : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Радиус обнаружения сделки")]
    public float detectionRadius = 10f;

    [Tooltip("Скорость погони")]
    public float chaseSpeed = 5f;

    [Tooltip("Дистанция поимки игрока")]
    public float catchDistance = 1f;

    [Header("Audio")]
    [Tooltip("Звук сирены")]
    public AudioClip sirenSound;

    [Header("References")]
    [Tooltip("Менеджер состояния игры")]
    public GameStateManager gameStateManager;

    [Tooltip("Transform игрока")]
    public Transform playerTransform;

    private AudioSource audioSource;
    private bool isChasing = false;

    void Start()
    {
        // Настройка аудио
        audioSource = GetComponent<AudioSource>();
        if (sirenSound != null && audioSource != null)
        {
            audioSource.clip = sirenSound;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D звук
            audioSource.maxDistance = detectionRadius;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 0f;
            audioSource.Play();
        }

        // Автопоиск компонентов
        if (gameStateManager == null)
            gameStateManager = FindObjectOfType<GameStateManager>();

        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (playerTransform == null || gameStateManager == null)
            return;

        // Если игра окончена, останавливаем погоню
        if (gameStateManager.IsGameOver)
        {
            StopChase();
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // Управление громкостью сирены по расстоянию (для 3D звука это дополнительный контроль)
        if (audioSource != null && audioSource.isPlaying)
        {
            if (distance >= detectionRadius)
            {
                audioSource.volume = 0f;
            }
            else
            {
                // Громче когда ближе
                float normalizedDistance = distance / detectionRadius;
                audioSource.volume = Mathf.Lerp(1f, 0.3f, normalizedDistance);
            }
        }

        // Проверяем нужно ли начать погоню
        if (gameStateManager.IsInBlackMarketDeal && distance <= detectionRadius)
        {
            if (!isChasing)
            {
                StartChase();
            }

            ChasePlayer(distance);
        }
        else if (isChasing)
        {
            // Останавливаем погоню если сделка завершена или игрок убежал
            StopChase();
        }
    }

    void StartChase()
    {
        isChasing = true;
        Debug.Log($"[PoliceOfficer] {name} начал погоню!");

        // Увеличиваем громкость сирены
        if (audioSource != null)
        {
            audioSource.volume = 1f;
        }
    }

    void ChasePlayer(float distance)
    {
        // Двигаемся к игроку
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        transform.position += direction * chaseSpeed * Time.deltaTime;

        // Смотрим на игрока
        transform.LookAt(playerTransform);

        // Проверяем поимку
        if (distance <= catchDistance)
        {
            CatchPlayer();
        }
    }

    void CatchPlayer()
    {
        Debug.Log($"[PoliceOfficer] {name} поймал игрока!");

        if (gameStateManager != null)
        {
            gameStateManager.OnPlayerCaughtByPolice(name);
        }

        StopChase();
    }

    void StopChase()
    {
        if (!isChasing)
            return;

        isChasing = false;
        Debug.Log($"[PoliceOfficer] {name} прекратил погоню");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isChasing ? Color.red : Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }
#endif
}
