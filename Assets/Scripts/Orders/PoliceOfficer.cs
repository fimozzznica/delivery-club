using UnityEngine;

/// <summary>
/// Полицейский, который ловит игрока при продаже скупщику
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PoliceOfficer : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Радиус слышимости сирены")]
    public float sirenRadius = 10f;

    [Tooltip("Скорость погони")]
    public float chaseSpeed = 5f;

    [Header("Audio")]
    [Tooltip("Звук сирены (mp3)")]
    public AudioClip sirenSound;

    private AudioSource audioSource;
    private Transform playerTransform;
    private GameStateManager gameState;
    private bool isChasing = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (sirenSound != null)
        {
            audioSource.clip = sirenSound;
            audioSource.loop = true;
            audioSource.spatialBlend = 1f; // 3D звук
            audioSource.maxDistance = sirenRadius;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 0f;
            audioSource.Play();
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        gameState = FindObjectOfType<GameStateManager>();
    }

    void Update()
    {
        if (playerTransform == null || gameState == null)
            return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // Управление громкостью сирены по расстоянию
        if (audioSource != null && audioSource.isPlaying)
        {
            if (distance >= sirenRadius)
            {
                audioSource.volume = 0f; // Тишина за пределами радиуса
            }
            else
            {
                // Плавное затухание: громко вблизи, тихо на границе
                audioSource.volume = 1f - (distance / sirenRadius);
            }
        }

        // Проверяем, нужно ли ловить игрока
        if (gameState.IsInBlackMarketDeal && distance <= sirenRadius)
        {
            if (!isChasing)
            {
                StartChase();
            }

            ChasePlayer();
        }
    }

    void StartChase()
    {
        isChasing = true;
        Debug.Log("[PoliceOfficer] Начинаю погоню!");
    }

    void ChasePlayer()
    {
        // Двигаемся к игроку
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        transform.position += direction * chaseSpeed * Time.deltaTime;

        // Смотрим на игрока
        transform.LookAt(playerTransform);

        // Проверяем достижение игрока
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance <= 1f)
        {
            CatchPlayer();
        }
    }

    void CatchPlayer()
    {
        Debug.Log("[PoliceOfficer] Игрок пойман! Game Over!");

        if (gameState != null)
        {
            gameState.GameOver();
        }

        isChasing = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, sirenRadius);
    }
}
