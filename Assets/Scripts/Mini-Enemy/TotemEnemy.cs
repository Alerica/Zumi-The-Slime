using System.Collections;
using UnityEngine;

public class TotemEnemy : MonoBehaviour
{
    private Transform target;
    public float jumpCooldown = 1.2f;
    public float jumpForce = 6f;
    public float smashJumpForce = 60f;
    public float smashRange = 10f;
    public int smashDamage = 1;
    public GameObject smashIndicatorPrefab;

    private Rigidbody rb;
    private float cooldownTimer;
    private bool isSmashing = false;
    [SerializeField] float extraGravity = 20f;

    SmashState smashState = SmashState.Idle;

    public TotemProjectile totemProjectile;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cooldownTimer = jumpCooldown;
        target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (target == null) return;

        float distance = Vector3.Distance(transform.position, target.position);
        cooldownTimer -= Time.deltaTime;
        
        RotateTowardsTarget();

        if (cooldownTimer <= 0f)
        {
            if (distance <= smashRange)
            {
                StartCoroutine(SmashAttack());
            }
            else
            {
                if (totemProjectile) totemProjectile.Shoot();
                else Debug.LogWarning("TotemProjectile not assigned in TotemEnemy.");


                if (!isSmashing) JumpTowards(target.position);
            }
            cooldownTimer = jumpCooldown;
        }
    }

    void FixedUpdate()
    {
        if (isSmashing)
            rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
    }

    void JumpTowards(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        Vector3 jumpVector = new Vector3(dir.x, 1f, dir.z).normalized * jumpForce;
        rb.AddForce(jumpVector, ForceMode.VelocityChange);
    }

    IEnumerator SmashAttack()
    {
        if (isSmashing) yield break;
        isSmashing = true;
        smashState = SmashState.Anticipating;

        // Anticipation delay
        yield return new WaitForSeconds(0.3f);

        GameObject instance = null;
        if (smashIndicatorPrefab)
        {
            Vector3 groundPos = new Vector3(transform.position.x, GetGroundY(), transform.position.z);
            instance = Instantiate(smashIndicatorPrefab, groundPos, Quaternion.identity);
            instance.transform.localScale = new Vector3(smashRange, 0.01f, smashRange);
        }

        yield return new WaitForSeconds(1.2f);

        // Jump
        rb.AddForce(Vector3.up * smashJumpForce, ForceMode.VelocityChange);
        smashState = SmashState.Jumping;

        // Wait until falling
        yield return new WaitUntil(() => rb.linearVelocity.y < -0.1f);
        smashState = SmashState.Falling;

        // Wait until grounded
        yield return new WaitUntil(() =>
        smashState == SmashState.Falling &&
        Mathf.Abs(rb.linearVelocity.y) < 0.05f && rb.linearVelocity.y <= 0f);


        DealSmashDamage();

        if (instance) Destroy(instance, 2.5f);

        yield return new WaitForSeconds(0.5f);
        isSmashing = false;
        smashState = SmashState.Idle;
    }


    void DealSmashDamage()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            float dist = Vector3.Distance(player.transform.position, transform.position);
            if (dist <= (smashRange - 3))
            {
                SlimeHealth health = player.GetComponentInChildren<SlimeHealth>();
                if (health != null)
                {
                    Debug.Log($"Dealing {smashDamage} damage to player from TotemEnemy.");
                    health.TakeDamage(smashDamage);
                }

            }
        }
        else
        {
            Debug.LogWarning("Player not found for TotemEnemy smash damage.");
        }
    }


    float GetGroundY()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 10f))
        {
            return hit.point.y + 0.01f;
        }
        return transform.position.y - 1f;
    }
    
    void RotateTowardsTarget()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0f; 
        if (direction.magnitude > 0.1f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }


}

public enum SmashState
{
    Idle,
    Anticipating,
    Jumping,
    Falling,
    Landed
}

