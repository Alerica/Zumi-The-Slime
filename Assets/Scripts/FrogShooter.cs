using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FrogShooter : MonoBehaviour
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
    [Range(0f, 1f)]
    public float gravityMultiplier = 0.3f; // Reduce bullet drop (0 = no drop, 1 = full gravity)
    
    [Header("Ball Positions")]
    public Transform mouthPosition; 
    public Transform shoulderPosition; 
    public Transform shootPosition; 
    public float ballFloatAmount = 0.1f;
    public float ballFloatSpeed = 2f;
    
    [Header("Aiming")]
    public GameObject aimReticle; 
    private Camera playerCamera;
    private Vector3 aimDirection;
    private Vector3 aimPoint;
    private bool isAiming = false;

    [Header("Effects")]
    public ParticleSystem shootEffect;
    public AudioSource audioSource;
    public AudioClip[] shootSounds;
    public AudioClip[] reloadSounds;
    public BallUI ballUI;
    
    [Header("Camera")]
    public NewCamera newCameraController;
    public bool useScreenCenterAiming = true; 
    
    private GameObject currentBall;
    private GameObject nextBall;
    private int currentBallColor;
    private int nextBallColor;
    private bool canShoot = true;
    
    void Start()
    {
        playerCamera = Camera.main ?? GetComponentInChildren<Camera>();
        newCameraController = newCameraController ?? GetComponent<NewCamera>();
        EnsurePositions();
        if (ballPrefab == null)
            Debug.LogError("Ball Prefab is not assigned! Please assign a ball prefab in the inspector.");

        SpawnInitialBalls();
    }

    void EnsurePositions()
    {
        if (mouthPosition == null)
        {
            var m = new GameObject("MouthPosition");
            m.transform.SetParent(transform);
            m.transform.localPosition = new Vector3(0, 0.5f, 0.5f);
            mouthPosition = m.transform;
        }
        if (shoulderPosition == null)
        {
            var s = new GameObject("ShoulderPosition");
            s.transform.SetParent(transform);
            s.transform.localPosition = new Vector3(0.3f, 0.3f, -0.2f);
            shoulderPosition = s.transform;
        }
        if (shootPosition == null)
        {
            var sp = new GameObject("ShootPosition");
            sp.transform.SetParent(transform);
            sp.transform.localPosition = new Vector3(0, 0.5f, 0.5f);
            shootPosition = sp.transform;
        }
    }

    void Update()
    {
        HandleAiming();
        HandleShooting();
        AnimateBalls();
    }

    void SpawnInitialBalls()
    {
        currentBallColor = Random.Range(0, ballMaterials.Length);
        currentBall = CreateBall(currentBallColor, mouthPosition.position);
        currentBall.transform.SetParent(mouthPosition);
        currentBall.transform.localPosition = Vector3.zero;

        nextBallColor = Random.Range(0, ballMaterials.Length);
        nextBall = CreateBall(nextBallColor, shoulderPosition.position);
        nextBall.transform.SetParent(shoulderPosition);
        nextBall.transform.localPosition = Vector3.zero; 
        
        ballUI.UpdateBallUI(currentBallColor, nextBallColor);
    }
    
    GameObject CreateBall(int colorIndex, Vector3 position)
    {
        GameObject ball;
        if (ballPrefab != null)
            ball = Instantiate(ballPrefab, position, Quaternion.identity);
        else
        {
            ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.transform.position = position;
            Debug.LogWarning("No ball prefab assigned! Creating default sphere.");
        }
        
        ball.transform.localScale = Vector3.one * ballScale;
        var rend = ball.GetComponent<Renderer>();
        if (rend != null && ballMaterials.Length > 0 && colorIndex < ballMaterials.Length)
            rend.material = ballMaterials[colorIndex];

        var rb = ball.GetComponent<Rigidbody>() ?? ball.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = true;

        var col = ball.GetComponent<Collider>() ?? ball.AddComponent<SphereCollider>();
        col.enabled = false;
        col.isTrigger = false;

        return ball;
    }
    
    void HandleAiming()
    {
        Ray ray;
        if (newCameraController != null)
        {
            ray = new Ray(newCameraController.GetCameraTransform().position,
                          newCameraController.GetAimDirection());
        }
        else
        {
            ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        }

        if (Physics.Raycast(ray, out var hit, maxAimDistance, aimLayerMask))
        {
            aimPoint = hit.point;
            isAiming = true;
            if (aimReticle != null)
            {
                aimReticle.SetActive(true);
                aimReticle.transform.position = hit.point;
                aimReticle.transform.rotation = Quaternion.LookRotation(hit.normal);
            }
        }
        else
        {
            aimPoint = ray.GetPoint(maxAimDistance);
            isAiming = true;
            if (aimReticle != null) aimReticle.SetActive(false);
        }

        aimDirection = (aimPoint - shootPosition.position).normalized;
        Debug.DrawLine(ray.origin, aimPoint, Color.green);
        Debug.DrawLine(shootPosition.position, aimPoint, Color.yellow);
    }
    
    void HandleShooting()
    {
        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Mouse0))
            && canShoot && currentBall != null)
        {
            Shoot();
        }
    }
    
    void Shoot()
    {
        canShoot = false;
        var ballToShoot = currentBall;
        ballToShoot.transform.SetParent(null);
        ballToShoot.transform.position = shootPosition.position;

        var behavior = ballToShoot.AddComponent<BallBehavior>();
        behavior.colorIndex = currentBallColor;

        var rb = ballToShoot.GetComponent<Rigidbody>();
        rb.isKinematic = false;

        rb.linearVelocity = Vector3.zero;
        rb.linearVelocity = aimDirection * shootForce;

        if (gravityMultiplier < 1f)
        {
            var customGrav = ballToShoot.AddComponent<CustomGravity>();
            customGrav.gravityMultiplier = gravityMultiplier;
            rb.useGravity = false;
        }
        else
        {
            rb.useGravity = true;
        }

        var col = ballToShoot.GetComponent<Collider>();
        col.enabled   = true;
        col.isTrigger = false;

        ballToShoot.name = "Ball_Color" + currentBallColor;
        Destroy(ballToShoot, 10f);

        if (shootEffect != null)
        {
            shootEffect.transform.position = shootPosition.position;
            shootEffect.Play();
        }
        PlayShootSound();

        currentBall = null;
        StartCoroutine(ReloadBall());
    }

    IEnumerator ReloadBall()
    {
        yield return new WaitForSeconds(0.1f);

        currentBall = nextBall;
        currentBallColor = nextBallColor;

        float moveTime = 0.3f;
        float elapsed = 0f;
        Vector3 startPos = currentBall.transform.position;
        currentBall.transform.SetParent(mouthPosition);

        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            currentBall.transform.position =
                Vector3.Lerp(startPos, mouthPosition.position, elapsed / moveTime);
            yield return null;
        }

        currentBall.transform.localPosition = Vector3.zero;
        nextBallColor = Random.Range(0, ballMaterials.Length);
        nextBall = CreateBall(nextBallColor, shoulderPosition.position);
        nextBall.transform.SetParent(shoulderPosition);

        PlayReloadSound();
        yield return new WaitForSeconds(shootCooldown);
        canShoot = true;
        ballUI.UpdateBallUI(currentBallColor, nextBallColor);
    }
    
    void AnimateBalls()
    {
        if (currentBall != null)
        {
            float floatY = Mathf.Sin(Time.time * ballFloatSpeed) * ballFloatAmount;
            currentBall.transform.localPosition = new Vector3(0, floatY, 0);
        }
        if (nextBall != null)
            nextBall.transform.Rotate(Vector3.up, 30f * Time.deltaTime);
    }
    
    void PlayShootSound()
    {
        if (audioSource != null && shootSounds.Length > 0)
        {
            var clip = shootSounds[Random.Range(0, shootSounds.Length)];
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clip);
        }
    }

    void PlayReloadSound()
    {
        if (audioSource != null && reloadSounds.Length > 0)
        {
            var clip = reloadSounds[Random.Range(0, reloadSounds.Length)];
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.volume = 0.5f;
            audioSource.PlayOneShot(clip);
        }
    }

    void OnDrawGizmos()
    {
        if (shootPosition != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(shootPosition.position, 0.1f);
            if (isAiming)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(shootPosition.position, aimDirection * 5f);
            }
        }
        if (mouthPosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(mouthPosition.position, 0.08f);
        }
        if (shoulderPosition != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(shoulderPosition.position, 0.08f);
        }
    }
}

// Handle custom gravity multiplier
public class CustomGravity : MonoBehaviour
{
    public float gravityMultiplier = 1f;
    private Rigidbody rb;

    void Start() => rb = GetComponent<Rigidbody>();

    void FixedUpdate()
    {
        if (rb != null)
            rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
    }
}
