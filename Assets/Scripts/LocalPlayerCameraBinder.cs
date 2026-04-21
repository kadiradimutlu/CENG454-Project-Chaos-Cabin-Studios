using Fusion;
using UnityEngine;

public class LocalPlayerCameraBinder : NetworkBehaviour
{
    [SerializeField] private Transform cameraTarget;

    public override void Spawned()
    {
        if (!HasInputAuthority)
            return;

        CameraFollowRig followRig = FindObjectOfType<CameraFollowRig>();

        if (followRig == null)
        {
            Debug.LogWarning("LocalPlayerCameraBinder: CameraFollowRig bulunamadı.");
            return;
        }

        Transform targetToUse = cameraTarget != null ? cameraTarget : transform;
        followRig.SetTarget(targetToUse);

        Debug.Log("LocalPlayerCameraBinder: Local oyuncu kamerası bağlandı.");
    }
}