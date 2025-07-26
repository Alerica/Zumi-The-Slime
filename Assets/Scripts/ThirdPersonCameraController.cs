using UnityEngine;
using Unity.Cinemachine;

public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("Mouse Look Settings")]
    [SerializeField] float mouseSensitivity  = 2f;
    [SerializeField] float verticalLookLimit = 80f;
    [SerializeField] bool  invertY          = false;

    [Header("References (assign in Inspector)")]
    [Tooltip("A child Transform placed at roughly head height")]
    [SerializeField] Transform                cameraPivot;
    [Tooltip("Your Cinemachine Virtual Camera")]
    [SerializeField] CinemachineVirtualCamera virtualCamera;
    [Tooltip("Your Crosshair UI GameObject")]
    [SerializeField] GameObject               crosshairUI;

    float verticalRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        if (cameraPivot      == null) Debug.LogWarning("Camera Pivot not set!", this);
        if (virtualCamera    == null) Debug.LogWarning("Cinemachine VirtualCamera not set!", this);
        if (crosshairUI      == null) Debug.LogWarning("Crosshair UI not set!",   this);

        if (virtualCamera != null && cameraPivot != null)
        {
            virtualCamera.Follow = cameraPivot;
            virtualCamera.LookAt = cameraPivot;
        }
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * (invertY ? -1f : 1f);

        transform.Rotate(Vector3.up * mouseX, Space.Self);

        verticalRotation -= mouseY;
        verticalRotation  = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);
        cameraPivot.localEulerAngles = new Vector3(verticalRotation, 0f, 0f);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible   = !locked;
        }
    }

    public Vector3 GetCameraForward()
        => Camera.main != null ? Camera.main.transform.forward : cameraPivot.forward;

    public Transform GetCameraTransform()
        => Camera.main != null ? Camera.main.transform : cameraPivot;
}
