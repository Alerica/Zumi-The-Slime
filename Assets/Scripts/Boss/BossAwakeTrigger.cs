using UnityEngine;

public class BossAwakeTrigger : MonoBehaviour
{
    [Header("Boss Reference")]
    [SerializeField] private BossController1 bossController;
    
    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private string playerTag = "Player";
    
    [Header("Visual Feedback")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0f, 0f, 0.3f);
    
    private bool hasTriggered = false;
    
    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && hasTriggered) return;
        
        if (other.CompareTag(playerTag))
        {
            if (bossController != null)
            {
                bossController.AwakeBoss();
                hasTriggered = true;
                
                if (triggerOnce)
                {
                    GetComponent<Collider>().enabled = false;
                }
            }
            else
            {
                Debug.LogError("Boss Controller reference is missing!");
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = gizmoColor;
            
            if (col is BoxCollider box)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = oldMatrix;
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
            }
        }
    }
}