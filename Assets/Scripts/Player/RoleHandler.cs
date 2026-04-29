using UnityEngine;
using Fusion;

public class RoleHandler : NetworkBehaviour
{
    [Header("Components")]
    public PlayerController playerController;

    [Header("Kameralar")]
    public GameObject runnerCameraObj;
    public GameObject trapperCameraObj;

    public enum PlayerRole
    {
        None,
        Runner,
        Trapper
    }

    [Networked]
    public PlayerRole currentRole { get; set; }

    private PlayerRole _lastRole;

    public override void Spawned()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        _lastRole = PlayerRole.None;

        // HasStateAuthority ise ve server yeni basladiysa rol onceden atanmis olabilir,
        // Bu yuzden spawn oldugunda hemen ayarlari uyguluyoruz.
        ApplyRoleSettings(currentRole);
    }

    public override void Render()
    {
        if (_lastRole != currentRole)
        {
            ApplyRoleSettings(currentRole);
            _lastRole = currentRole;
        }
    }

    private void ApplyRoleSettings(PlayerRole role)
    {
        if (role == PlayerRole.None) return;

        bool isLocalPlayer = Object.HasInputAuthority;

        if (role == PlayerRole.Trapper)
        {
            if (playerController != null)
            {
                playerController.isMovementAllowed = false;
            }

            if (isLocalPlayer)
            {
                if (runnerCameraObj != null) runnerCameraObj.SetActive(false);
                if (trapperCameraObj != null) trapperCameraObj.SetActive(true);
            }
        }
        else if (role == PlayerRole.Runner)
        {
            if (playerController != null)
            {
                playerController.isMovementAllowed = true;
            }

            if (isLocalPlayer)
            {
                if (trapperCameraObj != null) trapperCameraObj.SetActive(false);
                if (runnerCameraObj != null) runnerCameraObj.SetActive(true);
            }
        }
    }
}
