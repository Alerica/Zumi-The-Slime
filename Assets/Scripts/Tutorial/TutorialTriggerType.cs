using UnityEngine;
using UnityEngine.Events;

public enum TutorialTriggerType
{
    OnStart,
    OnMove,
    OnReachCheckpoint,
    OnKeyPress,
    OnWait,
    OnEnemyDestroyed
}

[System.Serializable]
public class TutorialStep
{
    [TextArea] public string guideText;
    public TutorialTriggerType triggerType;
    public GameObject target; // Checkpoint
    public GameObject targetEnemy; // Enemy for OnEnemyDestroyed
    public KeyCode keyToPress; // optional
    public float waitDuration = 2f;
    public UnityEvent onFinishEvent; 
}
