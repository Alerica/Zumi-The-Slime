using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class InteractionEventWithDelays : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public class TimedEvent
    {
        public float delay;
        public UnityEvent onTrigger;
    }

    [Header("Timed Events List")]
    public List<TimedEvent> timedEvents = new List<TimedEvent>();

    public void Interact()
    {
        StartCoroutine(RunTimedEvents());
    }

    private IEnumerator RunTimedEvents()
    {
        foreach (var timedEvent in timedEvents)
        {
            yield return new WaitForSeconds(timedEvent.delay);
            timedEvent.onTrigger?.Invoke();
        }
    }
}
