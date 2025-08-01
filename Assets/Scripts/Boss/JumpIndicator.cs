using UnityEngine;

public class JumpIndicator : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private bool autoRotate = true;
    
    private void Update()
    {
        if (autoRotate)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
}