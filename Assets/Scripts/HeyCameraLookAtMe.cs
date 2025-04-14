using Unity.Cinemachine;
using UnityEngine;
using Unity.Netcode;

public class HeyCameraLookAtMe : MonoBehaviour //NetworkBehaviour
{
    void Start()
    {
        // TODO: uncomment to only update physics on owner's POV
        //if (!IsOwner)
        //    return;

        var cam = FindAnyObjectByType<CinemachineCamera>();
        cam.Target.TrackingTarget = transform;
    }
}
