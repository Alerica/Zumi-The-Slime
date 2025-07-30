using UnityEngine;

public class StickyPlatform : MonoBehaviour
{
    // Add to Player (NOT TO PLATFORM)
    private MovingPlatform currentPlatform;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (currentPlatform != null)
        {
            rb.position += currentPlatform.Velocity * Time.fixedDeltaTime;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.collider.TryGetComponent(out MovingPlatform platform))
        {
            currentPlatform = platform;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.collider.GetComponent<MovingPlatform>() == currentPlatform)
        {
            currentPlatform = null;
        }
    }
}
