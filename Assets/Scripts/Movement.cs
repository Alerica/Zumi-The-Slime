using UnityEngine;

public class FrogMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;
    public float airControl = 0.3f;
    public float groundDrag = 8f;
    public float airDrag = 2f;
    public float gravityMultiplier = 2f;
    public float maxFallSpeed = 20f;
    public float turnSpeed = 8f;
    public float inputSmoothTime = 0.1f;
    
    [Header("Jump Settings")]
    public float jumpCooldown = 0.3f;
    public float coyoteTime = 0.2f;
    public float jumpBufferTime = 0.2f;
    public AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("Hop Settings")]
    public float hopForce = 5f;
    public float hopCooldown = 0.1f;
    public float maxHopSpeed = 8f;
    
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] jumpSounds;
    public AudioClip[] landSounds;
    public AudioClip[] hopSounds;
    
    // Private variables
    private Rigidbody rb;
    private Vector3 moveDirection;
    private Vector3 worldMoveDirection;
    private bool isGrounded;
    private bool wasGrounded;
    private bool canJump = true;
    private bool canHop = true;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float jumpTimer;
    private bool jumpPressed;
    
    private Vector3 originalScale;
    private bool isSquashing = false;
    private float squashTimer = 0f;
    private const float squashDuration = 0.15f;
    private const float stretchDuration = 0.1f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;
        
        rb.freezeRotation = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
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
        HandleVisualEffects();


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
        
        moveDirection = new Vector3(horizontal, 0, vertical).normalized;
        
        if (moveDirection.magnitude > 0.1f)
        {
            worldMoveDirection = transform.TransformDirection(moveDirection);
            worldMoveDirection.y = 0; 
            worldMoveDirection.Normalize();
        }
        else
        {
            worldMoveDirection = Vector3.zero;
        }
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpPressed = true;
            jumpBufferTimer = jumpBufferTime;
        }
        
        if (Input.GetKeyUp(KeyCode.Space))
        {
            jumpPressed = false;
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
        
        if (wasGrounded && !isGrounded)
        {
            OnLeftGround();
        }
    }
    
    void HandleTimers()
    {
        if (coyoteTimer > 0) coyoteTimer -= Time.deltaTime;
        if (jumpBufferTimer > 0) jumpBufferTimer -= Time.deltaTime;
        if (!canJump) jumpTimer -= Time.deltaTime;
        if (jumpTimer <= 0) canJump = true;
    }
    
    void HandleMovement()
    {
        float currentMoveSpeed = isGrounded ? moveSpeed : moveSpeed * airControl;
        
        if (worldMoveDirection.magnitude > 0.1f)
        {
            if (isGrounded)
            {
                if (canHop && rb.linearVelocity.magnitude < maxHopSpeed && moveDirection.magnitude > 0.1f)
                {
                    Vector3 hopDirection = Vector3.zero;
                    
                    if (Mathf.Abs(moveDirection.z) > 0.1f)
                    {
                        hopDirection += transform.forward * moveDirection.z * hopForce;
                    }
                    
                    if (Mathf.Abs(moveDirection.x) > 0.1f)
                    {
                        hopDirection += transform.right * moveDirection.x * hopForce;
                    }
                    
                    hopDirection.y = hopForce * 0.3f;
                    
                    rb.AddForce(hopDirection, ForceMode.Impulse);
                    
                    StartCoroutine(HopCooldown());
                    PlayHopSound();
                    
                    if (Mathf.Abs(moveDirection.x) > Mathf.Abs(moveDirection.z))
                    {
                        DoSquashStretch(0.9f, 1.1f, 0.05f);
                    }
                    else
                    {
                        DoSquashStretch(0.8f, 1.2f, 0.05f);
                    }
                }
                else if (moveDirection.magnitude > 0.1f)
                {
                    Vector3 targetVelocity = Vector3.zero;
                    
                    if (Mathf.Abs(moveDirection.z) > 0.1f)
                    {
                        targetVelocity += transform.forward * moveDirection.z * currentMoveSpeed * 0.3f; // Reduced for hop-based movement
                    }
                    
                    if (Mathf.Abs(moveDirection.x) > 0.1f)
                    {
                        targetVelocity += transform.right * moveDirection.x * currentMoveSpeed * 0.3f; // Reduced for hop-based movement
                    }
                    
                    targetVelocity.y = rb.linearVelocity.y;
                    rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 10f);
                }
            }
            else
            {
                if (Mathf.Abs(moveDirection.z) > 0.1f)
                {
                    rb.AddForce(transform.forward * moveDirection.z * currentMoveSpeed * 10f);
                }
                
                if (Mathf.Abs(moveDirection.x) > 0.1f)
                {
                    rb.AddForce(transform.right * moveDirection.x * currentMoveSpeed * 10f);
                }
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
        
        Vector3 jumpDirection = Vector3.up * jumpForce;
        
        if (Mathf.Abs(moveDirection.z) > 0.1f)
        {
            jumpDirection += transform.forward * moveDirection.z * (jumpForce * 0.3f);
        }
        
        rb.AddForce(jumpDirection, ForceMode.Impulse);
        
        jumpBufferTimer = 0;
        coyoteTimer = 0;
        canJump = false;
        jumpTimer = jumpCooldown;
        
        DoSquashStretch(0.6f, 1.4f, stretchDuration);
        PlayJumpSound();
    }
    
    void OnLanding()
    {
        canJump = true;
        DoSquashStretch(1.3f, 0.7f, squashDuration);
        PlayLandSound();
        
        if (rb.linearVelocity.y < -5f)
        {
            rb.AddForce(Vector3.up * 2f, ForceMode.Impulse);
        }
    }
    
    void OnLeftGround()
    {
        DoSquashStretch(0.9f, 1.1f, 0.05f);
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
        
        // Return to original scale
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
    
    System.Collections.IEnumerator HopCooldown()
    {
        canHop = false;
        yield return new WaitForSeconds(hopCooldown);
        canHop = true;
    }
    
    void HandleVisualEffects()
    {
        Vector3 currentEuler = transform.rotation.eulerAngles;
        
        float targetX = 0f;
        float targetZ = 0f;
        
        if (isGrounded && Mathf.Abs(moveDirection.z) > 0.1f && rb.linearVelocity.magnitude > 2f)
        {
            targetX = Mathf.Clamp(moveDirection.z * 5f, -10f, 10f); // Max 10 degrees lean
        }
        
        currentEuler.x = Mathf.LerpAngle(currentEuler.x, targetX, Time.deltaTime * 10f);
        currentEuler.z = Mathf.LerpAngle(currentEuler.z, targetZ, Time.deltaTime * 10f);
        
        transform.rotation = Quaternion.Euler(currentEuler);
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
    
    void PlayHopSound()
    {
        if (audioSource && hopSounds.Length > 0)
        {
            AudioClip clip = hopSounds[Random.Range(0, hopSounds.Length)];
            audioSource.pitch = Random.Range(1.0f, 1.3f);
            audioSource.volume = 0.3f;
            audioSource.PlayOneShot(clip);
        }
    }
    void TurnAround()
    {
        transform.Rotate(0f, 180f, 0f, Space.Self);
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