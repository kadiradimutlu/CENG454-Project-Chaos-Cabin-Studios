using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _networkRunner;

    private void Awake()
    {
        _networkRunner = GetComponent<NetworkRunner>();
        if (_networkRunner == null)
        {
            _networkRunner = gameObject.AddComponent<NetworkRunner>();
        }
    }

    public async Task StartGame(GameMode mode, string roomName)
    {
        _networkRunner.ProvideInput = true;

        await _networkRunner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = roomName,
            Scene = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            SceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>()
        });
    }

    // --- INetworkRunnerCallbacks Implementations ---
    // (Bunlar arayüzün zorunlu metotlarıdır, gerektiğinde içlerini dolduracağız)
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { Debug.Log($"Player {player} joined."); }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { Debug.Log($"Player {player} left."); }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { Debug.Log("Runner shut down."); }
    public void OnConnectedToServer(NetworkRunner runner) { Debug.Log("Connected to server."); }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { Debug.Log("Disconnected from server."); }
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
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
}