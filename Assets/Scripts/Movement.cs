using UnityEngine;
using System.Collections;

public class ImprovedFrogMovement : MonoBehaviour
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

    [Header("Hop Settings")]
    public float hopForce = 5f;
    public float hopCooldown = 0.1f;
    public float maxHopSpeed = 8f;

    [Header("Dodge Settings")]
    public float dodgeForce = 12f;             
    public float dodgeDuration = 0.5f;         
    public float dodgeCooldown = 1f;           
    public Collider dodgeCollider;

    [Header("Immunity Settings")]
    public bool isImmune = false;  // damage scripts can check this
    // [SerializeField] private Renderer playerRenderer;
    // [SerializeField] private Color immuneColor = Color.cyan;
    // [SerializeField] private float blinkSpeed = 10f;

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
    public AudioClip[] dodgeSounds;   

    private Rigidbody rb;
    private Vector3 moveDirection;
    private Vector3 smoothedMoveDirection;
    private Vector3 moveVelocity;
    private bool isGrounded;
    private bool wasGrounded;
    private bool canJump = true;
    private bool canHop = true;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float jumpTimer;

    private Vector3 originalScale;
    private bool isSquashing = false;
    private bool canRotateWithMovement = true;

    // Dodge internal state
    private bool canDodge = true;
    private bool isDodging = false;

    // Immunity internal state
    private Coroutine immunityCoroutine;
    private Coroutine blinkCoroutine;
    private Color originalColor;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;

        rb.freezeRotation = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (cameraController == null)
            cameraController = GetComponent<NewCamera>();

        if (groundCheck == null)
        {
            GameObject gc = new GameObject("GroundCheck");
            gc.transform.SetParent(transform);
            gc.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = gc.transform;
        }

        
    }

    void Update()
    {
        HandleInput();
        GroundCheck();
        HandleTimers();
        // HandleRotation();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleJumping();
        ApplyDrag();
        ApplyGravity();
        if (!isDodging && canRotateWithMovement && smoothedMoveDirection.sqrMagnitude > 0.01f && !cameraController.IsAiming())
        {
            Quaternion target = Quaternion.LookRotation(smoothedMoveDirection, Vector3.up);
            // Slerp in physics time
            Quaternion newRot = Quaternion.Slerp(rb.rotation, target, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRot);
        }

    }

    void HandleInput()
    {
        if(GameManager.Instance)
        {
            if (GameManager.Instance.lockedMovement) return; 
        }
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v).normalized;

        if (cameraController != null)
        {
            Transform ct = cameraController.GetCameraTransform();
            Vector3 f = ct.forward; f.y = 0; f.Normalize();
            Vector3 r = ct.right;   r.y = 0; r.Normalize();
            moveDirection = (f * inputDir.z + r * inputDir.x).normalized;
        }
        else
        {
            moveDirection = inputDir;
        }

        smoothedMoveDirection = Vector3.SmoothDamp(
            smoothedMoveDirection,
            moveDirection,
            ref moveVelocity,
            inputSmoothTime
        );

        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferTimer = jumpBufferTime;

        // DODGE with immunity
        if (Input.GetKeyDown(KeyCode.LeftShift)
            && canDodge
            && !isDodging)
        {
            StartCoroutine(Dodge());
        }
    }

    void GroundCheck()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded)
            coyoteTimer = coyoteTime;

        if (!wasGrounded && isGrounded)
            OnLanding();
    }

    void HandleTimers()
    {
        if (coyoteTimer > 0) coyoteTimer -= Time.deltaTime;
        if (jumpBufferTimer > 0) jumpBufferTimer -= Time.deltaTime;
        if (!canJump) jumpTimer -= Time.deltaTime;
        if (jumpTimer <= 0) canJump = true;
    }

    void HandleRotation()
    {
        if (cameraController != null
            && !cameraController.IsAiming()
            && smoothedMoveDirection.magnitude > 0.1f
            && canRotateWithMovement)
        {
            Quaternion target = Quaternion.LookRotation(smoothedMoveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                target,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    public void SetMovementRotationEnabled(bool enabled)
    {
        canRotateWithMovement = enabled;
    }

    void HandleMovement()
    {
        if (isDodging) return;

        if (smoothedMoveDirection.magnitude > 0.1f)
        {
            if (isGrounded)
            {
                if (canHop && rb.linearVelocity.magnitude < maxHopSpeed)
                {
                    Vector3 hop = smoothedMoveDirection * hopForce;
                    hop.y = hopForce * 0.3f;
                    rb.AddForce(hop, ForceMode.Impulse);
                    StartCoroutine(HopCooldown());

                    if (Mathf.Abs(smoothedMoveDirection.x) > Mathf.Abs(smoothedMoveDirection.z))
                        DoSquashStretch(0.9f, 1.1f, 0.05f);
                    else
                        DoSquashStretch(0.8f, 1.2f, 0.05f);
                }
                else
                {
                    Vector3 targetV = smoothedMoveDirection * moveSpeed * 0.3f;
                    targetV.y = rb.linearVelocity.y;
                    rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetV, Time.fixedDeltaTime * 10f);
                }
            }
            else
            {
                rb.AddForce(smoothedMoveDirection * moveSpeed * airControl * 10f, ForceMode.Force);
            }
        }
    }

    void HandleJumping()
    {
        if (jumpBufferTimer > 0 && coyoteTimer > 0 && canJump)
            Jump();
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
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -maxFallSpeed, rb.linearVelocity.z);
        }
    }

    void DoSquashStretch(float sx, float sy, float duration)
    {
        if (!isSquashing)
            StartCoroutine(SquashStretchCoroutine(sx, sy, duration));
    }

    IEnumerator SquashStretchCoroutine(float sx, float sy, float duration)
    {
        isSquashing = true;
        Vector3 target = new Vector3(sx, sy, sx);
        Vector3 start = transform.localScale;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, target, t / duration);
            yield return null;
        }

        t = 0f;
        start = transform.localScale;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, originalScale, t / duration);
            yield return null;
        }

        transform.localScale = originalScale;
        isSquashing = false;
    }

    IEnumerator HopCooldown()
    {
        canHop = false;
        yield return new WaitForSeconds(hopCooldown);
        canHop = true;
    }

    // ===== IMMUNITY SYSTEM BUILT INTO MOVEMENT =====
    
    /// <summary>
    /// Set immunity state - can be called by other scripts too
    /// </summary>
    public void SetImmunity(bool immune, float duration = 0f)
    {
        if (immunityCoroutine != null)
        {
            StopCoroutine(immunityCoroutine);
            immunityCoroutine = null;
        }

        isImmune = immune;
        Debug.Log($"Player immunity: {(immune ? "ON" : "OFF")}");

        if (immune && duration > 0)
        {
            immunityCoroutine = StartCoroutine(TemporaryImmunity(duration));
        }

    }

    private IEnumerator TemporaryImmunity(float duration)
    {
        yield return new WaitForSeconds(duration);
        isImmune = false;
        Debug.Log("Player immunity ended.");
    }


    IEnumerator Dodge()
    {
        canDodge = false;
        isDodging = true;
        
        PlayDodgeSound(); 

        // GRANT IMMUNITY DURING DODGE
        SetImmunity(true, dodgeDuration);

        canRotateWithMovement = false;

        Vector3 dir = smoothedMoveDirection.magnitude > 0.1f
                      ? smoothedMoveDirection.normalized
                      : transform.forward;

        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        Vector3 impulse = dir * dodgeForce + Vector3.up * (dodgeForce * 0.2f);
        rb.AddForce(impulse, ForceMode.Impulse);

        yield return new WaitForSeconds(dodgeDuration);
        

        canRotateWithMovement = true;
        isDodging = false;
        
        

        yield return new WaitForSeconds(dodgeCooldown);
        canDodge = true;
    }

    void PlayJumpSound()
    {
        if (audioSource && jumpSounds.Length > 0)
            audioSource.PlayOneShot(jumpSounds[Random.Range(0, jumpSounds.Length)],
                                     Random.Range(0.9f, 1.1f));
    }

    void PlayLandSound()
    {
        if (audioSource && landSounds.Length > 0)
            audioSource.PlayOneShot(landSounds[Random.Range(0, landSounds.Length)],
                                     Random.Range(0.8f, 1.0f));
    }
    
    void PlayDodgeSound()
    {
        if (audioSource != null && dodgeSounds.Length > 0)
            audioSource.PlayOneShot(
                dodgeSounds[Random.Range(0, dodgeSounds.Length)],
                Random.Range(0.9f, 1.1f)
            );
    }


    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }

    void OnDestroy()
    {
        if (immunityCoroutine != null) StopCoroutine(immunityCoroutine);
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
    }
}