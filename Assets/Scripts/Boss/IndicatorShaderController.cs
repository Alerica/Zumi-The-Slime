using UnityEngine;

public class IndicatorShaderController : MonoBehaviour
{
    private Renderer indicatorRenderer;
    private MaterialPropertyBlock propertyBlock;
    
    [Header("Shader Properties")]
    [SerializeField] private string colorProperty = "_Color";
    [SerializeField] private string emissionProperty = "_EmissionColor";
    [SerializeField] private float emissionIntensity = 2f;
    
    private void Awake()
    {
        indicatorRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }
    
    public void SetIndicatorColor(Color color)
    {
        if (indicatorRenderer)
        {
            indicatorRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorProperty, color);
            propertyBlock.SetColor(emissionProperty, color * emissionIntensity);
            indicatorRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}