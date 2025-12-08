using Futurift.DataSenders;
using Futurift.Options;
//using Unity.Android.Gradle.Manifest;
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
        [SerializeField] private Terrain terrain;
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
        [Header("UI Settings")]
        [SerializeField] private GameObject healthBarCanvas;
        [SerializeField] private float fadeOutTime = 0.2f;

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
                toggleHealthBarAction.action.performed += OnToggleHealthBar;
            }
        }

        private void OnDisable()
        {
            _controller?.Stop();
            if (moveAction != null) moveAction.action.Disable();
            if (rotateAction != null) rotateAction.action.Disable();
        }

        private void OnPlayerDeath()
        {
            // Останавливаем FutuRiftController
            _controller.Pitch = 0f;
            _controller.Roll = 0f;
        }

        private void OnToggleHealthBar(InputAction.CallbackContext context)
        {
            if (healthBarCanvas != null)
            {
                healthBarCanvas.SetActive(!healthBarCanvas.activeSelf);
            }
        }
        private void FixedUpdate()
        {
            Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
            Vector2 rotateInput = rotateAction.action.ReadValue<Vector2>();
            if (moveInput.magnitude < 0.3f) moveInput = Vector2.zero; // Мёртвая зона для движения
            if (rotateInput.magnitude < 0.3f) rotateInput = Vector2.zero; // Мёртвая зона для поворота

            Vector3 moveDelta = (transform.forward * moveInput.y + transform.right * moveInput.x) * moveSpeed * Time.fixedDeltaTime;
            if (moveInput.magnitude < 0.1f) moveDelta.x = 0; // Принудительное обнуление бокового смещения
            Vector3 newPosition = rb.position + moveDelta;

            rb.MovePosition(newPosition);

            if (moveInput.magnitude < 0.1f)
            {
                if (Mathf.Abs(rb.linearVelocity.x) < 0.2f) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, rb.linearVelocity.z);
                if (Mathf.Abs(rb.linearVelocity.y) < 0.2f) rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                if (Mathf.Abs(rb.linearVelocity.z) < 0.2f) rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, 0);
                rb.Sleep(); // Усыпляем при остановке
            }
            else
            {
                rb.WakeUp(); // Пробуждаем при движении
            }

            if (xrOrigin != null && moveInput.magnitude < 0.1f)
            {
                xrOrigin.position = transform.position;
                xrOrigin.rotation = transform.rotation;
            }

            currentYaw += rotateInput.x * rotationSpeed * Time.fixedDeltaTime;
            currentYaw = Mathf.Repeat(currentYaw, 360f);
            currentPitch -= rotateInput.y * rotationSpeed * Time.fixedDeltaTime;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

            targetRoll = -moveInput.x * maxRoll; // Рассчитываем целевой крен
            targetRoll = Mathf.Clamp(targetRoll, -maxRoll, maxRoll);


            if (Mathf.Abs(moveInput.x) < 0.3f) // Используем тот же порог, что для moveInput
            {
                targetRoll = 0f; // Плавно возвращаем к нулю
            }
            currentRoll = Mathf.SmoothDamp(currentRoll, targetRoll, ref rollVelocity, rollSmoothTime);

            transform.rotation = Quaternion.Euler(currentPitch, currentYaw, currentRoll);

            _controller.Pitch = currentPitch;
            _controller.Roll = currentRoll;
        }
    }
}