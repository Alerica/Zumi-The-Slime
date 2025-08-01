using UnityEngine;

public class TotemProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public float projectileSpeed = 15f;
    public float projectileLifetime = 4f;

    public void Shoot()
    {
        if (projectilePrefab == null || shootPoint == null)
        {
            Debug.LogWarning("TotemProjectile: Missing projectilePrefab or shootPoint.");
            return;
        }

        // Instantiate projectile facing forward
        GameObject projectile = Instantiate(projectilePrefab, shootPoint.position, shootPoint.rotation);

        // Apply forward velocity
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(shootPoint.forward * projectileSpeed, ForceMode.Impulse);
    
        }

        // Destroy after time
        Destroy(projectile, projectileLifetime);
    }
}
