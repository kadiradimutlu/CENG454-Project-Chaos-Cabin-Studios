using UnityEngine;
using Fusion;

public class RoleHandler : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private PlayerMovement playerMovement;

    public enum PlayerRole
    {
        None,
        Runner,
        Trapper
    }

    [Networked] public PlayerRole currentRole { get; set; }

    private PlayerRole _lastRole;

    public override void Spawned()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        _lastRole = PlayerRole.None;
        ApplyRoleSettings(currentRole);
    }

    public override void Render()
    {
        if (_lastRole == currentRole)
            return;

        ApplyRoleSettings(currentRole);
        _lastRole = currentRole;
    }

    private void ApplyRoleSettings(PlayerRole role)
    {
        if (playerMovement == null)
            return;

        if (!Object.HasInputAuthority && !Object.HasStateAuthority)
            return;

        switch (role)
        {
            case PlayerRole.Runner:
                playerMovement.SetMovementAllowed(true);
                break;

            case PlayerRole.Trapper:
                playerMovement.SetMovementAllowed(true);
                break;

            case PlayerRole.None:
            default:
                playerMovement.SetMovementAllowed(true);
                break;
        }
    }
}
