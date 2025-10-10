using UnityEngine;

public class DisappearAfterTime : MonoBehaviour
{
    public float lifetime = 5f; // Время до исчезновения

    private void OnEnable()
    {
        // Запускаем таймер при активации объекта
        Invoke(nameof(Deactivate), lifetime);
    }

    private void Deactivate()
    {
        gameObject.SetActive(false); // Выключаем объект
    }

    private void OnDisable()
    {
        // Отменяем Invoke, если объект деактивируется раньше времени
        CancelInvoke(nameof(Deactivate));
    }
}