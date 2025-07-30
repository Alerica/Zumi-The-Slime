using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class BossController : MonoBehaviour
{
    public float followSpeed = 3f;
    public float stopDistance = 5f;
    public float dashSpeed = 10f;
    public float dashDuration = 0.5f;

    [SerializeField] private GameObject dashIndicator;
    [SerializeField] private float indicatorTime = 0.5f;
    [SerializeField] private float backDistance = 2f;
    [SerializeField] private float backDuration = 0.3f;
    public float dashCooldown = 2f;

    public int maxHealth = 100;
    private int currentHealth;

    public int damagePerBall = 1;
    public int damagePerCombo = 1;

    private Transform player;
    private bool isAttacking;
    private float lastDashTime = -Mathf.Infinity;

    private SplineBallSpawner spawner;

    void Start()
    {
        currentHealth = maxHealth;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        spawner = GetComponentInChildren<SplineBallSpawner>();
        if (spawner != null)
            spawner.OnShotReport += HandleShotReport;
    }

    void Update()
    {
        if (player == null || isAttacking) return;

        float dist = Vector3.Distance(transform.position, player.position);
        bool canDash = Time.time >= lastDashTime + dashCooldown;

        if (dist <= stopDistance && canDash)
        {
            StartCoroutine(AttackSequence());
        }
        else
        {
            Vector3 dir = (player.position - transform.position).normalized;
            transform.position += dir * followSpeed * Time.deltaTime;
        }
    }

    IEnumerator AttackSequence()
    {
        isAttacking = true;
        lastDashTime = Time.time;

        Vector3 dir = (player.position - transform.position).normalized;

        if (dashIndicator != null)
            dashIndicator.SetActive(true);

        yield return new WaitForSeconds(indicatorTime);

        if (dashIndicator != null)
            dashIndicator.SetActive(false);

        float elapsed = 0f;
        while (elapsed < backDuration)
        {
            transform.position += -dir * (backDistance / backDuration) * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < dashDuration)
        {
            transform.position += dir * dashSpeed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }

        isAttacking = false;
    }

    private void HandleShotReport(int destroyedCount, int comboCount, List<int> enteredTypes)
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
        if (currentHealth == 0) Die();
    }

    private void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    private void Die()
    {
        Debug.Log("Boss died!");
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (spawner != null)
            spawner.OnShotReport -= HandleShotReport;
    }
}
