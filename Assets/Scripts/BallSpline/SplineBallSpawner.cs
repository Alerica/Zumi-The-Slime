using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Random = UnityEngine.Random;

public class SplineBallSpawner : MonoBehaviour
{
    [Header("Ball Settings")]
    public GameObject ballPrefab;
    public Material[] ballMaterials; 
    public float ballSize = 0.4f;
    public float ballSpacing = 0.5f; 
    public float chainSpeed = 1f; 
    public int totalBallCount = 30;
    
    [Header("Spawning Settings")]
    public SpawningMode spawningMode = SpawningMode.RealZuma;
    public int minGroupSize = 2;
    public int maxGroupSize = 6;
    public float groupProbability = 0.7f;
    
    [Header("Matching Settings")]
    public int minMatchCount = 3;
    public float matchCheckDelay = 0.1f;
    public float destroyAnimationTime = 0.3f;
    public float snapBackSpeed = 2f;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip matchSound;
    

    
    public enum SpawningMode
    {
        Original,      
        RealZuma,      
        Clustered,     
        Random         
    }
    
    private SplineContainer splineContainer;
    private List<SplineBall> ballChain = new List<SplineBall>();
    private float splineLength = 0f;
    private bool isProcessingMatches = false;
    private bool isPaused = false;
    private int ballIdCounter = 0;
    
    [System.Serializable]
    public class SplineBall
    {
        public GameObject gameObject;
        public int colorIndex;
        public float distanceOnSpline;
        public bool isBeingDestroyed = false;
        public int id;
        
        public SplineBall(GameObject go, int color, float distance, int ballId)
        {
            gameObject = go;
            colorIndex = color;
            distanceOnSpline = distance;
            id = ballId;
        }
    }
    
    void Start()
    {
        splineContainer = GetComponent<SplineContainer>();
        
        if (splineContainer != null && splineContainer.Splines.Count > 0)
        {
            splineLength = splineContainer.CalculateLength(0);
            SpawnInitialBalls();
        }
        else
        {
            Debug.LogError("No spline found! Make sure this script is on a GameObject with a SplineContainer component.");
        }
    }
    
    void Update()
    {
        if (splineContainer != null && !isPaused)
        {
            MoveBallsAlongSpline();
            UpdateBallPositions();
        }
    }
    
    void SpawnInitialBalls()
    {
        if (splineLength <= 0) return;
        
        float currentDistance = 0f;
        float distanceIncrement = ballSpacing / splineLength;
        
        List<int> colorSequence = GenerateColorSequence(totalBallCount);
        
        for (int i = 0; i < colorSequence.Count && i < totalBallCount; i++)
        {
            if (currentDistance > 1f) break;
            
            GameObject ball = CreateBall(colorSequence[i], currentDistance);
            SplineBall ballData = new SplineBall(ball, colorSequence[i], currentDistance, ballIdCounter++);
            ballChain.Add(ballData);
            
            currentDistance += distanceIncrement;
        }
        
        Debug.Log($"Spawned {ballChain.Count} balls on spline using {spawningMode} mode");
    }
    
    List<int> GenerateColorSequence(int ballCount)
    {
        switch (spawningMode)
        {
            case SpawningMode.Original:
                return GenerateOriginalSequence(ballCount);
            case SpawningMode.RealZuma:
                return GenerateRealZumaSequence(ballCount);
            case SpawningMode.Clustered:
                return GenerateClusteredSequence(ballCount);
            case SpawningMode.Random:
                return GenerateRandomSequence(ballCount);
            default:
                return GenerateRealZumaSequence(ballCount);
        }
    }
    
    List<int> GenerateOriginalSequence(int ballCount)
    {
        List<int> sequence = new List<int>();
        
        for (int i = 0; i < ballCount; i++)
        {
            if (i < 2)
            {
                sequence.Add(Random.Range(0, ballMaterials.Length));
            }
            else
            {
                int colorIndex = GetSafeColorIndex(sequence, i);
                sequence.Add(colorIndex);
            }
        }
        
        return sequence;
    }
    
