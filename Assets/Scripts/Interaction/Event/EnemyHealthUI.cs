using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EnemyHealthUI : MonoBehaviour
{
    [Header("Image References")]
    public Image fillImage;      // Green actual fill
    public Image damageImage;    // Red delayed fill (damage)

    [Header("Settings")]
    public float fillSpeed = 1.5f;

    private float currentFill = 1f;

    public void UpdateHealth(float normalizedHealth)
    {
        Debug.Log($"Updating enemy health UI: {normalizedHealth * 100}%");
        normalizedHealth = Mathf.Clamp01(normalizedHealth);

        if (normalizedHealth < currentFill)
        {
            damageImage.fillAmount = fillImage.fillAmount;
            fillImage.fillAmount = normalizedHealth;

            damageImage.enabled = true;
            StopAllCoroutines();
            StartCoroutine(LerpFill(damageImage, normalizedHealth));
        }
        else
        {
            // Instant update when health increases (enemy usually doesn't heal)
            fillImage.fillAmount = normalizedHealth;
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
        damageImage.enabled = false;
    }
}
