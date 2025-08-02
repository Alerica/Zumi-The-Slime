using UnityEngine;

public class HealToPlayer : MonoBehaviour
{
    [SerializeField] private int healAmount = 1;

    public void Heal()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            ImprovedFrogMovement movement = player.GetComponent<ImprovedFrogMovement>();
            if (movement != null && movement.isImmune)
            {
                Debug.Log("Player is immune! Heal skipped.");
            }
            else
            {
                SlimeHealth slimeHealth = player.GetComponentInChildren<SlimeHealth>();
                if (slimeHealth != null)
                {
                    Debug.Log($"Healing player by {healAmount}.");
                    slimeHealth.Heal(healAmount);
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
