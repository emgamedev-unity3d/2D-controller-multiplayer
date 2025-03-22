using Unity.Cinemachine;
using Unity.Netcode;

public class HeyCameraLookAtMe_Networked : NetworkBehaviour
{
    void Start()
    {
        if (!IsOwner)
            return;

        var cam = FindAnyObjectByType<CinemachineCamera>();
        cam.Target.TrackingTarget = transform;
    }
}
