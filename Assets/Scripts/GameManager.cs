using System.Collections;
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

    [Header("Fade Effect")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 1f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip deathSound;
    public AudioClip damageSound;
    public AudioClip healSound;

    public bool lockedMovement = false; 
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes (Do not destroy on load)
            
            // Get AudioSource if not assigned
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
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

        lockedMovement = true; // Lock movement during death sequence
        StartCoroutine(HandlePlayerDeathAndRevive());
    }

    private IEnumerator HandlePlayerDeathAndRevive()
    {
        Debug.Log("Player has died. Starting death sequence...");
        
        // Play death sound
        PlayDeathSound();
        
        yield return StartCoroutine(FadeInPanel());

        slimeHealth.TakeDamage(slimeHealth.maxHealth);
        RegisterDeath();

        yield return new WaitForSeconds(3f); // small pause before revive
        RevivePlayer(); // this includes setting position

        RegisterRevive();

        yield return StartCoroutine(FadeOutPanel());
    }

    private void PlayDeathSound()
    {
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        else
        {
            Debug.LogWarning("AudioSource or Death Sound not assigned in GameManager.");
        }
    }

    IEnumerator FadeInPanel()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            fadePanel.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        fadePanel.alpha = 1f;
    }

    IEnumerator FadeOutPanel()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            fadePanel.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        fadePanel.alpha = 0f;
    }

    public void PlayDamageSound()
    {
        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
        else
        {
            Debug.LogWarning("AudioSource or Damage Sound not assigned in GameManager.");
        }
    }

    public void PlayHealSound()
    {
        if (audioSource != null && healSound != null)
        {
            audioSource.PlayOneShot(healSound);
        }
        else
        {
            Debug.LogWarning("AudioSource or Heal Sound not assigned in GameManager.");
        }
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
        lockedMovement = false; // Unlock movement after revive
        Debug.Log("Player has been revived at checkpoint.");
    }
}