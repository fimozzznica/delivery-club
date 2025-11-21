using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCPatrollnArea : MonoBehaviour
{
    [Header("Area settings")]
    public string allowedAreaName = "AllowedArea"; // имя Area в Navigation -> Areas
    public Transform centerTransform;             // центр области (если null — берём this.transform)
    public float radius = 10f;                    // радиус поиска точек внутри области

    [Header("Patrol settings")]
    public float nextPointDistance = 0.5f;        // дистанция, при которой считаем, что достигли точки
    public float pauseAtPoint = 0.5f;             // пауза между точками

    [Header("Animation (optional)")]
    public Animator animator;                     // если надо, можно оставить пустым
    public string speedParamName = "Speed";       // имя параметра в Animator (float)

    private NavMeshAgent agent;
    private int areaMask;
    private Vector3 centerPosition;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        int areaIdx = NavMesh.GetAreaFromName(allowedAreaName);
        if (areaIdx == -1)
        {
            Debug.LogError($"[NPCPatrolInArea] Area '{allowedAreaName}' not found. Проверь Navigation->Areas и имя.");
            areaMask = NavMesh.AllAreas;
        }
        else
        {
            areaMask = 1 << areaIdx;
            agent.areaMask = areaMask; // опционально: жёсткое ограничение агента
        }

        // Центр поиска
        centerPosition = (centerTransform != null) ? centerTransform.position : transform.position;

        // Убедимся, что агент не стоит на месте
        agent.isStopped = false;
        agent.updateRotation = true;
        agent.updatePosition = true;
        
        StartCoroutine(PatrolRoutine());
    }

    System.Collections.IEnumerator PatrolRoutine()
    {
        while (true)
        {
            // Ждём, пока не достигнем текущей цели (или если цели нет)
            while (agent.pathPending || agent.remainingDistance > nextPointDistance)
            {
                UpdateAnimatorSpeed();
                yield return null;
            }

            // Пауза у точки
            yield return new WaitForSeconds(pauseAtPoint);

            // Берём следующую точку в области
            Vector3 next;
            bool ok = TryGetRandomPointInArea(centerPosition, radius, out next);
            if (ok)
            {
                agent.SetDestination(next);
            }
            else
            {
                // если не нашли — снова
                Debug.LogWarning($"[NPCPatrolInArea] Не смог найти точку в области '{allowedAreaName}'. Увеличь radius или проверь NavMesh.");
                yield return new WaitForSeconds(1f);
            }
        }
    }

    // попытка найти случайную точку на NavMesh внутри радиуса
    bool TryGetRandomPointInArea(Vector3 center, float maxRadius, out Vector3 result)
    {
        const int maxAttempts = 30;
        NavMeshHit hit;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * maxRadius;
            randomPoint.y = center.y; 
            
            if (NavMesh.SamplePosition(randomPoint, out hit, maxRadius, areaMask))
            {
                result = hit.position;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    void UpdateAnimatorSpeed()
    {
        if (animator == null) return;
        
        float speed = agent.velocity.magnitude;
        animator.SetFloat(speedParamName, speed, 0.1f, Time.deltaTime);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = (centerTransform != null) ? centerTransform.position : transform.position;
        Gizmos.DrawWireSphere(center, radius);
    }
}