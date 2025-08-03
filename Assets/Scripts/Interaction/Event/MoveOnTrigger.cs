using UnityEngine;

public class MoveOnTrigger : MonoBehaviour
{
    public Transform target;         // Object to move
    public Vector3 destination;      // Local or world destination
    public float moveDuration = 1f;  // How long it takes
    public bool useLocalPosition = false;
    
    [Header("Audio")]
    public AudioSource audioSource;  // assign in Inspector
    public AudioClip moveClip;       // the sound to play

    public void Move()
    {
        PlayMoveSound();
        if (target != null)
            StartCoroutine(MoveToPosition());
    }
    
    public void PlayMoveSound()
    {
        if (audioSource != null && moveClip != null)
        {
            audioSource.PlayOneShot(moveClip);
        }
    }

    private System.Collections.IEnumerator MoveToPosition()
    {
        Vector3 startPos = useLocalPosition ? target.localPosition : target.position;
        Vector3 endPos = destination;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            Vector3 newPos = Vector3.Lerp(startPos, endPos, t);

            if (useLocalPosition)
                target.localPosition = newPos;
            else
                target.position = newPos;

            yield return null;
        }

        // Snap to final pos
        if (useLocalPosition)
            target.localPosition = endPos;
        else
            target.position = endPos;
    }
}
