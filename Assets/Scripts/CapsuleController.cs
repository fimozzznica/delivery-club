using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class CapsuleController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float turnSpeed = 90f;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (Keyboard.current == null) return;

        Vector2 moveInput = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) moveInput.y -= 1;
        if (Keyboard.current.sKey.isPressed) moveInput.y += 1;
        if (Keyboard.current.aKey.isPressed) moveInput.x += 1;
        if (Keyboard.current.dKey.isPressed) moveInput.x -= 1;

        Vector3 move = transform.forward * moveInput.y + transform.right * moveInput.x;
        rb.MovePosition(rb.position + move * moveSpeed * Time.fixedDeltaTime);

        float turn = 0f;
        if (Keyboard.current.qKey.isPressed) turn -= 1;
        if (Keyboard.current.eKey.isPressed) turn += 1;
        Quaternion deltaRotation = Quaternion.Euler(0f, turn * turnSpeed * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }
}