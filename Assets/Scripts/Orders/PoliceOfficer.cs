using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PoliceOfficer : MonoBehaviour
{
    [Tooltip("Радиус обнаружения сделки")]
    public float detectionRadius = 10f;

    [Tooltip("Скорость погони")]
    public float chaseSpeed = 5f;

    [Tooltip("Дистанция поимки игрока")]
    public float catchDistance = 1f;
    
    [Tooltip("Звук сирены")]
    public AudioClip sirenSound;
    
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
            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = detectionRadius;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 0f;
            audioSource.Play();
        }

        if (gameStateManager == null) { gameStateManager = FindObjectOfType<GameStateManager>(); }

        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) { playerTransform = player.transform; }
        }
    }

    void Update()
    {
        if (playerTransform == null || gameStateManager == null) { return; }
        if (gameStateManager.IsGameOver)
        {
            StopChase();
            return;
        }
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (audioSource != null && audioSource.isPlaying)
        {
            if (distance >= detectionRadius) { audioSource.volume = 0f; }
            else
            {
                float normalizedDistance = distance / detectionRadius;
                audioSource.volume = Mathf.Lerp(1f, 0.3f, normalizedDistance);
            }
        }

        if (gameStateManager.IsInBlackMarketDeal && distance <= detectionRadius)
        {
            if (!isChasing) { StartChase(); }
            ChasePlayer(distance);
        }
        else if (isChasing) { StopChase(); }
    }

    void StartChase()
    {
        isChasing = true;

        if (audioSource != null) { audioSource.volume = 1f; }
    }

    void ChasePlayer(float distance)
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        transform.position += direction * chaseSpeed * Time.deltaTime;
        transform.LookAt(playerTransform);
        if (distance <= catchDistance) { CatchPlayer(); }
    }

    void CatchPlayer()
    {
        if (gameStateManager != null) { gameStateManager.OnPlayerCaughtByPolice(name); }
        StopChase();
    }

    void StopChase()
    {
        if (!isChasing) { return; }
        isChasing = false;
    }
}
