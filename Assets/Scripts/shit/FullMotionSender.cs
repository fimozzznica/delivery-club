using Bhaptics.SDK2;
using System;
using UnityEngine;

public class FullMotionSender : MonoBehaviour
{
    public Rigidbody vehicleRigidbody;
    private Vector3 lastVelocity;
    private Vector3 lastAngularVelocity;

    [Header("Bhaptics Feedback Settings")]
    [SerializeField] private bool enableHaptics = true;
    [SerializeField] private bool debugHaptics = false;
    [SerializeField] private float accelThreshold = 2.5f;
    [SerializeField] private float brakeThreshold = -3.5f;
    [SerializeField] private float lateralThreshold = 2.5f;
    [SerializeField] private float collisionIntensityScale = 3.6f;
    [SerializeField] private float vibrationIntensityScale = 0.1f;

    private float _lastBrakeInput = 0f;
    private Vector3 _previousAcceleration = Vector3.zero;

    void Start()
    {
        if (vehicleRigidbody == null)
            vehicleRigidbody = GetComponent<Rigidbody>();

        lastVelocity = vehicleRigidbody.linearVelocity;
        lastAngularVelocity = vehicleRigidbody.angularVelocity;
    }

    void FixedUpdate()
    {
        Vector3 currentVelocity = vehicleRigidbody.linearVelocity;
        Vector3 linearAcc = (currentVelocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = currentVelocity;
        
        Vector3 angularVel = vehicleRigidbody.angularVelocity;
        Vector3 angularAcc = (angularVel - lastAngularVelocity) / Time.fixedDeltaTime;
        lastAngularVelocity = angularVel;
        
        Vector3 position = vehicleRigidbody.transform.position;
        Vector3 rotationEuler = vehicleRigidbody.transform.rotation.eulerAngles;
        
        MotionData data = new MotionData(linearAcc, angularVel, angularAcc, position, rotationEuler);
        
        if (enableHaptics)
        {
            HandleHaptics(linearAcc, currentVelocity, angularVel);
        }

        _previousAcceleration = linearAcc;
    }

    private void HandleHaptics(Vector3 linearAcceleration, Vector3 currentVelocity, Vector3 angularVelocity)
    {
        Vector3 localAcceleration = transform.InverseTransformDirection(linearAcceleration);

        float forwardAccel = localAcceleration.z;  
        float lateralAccel = localAcceleration.x;  
        float verticalAccel = localAcceleration.y;

        float forwardSpeed = Vector3.Dot(currentVelocity, transform.forward);

        // Давление в спину
        if (forwardAccel > accelThreshold && forwardSpeed > 1f)
        {
            float intensity = Mathf.Clamp01(forwardAccel / 10f);
            BhapticsLibrary.Play("davlenie_kovsha", 0, intensity, 1, 0, 0);
            if (debugHaptics)
                Debug.Log($"[HAPTICS] Давление кресла: {intensity:F2}, Accel: {forwardAccel:F2}");
        }

        // Поворот влево
        if (lateralAccel < -lateralThreshold)
        {
            float intensity = Mathf.Clamp01(Mathf.Abs(lateralAccel) / 10f);
            BhapticsLibrary.Play("left_povorot", 0, intensity, 1, 0, 0);
            if (debugHaptics)
                Debug.Log($"[HAPTICS] Поворот влево: {intensity:F2}, Accel: {lateralAccel:F2}");
        }

        // Поворот вправо
        if (lateralAccel > lateralThreshold)
        {
            float intensity = Mathf.Clamp01(Mathf.Abs(lateralAccel) / 10f);
            BhapticsLibrary.Play("right_povorot", 0, intensity, 1, 0, 0);
            if (debugHaptics)
                Debug.Log($"[HAPTICS] Поворот вправо: {intensity:F2}, Accel: {lateralAccel:F2}");
        }
    }

    // Событие столкновения
    void OnCollisionEnter(Collision collision)
    {
        if (!enableHaptics) return;

        // ускорение от удара
        Vector3 impactForce = collision.impulse / Time.fixedDeltaTime;
        Vector3 impactAcc = impactForce / vehicleRigidbody.mass;

        // Эффект столкновения для Bhaptics
        float intensity = Mathf.Clamp01(impactAcc.magnitude * collisionIntensityScale / 100f);
        BhapticsLibrary.Play("remen_bezopasnosti", 0, intensity, 1, 0, 0);

        if (debugHaptics)
            Debug.Log($"[HAPTICS] Столкновение: {intensity:F2}, Force: {impactAcc.magnitude:F2}");
        
        lastVelocity += impactAcc * Time.fixedDeltaTime;
    }
}

[Serializable]
public class MotionData
{
    public Vector3 linearAcceleration;  
    public Vector3 angularVelocity;     
    public Vector3 angularAcceleration; 
    public Vector3 position;           
    public Vector3 rotationEuler;  

    public MotionData(Vector3 linAcc, Vector3 angVel, Vector3 angAcc, Vector3 pos, Vector3 rot)
    {
        linearAcceleration = linAcc;
        angularVelocity = angVel;
        angularAcceleration = angAcc;
        position = pos;
        rotationEuler = rot;
    }
}