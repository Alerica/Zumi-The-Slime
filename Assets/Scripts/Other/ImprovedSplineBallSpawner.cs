// ImprovedSplineBallSpawner.cs
using UnityEngine;
using UnityEngine.Splines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class ImprovedSplineBallSpawner : MonoBehaviour
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

    private class SplineBall
    {
        public GameObject go;
        public int colorIndex;
        public float distanceOnSpline;

        public SplineBall(GameObject go, int colorIndex, float distanceOnSpline)
        {
            this.go = go;
            this.colorIndex = colorIndex;
            this.distanceOnSpline = distanceOnSpline;
        }
    }

    [Header("Spline Settings")]
    public SplineContainer splineContainer;
    private float splineLength;
    private List<SplineBall> ballChain = new List<SplineBall>();
    private Queue<int> colorQueue = new Queue<int>();
    private bool isProcessing = false;
    private int comboCount = 0;
    private int totalDestroyed = 0;
    private AudioSource audioSourceCache;

    void Awake()
    {
        // splineLength = splineContainer.EvaluateLength();
        splineLength = splineContainer.CalculateLength();
        audioSourceCache = GetComponent<AudioSource>();
    }

    void Start()
    {
        GenerateColorQueue();
        SpawnInitialChain();
    }

    void Update()
    {
        // Optional: report statistics back to UI
        OnShotReport?.Invoke(comboCount, totalDestroyed, enteredTypes);
    }

    void SpawnInitialChain()
    {
        for (int i = 0; i < totalBallCount; i++)
        {
            float t = -bufferZoneSize + i * (ballSpacing / splineLength);
            var go = CreateBall(colorQueue.Dequeue(), t);
            ballChain.Add(new SplineBall(go, go.GetComponent<BallBehavior>().colorIndex, t));
        }
        RearrangeFrom(0);
    }

    void GenerateColorQueue()
    {
        var seq = Enumerable.Range(0, ballMaterials.Length)
                            .OrderBy(x => Random.value)
                            .ToList();
        // repeat until enough
        while (seq.Count < totalBallCount)
            seq.AddRange(seq);
        foreach (var c in seq.Take(totalBallCount))
            colorQueue.Enqueue(c);
    }

    GameObject CreateBall(int colorIndex, float distanceOnSpline)
    {
        Vector3 pos = EvaluatePosition(distanceOnSpline);
        var go = Instantiate(ballPrefab, pos, Quaternion.identity, transform);
        go.transform.localScale = Vector3.one * ballSize;
        var bb = go.GetComponent<BallBehavior>();
        if (bb != null) bb.colorIndex = colorIndex;
        var rend = go.GetComponent<Renderer>();
        if (rend != null && ballMaterials != null && colorIndex < ballMaterials.Length)
            rend.material = ballMaterials[colorIndex];
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

    IEnumerator SpawnRoutine()
    {
        // Example coroutine usage
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if (!isProcessing)
                InsertNextBall();
        }
    }

    void InsertNextBall()
    {
        if (colorQueue == null || colorQueue.Count == 0) return;
        float spawnPos = -bufferZoneSize;
        float minDist = ballSpacing / splineLength;
        if (ballChain.All(b => b.distanceOnSpline >= spawnPos + minDist))
        {
            int color = colorQueue.Dequeue();
            enteredTypes.Add(color);
            var go = CreateBall(color, -bufferZoneSize);
            ballChain.Insert(0, new SplineBall(go, color, -bufferZoneSize));
            RearrangeFrom(0);
        }
    }

    public void OnBallHit(GameObject projectile, int colorIndex)
    {
        float hitT = FindClosestT(projectile.transform.position);
        int insert = FindInsertionIndex(hitT);
        float dt = ballSpacing / splineLength;
        float newT = hitT;

        if (insert > 0 && insert < ballChain.Count)
            newT = (ballChain[insert - 1].distanceOnSpline + ballChain[insert].distanceOnSpline) * 0.5f;
        else if (insert == 0 && ballChain.Count > 0)
            newT = Mathf.Max(0f, ballChain[0].distanceOnSpline - dt);

        var go = CreateBall(colorIndex, newT);
        var sb = new SplineBall(go, colorIndex, newT);
        if (insert >= ballChain.Count) ballChain.Add(sb);
        else ballChain.Insert(insert, sb);

        Destroy(projectile);
        RearrangeFrom(Mathf.Max(0, insert - 1));
        StartCoroutine(ProcessChainReaction(insert, colorIndex));
    }

    /// <summary>
    /// Hitscan entry point: immediately insert a ball of the given color at the spline position
    /// closest to the worldPos.
    /// </summary>
    public void OnRaycastHit(Vector3 worldPos, int colorIndex)
    {
        // Find where along the spline the shot landed
        float hitT = FindClosestT(worldPos);
        // Create a dummy projectile and invoke existing hit logic
        OnBallHit(CreateDummyProjectile(worldPos), colorIndex);
    }

    /// <summary>
    /// Creates a minimal dummy projectile at the hit position so the existing OnBallHit
    /// overload can use it.
    /// </summary>
    private GameObject CreateDummyProjectile(Vector3 pos)
    {
        GameObject dummy = new GameObject("RaycastProjectile");
        dummy.transform.position = pos;
        var bb = dummy.AddComponent<BallBehavior>();
        return dummy;
    }

    IEnumerator ProcessChainReaction(int insertionIndex, int insertionColor)
    {
        isProcessing = true;
        yield return new WaitForSeconds(matchCheckDelay);

        int left = insertionIndex;
        while (left > 0 && ballChain[left - 1].colorIndex == insertionColor)
            left--;

        int right = insertionIndex + 1;
        while (right < ballChain.Count && ballChain[right].colorIndex == insertionColor)
            right++;

        int matchCount = right - left;
        if (matchCount < minMatchCount)
        {
            isProcessing = false;
            yield break;
        }

        int comboBonus = Mathf.Max(0, matchCount - minMatchCount) * minMatchCount;
        comboCount++;
        totalDestroyed += matchCount;
        audioSource?.PlayOneShot(matchSound);

        // smooth knockback
        float pushAmount = Mathf.Min(knockbackForce * (matchCount) * 0.2f, bufferZoneSize * 0.8f);
        yield return StartCoroutine(SmoothKnockback(pushAmount));

        // only destroy balls that have actually come out of the buffer
        var indicesToDestroy = Enumerable.Range(left, matchCount)
                                         .Where(i => ballChain[i].distanceOnSpline >= 0f)
                                         .ToList();
        yield return DestroyMatchedBalls(indicesToDestroy);

        isProcessing = false;
    }

    IEnumerator SmoothKnockback(float amount)
    {
        float elapsed = 0f;
        Vector3[] origPositions = ballChain.Select(b => b.go.transform.position).ToArray();
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / knockbackDuration;
            for (int i = 0; i < ballChain.Count; i++)
            {
                float dist = ballChain[i].distanceOnSpline;
                Vector3 dir = (ballChain[i].go.transform.position - transform.position).normalized;
                ballChain[i].go.transform.position = origPositions[i] + dir * (amount * (1f - t));
            }
            yield return null;
        }
        RearrangeFrom(0);
    }

    IEnumerator DestroyMatchedBalls(List<int> indices)
    {
        // play destroy animation
        for (int i = indices.Count - 1; i >= 0; i--)
        {
            int idx = indices[i];
            Destroy(ballChain[idx].go);
            ballChain.RemoveAt(idx);
        }
        yield return new WaitForSeconds(destroyAnimationTime);
        RearrangeFrom(indices.Min());
    }

    private int FindInsertionIndex(float t)
    {
        for (int i = 0; i < ballChain.Count; i++)
            if (ballChain[i].distanceOnSpline > t) return i;
        return ballChain.Count;
    }

    float FindClosestT(Vector3 worldPos)
    {
        float best = 0f, min = float.MaxValue;
        for (int i = 0; i <= 100; i++)
        {
            float t = i / 100f;
            Vector3 pt = EvaluatePosition(t);
            float d = Vector3.Distance(worldPos, pt);
            if (d < min) { min = d; best = t; }
        }
        return best;
    }

    private void RearrangeFrom(int start)
    {
        float dt = ballSpacing / splineLength;
        for (int i = Mathf.Max(0, start + 1); i < ballChain.Count; i++)
            ballChain[i].distanceOnSpline = ballChain[i - 1].distanceOnSpline + dt;
    }

    private class ChainBallTrigger : MonoBehaviour
    {
        public ImprovedSplineBallSpawner spawner;
        void OnTriggerEnter(Collider other)
        {
            var shot = other.GetComponent<BallBehavior>();
            if (shot != null) spawner.OnBallHit(other.gameObject, shot.colorIndex);
        }
    }
}
