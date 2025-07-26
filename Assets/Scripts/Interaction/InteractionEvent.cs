using UnityEngine;
using UnityEngine.Events;

public class InteractionEvent : MonoBehaviour, IInteractable
{
    [Header("Event to Trigger")]
    public UnityEvent onInteract;

    public void Interact()
    {
        onInteract?.Invoke();
    }
}
