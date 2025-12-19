using Futurift.DataSenders;
using Futurift.Options;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Futurift
{
    public class SimpleController : MonoBehaviour
    {
        [SerializeField] private string ipAddress = "127.0.0.1";
        [SerializeField] private int port = 6065;
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 2.5f;
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 45f;
        [SerializeField] private float maxPitch = 15f; // Вниз
        [SerializeField] private float minPitch = -21f; // Вверх
        [SerializeField] private float maxYaw = 180f;
        [SerializeField] private float maxRoll = 10f; // Макс. крен влево/вправо
        [SerializeField] private float rollSmoothTime = 0.15f; // Время сглаживания крена
        [Header("Input Actions")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference rotateAction;
        [SerializeField] private InputActionReference toggleHealthBarAction;
        [SerializeField] private Transform xrOrigin; // Ссылка на XR Origin (если нужно синхронизировать)
        [SerializeField] private float heightSmoothTime = 0.1f; // Время сглаживания высоты

        private FutuRiftController _controller;
        private Rigidbody rb;
        private float currentPitch = 0f;
        private float currentYaw = 0f;
        private float currentRoll = 0f;
        private float targetRoll = 0f; // Целевой крен
        private float rollVelocity = 0f; // Для SmoothDamp
        private float heightVelocity = 0f; // Для сглаживания высоты
        private bool isWalking = false;
        private bool isRotating = false;
        private bool isDead = false;
        private void Awake()
        {
            var udpOptions = new UdpOptions
            {
                ip = ipAddress,
                port = port
            };
            _controller = new FutuRiftController(new UdpPortSender(udpOptions));
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = false; // Динамический режим
                rb.freezeRotation = true; // Для ручного управления
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Для точных столкновений
                rb.linearDamping = 5f; // Увеличим сопротивление
            }
            Vector3 initialRotation = transform.eulerAngles;
            if (transform.forward.z < 0) // Если Forward по -Z
            {
            }
            currentYaw = Mathf.Repeat(initialRotation.y, 360f);
            currentPitch = initialRotation.x > 180f ? initialRotation.x - 360f : initialRotation.x;
            currentRoll = Mathf.Approximately(initialRotation.z, 0f) ? 0f : (initialRotation.z > 180f ? initialRotation.z - 360f : initialRotation.z);
            targetRoll = currentRoll;

        }

        private void OnEnable()
        {
            _controller?.Start();
            if (moveAction != null) moveAction.action.Enable();
            if (rotateAction != null) rotateAction.action.Enable();
            if (toggleHealthBarAction != null)
            {
                toggleHealthBarAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            _controller?.Stop();
            if (moveAction != null) moveAction.action.Disable();
            if (rotateAction != null) rotateAction.action.Disable();
        }

        private void FixedUpdate()
        {
            Vector2 moveInput = moveAction.action.ReadValue<Vector2>(); //считываем контроллеры
            Vector2 rotateInput = rotateAction.action.ReadValue<Vector2>();

            Vector3 moveDelta = (transform.forward * moveInput.y + transform.right * moveInput.x) * moveSpeed * Time.fixedDeltaTime;
            Vector3 newPosition = rb.position + moveDelta;

            rb.MovePosition(newPosition);

            currentYaw += rotateInput.x * rotationSpeed * Time.fixedDeltaTime;
            currentYaw = Mathf.Repeat(currentYaw, 360f);
            currentPitch -= rotateInput.y * rotationSpeed * Time.fixedDeltaTime;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

            targetRoll = -moveInput.x * maxRoll; // Рассчитываем целевой крен
            targetRoll = Mathf.Clamp(targetRoll, -maxRoll, maxRoll);

            currentRoll = Mathf.SmoothDamp(currentRoll, targetRoll, ref rollVelocity, rollSmoothTime); // сглаживание

            transform.rotation = Quaternion.Euler(currentPitch, currentYaw, currentRoll); // переводим в обычные углы

            _controller.Pitch = currentPitch;
            _controller.Roll = currentRoll; //применяем к капсуле
        }
    }
}