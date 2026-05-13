using UnityEngine;
using Fusion;

public class RoleHandler : NetworkBehaviour
{
    [Header("Components")]
    private PlayerMovement _playerMovement;

   

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
        if(_playerMovement == null )
        {
            _playerMovement = GetComponent<PlayerMovement>();
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
            if (playerController != null)
            {
                playerController.isMovementAllowed = false;
            }

            if (isLocalPlayer)
            {
                
            }
        }
        else if (role == PlayerRole.Runner)
        {
            
            }

            if (isLocalPlayer)
            {
                
            }
        }
    }
}