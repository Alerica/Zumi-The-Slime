using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Checkpoint")]
    public Transform currentCheckpoint;  

    [Header("Player")]
    public SlimeHealth slimeHealth;

    [Header("Player Settings")]
    public bool autoRevive = true; // Automatically revive player on death

    [Header("Player Stats")]
    public int deathCount { get; private set; }
    public int reviveCount { get; private set; }



    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes (Do not destroy on load)
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetCheckpoint(Transform checkpoint)
    {
        currentCheckpoint = checkpoint;
    }

    public Vector3 GetSpawnPosition()
    {
        if (currentCheckpoint != null)
            return currentCheckpoint.position;

        Debug.LogWarning("Current checkpoint is not set. Returning zero vector.");
        return Vector3.zero;
    }

    public void RegisterDeath()
    {
        deathCount++;
    }

    public void RegisterRevive()
    {
        reviveCount++;
    }

    public void ResetStats()
    {
        deathCount = 0;
        reviveCount = 0;
    }

    public void KillPlayer()
    {
        if (slimeHealth == null) 
        {
            Debug.LogWarning("SlimeHealth is not assigned.");
            return;
        }

        slimeHealth.TakeDamage(slimeHealth.maxHealth);
        RegisterDeath();

        RevivePlayer();
        RegisterRevive();
    }

    public void RevivePlayer()
    {
        if (slimeHealth == null)
        {
            Debug.LogWarning("SlimeHealth is not assigned.");
            return;
        }

        slimeHealth.Revive();
        RegisterRevive();
        slimeHealth.transform.position = currentCheckpoint.transform.position;
        Debug.Log("Player has been revived at checkpoint.");
    }
    
}
