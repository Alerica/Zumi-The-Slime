using UnityEngine;

public class MoveCycle : MonoBehaviour
{
    public Vector3 moveOffset = new Vector3(0, 5, 0);  // Offset to move from original position
    public float moveDuration = 1f;                   // Time taken to move to offset and back
    public float waitDuration = 5f;                   // Time to wait before moving again
    public bool useLocalPosition = false;            
    private Vector3 originalPosition;
    private bool isMoving = false;

    private void Start()
    {
        originalPosition = useLocalPosition ? transform.localPosition : transform.position;
        StartCoroutine(MoveCycleRoutine());
    }

    private System.Collections.IEnumerator MoveCycleRoutine()
    {
        while (true)
        {
            yield return MoveTo(originalPosition + moveOffset);
            yield return new WaitForSeconds(waitDuration);
            yield return MoveTo(originalPosition);
            yield return new WaitForSeconds(waitDuration);
        }
    }

    private System.Collections.IEnumerator MoveTo(Vector3 targetPosition)
    {
        isMoving = true;

        Vector3 startPos = useLocalPosition ? transform.localPosition : transform.position;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            Vector3 newPos = Vector3.Lerp(startPos, targetPosition, t);

            if (useLocalPosition)
                transform.localPosition = newPos;
            else
                transform.position = newPos;

            yield return null;
        }

        // Snap to final pos
        if (useLocalPosition)
            transform.localPosition = targetPosition;
        else
            transform.position = targetPosition;

        isMoving = false;
    }
}
