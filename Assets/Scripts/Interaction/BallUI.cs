using UnityEngine;
using UnityEngine.UI;

public class BallUI : MonoBehaviour
{
    [Header("UI References")]
    public Image currentBallImage;
    public Image nextBallImage;

    [Header("Ball Colors")]
    public Color[] ballColors; 
    public void UpdateBallUI(int currentColorIndex, int nextColorIndex)
    {
        currentBallImage.color = ballColors[currentColorIndex];
        nextBallImage.color = ballColors[nextColorIndex];
    }
}
