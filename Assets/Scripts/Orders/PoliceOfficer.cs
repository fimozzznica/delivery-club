using UnityEngine;

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
        
        if (gameStateManager.IsGameOver)
        {
            StopChase();
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // Управление громкостью сирены по расстоянию 
        if (audioSource != null && audioSource.isPlaying)
        {
            if (distance >= detectionRadius)
            {
                audioSource.volume = 0f;
            }
            else
            {
                float normalizedDistance = distance / detectionRadius;
                audioSource.volume = Mathf.Lerp(1f, 0.3f, normalizedDistance);
            }
        }

        if (gameStateManager.IsInBlackMarketDeal)
        {
            Debug.Log($"[PoliceOfficer] {name} - Сделка активна! Расстояние до игрока: {distance:F1}м (радиус: {detectionRadius}м)");
        }


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
            Debug.Log($"[PoliceOfficer] {name} - Условия погони не выполнены. Сделка активна: {gameStateManager.IsInBlackMarketDeal}, Расстояние: {distance:F1}м");
            StopChase();
        }
    }

    void StartChase()
    {
        isChasing = true;
        Debug.Log($"[PoliceOfficer] 🚨 {name} НАЧАЛ ПОГОНЮ! Игрок обнаружен в радиусе {detectionRadius}м!");
        
        if (audioSource != null)
        {
            audioSource.volume = 1f;
            Debug.Log($"[PoliceOfficer] {name} - Сирена включена на полную громкость");
        }
    }

    void ChasePlayer(float distance)
    {
        Debug.Log($"[PoliceOfficer] {name} - Преследование! Расстояние: {distance:F1}м, дистанция поимки: {catchDistance}м");
        
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        transform.position += direction * chaseSpeed * Time.deltaTime;
        
        transform.LookAt(playerTransform);
        
        if (distance <= catchDistance)
        {
            Debug.Log($"[PoliceOfficer] {name} - Игрок в зоне поимки!");
            CatchPlayer();
        }
    }

    void CatchPlayer()
    {
        Debug.Log($"[PoliceOfficer] 👮 {name} ПОЙМАЛ ИГРОКА! GAME OVER!");

        if (gameStateManager != null)
        {
            gameStateManager.OnPlayerCaughtByPolice(name);
        }
        else
        {
            Debug.LogError($"[PoliceOfficer] {name} - GameStateManager отсутствует!");
        }

        StopChase();
    }

    void StopChase()
    {
        if (!isChasing)
            return;

        isChasing = false;
        Debug.Log($"[PoliceOfficer] ✋ {name} прекратил погоню (сделка завершена или игрок сбежал)");
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
