using UnityEngine;

public class TotemProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public float projectileSpeed = 15f;
    public float projectileLifetime = 4f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shootSound;

    void Awake()
    {
        // Get AudioSource if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void Shoot()
    {
        if (projectilePrefab == null || shootPoint == null)
        {
            Debug.LogWarning("TotemProjectile: Missing projectilePrefab or shootPoint.");
            return;
        }

        // Play shooting sound
        PlayShootSound();

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

    private void PlayShootSound()
    {
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
        else
        {
            Debug.LogWarning("AudioSource or Shoot Sound not assigned in TotemProjectile.");
        }
    }
}