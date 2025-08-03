using UnityEngine;

public class OnExitTrigger : MonoBehaviour
{
    public InteractionEvent targetEvent;
    public string requiredTag = "Player";

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"Trigger (exited) by: {other.name}");
        if (other.CompareTag(requiredTag))
            targetEvent?.Interact();
    }
}
