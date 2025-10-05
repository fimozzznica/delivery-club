using UnityEngine;

public class CarFollowPlayerOnCollision : MonoBehaviour
{
    public Transform player; // Сюда привяжи игрока через инспектор
    private bool followPlayer = false;

    void Update()
    {
        if (followPlayer && player != null)
        {
            // Машина копирует позицию и поворот игрока
            transform.position = player.position;
            transform.rotation = player.rotation;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Chair"))
        {
            followPlayer = true;
        }
    }
}
