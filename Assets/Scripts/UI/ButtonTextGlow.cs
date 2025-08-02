using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEditor;

public class ButtonTextGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI targetText;
    private Color normalColor;
    private Color glowColor;
    public MenuNavigation menu;

    void Start()
    {
        if (targetText == null)
            targetText = GetComponentInChildren<TextMeshProUGUI>();

        normalColor = new Color(1f, 1f, 1f, 0.4f); 
        glowColor = new Color(2f, 2f, 2f, 1f);    

        targetText.color = normalColor;
    }

    

    public void ApplyGlow()
    {
        targetText.fontMaterial.SetColor("_FaceColor", glowColor);
    }

    public void RemoveGlow()
    {
        targetText.fontMaterial.SetColor("_FaceColor", normalColor);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Button hovered: " + targetText.text);
        menu.SetSelectedViaMouse(this);
        ApplyGlow();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("Button exited: " + targetText.text);
        RemoveGlow();
    }

}
