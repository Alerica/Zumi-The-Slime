using UnityEngine;

public class StickyPlatform : MonoBehaviour
{
    private Rigidbody rb;
    private Transform currentPlatform;
    private Vector3 lastPlatformPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (currentPlatform != null)
        {
            Vector3 platformMovement = currentPlatform.position - lastPlatformPosition;
            rb.position += platformMovement;
            lastPlatformPosition = currentPlatform.position;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        currentPlatform = collision.collider.transform;
        lastPlatformPosition = currentPlatform.position;
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.collider.transform == currentPlatform)
        {
            currentPlatform = null;
        }
    }
}
