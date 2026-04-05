using UnityEngine;

public class RotateAbout : MonoBehaviour
{
    public float rotationSpeed = 50f;     // degrees per second
    public Vector3 rotationAxis = Vector3.up; // axis to rotate around
    public float scaleAmplitude = 0.2f;   // how much to scale (+/-)
    public float scaleFrequency = 2f;     // speed of pulsing

    private Vector3 baseScale;

    void Start()
    {
        // Force the sprite to lie flat on the ground (X = 90°)
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Save the original scale
        baseScale = transform.localScale;
    }

    void Update()
    {
        // Rotate around the chosen axis
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);

        // Sine wave scaling
        float scaleOffset = Mathf.Sin(Time.time * scaleFrequency) * scaleAmplitude;
        transform.localScale = baseScale * (1f + scaleOffset);
    }
}
