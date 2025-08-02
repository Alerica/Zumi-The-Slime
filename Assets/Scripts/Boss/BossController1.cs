using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class BossController1 : MonoBehaviour
{
    [Header("Boss Stats")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private int damagePerBall = 5;
    [SerializeField] private int damagePerCombo = 10;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float stoppingDistance = 8f;

    [Header("Dash Attack Settings")]
    [SerializeField] private float dashDistance = 15f;
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashWindupTime = 1.5f;
    [SerializeField] private float dashCooldown = 3f;
    [SerializeField] private float minDashDistance = 10f;
    [SerializeField] private AnimationCurve dashSpeedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Jump Attack Settings")]
    [SerializeField] private float jumpHeight = 10f;
    [SerializeField] private float jumpDuration = 2f;
    [SerializeField] private float jumpWindupTime = 1.5f;
    [SerializeField] private float jumpCooldown = 5f;
    [SerializeField] private float jumpSlamRadius = 5f;
    [SerializeField] private float minJumpDistance = 8f;
    [SerializeField] private AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Laser Attack Settings")]
    [SerializeField] private Transform laserOrigin;
    [SerializeField] private float laserChargeTime = 2f;
    [SerializeField] private float laserDuration = 3f;
    [SerializeField] private float laserCooldown = 6f;
    [SerializeField] private float laserMaxDistance = 50f;
    [SerializeField] private float laserDamagePerSecond = 10f;
    [SerializeField] private float minLaserDistance = 5f;
    [SerializeField] private float maxLaserDistance = 20f;
    [SerializeField] private LayerMask laserHitLayers = -1;
    [SerializeField] private float laserWidth = 0.5f;
    [SerializeField] private AnimationCurve laserWidthCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Attack Indicators")]
    [SerializeField] private GameObject dashIndicatorPrefab;
    [SerializeField] private GameObject jumpIndicatorPrefab;
    [SerializeField] private Color indicatorStartColor = new Color(1f, 0.5f, 0f, 0.3f);
    [SerializeField] private Color indicatorEndColor = new Color(1f, 0f, 0f, 0.8f);
    [SerializeField] private float indicatorPulseSpeed = 2f;

    [Header("Colliders")]
    [SerializeField] private Collider normalCollider;
    [SerializeField] private Collider dashCollider;
    [SerializeField] private Collider jumpCollider;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject awakeTriggerBoundary;
    [SerializeField] private LayerMask groundLayer = -1;
    [SerializeField] private Animator animatorOverride;
    [SerializeField] private SplineBallSpawner splineSpawner;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem dashChargeEffect;
    [SerializeField] private ParticleSystem jumpChargeEffect;
    [SerializeField] private ParticleSystem landingEffect;
    [SerializeField] private TrailRenderer dashTrail;
    [SerializeField] private LineRenderer laserLineRenderer;
    [SerializeField] private ParticleSystem laserChargeParticles;
    [SerializeField] private ParticleSystem laserImpactParticles;
    [SerializeField] private Light laserLight;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dashWindupSound;
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private AudioClip jumpWindupSound;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip laserChargeSound;
    [SerializeField] private AudioClip laserFireSound;

    [Header("other")] 
    [SerializeField] private float awakeTime = 3f; 

    private Rigidbody rb;
    private Animator animator;
    private BossState currentState = BossState.Dormant;
    private float lastDashTime;
    private float lastJumpTime;
    private float lastLaserTime;
    private bool isAwake = false;
    private bool isPerformingAttack = false;
    private GameObject currentDashIndicator;
    private GameObject currentJumpIndicator;
    private Vector3 jumpTargetPosition;
    private Coroutine currentAttackCoroutine;
    private bool isFiringLaser = false;
    private Vector3 laserEndPoint;
    private LaserDamageZone laserDamageZone;

    private enum BossState
    {
        Dormant,
        Awakening,
        Idle,
        Walking,
        DashStart,
        DashOngoing,
        DashEnd,
        JumpStart,
        JumpOngoing,
        JumpEnd,
        LaserCharge,
        LaserFire,
        Die
    }

    private enum ColliderType
    {
        Normal,
        Dash,
        Jump
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = animatorOverride != null
            ? animatorOverride
            : GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        currentHealth = maxHealth;
        if (splineSpawner != null) splineSpawner.OnShotReport += HandleShotReport;
        if (laserLineRenderer == null && laserOrigin != null)
        {
            GameObject laserObj = new GameObject("LaserBeam");
            laserObj.transform.SetParent(laserOrigin);
            laserObj.transform.localPosition = Vector3.zero;
            laserLineRenderer = laserObj.AddComponent<LineRenderer>();
            SetupLaserVisual();
        }
        laserDamageZone = GetComponent<LaserDamageZone>() ?? gameObject.AddComponent<LaserDamageZone>();
        laserDamageZone.SetDamagePerSecond(laserDamagePerSecond);
        laserDamageZone.SetBeamRadius(laserWidth * 0.5f);
        SetActiveCollider(ColliderType.Normal);
        SetState(BossState.Dormant);
    }

    private void Update()
    {
        if (!isAwake || currentState == BossState.Die) return;

        if (!isPerformingAttack)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (ShouldPerformLaserAttack(distanceToPlayer))
                currentAttackCoroutine = StartCoroutine(PerformLaserAttack());
            else if (ShouldPerformJumpAttack(distanceToPlayer))
                currentAttackCoroutine = StartCoroutine(PerformJumpAttack());
            else if (ShouldPerformDashAttack(distanceToPlayer))
                currentAttackCoroutine = StartCoroutine(PerformDashAttack());
            else if (distanceToPlayer > stoppingDistance)
                MoveTowardsPlayer();
            else
                SetState(BossState.Idle);
        }

        if (isFiringLaser && laserOrigin != null)
            UpdateLaserVisual();

        if (currentState != BossState.Dormant && currentState != BossState.Die)
            FacePlayer();
    }

    private void MoveTowardsPlayer()
    {
        if (currentState != BossState.Walking) SetState(BossState.Walking);
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        rb.MovePosition(transform.position + direction * walkSpeed * Time.deltaTime);
    }

    private void FacePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private bool ShouldPerformDashAttack(float distance)
    {
        return distance >= minDashDistance
            && Time.time - lastDashTime >= dashCooldown
            && currentHealth > maxHealth / 2;
    }

    private bool ShouldPerformJumpAttack(float distance)
    {
        return distance >= minJumpDistance
            && Time.time - lastJumpTime >= jumpCooldown
            && currentHealth <= maxHealth / 2;
    }

    private bool ShouldPerformLaserAttack(float distance)
    {
        return distance >= minLaserDistance
            && distance <= maxLaserDistance
            && Time.time - lastLaserTime >= laserCooldown
            && currentHealth > 0;
    }

    private IEnumerator PerformLaserAttack()
    {
        isPerformingAttack = true;
        lastLaserTime = Time.time;
        SetState(BossState.LaserCharge);
        PlaySound(laserChargeSound);
        if (laserChargeParticles != null) laserChargeParticles.transform.position = laserOrigin.position;
        if (laserChargeParticles != null) laserChargeParticles.Play();
        if (laserLight != null)
        {
            laserLight.enabled = true;
            laserLight.intensity = 0;
        }
        float chargeTimer = 0;
        while (chargeTimer < laserChargeTime)
        {
            chargeTimer += Time.deltaTime;
            float t = chargeTimer / laserChargeTime;
            FacePlayer();
            if (laserLight != null) laserLight.intensity = Mathf.Lerp(0, 10, t);
            yield return null;
        }
        SetState(BossState.LaserFire);
        PlaySound(laserFireSound);
        isFiringLaser = true;
        if (laserChargeParticles != null) laserChargeParticles.Stop();
        if (laserLineRenderer != null) laserLineRenderer.enabled = true;
        if (laserDamageZone != null) laserDamageZone.ActivateLaser(true);
        float fireTimer = 0;
        while (fireTimer < laserDuration)
        {
            fireTimer += Time.deltaTime;
            CheckLaserHit();
            yield return null;
        }
        isFiringLaser = false;
        if (laserDamageZone != null) laserDamageZone.ActivateLaser(false);
        if (laserLineRenderer != null) laserLineRenderer.enabled = false;
        if (laserLight != null) laserLight.enabled = false;
        if (laserImpactParticles != null) laserImpactParticles.Stop();
        yield return new WaitForSeconds(0.5f);
        SetState(BossState.Idle);
        isPerformingAttack = false;
    }

    private void SetupLaserVisual()
    {
        // Temporary to make it easier to edit 
        if (laserLineRenderer == null) return;
        laserLineRenderer.startWidth = laserWidth;
        laserLineRenderer.endWidth = laserWidth * 0.8f;
        laserLineRenderer.positionCount = 2;
        laserLineRenderer.enabled = false;
        if (laserLineRenderer.material == null)
        {
            Material laserMat = new Material(Shader.Find("Sprites/Default"));
            laserMat.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            laserMat.SetFloat("_Mode", 3);
            laserMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            laserMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            laserMat.renderQueue = 3000;
            laserLineRenderer.material = laserMat;
        }
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(new Color(1f, 0.3f, 0.3f), 0.5f),
                new GradientColorKey(new Color(1f, 0.1f, 0.1f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.8f, 1.0f)
            }
        );
        laserLineRenderer.colorGradient = gradient;
    }

    private void UpdateLaserVisual()
    {
        // Temporary to make it easier to edit 
        if (laserLineRenderer == null || laserOrigin == null) return;
        Vector3 origin = laserOrigin.position;
        Vector3 direction = laserOrigin.forward;
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, laserMaxDistance, laserHitLayers))
        {
            laserEndPoint = hit.point;
            if (laserImpactParticles != null)
            {
                laserImpactParticles.transform.position = hit.point;
                laserImpactParticles.transform.rotation = Quaternion.LookRotation(hit.normal);
                if (!laserImpactParticles.isPlaying) laserImpactParticles.Play();
            }
        }
        else
        {
            laserEndPoint = origin + direction * laserMaxDistance;
            if (laserImpactParticles != null) laserImpactParticles.Stop();
        }
        laserLineRenderer.SetPosition(0, origin);
        laserLineRenderer.SetPosition(1, laserEndPoint);
        float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.1f;
        laserLineRenderer.startWidth = laserWidth * pulse;
        laserLineRenderer.endWidth = laserWidth * 0.8f * pulse;
    }

    private void CheckLaserHit()
    {
        if (laserOrigin == null || player == null) return;
        Vector3 origin = laserOrigin.position;
        Vector3 direction = laserOrigin.forward;
        if (laserDamageZone != null)
            laserDamageZone.UpdateLaserDamage(origin, direction, laserMaxDistance);
        else
        {
            RaycastHit hit;
            if (Physics.SphereCast(origin, laserWidth * 0.5f, direction, out hit, laserMaxDistance, laserHitLayers))
                if (hit.collider.CompareTag("Player"))
                    Debug.Log($"Laser hit player! Would deal {laserDamagePerSecond * Time.deltaTime} damage");
        }
    }

    // private IEnumerator PerformDashAttack()
    // {
    //     isPerformingAttack = true;
    //     lastDashTime = Time.time;
    //     SetState(BossState.DashStart);
    //     Vector3 dashDirection = (player.position - transform.position).normalized;
    //     dashDirection.y = 0;
    //     CreateDashIndicator(dashDirection);
    //     PlaySound(dashWindupSound);
    //     if (dashChargeEffect != null) dashChargeEffect.Play();
    //     float windupTimer = 0;
    //     while (windupTimer < dashWindupTime)
    //     {
    //         windupTimer += Time.deltaTime;
    //         if (currentDashIndicator != null)
    //             UpdateIndicatorColor(currentDashIndicator, windupTimer / dashWindupTime);
    //         FacePlayer();
    //         dashDirection = (player.position - transform.position).normalized;
    //         dashDirection.y = 0;
    //         yield return null;
    //     }
    //     SetState(BossState.DashOngoing);
    //     SetActiveCollider(ColliderType.Dash);
    //     DestroyIndicator(currentDashIndicator);
    //     PlaySound(dashSound);
    //     if (dashTrail != null) dashTrail.enabled = true;
    //     Vector3 startPos = transform.position;
    //     Vector3 endPos = startPos + dashDirection * dashDistance;
    //     float dashTimer = 0;
    //     float dashTime = dashDistance / dashSpeed;
    //     while (dashTimer < dashTime)
    //     {
    //         dashTimer += Time.deltaTime;
    //         transform.position = Vector3.Lerp(startPos, endPos, dashSpeedCurve.Evaluate(dashTimer / dashTime));
    //         yield return null;
    //     }
    //     SetState(BossState.DashEnd);
    //     SetActiveCollider(ColliderType.Normal);
    //     if (dashTrail != null) dashTrail.enabled = false;
    //     yield return new WaitForSeconds(0.5f);
    //     SetState(BossState.Idle);
    //     isPerformingAttack = false;
    // }
    //
    
    private IEnumerator PerformDashAttack()
{
    isPerformingAttack = true;
    lastDashTime = Time.time;
    SetState(BossState.DashStart);
    
    // Lock the dash direction at the start - no more tracking after this point
    Vector3 dashDirection = (player.position - transform.position).normalized;
    dashDirection.y = 0;
    
    // Store the locked rotation for the dash
    Quaternion lockedRotation = Quaternion.LookRotation(dashDirection);
    transform.rotation = lockedRotation; // Face the direction immediately
    
    CreateDashIndicator(dashDirection);
    PlaySound(dashWindupSound);
    if (dashChargeEffect != null) dashChargeEffect.Play();
    
    float windupTimer = 0;
    while (windupTimer < dashWindupTime)
    {
        windupTimer += Time.deltaTime;
        if (currentDashIndicator != null)
            UpdateIndicatorColor(currentDashIndicator, windupTimer / dashWindupTime);
        
        // Keep the boss locked in the dash direction - no more FacePlayer() calls
        transform.rotation = lockedRotation;
        
        yield return null;
    }
    
    SetState(BossState.DashOngoing);
    SetActiveCollider(ColliderType.Dash);
    DestroyIndicator(currentDashIndicator);
    PlaySound(dashSound);
    if (dashTrail != null) dashTrail.enabled = true;
    
    Vector3 startPos = transform.position;
    Vector3 endPos = startPos + dashDirection * dashDistance;
    float dashTimer = 0;
    float dashTime = dashDistance / dashSpeed;
    
    while (dashTimer < dashTime)
    {
        // dashTimer += Time.deltaTime;
        // transform.position = Vector3.Lerp(startPos, endPos, dashSpeedCurve.Evaluate(dashTimer / dashTime));
        // // Keep rotation locked during dash execution too
        // transform.rotation = lockedRotation;
        // yield return null;
        dashTimer += Time.deltaTime;
        float t = dashSpeedCurve.Evaluate(dashTimer / dashTime);
        Vector3 targetPos = Vector3.Lerp(startPos, endPos, t);
        rb.MovePosition(targetPos);
        yield return null;
    }
    
    SetState(BossState.DashEnd);
    SetActiveCollider(ColliderType.Normal);
    if (dashTrail != null) dashTrail.enabled = false;
    
    yield return new WaitForSeconds(0.5f);
    SetState(BossState.Idle);
    isPerformingAttack = false;
}
    private IEnumerator PerformJumpAttack()
    {
        isPerformingAttack = true;
        lastJumpTime = Time.time;
        SetState(BossState.JumpStart);
        jumpTargetPosition = player.position;
        CreateJumpIndicator(jumpTargetPosition);
        PlaySound(jumpWindupSound);
        if (jumpChargeEffect != null) jumpChargeEffect.Play();
        float windupTimer = 0;
        while (windupTimer < jumpWindupTime)
        {
            windupTimer += Time.deltaTime;
            if (currentJumpIndicator != null)
            {
                float t = windupTimer / jumpWindupTime;
                UpdateIndicatorColor(currentJumpIndicator, t);
                currentJumpIndicator.transform.localScale = Vector3.one * jumpSlamRadius * 2f * (1f + Mathf.Sin(windupTimer * indicatorPulseSpeed) * 0.1f);
            }
            yield return null;
        }
        SetState(BossState.JumpOngoing);
        SetActiveCollider(ColliderType.Jump);
        PlaySound(jumpSound);
        Vector3 startPos = transform.position;
        Vector3 peakPos = new Vector3(jumpTargetPosition.x, startPos.y + jumpHeight, jumpTargetPosition.z);
        Vector3 endPos = new Vector3(jumpTargetPosition.x, startPos.y, jumpTargetPosition.z);
        float jumpTimer = 0;
        while (jumpTimer < jumpDuration)
        {
            jumpTimer += Time.deltaTime;
            float t = jumpTimer / jumpDuration;
            if (t < 0.5f)
                transform.position = Vector3.Lerp(startPos, peakPos, jumpCurve.Evaluate(t * 2f));
            else
            {
                transform.position = Vector3.Lerp(peakPos, endPos, jumpCurve.Evaluate((t - 0.5f) * 2f));
                if (currentJumpIndicator != null)
                    UpdateIndicatorColor(currentJumpIndicator, (t - 0.5f) * 2f);
            }
            yield return null;
        }
        DestroyIndicator(currentJumpIndicator);
        PlaySound(landSound);
        if (landingEffect != null)
        {
            landingEffect.transform.position = transform.position;
            landingEffect.Play();
        }
        SetState(BossState.JumpEnd);
        SetActiveCollider(ColliderType.Normal);
        yield return new WaitForSeconds(0.5f);
        SetState(BossState.Idle);
        isPerformingAttack = false;
    }

    private void CreateDashIndicator(Vector3 direction)
    {
        if (dashIndicatorPrefab == null) return;
        currentDashIndicator = Instantiate(dashIndicatorPrefab, transform.position, Quaternion.identity);
        LineRenderer line = currentDashIndicator.GetComponent<LineRenderer>();
        if (line != null)
        {
            line.SetPosition(0, transform.position + Vector3.up * 0.5f);
            line.SetPosition(1, transform.position + direction * dashDistance + Vector3.up * 0.5f);
        }
    }

    private void CreateJumpIndicator(Vector3 position)
    {
        if (jumpIndicatorPrefab == null) return;
        currentJumpIndicator = Instantiate(jumpIndicatorPrefab, position + Vector3.up * 0.1f, Quaternion.Euler(90, 0, 0));
        currentJumpIndicator.transform.localScale = Vector3.one * jumpSlamRadius * 2f;
    }

    private void UpdateIndicatorColor(GameObject indicator, float t)
    {
        if (indicator == null) return;
        Renderer renderer = indicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color color = Color.Lerp(indicatorStartColor, indicatorEndColor, t);
            renderer.material.color = color;
        }
        LineRenderer line = indicator.GetComponent<LineRenderer>();
        if (line != null)
        {
            Color color = Color.Lerp(indicatorStartColor, indicatorEndColor, t);
            line.startColor = color;
            line.endColor = color;
        }
    }

    private void DestroyIndicator(GameObject indicator)
    {
        if (indicator != null) Destroy(indicator);
    }

    private void SetActiveCollider(ColliderType type)
    {
        if (normalCollider != null) normalCollider.enabled = type == ColliderType.Normal;
        if (dashCollider != null) dashCollider.enabled = type == ColliderType.Dash;
        if (jumpCollider != null) jumpCollider.enabled = type == ColliderType.Jump;
    }

    private void SetState(BossState newState)
    {
        currentState = newState;
        animator.SetBool("Wake", false);
        animator.SetBool("Idle", false);
        animator.SetBool("Walk", false);
        animator.SetBool("DashStart", false);
        animator.SetBool("DashOngoing", false);
        animator.SetBool("DashEnd", false);
        animator.SetBool("JumpStart", false);
        animator.SetBool("JumpOngoing", false);
        animator.SetBool("JumpEnd", false);
        animator.SetBool("LaserCharge", false);
        animator.SetBool("LaserFire", false);
        animator.SetBool("Die", false);
        switch (newState)
        {
            case BossState.Dormant:
            case BossState.Idle:
                animator.SetBool("Idle", true);
                break;
            case BossState.Awakening:
                animator.SetBool("Wake", true);
                break;
            case BossState.Walking:
                animator.SetBool("Walk", true);
                break;
            case BossState.DashStart:
                animator.SetBool("DashStart", true);
                break;
            case BossState.DashOngoing:
                animator.SetBool("DashOngoing", true);
                break;
            case BossState.DashEnd:
                animator.SetBool("DashEnd", true);
                break;
            case BossState.JumpStart:
                animator.SetBool("JumpStart", true);
                break;
            case BossState.JumpOngoing:
                animator.SetBool("JumpOngoing", true);
                break;
            case BossState.JumpEnd:
                animator.SetBool("JumpEnd", true);
                break;
            case BossState.LaserCharge:
                animator.SetBool("LaserCharge", true);
                break;
            case BossState.LaserFire:
                animator.SetBool("LaserFire", true);
                break;
            case BossState.Die:
                animator.SetBool("Die", true);
                break;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    public void AwakeBoss()
    {
        if (!isAwake)
            StartCoroutine(AwakeSequence());
    }

    private IEnumerator AwakeSequence()
    {
        SetState(BossState.Awakening);
        yield return new WaitForSeconds(awakeTime);
        isAwake = true;
        SetState(BossState.Idle);
    }

    private void OnDestroy()
    {
        if (splineSpawner != null)
            splineSpawner.OnShotReport -= HandleShotReport;
    }

    public void HandleShotReport(int destroyedCount, int comboCount, List<int> enteredTypes)
    {
        int damage = destroyedCount * damagePerBall + comboCount * damagePerCombo;
        TakeDamage(damage);
        int healAmount = enteredTypes.Count;
        Heal(healAmount);
        Debug.Log($"Destroyed:{destroyedCount} Combos:{comboCount} DamageTaken:{damage} Healed:{healAmount} Health:{currentHealth}/{maxHealth}");
    }

    private void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);
        if (currentHealth == 0)
            Die();
    }

    private void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (currentState == BossState.Die) return;
        SetState(BossState.Die);
        if (currentAttackCoroutine != null)
            StopCoroutine(currentAttackCoroutine);
        DestroyIndicator(currentDashIndicator);
        DestroyIndicator(currentJumpIndicator);
        SetActiveCollider(ColliderType.Normal);
        isFiringLaser = false;
        if (laserLineRenderer != null)
            laserLineRenderer.enabled = false;
        if (laserLight != null)
            laserLight.enabled = false;
        if (laserImpactParticles != null)
            laserImpactParticles.Stop();
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minDashDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, minJumpDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
        if (player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position + Vector3.up, player.position + Vector3.up);
        }
    }
}
