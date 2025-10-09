using UnityEngine;
using System;

public class FullMotionSender : MonoBehaviour
{
    public Rigidbody vehicleRigidbody;
    private Vector3 lastVelocity;
    private Vector3 lastAngularVelocity;

    void Start()
    {
        if (vehicleRigidbody == null)
            vehicleRigidbody = GetComponent<Rigidbody>();

        lastVelocity = vehicleRigidbody.linearVelocity;
        lastAngularVelocity = vehicleRigidbody.angularVelocity;
    }

    void FixedUpdate()
    {
        // === 1. Линейное ускорение ===
        Vector3 currentVelocity = vehicleRigidbody.linearVelocity;
        Vector3 linearAcc = (currentVelocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = currentVelocity;

        // === 2. Угловая скорость ===
        Vector3 angularVel = vehicleRigidbody.angularVelocity;
        Vector3 angularAcc = (angularVel - lastAngularVelocity) / Time.fixedDeltaTime;
        lastAngularVelocity = angularVel;

        // === 3. Абсолютное положение в пространстве ===
        Vector3 position = vehicleRigidbody.transform.position;
        Vector3 rotationEuler = vehicleRigidbody.transform.rotation.eulerAngles;

        // === 4. Формируем структуру данных для капсулы ===
        MotionData data = new MotionData(linearAcc, angularVel, angularAcc, position, rotationEuler);

        // === 5. Пример вывода / отправки ===
        string json = JsonUtility.ToJson(data);
        Debug.Log(json); // здесь можно отправлять в капсулу
    }

    // === 6. Событие столкновения ===
    void OnCollisionEnter(Collision collision)
    {
        // Дополнительное ускорение от удара
        Vector3 impactForce = collision.impulse / Time.fixedDeltaTime;
        Vector3 impactAcc = impactForce / vehicleRigidbody.mass;

        // Можно добавить к последнему ускорению для капсулы
        lastVelocity += impactAcc * Time.fixedDeltaTime; 
    }
}

// ===== Структура данных для капсулы =====
[Serializable]
public class MotionData
{
    public Vector3 linearAcceleration;  // m/s²
    public Vector3 angularVelocity;     // рад/с
    public Vector3 angularAcceleration; // рад/с²
    public Vector3 position;            // мировые координаты
    public Vector3 rotationEuler;       // Pitch/Yaw/Roll в градусах

    public MotionData(Vector3 linAcc, Vector3 angVel, Vector3 angAcc, Vector3 pos, Vector3 rot)
    {
        linearAcceleration = linAcc;
        angularVelocity = angVel;
        angularAcceleration = angAcc;
        position = pos;
        rotationEuler = rot;
    }
}
