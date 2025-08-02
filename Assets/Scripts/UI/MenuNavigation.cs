using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MenuNavigation : MonoBehaviour
{
    public List<ButtonTextGlow> menuButtons;
    private int selectedIndex = 0;
    private bool usingKeyboard = true;

    void Start()
    {
        // HighlightButton(selectedIndex);
    }

    void Update()
    {
        // Keyboard Navigation
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            MoveSelection(1);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            MoveSelection(-1);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            menuButtons[selectedIndex].GetComponent<Button>().onClick.Invoke();
        }

        // Detect mouse movement
        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
        {
            usingKeyboard = false;
        }
    }

    void MoveSelection(int direction)
    {
        usingKeyboard = true;
        UnhighlightButton(selectedIndex);
        selectedIndex = (selectedIndex + direction + menuButtons.Count) % menuButtons.Count;
        HighlightButton(selectedIndex);
    }

    void HighlightButton(int index)
    {
        Debug.Log("Highlighting button: " + menuButtons[index].targetText.text);
        menuButtons[index].ApplyGlow();  // Custom method to glow
    }

    void UnhighlightButton(int index)
    {
        Debug.Log("UnHighlighting button: " + menuButtons[index].targetText.text);
        menuButtons[index].RemoveGlow(); // Custom method to reset
    }

    public void SetSelectedViaMouse(ButtonTextGlow newSelection)
    {
        if (!usingKeyboard) return;

        UnhighlightButton(selectedIndex);
        selectedIndex = menuButtons.IndexOf(newSelection);
        HighlightButton(selectedIndex);
    }
}
