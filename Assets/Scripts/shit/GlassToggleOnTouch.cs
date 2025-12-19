using UnityEngine;

public class GlassToggleOnTrigger : MonoBehaviour
{
    [Tooltip("Смещение по Z при открытии")]
    public float moveDistanceZ = 2.1f;

    [Tooltip("Если true — работаем в локальных координатах (рекомендуется при наличии родителя)")]
    public bool useLocalPosition = true;

    private bool isOpen = false;

    // Начальная позиция (локальная или мировая в зависимости от useLocalPosition)
    private Vector3 initialPosition;
    private float initialZ;

    private void Start()
    {
        if (useLocalPosition)
        {
            initialPosition = transform.localPosition;
        }
        else
        {
            initialPosition = transform.position;
        }

        initialZ = initialPosition.z;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Hand")) return;

        // Получаем текущую позицию (локальную или мировую)
        if (useLocalPosition)
        {
            Vector3 p = transform.localPosition;
            p.z = isOpen ? initialZ : (initialZ + moveDistanceZ);
            transform.localPosition = p;
        }
        else
        {
            Vector3 p = transform.position;
            p.z = isOpen ? initialZ : (initialZ + moveDistanceZ);
            transform.position = p;
        }

        isOpen = !isOpen;
    }
}