    List<int> GenerateRealZumaSequence(int ballCount)
    {
        List<int> sequence = new List<int>();
        int position = 0;
        
        while (position < ballCount)
        {
            int currentColor = Random.Range(0, ballMaterials.Length);
            int groupSize = Random.Range(minGroupSize, Mathf.Min(maxGroupSize + 1, ballCount - position + 1));
            
            if (Random.value > groupProbability)
            {
                groupSize = 1;
            }
            
            for (int i = 0; i < groupSize && position < ballCount; i++)
            {
                sequence.Add(currentColor);
                position++;
            }
        }
        
        return sequence;
    }
    
    List<int> GenerateClusteredSequence(int ballCount)
    {
        List<int> sequence = new List<int>();
        int position = 0;
        
        while (position < ballCount)
        {
            int currentColor = Random.Range(0, ballMaterials.Length);
            int groupSize = Random.Range(minGroupSize, maxGroupSize + 1);
            
            while (Random.value < 0.8f && groupSize < maxGroupSize && position + groupSize < ballCount)
            {
                groupSize++;
            }
            
            for (int i = 0; i < groupSize && position < ballCount; i++)
            {
                sequence.Add(currentColor);
                position++;
            }
        }
        
        return sequence;
    }
    
    List<int> GenerateRandomSequence(int ballCount)
    {
        List<int> sequence = new List<int>();
        
        for (int i = 0; i < ballCount; i++)
        {
            sequence.Add(Random.Range(0, ballMaterials.Length));
        }
        
        return sequence;
    }
    
    int GetSafeColorIndex(List<int> sequence, int position)
    {
        int attempts = 0;
        int colorIndex;
        
        do
        {
            colorIndex = Random.Range(0, ballMaterials.Length);
            attempts++;
            
            if (position >= 2 && 
                sequence[position - 1] == colorIndex && 
                sequence[position - 2] == colorIndex)
            {
                continue;
            }
            
            break;
        } while (attempts < 10);
        
        return colorIndex;
    }
    

    
    GameObject CreateBall(int colorIndex, float splinePosition)
    {
        Vector3 position = GetPositionOnSpline(splinePosition);
        GameObject ball = Instantiate(ballPrefab, position, Quaternion.identity);
        ball.transform.SetParent(transform);
        ball.transform.localScale = Vector3.one * ballSize;
        ball.name = $"Ball_{ballIdCounter}_{ballMaterials[colorIndex].name}";
        
        if (ballMaterials != null && ballMaterials.Length > 0 && colorIndex < ballMaterials.Length)
        {
            Renderer renderer = ball.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = ballMaterials[colorIndex];
            }
        }
        
