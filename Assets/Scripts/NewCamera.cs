using UnityEngine;
using Unity.Cinemachine;

public class NewCamera : MonoBehaviour
{
    [Header("Cinemachine Cameras")]
    public CinemachineCamera normalCamera;
    public CinemachineCamera aimCamera;
    
    [Header("Camera Target")]
    public Transform cameraTarget;
    
    [Header("Mouse Settings")]
    public float mouseSensitivity = 2f;
    public float verticalClampMin = -30f;
    public float verticalClampMax = 60f;
    
    [Header("Camera Settings")]
    public float cameraHeight = 1.5f;
    public float aimTransitionSpeed = 5f;
    public float shootRotationSpeed = 8f;
    
    [Header("Camera Distance Settings")]
    public float defaultCameraDistance = 5f;
    public float minCameraDistance = 2f;
    public float maxCameraDistance = 15f;
    public float scrollSensitivity = 2f;
    public float distanceChangeSpeed = 8f;
    
    private float mouseX;
    private float mouseY;
    private bool isAiming = false;
    private bool isRotatingToShoot = false;
    private Transform playerTransform;
    private Vector3 aimDirection;
    private Vector3 lastShootDirection;
    
    // Distance control variables
    private float currentCameraDistance;
    private float targetCameraDistance;
    
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        
        playerTransform = transform;
        
        // Initialize camera distance
        currentCameraDistance = defaultCameraDistance;
        targetCameraDistance = defaultCameraDistance;
        
        if (cameraTarget == null)
        {
            GameObject target = new GameObject("CameraTarget");
            cameraTarget = target.transform;
            cameraTarget.SetParent(transform);
            cameraTarget.localPosition = new Vector3(0, cameraHeight, 0);
        }
        
        SetupCameraTargets();
        
        normalCamera.Priority = 10;
        aimCamera.Priority = 0;
    }
    
    void SetupCameraTargets()
    {
        if (normalCamera != null)
        {
            normalCamera.Follow = cameraTarget;
            normalCamera.LookAt = cameraTarget;
        }
        
        if (aimCamera != null)
        {
            aimCamera.Follow = cameraTarget;
            aimCamera.LookAt = cameraTarget;
        }
    }
    
    void Update()
    {
        HandleMouseInput();
        HandleScrollWheel();
        HandleAiming();
        HandleShooting();
        UpdateCameraDistance();
        RotateCameraTarget();
        
        if (isAiming)
        {
            HandleAimingRotation();
        }
        else if (isRotatingToShoot)
        {
            HandleShootRotation();
        }
    }
    
    void HandleMouseInput()
    {
        float mouseXInput = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseYInput = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        mouseX += mouseXInput;
        mouseY -= mouseYInput;
        mouseY = Mathf.Clamp(mouseY, verticalClampMin, verticalClampMax);
    }
    
    void HandleScrollWheel()
    {
        // Only allow distance control when not aiming (free mode)
        if (!isAiming)
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scrollInput) > 0.01f)
            {
                targetCameraDistance -= scrollInput * scrollSensitivity;
                targetCameraDistance = Mathf.Clamp(targetCameraDistance, minCameraDistance, maxCameraDistance);
            }
        }
    }
    
    void UpdateCameraDistance()
    {
        // Smoothly interpolate to target distance
        currentCameraDistance = Mathf.Lerp(currentCameraDistance, targetCameraDistance, distanceChangeSpeed * Time.deltaTime);
        
        // Apply distance to normal camera when not aiming
        if (!isAiming && normalCamera != null)
        {
            // Try to get ThirdPersonFollow component
            var thirdPersonFollow = normalCamera.GetComponent<CinemachineThirdPersonFollow>();
            if (thirdPersonFollow != null)
            {
                thirdPersonFollow.CameraDistance = currentCameraDistance;
            }
            else
            {
                // Fallback: manually position camera relative to target
                Vector3 offset = -cameraTarget.forward * currentCameraDistance;
                
                // Try to get the camera's transform component
                var transposer = normalCamera.GetComponent<CinemachineTransposer>();
                if (transposer != null)
                {
                    // transposer.FollowOffset = offset + Vector3.up * cameraHeight;
                    transposer.m_FollowOffset = offset + Vector3.up * cameraHeight;
                }
            }
        }
    }
    
    void HandleAiming()
    {
        if (Input.GetMouseButtonDown(1))
        {
            StartAiming();
        }
        else if (Input.GetMouseButtonUp(1))
        {
            StopAiming();
        }
    }
    
    void HandleShooting()
    {
        if (Input.GetMouseButtonDown(0) && !isAiming)
        {
            StartShootRotation();
        }
    }
    
    void StartAiming()
    {
        isAiming = true;
        isRotatingToShoot = false;
        normalCamera.Priority = 0;
        aimCamera.Priority = 10;
    }
    
    void StopAiming()
    {
        isAiming = false;
        normalCamera.Priority = 10;
        aimCamera.Priority = 0;
    }
    
    void StartShootRotation()
    {
        Vector3 cameraForward = GetCameraForward();
        cameraForward.y = 0;
        cameraForward = cameraForward.normalized;
        if (cameraForward != Vector3.zero)
        {
            lastShootDirection = cameraForward;
            isRotatingToShoot = true;
        }
    }
    
    void RotateCameraTarget()
    {
        cameraTarget.rotation = Quaternion.Euler(mouseY, mouseX, 0);
        cameraTarget.position = playerTransform.position + Vector3.up * cameraHeight;
    }
    
    void HandleAimingRotation()
    {
        Vector3 cameraForward = GetCameraForward();
        cameraForward.y = 0;
        cameraForward = cameraForward.normalized;
        
        if (cameraForward != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
            playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRotation, aimTransitionSpeed * Time.deltaTime);
        }
    }
    
    void HandleShootRotation()
    {
        if (lastShootDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lastShootDirection);
            playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRotation, shootRotationSpeed * Time.deltaTime);
            
            float angle = Quaternion.Angle(playerTransform.rotation, targetRotation);
            if (angle < 5f)
            {
                isRotatingToShoot = false;
                
                // Re-enable movement rotation
                NewMovement frogMovement = GetComponent<NewMovement>();
                if (frogMovement != null)
                {
                    frogMovement.SetMovementRotationEnabled(true);
                }
            }
        }
    }
    
    // Public methods for accessing camera distance
    public float GetCurrentCameraDistance()
    {
        return currentCameraDistance;
    }
    
    public void SetCameraDistance(float distance)
    {
        targetCameraDistance = Mathf.Clamp(distance, minCameraDistance, maxCameraDistance);
    }
    
    public bool IsAiming()
    {
        return isAiming;
    }
    
    public Transform GetCameraTransform()
    {
        Camera activeCamera = Camera.main;
        return activeCamera != null ? activeCamera.transform : cameraTarget;
    }
    
    public Vector3 GetCameraForward()
    {
        Camera activeCamera = Camera.main;
        return activeCamera != null ? activeCamera.transform.forward : cameraTarget.forward;
    }
    
    public Vector3 GetAimDirection()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
            return ray.direction;
        }
        return cameraTarget.forward;
    }
    
    public Vector3 GetScreenCenterPoint(float distance = 100f)
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
            return ray.origin + ray.direction * distance;
        }
        return cameraTarget.position + cameraTarget.forward * distance;
    }
}