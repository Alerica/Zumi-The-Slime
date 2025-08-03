using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("References")]
    public GameObject playerUI;
    public SlimeHealth slimeHealth;
    public GameObject escapePanel;

    private bool isUIVisible = true;
    private bool isEscapePanelVisible = false;

    void Update()
    {


        // Toggle Player UI
        if (Input.GetKeyDown(KeyCode.F12))
        {
            isUIVisible = !isUIVisible;
            if (playerUI != null)
                playerUI.SetActive(isUIVisible);
        }

        // Kill Player
        if (Input.GetKeyDown(KeyCode.F11))
        {
            GameManager.Instance?.KillPlayer();
        }

        // Heal Player
        if (Input.GetKeyDown(KeyCode.F10))
        {
            slimeHealth?.Heal(100); // Assumes Heal(int amount)
        }

        // Toggle Escape Panel and Pause/Resume
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isEscapePanelVisible = !isEscapePanelVisible;
            if (escapePanel != null)
                escapePanel.SetActive(isEscapePanelVisible);

            Time.timeScale = isEscapePanelVisible ? 0f : 1f;

            Cursor.lockState = isEscapePanelVisible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isEscapePanelVisible;
        }
        
        if (Time.timeScale == 0f) return;

        if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
