using UnityEngine;

public class DealDamageToPlayer : MonoBehaviour
{
    [SerializeField] private int damageAmount = 1;

    public void DealDamage()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            SlimeHealth slimeHealth = player.GetComponentInChildren<SlimeHealth>();
            if (slimeHealth != null)
            {
                Debug.Log($"Dealing {damageAmount} damage to player.");
                slimeHealth.TakeDamage(damageAmount);
            }
            else
            {
                Debug.LogWarning("SlimeHealth component not found on player or its children.");
            }
        }
        else
        {
            Debug.LogWarning("Player not found.");
        }
    }
}
