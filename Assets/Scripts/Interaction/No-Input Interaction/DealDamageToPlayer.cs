using UnityEngine;

public class DealDamageToPlayer : MonoBehaviour
{
    [SerializeField] private int damageAmount = 1;

    public void DealDamage()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // Check immunity in movement script
            ImprovedFrogMovement movement = player.GetComponent<ImprovedFrogMovement>();
            if (movement != null && movement.isImmune)
            {
                Debug.Log("Player is immune! Damage blocked.");
                
            }
            else
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
        }
        else
        {
            Debug.LogWarning("Player not found.");
        }
    }
}
