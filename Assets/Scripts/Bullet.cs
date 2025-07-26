using UnityEngine;

public class Bullet : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        IInteractable interactable = collision.collider.GetComponent<IInteractable>();
        interactable?.Interact();

        // Destroy(gameObject); // Optional: destroy bullet after hit
    }
}
