using UnityEngine;

public class CameraIdleJiggle : MonoBehaviour
{
    public float intensity = 0.05f;      // How far it moves
    public float frequency = 0.5f;       // How fast it changes
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float offsetX = Mathf.PerlinNoise(Time.time * frequency, 0f) - 0.5f;
        float offsetY = Mathf.PerlinNoise(0f, Time.time * frequency) - 0.5f;
        float offsetZ = Mathf.PerlinNoise(Time.time * frequency, Time.time * frequency) - 0.5f;

        Vector3 offset = new Vector3(offsetX, offsetY, offsetZ) * intensity;
        transform.localPosition = startPos + offset;
    }
}
