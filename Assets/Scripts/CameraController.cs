using UnityEngine;

public class TopDownCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;     // assign your player
    [SerializeField] private float yDistance = 15f; // height above player
    [SerializeField] private float zDistance = 0f;  // optional offset forward/back
    [SerializeField] private float angle = 45f;     // tilt angle downward
    [SerializeField] private float smoothSpeed = 5f;

    private void LateUpdate()
    {
        if (player == null) return;

        // Desired position: follow X/Z, fixed Y
        Vector3 targetPos = new Vector3(
            player.position.x,
            player.position.y + yDistance,
            player.position.z + zDistance
        );

        // Smoothly move camera
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);

        // Look at player with a fixed downward tilt
        Quaternion lookRotation = Quaternion.Euler(angle, 0f, 0f);
        transform.rotation = lookRotation;
    }
}
