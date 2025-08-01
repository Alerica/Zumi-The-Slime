using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 10;
    public int currentHealth { get; private set; }
    public bool IsDead { get; private set; } = false;

    public event Action<int, int> OnHealthChanged;

    [Header("Health Bar")]
    public EnemyHealthBar healthBar; // Assign in inspector or via code

    private void Awake()
    {
        currentHealth = maxHealth;
        if (healthBar != null)
            healthBar.SetHealth((float)currentHealth / maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (healthBar != null)
            healthBar.SetHealth((float)currentHealth / maxHealth);

        if (currentHealth == 0)
        {
            Die();
        }
    }

    private void Die()
    {
        IsDead = true;
        Destroy(gameObject);
    }
}
