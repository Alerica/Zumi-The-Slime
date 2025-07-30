using UnityEngine;

public class OnEnterTrigger : MonoBehaviour
{
    public InteractionEvent targetEvent;
    public string requiredTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"PressurePlate triggered by: {other.name}");
        if (other.CompareTag(requiredTag))
            targetEvent?.Interact();
    }
}
