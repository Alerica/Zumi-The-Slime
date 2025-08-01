// TEmporary LaserBeam visual delete later 




using UnityEngine;
[RequireComponent(typeof(LineRenderer))]
public class LaserBeamVisual : MonoBehaviour
{
    [Header("Laser Settings")]
    [SerializeField] private float baseWidth = 0.5f;
    [SerializeField] private float endWidthMultiplier = 0.8f;
    [SerializeField] private int segments = 50; // More = smoother beam
    
    [Header("Visual Effects")]
    [SerializeField] private bool useNoise = true;
    [SerializeField] private float noiseScale = 0.1f;
    [SerializeField] private float noiseSpeed = 5f;
    [SerializeField] private AnimationCurve widthCurve = AnimationCurve.Linear(0, 1, 1, 0.8f);
    
    [Header("Color Settings")]
    [SerializeField] private Gradient beamGradient;
    [SerializeField] private Color coreColor = Color.white;
    [SerializeField] private Color outerColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private float glowIntensity = 2f;
    
    [Header("Texture Scrolling")]
    [SerializeField] private float textureScrollSpeed = 2f;
    [SerializeField] private Texture2D beamTexture;
    
    private LineRenderer lineRenderer;
    private Material laserMaterial;
    private float textureOffset = 0f;
    
    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        SetupLaser();
    }
    
    void SetupLaser()
    {
        lineRenderer.positionCount = segments;
        lineRenderer.useWorldSpace = true;
        
        CreateLaserMaterial();
        
        if (beamGradient == null || beamGradient.colorKeys.Length == 0)
        {
            beamGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(coreColor, 0.0f);
            colorKeys[1] = new GradientColorKey(outerColor, 0.5f);
            colorKeys[2] = new GradientColorKey(outerColor * 0.5f, 1.0f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphaKeys[1] = new GradientAlphaKey(0.6f, 1.0f);
            
            beamGradient.SetKeys(colorKeys, alphaKeys);
        }
        
        lineRenderer.colorGradient = beamGradient;
    }
    
    void CreateLaserMaterial()
    {
        laserMaterial = new Material(Shader.Find("Sprites/Default"));
        
        laserMaterial.SetFloat("_Mode", 3);
        laserMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        laserMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        laserMaterial.SetInt("_ZWrite", 0);
        laserMaterial.DisableKeyword("_ALPHATEST_ON");
        laserMaterial.EnableKeyword("_ALPHABLEND_ON");
        laserMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        laserMaterial.renderQueue = 3000;
        
        if (beamTexture != null)
        {
            laserMaterial.mainTexture = beamTexture;
        }
        
        laserMaterial.color = outerColor * glowIntensity;
        
        lineRenderer.material = laserMaterial;
    }
    
    public void UpdateBeam(Vector3 origin, Vector3 endPoint)
    {
        if (!lineRenderer.enabled) return;
        
        Vector3 direction = (endPoint - origin).normalized;
        float distance = Vector3.Distance(origin, endPoint);
        
        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);
            Vector3 point = Vector3.Lerp(origin, endPoint, t);
            
            if (useNoise && i > 0 && i < segments - 1)
            {
                float noiseX = Mathf.PerlinNoise(t * noiseScale + Time.time * noiseSpeed, 0) - 0.5f;
                float noiseY = Mathf.PerlinNoise(0, t * noiseScale + Time.time * noiseSpeed) - 0.5f;
                
                Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
                if (perpendicular == Vector3.zero)
                    perpendicular = Vector3.Cross(direction, Vector3.right).normalized;
                
                Vector3 perpendicular2 = Vector3.Cross(direction, perpendicular).normalized;
                
                point += perpendicular * noiseX * noiseScale * distance * 0.02f;
                point += perpendicular2 * noiseY * noiseScale * distance * 0.02f;
            }
            
            lineRenderer.SetPosition(i, point);
        }
        
        AnimationCurve finalWidthCurve = new AnimationCurve();
        for (int i = 0; i < widthCurve.length; i++)
        {
            Keyframe key = widthCurve[i];
            float width = baseWidth * key.value;
            finalWidthCurve.AddKey(key.time, width);
        }
        lineRenderer.widthCurve = finalWidthCurve;
        
        if (laserMaterial != null && beamTexture != null)
        {
            textureOffset += Time.deltaTime * textureScrollSpeed;
            laserMaterial.mainTextureOffset = new Vector2(textureOffset, 0);
        }
        
        float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.1f;
        lineRenderer.widthMultiplier = pulse;
    }
    
    public void SetBeamActive(bool active)
    {
        lineRenderer.enabled = active;
    }
    
    public void SetBeamColor(Color color)
    {
        outerColor = color;
        if (laserMaterial != null)
        {
            laserMaterial.color = outerColor * glowIntensity;
        }
    }
    
    void OnDestroy()
    {
        if (laserMaterial != null)
        {
            Destroy(laserMaterial);
        }
    }
}