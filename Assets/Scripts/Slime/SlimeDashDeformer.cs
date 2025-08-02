using UnityEngine;

public class SlimeDashDeformer : MonoBehaviour
{
    [Header("Dash Deformation")]
    [SerializeField] private float dashStretchAmount = 0.8f;     // How much to stretch forward
    [SerializeField] private float dashSquashAmount = 0.4f;     // How much to squash vertically
    [SerializeField] private float dashSideSquash = 0.3f;      // How much to squash sideways
    [SerializeField] private float dashSmooth = 8f;            // How fast to deform
    [SerializeField] private float returnSmooth = 4f;          // How fast to return to normal
    
    [Header("References")]
    [SerializeField] private ImprovedFrogMovement playerMovement;
    [SerializeField] private SlimeDeformer normalDeformer;      // Optional: disable during dash
    
    private Vector3 originalScale;
    private Vector3 targetScale;
    private Vector3 dashDirection;
    private bool wasDashing = false;
    
    void Start()
    {
        // Get player movement if not assigned
        if (playerMovement == null)
            playerMovement = GetComponent<ImprovedFrogMovement>();
            
        // Get normal deformer if not assigned
        if (normalDeformer == null)
            normalDeformer = GetComponent<SlimeDeformer>();
            
        originalScale = transform.localScale;
        targetScale = originalScale;
    }
    
    void Update()
    {
        if (playerMovement == null) return;

        bool isDashing = playerMovement.isImmune;
        
        if (isDashing)
        {
            HandleDashDeformation();
            
            // Disable normal deformer during dash (optional)
            if (normalDeformer != null)
                normalDeformer.enabled = false;
        }
        else
        {
            HandleNormalState();
            
            // Re-enable normal deformer when not dashing
            if (normalDeformer != null && !normalDeformer.enabled)
                normalDeformer.enabled = true;
        }
        
        // Apply the deformation
        float smoothSpeed = isDashing ? dashSmooth : returnSmooth;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * smoothSpeed);
        
        wasDashing = isDashing;
    }
    
    void HandleDashDeformation()
    {
        // Get dash direction from player's forward or movement direction
        dashDirection = playerMovement.transform.forward;
        
        // Create dash deformation based on direction
        Vector3 dashScale = originalScale;
        
        // Forward/backward dash
        if (Mathf.Abs(dashDirection.z) > Mathf.Abs(dashDirection.x))
        {
            if (dashDirection.z > 0) // Dashing forward
            {
                dashScale.z += dashStretchAmount;     // Stretch forward
                dashScale.y -= dashSquashAmount;      // Squash down
                dashScale.x -= dashSideSquash;        // Squash sides
            }
            else // Dashing backward
            {
                dashScale.z += dashStretchAmount;     // Stretch backward
                dashScale.y -= dashSquashAmount;      // Squash down  
                dashScale.x -= dashSideSquash;        // Squash sides
            }
        }
        // Left/right dash
        else
        {
            if (dashDirection.x > 0) // Dashing right
            {
                dashScale.x += dashStretchAmount;     // Stretch right
                dashScale.y -= dashSquashAmount;      // Squash down
                dashScale.z -= dashSideSquash;        // Squash front/back
            }
            else // Dashing left
            {
                dashScale.x += dashStretchAmount;     // Stretch left
                dashScale.y -= dashSquashAmount;      // Squash down
                dashScale.z -= dashSideSquash;        // Squash front/back
            }
        }
        
        targetScale = dashScale;
    }
    
    void HandleNormalState()
    {
        // Return to original scale when not dashing
        targetScale = originalScale;
    }
    
    // Optional: Call this for more dramatic dash effect
    public void SetDashIntensity(float intensity)
    {
        dashStretchAmount = 0.8f * intensity;
        dashSquashAmount = 0.4f * intensity;
        dashSideSquash = 0.3f * intensity;
    }
    
    // Optional: Get current deformation state
    public bool IsDashDeformed()
    {
        return playerMovement != null && playerMovement.isImmune;
    }
}