using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private TMP_Text tutorialText;
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private float typingSpeed = 0.05f;
    private Coroutine typingCoroutine;
    public List<TutorialStep> steps = new List<TutorialStep>();
    private int currentStepIndex = 0;
    private bool hasMoved = false;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    [Header("Customization")]
    public float minimumDistanceToTrigger = 5f; // Minimum distance to trigger a checkpoint

    void Start()
    {
        ShowCurrentStep();
    }

    void Update()
    {
        if (currentStepIndex >= steps.Count) return;

        var step = steps[currentStepIndex];

        switch (step.triggerType)
        {
            case TutorialTriggerType.OnStart:
                ProceedToNext();
                break;

            case TutorialTriggerType.OnMove:
                if (!hasMoved && PlayerMoved())
                {
                    hasMoved = true;
                    ProceedToNext();
                }
                break;

            case TutorialTriggerType.OnReachCheckpoint:
                // Debug.Log("Distance to reach checkpoint: " + (step.target.transform.position - GameObject.FindWithTag("Player").transform.position).magnitude);
                distanceText.text = "" + Vector3.Distance(GameObject.FindWithTag("Player").transform.position, step.target.transform.position).ToString("F2") + " m";
                if (step.target && PlayerReachedTarget(step.target))
                    ProceedToNext();
                break;

            case TutorialTriggerType.OnKeyPress:
                // Debug.Log("Waiting for key press: " + step.keyToPress);
                if (Input.GetKeyDown(step.keyToPress))
                    ProceedToNext();
                break;

            case TutorialTriggerType.OnWait:
                if (!isWaiting)
                {
                    isWaiting = true;
                    waitTimer = step.waitDuration;
                }

                if (isWaiting)
                {
                    waitTimer -= Time.deltaTime;
                    if (waitTimer <= 0f)
                    {
                        isWaiting = false;
                        ProceedToNext();
                    }
                }
                break;

            case TutorialTriggerType.OnEnemyDestroyed:
                if (steps[currentStepIndex].targetEnemy == null)
                ProceedToNext();
                break;
                
        }
    }


    void ProceedToNext()
    {
        steps[currentStepIndex].onFinishEvent?.Invoke();
        currentStepIndex++;
        isWaiting = false;
        ShowCurrentStep();
    }

        void ShowCurrentStep()
    {
        if (currentStepIndex >= steps.Count)
        {
            tutorialText.text = "";
            return;
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(steps[currentStepIndex].guideText));
    }


    bool PlayerMoved()
    {
        return Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0 ||
               Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0;
    }

    bool PlayerReachedTarget(GameObject target)
    {
        return Vector3.Distance(GameObject.FindWithTag("Player").transform.position, target.transform.position) < minimumDistanceToTrigger;
    }

    IEnumerator TypeText(string fullText)
    {
        tutorialText.text = "";
        foreach (char letter in fullText)
        {
            tutorialText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

}
