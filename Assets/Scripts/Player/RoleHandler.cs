using UnityEngine;
using Fusion;

public partial class RoleHandler : NetworkBehaviour
{
    [Header("Components")]
    private PlayerMovement _playerMovement;
    
    private PlayerMovement playerController; 

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
        if (_playerMovement == null)
        {
            _playerMovement = GetComponent<PlayerMovement>();
            playerController = _playerMovement; 
        }

        _lastRole = PlayerRole.None;
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

        if (_playerMovement != null)
        {

            //_playerMovement.isMovementAllowed = false;

    }

    else if (role == PlayerRole.Runner)

    {

        if (_playerMovement != null)
        {

            //_playerMovement.isMovementAllowed = true;

    }

}
}
}}
 
