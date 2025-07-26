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
    
    [Header("Ball Positions")]
    public Transform mouthPosition; 
    public Transform shoulderPosition; 
    public Transform shootPosition; 
    public float ballFloatAmount = 0.1f;
    public float ballFloatSpeed = 2f;
    
    [Header("Aiming")]
    public GameObject aimReticle; 
    public LineRenderer trajectoryLine; 
    public int trajectoryPoints = 30;
    public float trajectoryTimeStep = 0.1f;
    
    [Header("Effects")]
    public ParticleSystem shootEffect;
    public AudioSource audioSource;
    public AudioClip[] shootSounds;
    public AudioClip[] reloadSounds;
    
    private GameObject currentBall;
    private GameObject nextBall;
    private int currentBallColor;
    private int nextBallColor;
    private bool canShoot = true;
    private Camera playerCamera;
    private Vector3 aimDirection;
    private Vector3 aimPoint;
    private bool isAiming = false;
    
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }
        
        if (mouthPosition == null)
        {
            GameObject mouth = new GameObject("MouthPosition");
            mouth.transform.SetParent(transform);
            mouth.transform.localPosition = new Vector3(0, 0.5f, 0.5f);
            mouthPosition = mouth.transform;
            Debug.Log("Created MouthPosition at: " + mouthPosition.position);
        }
        
        if (shoulderPosition == null)
        {
            GameObject shoulder = new GameObject("ShoulderPosition");
            shoulder.transform.SetParent(transform);
            shoulder.transform.localPosition = new Vector3(0.3f, 0.3f, -0.2f);
            shoulderPosition = shoulder.transform;
            Debug.Log("Created ShoulderPosition at: " + shoulderPosition.position);
        }
        
        if (shootPosition == null)
        {
            GameObject shoot = new GameObject("ShootPosition");
            shoot.transform.SetParent(transform);
            shoot.transform.localPosition = new Vector3(0, 0.5f, 0.5f);
            shootPosition = shoot.transform;
            Debug.Log("Created ShootPosition at: " + shootPosition.position);
        }
        
        if (trajectoryLine == null)
        {
            GameObject lineObj = new GameObject("TrajectoryLine");
            lineObj.transform.SetParent(transform);
            trajectoryLine = lineObj.AddComponent<LineRenderer>();
            trajectoryLine.startWidth = 0.05f;
            trajectoryLine.endWidth = 0.02f;
            trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
            trajectoryLine.startColor = new Color(1, 1, 1, 0.5f);
            trajectoryLine.endColor = new Color(1, 1, 1, 0.1f);
        }
        
        if (ballPrefab == null)
        {
            Debug.LogError("Ball Prefab is not assigned! Please assign a ball prefab in the inspector.");
        }
        
        SpawnInitialBalls();
    }
    
    void Update()
    {
        HandleAiming();
        HandleShooting();
        AnimateBalls();
        UpdateTrajectory();
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
    }
    
    GameObject CreateBall(int colorIndex, Vector3 position)
    {
        GameObject ball = null;
        
        if (ballPrefab != null)
        {
            ball = Instantiate(ballPrefab, position, Quaternion.identity);
        }
        else
        {
            ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.transform.position = position;
            Debug.LogWarning("No ball prefab assigned! Creating default sphere.");
        }
        
        ball.transform.localScale = Vector3.one * ballScale;
        
        if (ballMaterials != null && ballMaterials.Length > 0 && colorIndex < ballMaterials.Length)
        {
            Renderer renderer = ball.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = ballMaterials[colorIndex];
            }
        }
        
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = ball.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = true;
        
        Collider col = ball.GetComponent<Collider>();
        if (col == null)
        {
            col = ball.AddComponent<SphereCollider>();
        }
        col.enabled = false;
        
        return ball;
    }
    
    void HandleAiming()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxAimDistance, aimLayerMask))
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
            
            if (aimReticle != null)
            {
                aimReticle.SetActive(false);
            }
        }
        
        aimDirection = (aimPoint - shootPosition.position).normalized;
    }
    
    void HandleShooting()
    {
        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Mouse0)) && canShoot && currentBall != null)
        {
            Shoot();
        }
    }
    
    void Shoot()
    {
        canShoot = false;
        
        Debug.Log($"Shoot Position Transform: {shootPosition}");
        Debug.Log($"Shoot Position World Pos: {shootPosition.position}");
        Debug.Log($"Current Ball Position Before: {currentBall.transform.position}");
        
        GameObject ballToShoot = currentBall;
        
        ballToShoot.transform.SetParent(null);
        
        ballToShoot.transform.position = shootPosition.position;
        
        Debug.Log($"Ball Position After Setting: {ballToShoot.transform.position}");
        
        Rigidbody rb = ballToShoot.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero; 
            rb.linearVelocity = aimDirection * shootForce;
            
            Debug.Log($"Ball Velocity Set To: {rb.linearVelocity}");
        }
        
        Collider col = ballToShoot.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }
        
        BallBehavior ballBehavior = ballToShoot.GetComponent<BallBehavior>();
        if (ballBehavior == null)
        {
            ballBehavior = ballToShoot.AddComponent<BallBehavior>();
        }
        ballBehavior.colorIndex = currentBallColor;
        
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
        
        // Animate the movement
        float moveTime = 0.3f;
        float elapsed = 0f;
        Vector3 startPos = currentBall.transform.position;
        currentBall.transform.SetParent(mouthPosition);
        
        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveTime;
            currentBall.transform.position = Vector3.Lerp(startPos, mouthPosition.position, t);
            yield return null;
        }
        
        currentBall.transform.localPosition = Vector3.zero;
        
        // Create new next ball
        nextBallColor = Random.Range(0, ballMaterials.Length);
        nextBall = CreateBall(nextBallColor, shoulderPosition.position);
        nextBall.transform.SetParent(shoulderPosition);
        
        PlayReloadSound();
        
        yield return new WaitForSeconds(shootCooldown);
        canShoot = true;
    }
    
    void AnimateBalls()
    {
        if (currentBall != null)
        {
            float floatY = Mathf.Sin(Time.time * ballFloatSpeed) * ballFloatAmount;
            currentBall.transform.localPosition = new Vector3(0, floatY, 0);
        }
        
        if (nextBall != null)
        {
            nextBall.transform.Rotate(Vector3.up, 30f * Time.deltaTime);
        }
    }
    
    void UpdateTrajectory()
    {
        if (trajectoryLine == null || !isAiming || shootPosition == null) return;
        
        Vector3 startPos = shootPosition.position;
        Vector3 velocity = aimDirection * shootForce;
        
        trajectoryLine.positionCount = trajectoryPoints;
        
        for (int i = 0; i < trajectoryPoints; i++)
        {
            float time = i * trajectoryTimeStep;
            Vector3 point = startPos + velocity * time;
            point.y += Physics.gravity.y * 0.5f * time * time; 
            
            trajectoryLine.SetPosition(i, point);
            
            if (i > 0)
            {
                Vector3 lastPoint = trajectoryLine.GetPosition(i - 1);
                if (Physics.Linecast(lastPoint, point, aimLayerMask))
                {
                    trajectoryLine.positionCount = i;
                    break;
                }
            }
        }
    }
    
    void PlayShootSound()
    {
        if (audioSource != null && shootSounds.Length > 0)
        {
            AudioClip clip = shootSounds[Random.Range(0, shootSounds.Length)];
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clip);
        }
    }
    
    void PlayReloadSound()
    {
        if (audioSource != null && reloadSounds.Length > 0)
        {
            AudioClip clip = reloadSounds[Random.Range(0, reloadSounds.Length)];
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.volume = 0.5f;
            audioSource.PlayOneShot(clip);
        }
    }
    
    void OnDrawGizmos()
    {
        // Visualize shoot position in editor
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
        
        // Visualize mouth position
        if (mouthPosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(mouthPosition.position, 0.08f);
        }
        
        // Visualize shoulder position
        if (shoulderPosition != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(shoulderPosition.position, 0.08f);
        }
    }
}

public class BallBehavior : MonoBehaviour
{
    public int colorIndex;
    public float lifetime = 10f; 
    
    void Start()
    {
        Destroy(gameObject, lifetime);
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Here you would implement:
        // - Check if hit another ball of same color
        // - Snap to position in chain
        // - Check for matches
        // - etc.
    }
}