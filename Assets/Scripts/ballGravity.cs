using UnityEngine;

public class ballGravity : MonoBehaviour
{
    [Header("Gravity Settings")]
    [Range(0f, 2f)]
    public float gravityMultiplier = 1f; // 1 = normal gravity, 0 = no gravity
    
    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            // Disable Unity's built-in gravity
            rb.useGravity = false;
        }
    }
    
    void FixedUpdate()
    {
        if (rb != null)
        {
            // Apply custom gravity based on multiplier
            Vector3 customGravity = Physics.gravity * gravityMultiplier;
            rb.AddForce(customGravity, ForceMode.Acceleration);
        }
    }
}