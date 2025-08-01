// RaycastFrogShooter.cs
using UnityEngine;
using System.Collections;

public class RaycastFrogShooter : MonoBehaviour
{
    [Header("Ball Settings")]
    public GameObject ballPrefab;
    public float ballScale = 0.5f;
    public Material[] ballMaterials;

    [Header("Shooting Settings")]
    public float shootForce = 30f;
    public float shootCooldown = 0.3f;
    public LayerMask aimLayerMask = -1;
    public float maxAimDistance = 50f;

    [Header("References")]
    public Camera playerCamera;
    public Transform shootPoint;
    public ParticleSystem shootEffect;
    public AudioSource shootSound;
    public ImprovedSplineBallSpawner spawner;

    private bool canShoot = true;
    private GameObject currentBall;
    private int currentColorIndex;

    void Start()
    {
        SpawnNextBall();
    }

    void Update()
    {
        if (canShoot && Input.GetButtonDown("Fire1"))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        canShoot = false;
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimLayerMask))
        {
            Vector3 hitPoint = hit.point;
            if (shootEffect != null)
            {
                shootEffect.transform.position = hitPoint;
                shootEffect.Play();
            }
            spawner.OnRaycastHit(hitPoint, currentColorIndex);
        }
        else
        {
            LaunchProjectile(ray.direction);
        }

        if (shootSound != null)
            shootSound.Play();

        if (currentBall != null)
        {
            Destroy(currentBall);
            currentBall = null;
        }

        StartCoroutine(Reload());
    }

    void LaunchProjectile(Vector3 direction)
    {
        var ball = currentBall;
        ball.transform.SetParent(null);
        ball.transform.position = shootPoint.position;

        var rb = ball.GetComponent<Rigidbody>() ?? ball.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.linearVelocity = direction * shootForce;

        var col = ball.GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }

    IEnumerator Reload()
    {
        yield return new WaitForSeconds(shootCooldown);
        SpawnNextBall();
        canShoot = true;
    }

    void SpawnNextBall()
    {
        currentColorIndex = Random.Range(0, ballMaterials.Length);
        currentBall = Instantiate(ballPrefab, shootPoint.position, Quaternion.identity, transform);
        currentBall.transform.localScale = Vector3.one * ballScale;

        var rend = currentBall.GetComponent<Renderer>();
        if (rend != null)
            rend.material = ballMaterials[currentColorIndex];
    }
}
