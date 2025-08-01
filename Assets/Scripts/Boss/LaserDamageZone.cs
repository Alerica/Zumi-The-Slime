using UnityEngine;
using System.Collections.Generic;

public class LaserDamageZone : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damagePerSecond = 10f;
    [SerializeField] private float damageTickRate = 0.1f; 
    [SerializeField] private string playerTag = "Player";
    
    [Header("Detection Settings")]
    [SerializeField] private float beamRadius = 0.5f;
    [SerializeField] private LayerMask hitLayers = -1;
    [SerializeField] private bool useSphereCast = true;
    
    private float lastDamageTime;
    private HashSet<GameObject> targetsInBeam = new HashSet<GameObject>();
    private bool isActive = false;
    
    public void SetDamagePerSecond(float dps)
    {
        damagePerSecond = dps;
    }
    
    public void SetBeamRadius(float radius)
    {
        beamRadius = radius;
    }
    
    public void ActivateLaser(bool active)
    {
        isActive = active;
        if (!active)
        {
            targetsInBeam.Clear();
        }
    }
    
    public void UpdateLaserDamage(Vector3 origin, Vector3 direction, float maxDistance)
    {
        if (!isActive) return;
        
        targetsInBeam.Clear();
        
        if (useSphereCast)
        {
            RaycastHit[] hits = Physics.SphereCastAll(origin, beamRadius, direction, maxDistance, hitLayers);
            
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.CompareTag(playerTag))
                {
                    targetsInBeam.Add(hit.collider.gameObject);
                }
            }
        }
        else
        {
            int rayCount = 5;
            for (int i = 0; i < rayCount; i++)
            {
                float angle = (i / (float)(rayCount - 1) - 0.5f) * 360f / rayCount;
                Vector3 offset = Quaternion.AngleAxis(angle, direction) * Vector3.up * beamRadius;
                
                RaycastHit hit;
                if (Physics.Raycast(origin + offset, direction, out hit, maxDistance, hitLayers))
                {
                    if (hit.collider.CompareTag(playerTag))
                    {
                        targetsInBeam.Add(hit.collider.gameObject);
                    }
                }
            }
        }
        
        if (Time.time - lastDamageTime >= damageTickRate)
        {
            ApplyDamageToTargets();
            lastDamageTime = Time.time;
        }
    }
    
    private void ApplyDamageToTargets()
    {
        float damage = damagePerSecond * damageTickRate;
        
        foreach (GameObject target in targetsInBeam)
        {
            if (target == null) continue;
            
            
            //Send message (easiest least efficient)
            target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            
            //  Interface approach (uncomment if using interfaces)
            // var damageable = target.GetComponent<IDamageable>();
            // if (damageable != null)
            // {
            //     damageable.TakeDamage(damage);
            // }
            
            // Direct component access (uncomment and adjust to your player health script)
            // var playerHealth = target.GetComponent<PlayerHealth>();
            // if (playerHealth != null)
            // {
            //     playerHealth.TakeDamage(damage);
            // }
            
            Debug.Log($"Laser dealing {damage} damage to {target.name}");
        }
    }
    
    public void DrawDebugLaser(Vector3 origin, Vector3 endPoint)
    {
        #if UNITY_EDITOR
        Debug.DrawLine(origin, endPoint, Color.red);
        
        // Draw beam radius
        Vector3 direction = (endPoint - origin).normalized;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        if (perpendicular == Vector3.zero)
            perpendicular = Vector3.Cross(direction, Vector3.right).normalized;
        
        Debug.DrawLine(origin + perpendicular * beamRadius, endPoint + perpendicular * beamRadius, Color.yellow);
        Debug.DrawLine(origin - perpendicular * beamRadius, endPoint - perpendicular * beamRadius, Color.yellow);
        #endif
    }
}