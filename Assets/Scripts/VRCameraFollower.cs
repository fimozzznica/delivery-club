using UnityEngine;

public class VRCameraFollower : MonoBehaviour
{
    public Transform vrSeat;

    void LateUpdate()
    {
        if (vrSeat == null) return;

        transform.position = vrSeat.position;

        // Сохраняем ориентацию кабины по Y (без влияния головы)
        Vector3 seatEuler = vrSeat.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, seatEuler.y, 0f);
    }
}