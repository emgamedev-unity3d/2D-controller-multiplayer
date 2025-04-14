using Unity.Cinemachine;
using UnityEngine;
using Unity.Netcode;

public class HeyCameraLookAtMe : MonoBehaviour //NetworkBehaviour
{
    void Start()
    {
        // TODO: uncomment to only find the camera on the player's device
        //if (!IsOwner)
        //    return;

        var cam = FindAnyObjectByType<CinemachineCamera>();
        cam.Target.TrackingTarget = transform;
    }
}
