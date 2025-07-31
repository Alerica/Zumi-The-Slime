using UnityEngine;

public class SlimeDeformer : MonoBehaviour
{
    private Vector3 originalScale;
    private Vector3 targetScale;

    private bool pressLeft, pressRight, pressFront, pressBack, pressTop;

    public float deformAmount = 0.3f;
    public float smooth = 4f;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    public void Press(SlimePressureSensor.Side side, bool pressing)
    {
        switch (side)
        {
            case SlimePressureSensor.Side.Left: pressLeft = pressing; break;
            case SlimePressureSensor.Side.Right: pressRight = pressing; break;
            case SlimePressureSensor.Side.Front: pressFront = pressing; break;
            case SlimePressureSensor.Side.Back: pressBack = pressing; break;
            case SlimePressureSensor.Side.Top: pressTop = pressing; break; // new line
        }
    }

    void Update()
    {
        targetScale = originalScale;

        // Horizontal squash
        if (pressLeft || pressRight)
            targetScale.x -= deformAmount;
        if (pressFront || pressBack)
            targetScale.z -= deformAmount;

        // Vertical squash/stretch
        if ((pressLeft || pressRight) || (pressFront || pressBack))
            targetScale.y += deformAmount; // squashed sideways = stretch upward
        if (pressTop)
        {
            targetScale.y -= deformAmount; // pressed from top = squash downward
            targetScale.x += deformAmount * 0.5f *7.5f; // bulge sideways
            targetScale.z += deformAmount * 0.5f *7.5f;
        }

        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * smooth);
    }
}
