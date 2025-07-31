using UnityEngine;

public class SlimePressureSensor : MonoBehaviour
{
    public enum Side { Left, Right, Front, Back, Top }
    public Side side;
    public SlimeDeformer deformer;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) // or use layer-based check
        {
            deformer.Press(side, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            deformer.Press(side, false);
        }
    }
}
