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
    
    private float mouseX;
    private float mouseY;
    private bool isAiming = false;
    private bool isRotatingToShoot = false;
    private Transform playerTransform;
    private Vector3 aimDirection;
    private Vector3 lastShootDirection;
    
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        
        playerTransform = transform;
        
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
        HandleAiming();
        HandleShooting();
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
        // cameraForward.normalize();
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
        // cameraForward.normalize();
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