        SplineBallBehavior behavior = ball.AddComponent<SplineBallBehavior>();
        behavior.colorIndex = colorIndex;
        behavior.spawner = this;
        behavior.ballId = ballIdCounter;
        
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb == null) rb = ball.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        
        Collider col = ball.GetComponent<Collider>();
        if (col == null) col = ball.AddComponent<SphereCollider>();
        col.isTrigger = true;
        
        return ball;
    }
    
    Vector3 GetPositionOnSpline(float t)
    {
        if (splineContainer == null || splineContainer.Splines.Count == 0) 
            return transform.position;
        
        t = Mathf.Clamp01(t);
        
        var spline = splineContainer.Splines[0];
        float3 position = SplineUtility.EvaluatePosition(spline, t);
        
        return splineContainer.transform.TransformPoint(position);
    }
    
    Vector3 GetTangentOnSpline(float t)
    {
        if (splineContainer == null || splineContainer.Splines.Count == 0) 
            return Vector3.forward;
        
        t = Mathf.Clamp01(t);
        
        var spline = splineContainer.Splines[0];
        float3 tangent = SplineUtility.EvaluateTangent(spline, t);
        
        return splineContainer.transform.TransformDirection(tangent).normalized;
    }
    
    void UpdateBallPositions()
    {
        for (int i = 0; i < ballChain.Count; i++)
        {
            if (ballChain[i] == null || ballChain[i].gameObject == null || ballChain[i].isBeingDestroyed) 
                continue;
            
            Vector3 position = GetPositionOnSpline(ballChain[i].distanceOnSpline);
            ballChain[i].gameObject.transform.position = position;
            
            Vector3 tangent = GetTangentOnSpline(ballChain[i].distanceOnSpline);
            if (tangent != Vector3.zero)
            {
                ballChain[i].gameObject.transform.rotation = Quaternion.LookRotation(tangent);
            }
        }
    }
    
    void MoveBallsAlongSpline()
    {
        List<SplineBall> ballsToRemove = new List<SplineBall>();
        
        foreach (var ball in ballChain)
        {
            if (ball == null || ball.isBeingDestroyed) continue;
            
            ball.distanceOnSpline += (chainSpeed * Time.deltaTime) / splineLength;
            
            if (ball.distanceOnSpline >= 1f)
            {
                ballsToRemove.Add(ball);
            }
        }
        
        foreach (var ball in ballsToRemove)
        {
            RemoveBallFromChain(ball);
            if (ball.gameObject != null)
            {
                Destroy(ball.gameObject);
            }
        }
    }
    
    public void OnBallHit(GameObject projectile, int projectileColorIndex)
    {
        if (isProcessingMatches || isPaused) return;
        
        float hitDistance = FindClosestPointOnSpline(projectile.transform.position);
        int insertIndex = FindInsertionIndex(hitDistance);
        
        GameObject newBall = CreateBall(projectileColorIndex, hitDistance);
        SplineBall newBallData = new SplineBall(newBall, projectileColorIndex, hitDistance, ballIdCounter++);
        
        if (insertIndex >= ballChain.Count)
        {
            ballChain.Add(newBallData);
        }
        else
        {
            ballChain.Insert(insertIndex, newBallData);
        }
        
        Destroy(projectile);
        RearrangeBalls(insertIndex);
        StartCoroutine(ProcessChainReaction(insertIndex));
    }
    
    IEnumerator ProcessChainReaction(int insertionIndex)
    {
        isProcessingMatches = true;
        isPaused = true;
        
        yield return new WaitForSeconds(matchCheckDelay);
        
        bool hasChainReaction = true;
        int checkIndex = insertionIndex;
        
        while (hasChainReaction)
        {
            var matchResult = FindMatchGroupFromIndex(checkIndex);
            
            if (matchResult.matchIndices.Count >= minMatchCount)
            {
                if (audioSource && matchSound)
                {
                    audioSource.PlayOneShot(matchSound);
                }
                
                int leftBoundaryIndex = matchResult.leftBoundary;
                int rightBoundaryIndex = matchResult.rightBoundary;
                
                yield return StartCoroutine(DestroyMatchedBalls(matchResult.matchIndices));
                yield return StartCoroutine(SnapBackAnimation());
                
                int newLeftIndex = leftBoundaryIndex;
                int newRightIndex = leftBoundaryIndex + 1;
                
                if (newLeftIndex >= 0 && 
                    newRightIndex < ballChain.Count && 
                    ballChain[newLeftIndex].colorIndex == ballChain[newRightIndex].colorIndex)
                {
                    checkIndex = newLeftIndex;
                    yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    hasChainReaction = false;
                }
            }
            else
            {
                hasChainReaction = false;
            }
        }
        
        isPaused = false;
        isProcessingMatches = false;
    }
    

    
    (List<int> matchIndices, int leftBoundary, int rightBoundary) FindMatchGroupFromIndex(int centerIndex)
    {
        if (centerIndex < 0 || centerIndex >= ballChain.Count || ballChain[centerIndex].isBeingDestroyed)
        {
            return (new List<int>(), -1, ballChain.Count);
        }
        
        List<int> matchIndices = new List<int>();
        int targetColor = ballChain[centerIndex].colorIndex;
        
        int leftIndex = centerIndex;
        while (leftIndex >= 0 && 
               !ballChain[leftIndex].isBeingDestroyed &&
               ballChain[leftIndex].colorIndex == targetColor &&
               (leftIndex == centerIndex || IsAdjacent(leftIndex, leftIndex + 1)))
        {
            matchIndices.Insert(0, leftIndex);
            leftIndex--;
        }
        
        int rightIndex = centerIndex + 1;
        while (rightIndex < ballChain.Count && 
               !ballChain[rightIndex].isBeingDestroyed &&
               ballChain[rightIndex].colorIndex == targetColor &&
               IsAdjacent(rightIndex - 1, rightIndex))
        {
            matchIndices.Add(rightIndex);
            rightIndex++;
        }
        
        int leftBoundary = leftIndex;
        int rightBoundary = rightIndex;
        
        return (matchIndices, leftBoundary, rightBoundary);
    }
    
    IEnumerator DestroyMatchedBalls(List<int> matchIndices)
    {
        foreach (int index in matchIndices)
        {
            if (index < ballChain.Count)
            {
                ballChain[index].isBeingDestroyed = true;
            }
        }
        
        List<GameObject> ballsToDestroy = new List<GameObject>();
        foreach (int index in matchIndices)
        {
            if (index < ballChain.Count && ballChain[index].gameObject != null)
            {
                ballsToDestroy.Add(ballChain[index].gameObject);
            }
        }
        
        float elapsedTime = 0f;
        while (elapsedTime < destroyAnimationTime)
        {
            float scale = Mathf.Lerp(ballSize, 0f, elapsedTime / destroyAnimationTime);
            foreach (var ball in ballsToDestroy)
            {
                if (ball != null)
                {
                    ball.transform.localScale = Vector3.one * scale;
                }
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        foreach (var ball in ballsToDestroy)
        {
            if (ball != null)
            {
                Destroy(ball);
            }
        }
        
        ballChain.RemoveAll(ball => ball.isBeingDestroyed || ball.gameObject == null);
    }
    
    IEnumerator SnapBackAnimation()
    {
        if (splineLength <= 0 || ballChain.Count == 0) yield break;
        
        float spacing = ballSpacing / splineLength;
        List<float> targetDistances = new List<float>();
        
        for (int i = 0; i < ballChain.Count; i++)
        {
            if (i == 0)
            {
                targetDistances.Add(ballChain[i].distanceOnSpline);
            }
            else
            {
                targetDistances.Add(targetDistances[i - 1] + spacing);
            }
        }
        
        float animationTime = 0.3f;
        float elapsedTime = 0f;
        
        List<float> startDistances = ballChain.Select(b => b.distanceOnSpline).ToList();
        
        while (elapsedTime < animationTime)
        {
            float t = elapsedTime / animationTime;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            for (int i = 0; i < ballChain.Count; i++)
            {
                ballChain[i].distanceOnSpline = Mathf.Lerp(startDistances[i], targetDistances[i], t);
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        for (int i = 0; i < ballChain.Count; i++)
        {
            ballChain[i].distanceOnSpline = targetDistances[i];
        }
    }
    
    int FindInsertionIndex(float splinePosition)
    {
        for (int i = 0; i < ballChain.Count; i++)
        {
            if (ballChain[i].distanceOnSpline > splinePosition)
            {
                return i;
            }
        }
        return ballChain.Count;
    }
    
    float FindClosestPointOnSpline(Vector3 worldPos)
    {
        if (splineContainer == null) return 0f;
        
        float closestDistance = float.MaxValue;
        float closestT = 0f;
        
        for (int i = 0; i < 200; i++)
        {
            float t = (float)i / 199f;
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
    
    void RearrangeBalls(int fromIndex)
    {
        if (splineLength <= 0) return;
        
        float spacing = ballSpacing / splineLength;
        
        for (int i = fromIndex + 1; i < ballChain.Count; i++)
        {
            float desiredDistance = ballChain[i - 1].distanceOnSpline + spacing;
            
            if (ballChain[i].distanceOnSpline < desiredDistance)
            {
                ballChain[i].distanceOnSpline = desiredDistance;
            }
        }
    }
    
    bool IsAdjacent(int index1, int index2)
    {
        if (index1 < 0 || index2 < 0 || index1 >= ballChain.Count || index2 >= ballChain.Count)
            return false;
        
        float spacing = ballSpacing / splineLength;
        float distance = Mathf.Abs(ballChain[index2].distanceOnSpline - ballChain[index1].distanceOnSpline);
        
        return distance <= spacing * 1.5f;
    }
    
    void RemoveBallFromChain(SplineBall ball)
    {
        ballChain.Remove(ball);
    }
    

}

public class SplineBallBehavior : MonoBehaviour
{
    public int colorIndex;
    public SplineBallSpawner spawner;
    public int ballId;
    
    void OnTriggerEnter(Collider other)
    {
        BallBehavior playerBall = other.GetComponent<BallBehavior>();
        if (playerBall != null && spawner != null)
        {
            spawner.OnBallHit(other.gameObject, playerBall.colorIndex);
        }
    }
}