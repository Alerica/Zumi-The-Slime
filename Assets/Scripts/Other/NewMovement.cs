using UnityEngine;

public class  NewMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;
    public float airControl = 0.8f;
    public float groundDrag = 8f;
    public float airDrag = 2f;
    public float gravityMultiplier = 2f;
    public float maxFallSpeed = 20f;
    public float rotationSpeed = 10f;
    public float inputSmoothTime = 0.1f;
    
    [Header("Jump Settings")]
    public float jumpCooldown = 0.3f;
    public float coyoteTime = 0.2f;
    public float jumpBufferTime = 0.2f;
    
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    
    [Header("Camera Reference")]
    public NewCamera cameraController;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] jumpSounds;
    public AudioClip[] landSounds;
    
    private Rigidbody rb;
    private Vector3 moveDirection;
    private Vector3 smoothedMoveDirection;
    private Vector3 moveVelocity;
    private bool isGrounded;
    private bool wasGrounded;
    private bool canJump = true;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float jumpTimer;
    
    private Vector3 originalScale;
    private bool isSquashing = false;
    private bool canRotateWithMovement = true;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;
        
        rb.freezeRotation = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        if (cameraController == null)
        {
            cameraController = GetComponent<NewCamera>();
        }
        
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = groundCheckObj.transform;
        }
    }
    
    void Update()
    {
        HandleInput();
        GroundCheck();
        HandleTimers();
        HandleRotation();
    }
    
    void FixedUpdate()
    {
        HandleMovement();
        HandleJumping();
        ApplyDrag();
        ApplyGravity();
    }
    
    void HandleInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        Vector3 inputDirection = new Vector3(horizontal, 0, vertical).normalized;
        
        if (cameraController != null)
        {
            Transform cameraTransform = cameraController.GetCameraTransform();
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();
            
            moveDirection = (cameraForward * inputDirection.z + cameraRight * inputDirection.x).normalized;
        }
        else
        {
            moveDirection = inputDirection;
        }
        
        smoothedMoveDirection = Vector3.SmoothDamp(smoothedMoveDirection, moveDirection, ref moveVelocity, inputSmoothTime);
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferTimer = jumpBufferTime;
        }
    }
    
    void GroundCheck()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        
        if (!wasGrounded && isGrounded)
        {
            OnLanding();
        }
    }
    
    void HandleTimers()
    {
        if (coyoteTimer > 0) coyoteTimer -= Time.deltaTime;
        if (jumpBufferTimer > 0) jumpBufferTimer -= Time.deltaTime;
        if (!canJump) jumpTimer -= Time.deltaTime;
        if (jumpTimer <= 0) canJump = true;
    }
    
    // void HandleRotation()
    // {
    //     if (cameraController != null && !cameraController.IsAiming() && smoothedMoveDirection.magnitude > 0.1f)
    //     {
    //         Quaternion targetRotation = Quaternion.LookRotation(smoothedMoveDirection);
    //         transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    //     }
    // }
    public void SetMovementRotationEnabled(bool enabled)
    {
        canRotateWithMovement = enabled;
    }
    
    void HandleRotation()
    {
        if (cameraController != null && !cameraController.IsAiming() && smoothedMoveDirection.magnitude > 0.1f && canRotateWithMovement)
        {
            Quaternion targetRotation = Quaternion.LookRotation(smoothedMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    void HandleMovement()
    {
        if (smoothedMoveDirection.magnitude > 0.1f)
        {
            float currentMoveSpeed = isGrounded ? moveSpeed : moveSpeed * airControl;
            Vector3 targetVelocity = smoothedMoveDirection * currentMoveSpeed;
            
            if (isGrounded)
            {
                Vector3 velocityChange = targetVelocity - new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }
            else
            {
                rb.AddForce(targetVelocity * 10f, ForceMode.Force);
            }
        }
    }
    
    void HandleJumping()
    {
        bool canCoyoteJump = coyoteTimer > 0;
        bool hasJumpBuffer = jumpBufferTimer > 0;
        
        if (hasJumpBuffer && canCoyoteJump && canJump)
        {
            Jump();
        }
    }
    
    void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        
        jumpBufferTimer = 0;
        coyoteTimer = 0;
        canJump = false;
        jumpTimer = jumpCooldown;
        
        DoSquashStretch(0.8f, 1.2f, 0.1f);
        PlayJumpSound();
    }
    
    void OnLanding()
    {
        canJump = true;
        DoSquashStretch(1.2f, 0.8f, 0.15f);
        PlayLandSound();
    }
    
    void ApplyDrag()
    {
        rb.linearDamping = isGrounded ? groundDrag : airDrag;
    }
    
    void ApplyGravity()
    {
        if (!isGrounded)
        {
            rb.AddForce(Vector3.down * gravityMultiplier * Physics.gravity.magnitude, ForceMode.Acceleration);
            
            if (rb.linearVelocity.y < -maxFallSpeed)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -maxFallSpeed, rb.linearVelocity.z);
            }
        }
    }
    
    void DoSquashStretch(float scaleX, float scaleY, float duration)
    {
        if (!isSquashing)
        {
            StartCoroutine(SquashStretchCoroutine(scaleX, scaleY, duration));
        }
    }
    
    System.Collections.IEnumerator SquashStretchCoroutine(float scaleX, float scaleY, float duration)
    {
        isSquashing = true;
        Vector3 targetScale = new Vector3(scaleX, scaleY, scaleX);
        Vector3 startScale = transform.localScale;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        elapsed = 0f;
        startScale = transform.localScale;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, originalScale, t);
            yield return null;
        }
        
        transform.localScale = originalScale;
        isSquashing = false;
    }
    
    void PlayJumpSound()
    {
        if (audioSource && jumpSounds.Length > 0)
        {
            AudioClip clip = jumpSounds[Random.Range(0, jumpSounds.Length)];
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clip);
        }
    }
    
    void PlayLandSound()
    {
        if (audioSource && landSounds.Length > 0)
        {
            AudioClip clip = landSounds[Random.Range(0, landSounds.Length)];
            audioSource.pitch = Random.Range(0.8f, 1.0f);
            audioSource.PlayOneShot(clip);
        }
    }
    
    
    
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
    
}