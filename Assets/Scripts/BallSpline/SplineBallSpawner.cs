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
    [Tooltip("Time for snap‑back animation after each match.")]
    public float snapBackDuration = 0.2f;
    [Tooltip("Time between sequential matches in a chain reaction.")]
    public float interMatchDelay = 0.05f;

    private SplineContainer splineContainer;
    private float splineLength;
    private readonly List<SplineBall> ballChain = new List<SplineBall>();
    private Queue<int> colorQueue;
    private bool isProcessing = false;
    private bool isPaused = false;
    private float currentKnockback = 0f;
    private bool isKnockingBack = false;

    [Serializable]
    public class SplineBall
    {
        public GameObject gameObject;
        public int colorIndex;
        public float distanceOnSpline;
        public bool isBeingDestroyed;
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
        var initialColors = GenerateRealZumaSequence(totalBallCount);
        colorQueue = new Queue<int>(initialColors);
        SpawnInitialBalls();
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
            ballChain.Add(new SplineBall(go, color, t));
        }
    }

    void MoveBallsAlongSpline()
    {
        float dt = (chainSpeed * Time.deltaTime) / splineLength;
        if (!isKnockingBack && currentKnockback > 0f)
            currentKnockback = Mathf.Max(0f, currentKnockback - dt);

        var toRemove = new List<SplineBall>();
        foreach (var ball in ballChain)
        {
            if (ball.isBeingDestroyed) continue;
            float extra = (!isKnockingBack && currentKnockback > 0f) ? dt : 0f;
            ball.distanceOnSpline += dt + extra;
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
            ball.gameObject.transform.position = EvaluatePosition(ball.distanceOnSpline);
            var rend = ball.gameObject.GetComponent<Renderer>();
            if (rend) rend.enabled = ball.distanceOnSpline >= 0f;
        }
    }

    void TrySpawnNext()
    {
        if (colorQueue == null || colorQueue.Count == 0) return;
        if (CanSpawnNewBall())
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
        if (isProcessing || isPaused) return;
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

    IEnumerator ProcessChainReaction(int insertionIndex, int insertionColor)
    {
        isProcessing = true;
        isPaused = true;
        yield return new WaitForSeconds(matchCheckDelay);

        int totalDestroyed = 0;
        int comboCount    = 0;

        while (true)
        {
            // Expand around center 
            int left = insertionIndex;
            while (left - 1 >= 0 && ballChain[left - 1].colorIndex == insertionColor) left--;
            int right = insertionIndex;
            while (right + 1 < ballChain.Count && ballChain[right + 1].colorIndex == insertionColor) right++;
            int matchCount = right - left + 1;
            if (matchCount < minMatchCount) break;

            comboCount++;
            totalDestroyed += matchCount;

            // Play sound
            audioSource?.PlayOneShot(matchSound);
            var indicesToDestroy = Enumerable.Range(left, matchCount).ToList();
            yield return ApplyKnockback(indicesToDestroy);
            yield return DestroyMatchedBalls(indicesToDestroy);
            yield return SnapBackAnimation();

            // After snap‑back, check for further chain reaction
            insertionIndex = left - 1;
            insertionColor = (insertionIndex >= 0 && insertionIndex < ballChain.Count)
                ? ballChain[insertionIndex].colorIndex
                : -1;

            if (insertionIndex < 0 || insertionIndex + 1 >= ballChain.Count) break;
            if (ballChain[insertionIndex].colorIndex != ballChain[insertionIndex + 1].colorIndex) break;

            yield return new WaitForSeconds(matchCheckDelay);
        }

        // report / send signal 
        OnShotReport?.Invoke(totalDestroyed, comboCount, new List<int>(enteredTypes));
        enteredTypes.Clear();

        isPaused     = false;
        isProcessing = false;
    }

    IEnumerator ApplyKnockback(List<int> indices)
    {
        if (!indices.Any()) yield break;
        isKnockingBack = true;
        float amount = Mathf.Min(knockbackForce * (1f + (indices.Count - minMatchCount) * 0.2f), bufferZoneSize * 0.8f);
        float start = currentKnockback;
        float target = start + amount;
        float elapsed = 0f;
        var originals = ballChain.ToDictionary(b => b, b => b.distanceOnSpline);

        while (elapsed < knockbackDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / knockbackDuration);
            currentKnockback = Mathf.Lerp(start, target, t);
            float delta = currentKnockback - start;
            foreach (var kv in originals)
                kv.Key.distanceOnSpline = kv.Value - delta;
            elapsed += Time.deltaTime;
            yield return null;
        }

        currentKnockback = target;
        isKnockingBack   = false;
    }

    IEnumerator DestroyMatchedBalls(List<int> indices)
    {
        foreach (int i in indices) ballChain[i].isBeingDestroyed = true;
        float elapsed = 0f;
        while (elapsed < destroyAnimationTime)
        {
            float scale = Mathf.Lerp(ballSize, 0f, elapsed / destroyAnimationTime);
            foreach (int i in indices)
                if (i < ballChain.Count)
                    ballChain[i].gameObject.transform.localScale = Vector3.one * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }
        for (int i = indices.Count - 1; i >= 0; i--)
        {
            int idx = indices[i];
            if (idx < ballChain.Count)
            {
                Destroy(ballChain[idx].gameObject);
                ballChain.RemoveAt(idx);
            }
        }
    }

    IEnumerator SnapBackAnimation()
    {
        yield return null;
        RearrangeFrom(0);
    }

    bool CanSpawnNewBall()
    {
        float spawnPos = -bufferZoneSize + currentKnockback;
        float minDist  = ballSpacing / splineLength;
        return ballChain.All(b => b.distanceOnSpline >= spawnPos + minDist);
    }

    GameObject CreateBall(int color, float t)
    {
        Vector3 pos = EvaluatePosition(t);
        var go = Instantiate(ballPrefab, pos, Quaternion.identity, transform);
        go.transform.localScale = Vector3.one * ballSize;
        var rend = go.GetComponent<Renderer>();
        if (rend) rend.material = ballMaterials[color];
        if (t < 0f && rend) rend.enabled = false;
        var col = go.GetComponent<Collider>() ?? go.AddComponent<SphereCollider>();
        col.isTrigger = true;
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
            float3 p0   = SplineUtility.EvaluatePosition(spline, 0f);
            float3 tan0 = SplineUtility.EvaluateTangent(spline, 0f);
            float3 ext  = p0 + tan0 * (t * splineLength);
            return splineContainer.transform.TransformPoint(ext);
        }
        t = Mathf.Clamp01(t);
        float3 loc = SplineUtility.EvaluatePosition(spline, t);
        return splineContainer.transform.TransformPoint(loc);
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
            ballChain[i].distanceOnSpline = ballChain[i - 1].distanceOnSpline + dt;
    }

    List<int> GenerateRealZumaSequence(int count)
    {
        var seq = new List<int>();
        int i = 0;
        while (i < count)
        {
            int c   = Random.Range(0, ballMaterials.Length);
            int grp = Random.Range(3, 6);
            for (int j = 0; j < grp && i < count; j++) { seq.Add(c); i++; }
        }
        return seq;
    }

    private class ChainBallTrigger : MonoBehaviour
    {
        public SplineBallSpawner spawner;
        void OnTriggerEnter(Collider other)
        {
            var shot = other.GetComponent<BallBehavior>();
            if (shot != null) spawner.OnBallHit(other.gameObject, shot.colorIndex);
        }
    }
}
