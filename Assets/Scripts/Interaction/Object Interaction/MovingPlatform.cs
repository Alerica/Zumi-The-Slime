using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform[] waypoints;
    public float speed = 2f;
    public float waitTime = 0.5f;

    private int currentIndex = 0;
    private float waitTimer;

    public Vector3 Velocity { get; private set; }
    private Vector3 lastPosition;

    private void FixedUpdate()
    {
        if (waypoints.Length < 2) return;

        // Move toward current waypoint
        Vector3 target = waypoints[currentIndex].position;
        Vector3 direction = (target - transform.position).normalized;
        Vector3 move = direction * speed * Time.fixedDeltaTime;

        // Snap if close enough
        if ((transform.position - target).sqrMagnitude <= move.sqrMagnitude)
        {
            transform.position = target;
            waitTimer += Time.fixedDeltaTime;
            if (waitTimer >= waitTime)
            {
                currentIndex = (currentIndex + 1) % waypoints.Length;
                waitTimer = 0f;
            }
        }
        else
        {
            transform.position += move;
        }

        // Velocity tracking
        Velocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = transform.position;
    }
}
