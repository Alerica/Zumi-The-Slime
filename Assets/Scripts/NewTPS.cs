using UnityEngine;
using Unity.Cinemachine;

public class NewTPS : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] float mouseSensitivity = 2f;
    [SerializeField] float verticalLookLimit = 80f;
    [SerializeField] bool invertY = false;
    
    [Header("Aim Settings")]
    [SerializeField] float normalFOV = 60f;
    [SerializeField] float aimFOV = 35f;
    [SerializeField] float aimSpeed = 5f;
    
    [Header("References")]
    [SerializeField] Transform player;
    [SerializeField] Transform cameraPivot;
    [SerializeField] CinemachineCamera virtualCamera;
    [SerializeField] GameObject crosshair;
    
    // Private variables
    float xRotation = 0f;
    bool isAiming = false;
    CinemachineFollow followComponent;
    
    void Start()
    {
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Get components
        if (virtualCamera != null)
        {
            followComponent = virtualCamera.GetComponent<CinemachineFollow>();
            virtualCamera.Lens.FieldOfView = normalFOV;
        }
        
        // Validate references
        if (player == null) player = transform;
        if (cameraPivot == null) Debug.LogWarning("Camera Pivot not assigned!");
        if (virtualCamera == null) Debug.LogWarning("Virtual Camera not assigned!");
    }
    
    void Update()
    {
        HandleMouseLook();
        HandleAiming();
        HandleCursorToggle();
    }
    
    void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        if (invertY) mouseY = -mouseY;
        
        // Rotate player horizontally
        player.Rotate(Vector3.up * mouseX);
        
        // Rotate camera pivot vertically
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalLookLimit, verticalLookLimit);
        
        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }
    
    void HandleAiming()
    {
        // Check for right mouse button (aim)
        bool aimInput = Input.GetMouseButton(1);
        
        if (aimInput != isAiming)
        {
            isAiming = aimInput;
            UpdateCrosshair();
        }
        
        // Smooth FOV transition
        if (virtualCamera != null)
        {
            float targetFOV = isAiming ? aimFOV : normalFOV;
            virtualCamera.Lens.FieldOfView = Mathf.Lerp(
                virtualCamera.Lens.FieldOfView,
                targetFOV,
                Time.deltaTime * aimSpeed
            );
        }
    }
    
    void UpdateCrosshair()
    {
        if (crosshair != null)
        {
            crosshair.SetActive(!isAiming);
        }
    }
    
    void HandleCursorToggle()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = !isLocked;
        }
    }
    
    // Public methods for other scripts
    public bool IsAiming => isAiming;
    
    public Vector3 GetCameraForward()
    {
        if (virtualCamera != null)
            return virtualCamera.transform.forward;
        if (cameraPivot != null)
            return cameraPivot.forward;
        return transform.forward;
    }
    
    public Transform GetCameraTransform()
    {
        if (virtualCamera != null)
            return virtualCamera.transform;
        if (cameraPivot != null)
            return cameraPivot;
        return transform;
    }
    
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }
}