using UnityEngine;
using UnityEngine.Splines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class SplineBallSpawner : MonoBehaviour
{
    public event Action<int, int, List<int>> OnShotReport;

    private List<int> enteredTypes = new List<int>();

    [Header("Ball Settings")]
    public GameObject ballPrefab;
    public Material[] ballMaterials;
    public float ballSize = 0.4f;
    public float ballSpacing = 0.5f;
    public float chainSpeed = 2f;
    public int totalBallCount = 30;
    public int initialVisibleCount = 10;
    [Tooltip("Fraction of spline length behind t=0 used as buffer.")]
    public float bufferZoneSize = 0.2f;

    [Header("Matching Settings")]
    public int minMatchCount = 3;
    public float matchCheckDelay = 0.1f;
    public float destroyAnimationTime = 0.3f;
    public float knockbackForce = 0.1f;
    public float knockbackDuration = 0.3f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip matchSound;

    [Header("Animation Speeds")]
    [Tooltip("Time for snap-back animation after each match.")]
    public float snapBackDuration = 0.2f;
    [Tooltip("Time between sequential matches in a chain reaction.")]
    public float interMatchDelay = 0.05f;

    [Header("Debug")]
    public bool showDebugGizmos = false;
    public float debugSphereSize = 0.1f;
    public bool verboseColorLogging = false;

    private SplineContainer splineContainer;
    private float splineLength;
    private readonly List<SplineBall> ballChain = new List<SplineBall>();
    private Queue<int> colorQueue;
    private bool isProcessing = false;
    private bool isPaused = false;

    // Cache for spline sampling - improves accuracy for tight curves
    private Vector3[] splineSamplePoints;
    private float[] splineSampleDistances;
    private const int SPLINE_SAMPLE_COUNT = 1000; // Much higher resolution

    [Header("Enemy Settings")] // Only required if set on enemy
    public EnemyHealth enemyHealth; 

    [Serializable]
    public class SplineBall
    {
        public GameObject gameObject;
        public int colorIndex;
        public float distanceOnSpline;
        public bool isBeingDestroyed;
        public Vector3 worldPosition; // Cache world position for accuracy
        
        public SplineBall(GameObject go, int color, float dist)
        {
            gameObject = go;
            colorIndex = color;
            distanceOnSpline = dist;
        }
    }

    void Start()
    {
        splineContainer = GetComponent<SplineContainer>();
        if (splineContainer == null || splineContainer.Splines.Count == 0)
        {
            Debug.LogError("No spline found! Attach a SplineContainer.");
            enabled = false;
            return;
        }
        
        splineLength = splineContainer.CalculateLength(0);
        PrecomputeSplineSamples();
        
        var initialColors = GenerateRealZumaSequence(totalBallCount);
        colorQueue = new Queue<int>(initialColors);
        SpawnInitialBalls();
    }

    void PrecomputeSplineSamples()
    {
        splineSamplePoints = new Vector3[SPLINE_SAMPLE_COUNT];
        splineSampleDistances = new float[SPLINE_SAMPLE_COUNT];
        
        for (int i = 0; i < SPLINE_SAMPLE_COUNT; i++)
        {
            float t = (float)i / (SPLINE_SAMPLE_COUNT - 1);
            splineSamplePoints[i] = EvaluatePosition(t);
            splineSampleDistances[i] = t;
        }
    }

    void Update()
    {
        if (isPaused) return;
        MoveBallsAlongSpline();
        UpdateBallPositions();
        TrySpawnNext();
    }

    void SpawnInitialBalls()
    {
        float dt = ballSpacing / splineLength;
        ballChain.Clear();
        for (int i = 0; i < initialVisibleCount && colorQueue.Count > 0; i++)
        {
            int color = colorQueue.Dequeue();
            float t = -bufferZoneSize + i * dt;
            var go = CreateBall(color, t);
            var ball = new SplineBall(go, color, t);
            ball.worldPosition = go.transform.position;
            ballChain.Add(ball);
        }
    }

    void MoveBallsAlongSpline()
    {
        float dt = (chainSpeed * Time.deltaTime) / splineLength;
        var toRemove = new List<SplineBall>();
        
        foreach (var ball in ballChain)
        {
            if (ball.isBeingDestroyed) continue;
            ball.distanceOnSpline += dt;
            if (ball.distanceOnSpline >= 1f) toRemove.Add(ball);
        }
        
        foreach (var ball in toRemove)
        {
            ballChain.Remove(ball);
            Destroy(ball.gameObject);
        }
    }

    void UpdateBallPositions()
    {
        foreach (var ball in ballChain)
        {
            if (ball.isBeingDestroyed) continue;
            
            Vector3 newPos = EvaluatePosition(ball.distanceOnSpline);
            ball.gameObject.transform.position = newPos;
            ball.worldPosition = newPos;
            
            var rend = ball.gameObject.GetComponent<Renderer>();
            if (rend) rend.enabled = ball.distanceOnSpline >= 0f;
        }
    }

    void TrySpawnNext()
    {
        if (colorQueue == null || colorQueue.Count == 0) return;
        
        float spawnPos = -bufferZoneSize;
        float minDist = ballSpacing / splineLength;
        
        if (ballChain.All(b => b.distanceOnSpline >= spawnPos + minDist))
        {
            int color = colorQueue.Dequeue();
            enteredTypes.Add(color);
            var go = CreateBall(color, -bufferZoneSize);
            var ball = new SplineBall(go, color, -bufferZoneSize);
            ball.worldPosition = go.transform.position;
            ballChain.Insert(0, ball);
            RearrangeFrom(0);
        }
    }

    private bool hasProcessedShot = false; // Prevent multiple hits from same projectile

    public void OnBallHit(GameObject projectile, int colorIndex)
    {
        if (isProcessing || isPaused || hasProcessedShot) return;
        
        if (verboseColorLogging)
        {
            Debug.Log($"Ball hit detected! Projectile color: {colorIndex}, Material count: {ballMaterials.Length}");
        }
        
        // Immediately set processing flags to prevent multiple hits
        hasProcessedShot = true;
        isProcessing = true;
        
        Vector3 hitPos = projectile.transform.position;
        
        // Disable all ball colliders temporarily to prevent multiple triggers
        foreach (var ball in ballChain)
        {
            var collider = ball.gameObject.GetComponent<Collider>();
            if (collider) collider.enabled = false;
        }
        
        // Find the most accurate insertion point using world positions
        int insertIndex = FindBestInsertionIndex(hitPos);
        float insertT = CalculateInsertionT(insertIndex, hitPos);
        
        // Create ball with EXACT color from projectile - no references to chain balls
        var go = CreateBall(colorIndex, insertT);
        var newBall = new SplineBall(go, colorIndex, insertT);
        newBall.worldPosition = go.transform.position;
        
        if (verboseColorLogging)
        {
            var rend = go.GetComponent<Renderer>();
            Debug.Log($"Created new ball - Stored color: {colorIndex}, Actual material: {(rend ? rend.material.name : "NULL")}");
        }
        
        if (insertIndex >= ballChain.Count) 
            ballChain.Add(newBall);
        else 
            ballChain.Insert(insertIndex, newBall);

        Destroy(projectile);
        RearrangeFrom(Mathf.Max(0, insertIndex - 1));
        
        // Re-enable colliders before processing
        foreach (var ball in ballChain)
        {
            var collider = ball.gameObject.GetComponent<Collider>();
            if (collider) collider.enabled = true;
        }
        
        StartCoroutine(ProcessChainReaction(insertIndex, colorIndex));
    }

    // Much more accurate insertion point finding
    int FindBestInsertionIndex(Vector3 hitPos)
    {
        if (ballChain.Count == 0) return 0;
        
        float minDist = float.MaxValue;
        int bestIndex = 0;
        
        // Check insertion before first ball
        Vector3 firstPos = ballChain[0].worldPosition;
        float dist = Vector3.Distance(hitPos, firstPos);
        if (dist < minDist)
        {
            minDist = dist;
            bestIndex = 0;
        }
        
        // Check insertion between balls
        for (int i = 0; i < ballChain.Count - 1; i++)
        {
            Vector3 pos1 = ballChain[i].worldPosition;
            Vector3 pos2 = ballChain[i + 1].worldPosition;
            Vector3 midPoint = (pos1 + pos2) * 0.5f;
            
            dist = Vector3.Distance(hitPos, midPoint);
            if (dist < minDist)
            {
                minDist = dist;
                bestIndex = i + 1;
            }
        }
        
        // Check insertion after last ball
        if (ballChain.Count > 0)
        {
            Vector3 lastPos = ballChain[ballChain.Count - 1].worldPosition;
            dist = Vector3.Distance(hitPos, lastPos);
            if (dist < minDist)
            {
                bestIndex = ballChain.Count;
            }
        }
        
        return bestIndex;
    }

    // Calculate precise T value for insertion
    float CalculateInsertionT(int insertIndex, Vector3 hitPos)
    {
        if (ballChain.Count == 0) return -bufferZoneSize;
        
        float dt = ballSpacing / splineLength;
        
        if (insertIndex == 0)
        {
            // Insert before first ball
            return Mathf.Max(-bufferZoneSize, ballChain[0].distanceOnSpline - dt);
        }
        else if (insertIndex >= ballChain.Count)
        {
            // Insert after last ball
            return ballChain[ballChain.Count - 1].distanceOnSpline + dt;
        }
        else
        {
            // Insert between balls - find the closest point on spline to hit position
            float startT = ballChain[insertIndex - 1].distanceOnSpline;
            float endT = ballChain[insertIndex].distanceOnSpline;
            
            return FindClosestTInRange(hitPos, startT, endT);
        }
    }

    // Precise closest point finding in a T range
    float FindClosestTInRange(Vector3 worldPos, float startT, float endT)
    {
        float bestT = (startT + endT) * 0.5f;
        float minDist = float.MaxValue;
        
        // Sample more densely in the given range
        int samples = 50;
        for (int i = 0; i <= samples; i++)
        {
            float t = Mathf.Lerp(startT, endT, (float)i / samples);
            Vector3 splinePos = EvaluatePosition(t);
            float dist = Vector3.Distance(worldPos, splinePos);
            
            if (dist < minDist)
            {
                minDist = dist;
                bestT = t;
            }
        }
        
        return bestT;
    }

    // Improved global closest T finding using precomputed samples
    float FindClosestT(Vector3 worldPos)
    {
        float minDist = float.MaxValue;
        int bestIndex = 0;
        
        // First pass: find closest sample point
        for (int i = 0; i < splineSamplePoints.Length; i++)
        {
            float dist = Vector3.Distance(worldPos, splineSamplePoints[i]);
            if (dist < minDist)
            {
                minDist = dist;
                bestIndex = i;
            }
        }
        
        // Refine around the best sample
        float baseT = splineSampleDistances[bestIndex];
        float deltaT = 1f / (SPLINE_SAMPLE_COUNT - 1);
        
        return FindClosestTInRange(worldPos, 
            Mathf.Max(0f, baseT - deltaT), 
            Mathf.Min(1f, baseT + deltaT));
    }

    IEnumerator ProcessChainReaction(int insertionIndex, int insertionColor)
    {
        isProcessing = true;
        isPaused = true;
        yield return new WaitForSeconds(matchCheckDelay);

        int totalDestroyed = 0;
        int comboCount = 0;

        while (true)
        {
            // Validate insertion index
            if (insertionIndex < 0 || insertionIndex >= ballChain.Count) break;
            
            // Find match span
            int left = insertionIndex;
            while (left - 1 >= 0 && ballChain[left - 1].colorIndex == insertionColor) left--;
            int right = insertionIndex;
            while (right + 1 < ballChain.Count && ballChain[right + 1].colorIndex == insertionColor) right++;
            
            int matchCount = right - left + 1;
            if (matchCount < minMatchCount) break;

            comboCount++;
            totalDestroyed += matchCount;
            audioSource?.PlayOneShot(matchSound);

            // Smooth knockback
            float pushAmount = Mathf.Min(knockbackForce * (1f + (matchCount - minMatchCount) * 0.2f), bufferZoneSize * 0.8f);
            yield return StartCoroutine(SmoothKnockback(pushAmount));

            // Only destroy balls that have actually come out of the buffer
            var indicesToDestroy = Enumerable.Range(left, matchCount)
                                             .Where(i => i < ballChain.Count && ballChain[i].distanceOnSpline >= 0f)
                                             .ToList();

            yield return DestroyMatchedBalls(indicesToDestroy);

            // Smooth snap-back
            yield return StartCoroutine(SnapBackAnimation());

            insertionIndex = left - 1;
            insertionColor = (insertionIndex >= 0 && insertionIndex < ballChain.Count)
                            ? ballChain[insertionIndex].colorIndex
                            : -1;

            if (insertionIndex < 0 || insertionIndex + 1 >= ballChain.Count) break;
            if (ballChain[insertionIndex].colorIndex != ballChain[insertionIndex + 1].colorIndex) break;
            
            yield return new WaitForSeconds(matchCheckDelay);
        }

        OnShotReport?.Invoke(totalDestroyed, comboCount, new List<int>(enteredTypes));
        enteredTypes.Clear();

        // Reset all processing flags
        hasProcessedShot = false;
        isPaused = false;
        isProcessing = false;
    }

    IEnumerator SmoothKnockback(float amount)
    {
        var originals = ballChain.Select(b => b.distanceOnSpline).ToArray();
        var targets = originals.Select(d => d - amount).ToArray();

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / knockbackDuration);
            for (int i = 0; i < ballChain.Count; i++)
            {
                float d = Mathf.Lerp(originals[i], targets[i], t);
                ballChain[i].distanceOnSpline = d;
                Vector3 newPos = EvaluatePosition(d);
                ballChain[i].gameObject.transform.position = newPos;
                ballChain[i].worldPosition = newPos;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        for (int i = 0; i < ballChain.Count; i++)
        {
            ballChain[i].distanceOnSpline = targets[i];
            Vector3 newPos = EvaluatePosition(targets[i]);
            ballChain[i].gameObject.transform.position = newPos;
            ballChain[i].worldPosition = newPos;
        }
    }

    IEnumerator SnapBackAnimation()
    {
        var originals = ballChain.Select(b => b.distanceOnSpline).ToArray();
        RearrangeFrom(0);
        var targets = ballChain.Select(b => b.distanceOnSpline).ToArray();
        
        for (int i = 0; i < ballChain.Count; i++)
            ballChain[i].distanceOnSpline = originals[i];

        float elapsed = 0f;
        while (elapsed < snapBackDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / snapBackDuration);
            for (int i = 0; i < ballChain.Count; i++)
            {
                float d = Mathf.Lerp(originals[i], targets[i], t);
                ballChain[i].distanceOnSpline = d;
                Vector3 newPos = EvaluatePosition(d);
                ballChain[i].gameObject.transform.position = newPos;
                ballChain[i].worldPosition = newPos;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        for (int i = 0; i < ballChain.Count; i++)
        {
            ballChain[i].distanceOnSpline = targets[i];
            Vector3 newPos = EvaluatePosition(targets[i]);
            ballChain[i].gameObject.transform.position = newPos;
            ballChain[i].worldPosition = newPos;
        }
    }

    IEnumerator DestroyMatchedBalls(List<int> indices)
    {
        foreach (int i in indices)
            if (i < ballChain.Count && ballChain[i].distanceOnSpline >= 0f)
                ballChain[i].isBeingDestroyed = true;

        if(enemyHealth != null)
        {
            enemyHealth.TakeDamage(indices.Count);
        }

        float elapsed = 0f;
        while (elapsed < destroyAnimationTime)
        {
            float scale = Mathf.Lerp(ballSize, 0f, elapsed / destroyAnimationTime);
            foreach (int i in indices)
                if (i < ballChain.Count && ballChain[i].distanceOnSpline >= 0f)
                    ballChain[i].gameObject.transform.localScale = Vector3.one * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = indices.Count - 1; i >= 0; i--)
        {
            int idx = indices[i];
            if (idx < ballChain.Count && ballChain[idx].distanceOnSpline >= 0f)
            {
                Destroy(ballChain[idx].gameObject);
                ballChain.RemoveAt(idx);
            }
        }
    }

    GameObject CreateBall(int color, float t)
    {
        Vector3 pos = EvaluatePosition(t);
        var go = Instantiate(ballPrefab, pos, Quaternion.identity, transform);
        go.transform.localScale = Vector3.one * ballSize;
        
        var rend = go.GetComponent<Renderer>();
        if (rend) 
        {
            // Ensure we're using the correct material - validate bounds
            int safeColorIndex = Mathf.Clamp(color, 0, ballMaterials.Length - 1);
            rend.material = ballMaterials[safeColorIndex];
            rend.enabled = t >= 0f;
            
            // Debug validation in tight splines
            if (verboseColorLogging)
            {
                Debug.Log($"Created ball with color index {color} (clamped to {safeColorIndex}), material: {rend.material.name}");
            }
        }
        
        var col = go.GetComponent<Collider>() ?? go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        
        // For tight splines, reduce collider size to prevent overlapping triggers
        if (col is SphereCollider sphereCol)
        {
            sphereCol.radius = Mathf.Min(0.5f, ballSpacing / (2f * ballSize)); // Adaptive radius
        }
        
        var rb = go.GetComponent<Rigidbody>() ?? go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        
        var trig = go.AddComponent<ChainBallTrigger>();
        trig.spawner = this;
        
        return go;
    }

    Vector3 EvaluatePosition(float t)
    {
        var spline = splineContainer.Splines[0];
        if (t < 0f)
        {
            float3 p0 = SplineUtility.EvaluatePosition(spline, 0f);
            float3 tan0 = SplineUtility.EvaluateTangent(spline, 0f);
            float3 ext = p0 + tan0 * (t * splineLength);
            return splineContainer.transform.TransformPoint(ext);
        }
        
        t = Mathf.Clamp01(t);
        float3 loc = SplineUtility.EvaluatePosition(spline, t);
        return splineContainer.transform.TransformPoint(loc);
    }

    int FindInsertionIndex(float t)
    {
        for (int i = 0; i < ballChain.Count; i++)
            if (ballChain[i].distanceOnSpline > t) return i;
        return ballChain.Count;
    }

    void RearrangeFrom(int start)
    {
        float dt = ballSpacing / splineLength;
        for (int i = Mathf.Max(0, start + 1); i < ballChain.Count; i++)
        {
            ballChain[i].distanceOnSpline = ballChain[i - 1].distanceOnSpline + dt;
            Vector3 newPos = EvaluatePosition(ballChain[i].distanceOnSpline);
            ballChain[i].gameObject.transform.position = newPos;
            ballChain[i].worldPosition = newPos;
        }
    }

    List<int> GenerateRealZumaSequence(int count)
    {
        var seq = new List<int>();
        int i = 0;
        while (i < count)
        {
            int c = Random.Range(0, ballMaterials.Length);
            int grp = Random.Range(3, 6);
            for (int j = 0; j < grp && i < count; j++) { seq.Add(c); i++; }
        }
        return seq;
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos || ballChain == null) return;
        
        // Draw ball positions
        for (int i = 0; i < ballChain.Count; i++)
        {
            var ball = ballChain[i];
            Gizmos.color = ball.isBeingDestroyed ? Color.red : Color.green;
            Gizmos.DrawSphere(ball.worldPosition, debugSphereSize);
            
            // Draw insertion order
            Gizmos.color = Color.white;
            Vector3 labelPos = ball.worldPosition + Vector3.up * 0.3f;
            UnityEditor.Handles.Label(labelPos, i.ToString());
        }
    }

    private class ChainBallTrigger : MonoBehaviour
    {
        public SplineBallSpawner spawner;
        private static HashSet<GameObject> processedProjectiles = new HashSet<GameObject>();
        
        void OnTriggerEnter(Collider other)
        {
            var shot = other.GetComponent<BallBehavior>();
            if (shot != null && !processedProjectiles.Contains(other.gameObject))
            {
                processedProjectiles.Add(other.gameObject);
                spawner.OnBallHit(other.gameObject, shot.colorIndex);
                
                // Clean up the set periodically to prevent memory leaks
                if (processedProjectiles.Count > 50)
                {
                    processedProjectiles.Clear();
                }
            }
        }
    }
}