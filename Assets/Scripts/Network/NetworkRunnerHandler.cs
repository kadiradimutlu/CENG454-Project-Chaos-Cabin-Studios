using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

[RequireComponent(typeof(NetworkRunner))]
[RequireComponent(typeof(NetworkSceneManagerDefault))]
public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _networkRunner;
    private NetworkSceneManagerDefault _sceneManager;

    private void Awake()
    {
        _networkRunner = GetComponent<NetworkRunner>();
        _sceneManager = GetComponent<NetworkSceneManagerDefault>();

        _networkRunner.ProvideInput = true;
        _networkRunner.AddCallbacks(this);
    }

    public NetworkRunner GetRunner()
    {
        return _networkRunner;
    }

    public async Task<StartGameResult> StartGame(GameMode mode, string roomName)
    {
        if (_networkRunner == null)
        {
            Debug.LogError("NetworkRunner bulunamadı.");
            return default;
        }

        var result = await _networkRunner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = roomName,
            SceneManager = _sceneManager,
            PlayerCount = 4
        });

        if (!result.Ok)
        {
            Debug.LogError($"StartGame başarısız: {result.ShutdownReason}");
        }
        else
        {
            Debug.Log($"StartGame başarılı. Mode: {mode}, Room: {roomName}");
        }

        return result;
    }

    public async Task ShutdownRunner()
    {
        if (_networkRunner != null && !_networkRunner.IsShutdown)
        {
            await _networkRunner.Shutdown();
        }
    }

    // ---------------- INetworkRunnerCallbacks ----------------

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player joined: {player}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player left: {player}");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Runner shutdown: {shutdownReason}");
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("Connected to server.");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"Disconnected from server: {reason}");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}