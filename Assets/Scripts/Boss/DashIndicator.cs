using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DashIndicator : MonoBehaviour
{
    private LineRenderer lineRenderer;
    
    [Header("Visual Settings")]
    [SerializeField] private float width = 0.5f;
    [SerializeField] private AnimationCurve widthCurve = AnimationCurve.Linear(0, 1, 1, 1);
    [SerializeField] private Material indicatorMaterial;
    
    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        SetupLineRenderer();
    }
    
    private void SetupLineRenderer()
    {
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.widthCurve = widthCurve;
        lineRenderer.positionCount = 2;
        
        if (indicatorMaterial)
        {
            lineRenderer.material = indicatorMaterial;
        }
    }
}