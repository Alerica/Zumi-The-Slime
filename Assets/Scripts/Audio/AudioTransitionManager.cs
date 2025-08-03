using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AudioTransitionManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource worldAudioSource;
    [SerializeField] private AudioSource bossAudioSource;
    
    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 2f;
    [SerializeField] private BossController1 bossController;
    
    private bool isPlayingBossAudio = false;
    private Coroutine fadeCoroutine;
    
    private void Start()
    {
            
    }
    
    private void Update()
    {
        if (bossController != null && isPlayingBossAudio)
        {
            if (bossController.currentHealth <= 0)
            {
                TransitionToWorldAudio();
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isPlayingBossAudio)
        {
            TransitionToBossAudio();
        }
    }
    
    private void TransitionToBossAudio()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeToBossAudio());
        isPlayingBossAudio = true;
    }
    
    private void TransitionToWorldAudio()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeToWorldAudio());
        isPlayingBossAudio = false;
    }
    
    private IEnumerator FadeToBossAudio()
    {
        float timer = 0f;
        float worldStartVolume = worldAudioSource.volume;
        
        if (bossAudioSource != null && !bossAudioSource.isPlaying)
            bossAudioSource.Play();
        
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeInDuration;
            
            worldAudioSource.volume = Mathf.Lerp(worldStartVolume, 0f, t);
            bossAudioSource.volume = Mathf.Lerp(0f, 1f, t);
            
            yield return null;
        }
        
        worldAudioSource.volume = 0f;
        bossAudioSource.volume = 1f;
    }
    
    private IEnumerator FadeToWorldAudio()
    {
        float timer = 0f;
        float bossStartVolume = bossAudioSource.volume;
        
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeInDuration;
            
            worldAudioSource.volume = Mathf.Lerp(0f, 1f, t);
            bossAudioSource.volume = Mathf.Lerp(bossStartVolume, 0f, t);
            
            yield return null;
        }
        
        worldAudioSource.volume = 1f;
        bossAudioSource.volume = 0f;
    }
}