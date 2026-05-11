using UnityEngine;

public class CameraRoleApplier : RoleApplierBehaviour
{
    [SerializeField] private GameObject runnerCameraObject;
    [SerializeField] private GameObject trapperCameraObject;

    public override void ApplyRole(PlayerRole role, bool isLocalPlayer)
    {
        if (!isLocalPlayer || role == PlayerRole.None)
        {
            return;
        }

        if (runnerCameraObject != null)
        {
            runnerCameraObject.SetActive(role == PlayerRole.Runner);
        }

        if (trapperCameraObject != null)
        {
            trapperCameraObject.SetActive(role == PlayerRole.Trapper);
        }
    }
}
