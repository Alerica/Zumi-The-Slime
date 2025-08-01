using UnityEngine;

public class CrosshairDrawer : MonoBehaviour
{
    [Tooltip("Optional: assign a crosshair sprite here.")]
    public Texture2D crosshairTexture;

    [Tooltip("Size of the crosshair (width & height).")]
    public float size = 32f;

    [Tooltip("Thickness of the lines when drawing the fallback crosshair.")]
    public float thickness = 2f;

    private Texture2D _linePixel;
    private bool _useGenerated;

    void Start()
    {
        // hide the OS cursor
        Cursor.visible = false;

        if (crosshairTexture == null)
        {
            // we’ll draw our own: create a 1×1 white pixel we can tint
            _useGenerated = true;
            _linePixel = new Texture2D(1, 1);
            _linePixel.SetPixel(0, 0, Color.white);
            _linePixel.Apply();
        }
    }

    void OnGUI()
    {
        float cx = Screen.width  * 0.5f;
        float cy = Screen.height * 0.5f;

        if (!_useGenerated)
        {
            // draw your assigned texture centered
            float x = cx - (size * 0.5f);
            float y = cy - (size * 0.5f);
            GUI.DrawTexture(new Rect(x, y, size, size), crosshairTexture);
        }
        else
        {
            // draw fallback: black horizontal and vertical lines
            GUI.color = Color.black;

            // horizontal line
            GUI.DrawTexture(
                new Rect(cx - size * 0.5f, cy - thickness * 0.5f, size, thickness),
                _linePixel
            );

            // vertical line
            GUI.DrawTexture(
                new Rect(cx - thickness * 0.5f, cy - size * 0.5f, thickness, size),
                _linePixel
            );

            GUI.color = Color.white; // reset color
        }
    }
}