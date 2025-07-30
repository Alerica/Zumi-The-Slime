using UnityEngine;

public class TeleportToTarget : MonoBehaviour
{
    public Transform objectToTeleport; 
    public Transform targetLocation;

    public void Teleport()
    {
        if (objectToTeleport != null && targetLocation != null)
        {
            objectToTeleport.position = targetLocation.position;
        }
        else
        {
            Debug.LogWarning("Object to teleport or target location is not set.");
        }
    }
}
