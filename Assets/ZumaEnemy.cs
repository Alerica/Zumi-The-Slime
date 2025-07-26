using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class SplinePoint
{
    public Vector3 position;
    public Vector3 tangent;
    
    public SplinePoint(Vector3 pos, Vector3 tan)
    {
        position = pos;
        tangent = tan;
    }
}

public class ZumaEnemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    public float health = 100f;
    public float damagePerBallDestroyed = 10f;
    public GameObject enemyBody; 
    
    [Header("Spline Settings")]
    public bool showSplineGizmos = true;
    public Color splineColor = Color.cyan;
    public float splineRadius = 2f; 
    public float splineHeight = 2f; 
    public int splineResolution = 50; 
    [Range(0.1f, 2f)]
    public float spiralTurns = 1.5f; 
    
    [Header("Ball Chain Settings")]
    public GameObject ballPrefab;
    public float ballSize = 0.4f;
    public float ballSpacing = 0.5f; 
    public float chainSpeed = 0.5f; 
    public int initialBallCount = 20;
    public Material[] ballMaterials; 
    
    [Header("Matching Settings")]
    public int minMatchCount = 3; 
    public float matchCheckDelay = 0.1f;
    public float ballRemoveDelay = 0.2f;
    
    [Header("Effects")]
    public GameObject ballDestroyEffect;
    public AudioSource audioSource;
    public AudioClip matchSound;
    public AudioClip damageSound;
    
    private List<SplinePoint> splinePoints = new List<SplinePoint>();
    private List<BallOnSpline> ballChain = new List<BallOnSpline>();
    private float totalSplineLength = 0f;
    private bool isProcessingMatches = false;
    
    [System.Serializable]
    public class BallOnSpline
    {
        public GameObject gameObject;
        public int colorIndex;
        public float distanceOnSpline; // 0 to 1
        public float targetDistance; 
        public bool isBeingDestroyed = false;
        public EnemyBallBehavior behavior;
        
        public BallOnSpline(GameObject go, int color, float distance)
        {
            gameObject = go;
            colorIndex = color;
            distanceOnSpline = distance;
            targetDistance = distance;
            behavior = go.GetComponent<EnemyBallBehavior>();
        }
    }
    
    void Start()
    {
        GenerateSpline();
        SpawnInitialBalls();
        
        if (enemyBody == null)
        {
            // Create default capsule if not assigned
            enemyBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyBody.transform.SetParent(transform);
            enemyBody.transform.localPosition = Vector3.zero;
            enemyBody.transform.localScale = new Vector3(1.5f, 2f, 1.5f);
        }
    }
    
    void Update()
    {
        // Only update if not dead
        if (health > 0)
        {
            UpdateBallPositions();
            MoveBallsAlongSpline();
        }
    }
    
    void GenerateSpline()
    {
        splinePoints.Clear();
        
        for (int i = 0; i < splineResolution; i++)
        {
            float t = (float)i / (splineResolution - 1);
            float angle = t * Mathf.PI * 2 * spiralTurns;
            
            // Create spiral path
            float x = Mathf.Cos(angle) * splineRadius;
            float z = Mathf.Sin(angle) * splineRadius;
            float y = Mathf.Lerp(-splineHeight/2, splineHeight/2, t);
            
            Vector3 position = transform.position + new Vector3(x, y, z);
            
            // Calculate tangent
            float nextAngle = ((float)(i + 1) / (splineResolution - 1)) * Mathf.PI * 2 * spiralTurns;
            float nextX = Mathf.Cos(nextAngle) * splineRadius;
            float nextZ = Mathf.Sin(nextAngle) * splineRadius;
            float nextY = Mathf.Lerp(-splineHeight/2, splineHeight/2, (float)(i + 1) / (splineResolution - 1));
            
            Vector3 nextPos = transform.position + new Vector3(nextX, nextY, nextZ);
            Vector3 tangent = (nextPos - position).normalized;
            
            splinePoints.Add(new SplinePoint(position, tangent));
        }
        
        CalculateSplineLength();
    }
    
    void CalculateSplineLength()
    {
        totalSplineLength = 0f;
        for (int i = 0; i < splinePoints.Count - 1; i++)
        {
            totalSplineLength += Vector3.Distance(splinePoints[i].position, splinePoints[i + 1].position);
        }
    }
    
    Vector3 GetPositionOnSpline(float t)
    {
        t = Mathf.Clamp01(t);
        int index = Mathf.FloorToInt(t * (splinePoints.Count - 1));
        
        if (index >= splinePoints.Count - 1)
            return splinePoints[splinePoints.Count - 1].position;
            
        float localT = (t * (splinePoints.Count - 1)) - index;
        return Vector3.Lerp(splinePoints[index].position, splinePoints[index + 1].position, localT);
    }
    
    Vector3 GetTangentOnSpline(float t)
    {
        t = Mathf.Clamp01(t);
        int index = Mathf.FloorToInt(t * (splinePoints.Count - 1));
        
        if (index >= splinePoints.Count - 1)
            return splinePoints[splinePoints.Count - 1].tangent;
            
        return splinePoints[index].tangent;
    }
    
    void SpawnInitialBalls()
    {
        float currentDistance = 0f;
        float distanceIncrement = ballSpacing / totalSplineLength;
        
        for (int i = 0; i < initialBallCount; i++)
        {
            if (currentDistance > 1f) break;
            
            int colorIndex = Random.Range(0, ballMaterials.Length);
            GameObject ball = CreateBall(colorIndex, currentDistance);
            
            BallOnSpline ballData = new BallOnSpline(ball, colorIndex, currentDistance);
            ballChain.Add(ballData);
            
            currentDistance += distanceIncrement;
        }
    }
    
    GameObject CreateBall(int colorIndex, float splinePosition)
    {
        Vector3 position = GetPositionOnSpline(splinePosition);
        GameObject ball = Instantiate(ballPrefab, position, Quaternion.identity);
        ball.transform.SetParent(transform);
        ball.transform.localScale = Vector3.one * ballSize;
        
        // Set material
        if (ballMaterials != null && ballMaterials.Length > 0 && colorIndex < ballMaterials.Length)
        {
            Renderer renderer = ball.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = ballMaterials[colorIndex];
            }
        }
        
        EnemyBallBehavior behavior = ball.AddComponent<EnemyBallBehavior>();
        behavior.colorIndex = colorIndex;
        behavior.parentEnemy = this;
        
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb == null) rb = ball.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        
        Collider col = ball.GetComponent<Collider>();
        if (col == null) col = ball.AddComponent<SphereCollider>();
        col.isTrigger = true;
        
        return ball;
    }
    
    void UpdateBallPositions()
    {
        for (int i = 0; i < ballChain.Count; i++)
        {
            if (ballChain[i].isBeingDestroyed) continue;
            
            Vector3 position = GetPositionOnSpline(ballChain[i].distanceOnSpline);
            ballChain[i].gameObject.transform.position = position;
            
            // Optional: make balls face along spline
            Vector3 tangent = GetTangentOnSpline(ballChain[i].distanceOnSpline);
            if (tangent != Vector3.zero)
            {
                ballChain[i].gameObject.transform.rotation = Quaternion.LookRotation(tangent);
            }
        }
    }
    
    void MoveBallsAlongSpline()
    {
        foreach (var ball in ballChain)
        {
            if (!ball.isBeingDestroyed)
            {
                ball.distanceOnSpline += chainSpeed * Time.deltaTime / totalSplineLength;
                
                if (ball.distanceOnSpline > 1f)
                {
                    ball.distanceOnSpline -= 1f;
                }
            }
        }
    }
    
    public void OnBallHit(GameObject projectile, int projectileColorIndex)
    {
        float hitDistance = FindClosestPointOnSpline(projectile.transform.position);
        
        InsertBall(projectileColorIndex, hitDistance);
        
        Destroy(projectile);
        
        if (!isProcessingMatches)
        {
            StartCoroutine(CheckForMatches());
        }
    }
    
    float FindClosestPointOnSpline(Vector3 worldPos)
    {
        float closestDistance = float.MaxValue;
        float closestT = 0f;
        
        for (int i = 0; i < 100; i++)
        {
            float t = (float)i / 99f;
            Vector3 splinePos = GetPositionOnSpline(t);
            float distance = Vector3.Distance(worldPos, splinePos);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestT = t;
            }
        }
        
        return closestT;
    }
    
    void InsertBall(int colorIndex, float splinePosition)
    {
        int insertIndex = 0;
        for (int i = 0; i < ballChain.Count; i++)
        {
            if (ballChain[i].distanceOnSpline > splinePosition)
            {
                insertIndex = i;
                break;
            }
        }
        
        // Create new ball
        GameObject newBall = CreateBall(colorIndex, splinePosition);
        BallOnSpline newBallData = new BallOnSpline(newBall, colorIndex, splinePosition);
        
        if (insertIndex >= ballChain.Count)
        {
            ballChain.Add(newBallData);
        }
        else
        {
            ballChain.Insert(insertIndex, newBallData);
        }
        
        RearrangeBalls(insertIndex);
    }
    
    void RearrangeBalls(int fromIndex)
    {
        float spacing = ballSpacing / totalSplineLength;
        
        for (int i = fromIndex + 1; i < ballChain.Count; i++)
        {
            float desiredDistance = ballChain[i - 1].distanceOnSpline + spacing;
            ballChain[i].targetDistance = desiredDistance;
            ballChain[i].distanceOnSpline = Mathf.Lerp(ballChain[i].distanceOnSpline, desiredDistance, Time.deltaTime * 5f);
        }
    }
    
    System.Collections.IEnumerator CheckForMatches()
    {
        isProcessingMatches = true;
        yield return new WaitForSeconds(matchCheckDelay);
        
        List<List<int>> matches = FindMatches();
        
        foreach (var match in matches)
        {
            if (match.Count >= minMatchCount)
            {
                foreach (int index in match.OrderByDescending(x => x))
                {
                    if (index < ballChain.Count)
                    {
                        DestroyBall(index);
                    }
                }
                
                if (audioSource && matchSound)
                {
                    audioSource.PlayOneShot(matchSound);
                }
                
                TakeDamage(match.Count * damagePerBallDestroyed);
                
                yield return new WaitForSeconds(ballRemoveDelay);
            }
        }
        
        CloseGaps();
        
        isProcessingMatches = false;
    }
    
    List<List<int>> FindMatches()
    {
        List<List<int>> allMatches = new List<List<int>>();
        bool[] processed = new bool[ballChain.Count];
        
        for (int i = 0; i < ballChain.Count; i++)
        {
            if (processed[i] || ballChain[i].isBeingDestroyed) continue;
            
            List<int> currentMatch = new List<int>();
            int currentColor = ballChain[i].colorIndex;
            
            for (int j = i; j < ballChain.Count; j++)
            {
                if (!ballChain[j].isBeingDestroyed && ballChain[j].colorIndex == currentColor)
                {
                    currentMatch.Add(j);
                    processed[j] = true;
                }
                else
                {
                    break;
                }
            }
            
            if (currentMatch.Count >= minMatchCount)
            {
                allMatches.Add(currentMatch);
            }
        }
        
        return allMatches;
    }
    
    void DestroyBall(int index)
    {
        if (index >= ballChain.Count) return;
        
        BallOnSpline ball = ballChain[index];
        ball.isBeingDestroyed = true;
        
        if (ballDestroyEffect != null)
        {
            Instantiate(ballDestroyEffect, ball.gameObject.transform.position, Quaternion.identity);
        }
        
        Destroy(ball.gameObject);
        ballChain.RemoveAt(index);
    }
    
    void CloseGaps()
    {
        float spacing = ballSpacing / totalSplineLength;
        
        for (int i = 1; i < ballChain.Count; i++)
        {
            float desiredDistance = ballChain[i - 1].distanceOnSpline + spacing;
            ballChain[i].distanceOnSpline = desiredDistance;
            ballChain[i].targetDistance = desiredDistance;
        }
    }
    
    void TakeDamage(float damage)
    {
        health -= damage;
        
        if (audioSource && damageSound)
        {
            audioSource.PlayOneShot(damageSound);
        }
        
        if (enemyBody != null)
        {
            StartCoroutine(DamageFlash());
        }
        
        if (health <= 0)
        {
            Die();
        }
    }
    
    System.Collections.IEnumerator DamageFlash()
    {
        Renderer renderer = enemyBody.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;
            renderer.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            renderer.material.color = originalColor;
        }
    }
    
    void Die()
    {
        // Destroy all balls
        foreach (var ball in ballChain)
        {
            if (ball.gameObject != null)
            {
                Destroy(ball.gameObject);
            }
        }
        
        // Clear the ball chain list
        ballChain.Clear();
        
        // Destroy enemy after a short delay
        Destroy(gameObject, 0.5f);
    }
    
    void OnDrawGizmos()
    {
        if (!showSplineGizmos) return;
        
        if (Application.isPlaying && splinePoints.Count > 0)
        {
            Gizmos.color = splineColor;
            
            for (int i = 0; i < splinePoints.Count - 1; i++)
            {
                Gizmos.DrawLine(splinePoints[i].position, splinePoints[i + 1].position);
            }
            
            // Draw balls
            Gizmos.color = Color.yellow;
            foreach (var ball in ballChain)
            {
                if (ball.gameObject != null)
                {
                    Gizmos.DrawWireSphere(ball.gameObject.transform.position, ballSize * 0.5f);
                }
            }
        }
    }
}

// Component for balls on the enemy
public class EnemyBallBehavior : MonoBehaviour
{
    public int colorIndex;
    public ZumaEnemy parentEnemy;
    
    void OnTriggerEnter(Collider other)
    {
        // Check if hit by player's ball
        BallBehavior playerBall = other.GetComponent<BallBehavior>();
        if (playerBall != null)
        {
            // Notify parent enemy
            if (parentEnemy != null)
            {
                parentEnemy.OnBallHit(other.gameObject, playerBall.colorIndex);
            }
        }
    }
}