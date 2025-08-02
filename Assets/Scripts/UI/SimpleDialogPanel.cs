using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SimpleDialogPanel : MonoBehaviour
{
    [System.Serializable]
    public class DialogLine
    {
        [TextArea(2, 4)]
        public string text;
        public float delay = 1.5f;
    }

    [Header("References")]
    public TextMeshProUGUI dialogText;
    public CanvasGroup canvasGroup;

    [Header("Settings")]
    public DialogLine[] dialogLines;
    public float fadeOutDuration = 1f;
    public float typingSpeed = 0.05f;

    private bool isSkipping = false;

    void Start()
    {
        StartCoroutine(RunDialog());
    }

    void Update()
    {
        isSkipping = Input.GetKey(KeyCode.Space);
    }

    IEnumerator RunDialog()
    {
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        foreach (DialogLine line in dialogLines)
        {
            yield return StartCoroutine(TypeText(line.text));

            float timer = 0f;
            while (timer < line.delay)
            {
                if (isSkipping) break;
                timer += Time.deltaTime;
                yield return null;
            }
        }

        // Fade out
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    IEnumerator TypeText(string fullText)
    {
        dialogText.text = "";
        foreach (char c in fullText)
        {
            dialogText.text += c;
            if (isSkipping) break;
            yield return new WaitForSeconds(typingSpeed);
        }

        // Instantly show full text if skipped
        dialogText.text = fullText;
    }
}
