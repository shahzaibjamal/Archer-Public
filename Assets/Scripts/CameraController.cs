using UnityEngine;

public class TopDownCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;     
    [SerializeField] private float yDistance = 15f; 
    [SerializeField] private float zDistance = 0f;  
    [SerializeField] private float angle = 45f;     
    [SerializeField] private float smoothSpeed = 5f;

    private void Awake()
    {
    }

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
