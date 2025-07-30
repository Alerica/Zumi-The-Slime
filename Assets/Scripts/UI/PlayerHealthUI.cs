using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Image References")]
    public Image fillImage;      // Green actual fill
    public Image damageImage;    // Red delayed fill (damage)
    public Image healImage;      // Blue delayed fill (healing)

    [Header("Settings")]
    public float fillSpeed = 1.5f;

    private float currentFill = 1f;

    public void UpdateHealth(float normalizedHealth)
    {
        normalizedHealth = Mathf.Clamp01(normalizedHealth);

        // Damage
        if (normalizedHealth < currentFill)
        {
            damageImage.fillAmount = fillImage.fillAmount;
            fillImage.fillAmount = normalizedHealth;

            healImage.enabled = false;
            damageImage.enabled = true;
            StopAllCoroutines();
            StartCoroutine(LerpFill(damageImage, normalizedHealth));
        }
        // Heal
        else if (normalizedHealth > currentFill)
        {
            healImage.fillAmount = normalizedHealth;

            damageImage.enabled = false;
            healImage.enabled = true;
            StopAllCoroutines();
            StartCoroutine(LerpFill(fillImage, normalizedHealth));
        }

        currentFill = normalizedHealth;
    }

    private IEnumerator LerpFill(Image img, float target)
    {
        float start = img.fillAmount;
        float t = 0f;

        while (Mathf.Abs(img.fillAmount - target) > 0.01f)
        {
            t += Time.deltaTime * fillSpeed;
            img.fillAmount = Mathf.Lerp(start, target, t);
            yield return null;
        }

        img.fillAmount = target;

        if (img == fillImage)
            healImage.enabled = false;

        if (img == damageImage)
            damageImage.enabled = false;
    }
}
