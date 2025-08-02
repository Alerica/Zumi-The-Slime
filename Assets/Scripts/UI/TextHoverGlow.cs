using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TextHoverGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI tmpText;
    private Color normalColor;
    private Color glowColor;

    void Start()
    {
        if (tmpText == null)
            tmpText = GetComponent<TextMeshProUGUI>();

        normalColor = new Color(1f, 1f, 1f, 0.01f);


        glowColor = new Color(5f, 5f, 5f, 1f);  // Intense bright white
        tmpText.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        tmpText.color = glowColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tmpText.color = normalColor;
    }
}
