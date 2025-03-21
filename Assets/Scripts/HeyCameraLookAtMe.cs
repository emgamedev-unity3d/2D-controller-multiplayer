using Unity.Cinemachine;
using UnityEngine;

public class HeyCameraLookAtMe : MonoBehaviour
{
    void Start()
    {
        var cam = FindAnyObjectByType<CinemachineCamera>();
        cam.Target.TrackingTarget = transform;
    }
}
