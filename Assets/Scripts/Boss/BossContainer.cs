using UnityEngine;

public class BossContainer : MonoBehaviour
{
    public Transform bossTarget;
    public Vector3 localMinBounds = new Vector3(-5f, 0f, -5f);
    public Vector3 localMaxBounds = new Vector3(5f, 5f, 5f);

    private Vector3 bossOrigin;

    void Start()
    {
        if (bossTarget != null)
            bossOrigin = bossTarget.position;
    }

    void LateUpdate()
    {
        Vector3 min = bossOrigin + localMinBounds;
        Vector3 max = bossOrigin + localMaxBounds;

        Vector3 pos = bossTarget.position;
        pos.x = Mathf.Clamp(pos.x, min.x, max.x);
        pos.y = Mathf.Clamp(pos.y, min.y, max.y);
        pos.z = Mathf.Clamp(pos.z, min.z, max.z);
        bossTarget.position = pos;
    }

    void OnDrawGizmosSelected()
    {
        if (bossTarget == null) return;

        // Use current position in editor, or stored origin at runtime
        Vector3 center = Application.isPlaying ? bossOrigin : bossTarget.position;
        Vector3 min = center + localMinBounds;
        Vector3 max = center + localMaxBounds;
        Vector3 size = max - min;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(min + size * 0.5f, size);
    }
}
