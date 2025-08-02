using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;      // Add this!
    public GameObject settingsPanel;

    [Header("Audio")]
    public AudioMixer audioMixer;

    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider UISlider;

    [Header("Scene")]
    public string gameSceneName = "GameScene";
    [Header("Animator")]
    public Animator animator;
    public float transitionDelay = 1.13f; // Delay before scene change after transition

    void Start()
    {
        if (musicSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
            musicSlider.value = volume;
            SetVolumeMusic(volume);

            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
            sfxSlider.value = sfxVolume;
            SetVolumeSFX(sfxVolume);

            float UIVolume = PlayerPrefs.GetFloat("UIVolume", 0.75f);
            UISlider.value = UIVolume;
            SetVolumeUI(UIVolume);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        mainMenuPanel?.SetActive(true);
        settingsPanel?.SetActive(false);
    }

    public void PlayGame()
    {
        Debug.Log("Starting game scene: " + gameSceneName);
        if (animator)
        {
            StartCoroutine(TransitionRoutine(gameSceneName));
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    private System.Collections.IEnumerator TransitionRoutine(string sceneName)
    {
        animator.SetTrigger("StartTransition"); 
        yield return new WaitForSeconds(transitionDelay);
        SceneManager.LoadScene(sceneName); 
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void SetVolume(float volume)
    {
        Debug.Log("Setting volume to: " + volume);
        audioMixer.SetFloat("Master", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("Volume", volume);
    }

    public void SetVolumeMusic(float volume)
    {
        Debug.Log("Setting volume to: " + volume);
        audioMixer.SetFloat("Music", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void SetVolumeSFX(float volume)
    {
        Debug.Log("Setting SFX volume to: " + volume);
        audioMixer.SetFloat("SFX", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    public void SetVolumeUI(float volume)
    {
        Debug.Log("Setting UI volume to: " + volume);
        audioMixer.SetFloat("UI", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("UIVolume", volume);
    }


    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
