using Fusion;
using UnityEngine;

public class RoleHandler : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private PlayerMovement playerMovement;

    public enum PlayerRole
    {
        None = 0,
        Runner = 1,
        Trapper = 2
    }

    [Networked] public PlayerRole currentRole { get; private set; }

    public bool IsSpawnReady { get; private set; }

    private PlayerRole lastAppliedRole = (PlayerRole)(-999);

    public override void Spawned()
    {
        IsSpawnReady = true;

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        ApplyRoleSettings(currentRole);
        lastAppliedRole = currentRole;

        Debug.Log($"RoleHandler Spawned | Player={Object.InputAuthority} | Role={currentRole}");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        IsSpawnReady = false;
    }

    public override void Render()
    {
        if (!IsSpawnReady)
            return;

        if (lastAppliedRole == currentRole)
            return;

        ApplyRoleSettings(currentRole);
        lastAppliedRole = currentRole;
    }

    public void SetRoleFromServer(PlayerRole role)
    {
        if (!Object.HasStateAuthority)
            return;

        currentRole = role;
        ApplyRoleSettings(currentRole);
        lastAppliedRole = currentRole;

        Debug.Log($"RoleHandler role set: {currentRole} | Player={Object.InputAuthority}");
    }

    public bool TryGetRole(out PlayerRole role)
    {
        role = PlayerRole.None;

        if (!IsSpawnReady)
            return false;

        role = currentRole;
        return true;
    }

    private void ApplyRoleSettings(PlayerRole role)
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (playerMovement == null)
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

    public static string GetRoleDisplayName(PlayerRole role)
    {
        switch (role)
        {
            case PlayerRole.Runner:
                return "RUNNER";

            case PlayerRole.Trapper:
                return "TRAPPER";

            case PlayerRole.None:
            default:
                return "CHOOSE";
        }
    }

    public static Color GetRoleColor(PlayerRole role)
    {
        switch (role)
        {
            case PlayerRole.Runner:
                return Color.green;

            case PlayerRole.Trapper:
                return Color.red;

            case PlayerRole.None:
            default:
                return Color.white;
        }
    }
}