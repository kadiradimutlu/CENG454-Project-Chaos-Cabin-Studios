using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class FinishZone : MonoBehaviour
{
    [SerializeField] private bool acceptOnlyRunners = true;

    private LobbyState lobbyState;
    private Collider zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();

        if (zoneCollider != null)
            zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        CacheLobbyState();

        if (lobbyState == null || !lobbyState.GameStarted || lobbyState.RoundStateValue != 2)
            return;

        NetworkObject playerObject = other.GetComponentInParent<NetworkObject>();

        if (playerObject == null)
            return;

        if (playerObject.GetComponent<PlayerMovement>() == null)
            return;

        if (acceptOnlyRunners && !IsRunner(playerObject))
            return;

        PlayerRef player = playerObject.InputAuthority;

        if (player == default)
            return;

        if (lobbyState.HasStateAuthority)
            lobbyState.ReportRunnerFinishedServer(player);
        else
            lobbyState.RPC_RequestRunnerFinished(player);
    }

    private bool IsRunner(NetworkObject playerObject)
    {
        RoleHandler roleHandler = playerObject.GetComponentInChildren<RoleHandler>(true);

        if (roleHandler == null)
            return false;

        return roleHandler.currentRole == RoleHandler.PlayerRole.Runner;
    }

    private void CacheLobbyState()
    {
        if (lobbyState != null)
            return;

        lobbyState = FindFirstObjectByType<LobbyState>();
    }
}
