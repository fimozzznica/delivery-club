using UnityEngine;

public class DisappearAndActivateOther : MonoBehaviour
{
    public float inactiveTime = 5f; // Время, на которое объект исчезает
    public GameObject objectToActivate; // Объект, который включится после паузы

    private void OnEnable()
    {
        // Запускаем таймер при активации объекта
        Invoke(nameof(HandleDeactivation), inactiveTime);
    }

    private void HandleDeactivation()
    {
        gameObject.SetActive(false); // Отключаем текущий объект

        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true); // Активируем другой объект
        }
        else
        {
            Debug.LogWarning("Не назначен объект для активации!");
        }
    }

    private void OnDisable()
    {
        // Если объект выключается раньше, сбрасываем Invoke
        CancelInvoke(nameof(HandleDeactivation));
    }
}
