using UnityEngine;
using System;

public class SlimeHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 10;
    public int currentHealth { get; private set; }
    public bool IsDead { get; private set; } = false;

    public event Action<int, int> OnHealthChanged; // (current, max)

    [Header("Health UI")]
    public PlayerHealthUI healthUI;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (healthUI != null) healthUI.UpdateHealth((float)currentHealth / maxHealth);

        if (currentHealth == 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (healthUI != null) healthUI.UpdateHealth((float)currentHealth / maxHealth);
    }

    private void Die()
    {
        IsDead = true;
        Debug.Log($"{gameObject.name} has died.");

        if(GameManager.Instance.autoRevive) GameManager.Instance.KillPlayer();
    }

    public void Revive()
    {
        currentHealth = maxHealth;
        IsDead = false;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (healthUI != null)
            healthUI.UpdateHealth((float)currentHealth / maxHealth);

        Debug.Log($"{gameObject.name} has been revived. (SLIME HEALTH SCRIPT)");
    }
}
