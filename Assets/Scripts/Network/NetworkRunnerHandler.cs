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
    public static NetworkRunnerHandler Instance { get; private set; }

    private NetworkRunner _networkRunner;
    private NetworkSceneManagerDefault _sceneManager;
    private MainMenuManager _mainMenuManager;

    private bool _selfCallbacksAdded = false;
    private bool _mainMenuCallbacksAdded = false;
    private bool _hasBeenStarted = false;
    private bool _isShuttingDown = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        CacheReferences();
        SetupRunner();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void CacheReferences()
    {
        _networkRunner = GetComponent<NetworkRunner>();
        _sceneManager = GetComponent<NetworkSceneManagerDefault>();

        if (_mainMenuManager == null)
            _mainMenuManager = FindObjectOfType<MainMenuManager>();
    }

    private void SetupRunner()
    {
        if (_networkRunner == null)
        {
            Debug.LogError("NetworkRunner bulunamadı.");
            return;
        }

        if (_sceneManager == null)
        {
            Debug.LogError("NetworkSceneManagerDefault bulunamadı.");
            return;
        }

        _networkRunner.ProvideInput = true;

        if (!_selfCallbacksAdded)
        {
            _networkRunner.AddCallbacks(this);
            _selfCallbacksAdded = true;
        }

        TryRegisterMainMenuCallbacks();
    }

    private void TryRegisterMainMenuCallbacks()
    {
        if (_networkRunner == null)
            return;

        if (_mainMenuManager == null)
            _mainMenuManager = FindObjectOfType<MainMenuManager>();

        if (_mainMenuManager != null && !_mainMenuCallbacksAdded)
        {
            _networkRunner.AddCallbacks(_mainMenuManager);
            _mainMenuCallbacksAdded = true;
            Debug.Log("MainMenuManager callback olarak NetworkRunner'a eklendi.");
        }
    }

    public NetworkRunner GetRunner()
    {
        if (_networkRunner == null)
        {
            CacheReferences();
            SetupRunner();
        }

        return _networkRunner;
    }

    public bool IsReusable()
    {
        return _networkRunner != null && !_hasBeenStarted && !_isShuttingDown;
    }

    public async Task<StartGameResult> StartGame(GameMode mode, string roomName)
    {
        CacheReferences();
        SetupRunner();

        if (_networkRunner == null)
        {
            Debug.LogError("NetworkRunner bulunamadı.");
            return default;
        }

        if (_sceneManager == null)
        {
            Debug.LogError("NetworkSceneManagerDefault bulunamadı.");
            return default;
        }

        if (_hasBeenStarted)
        {
            Debug.LogWarning("Bu NetworkRunner daha önce kullanıldı. Yeni instance oluşturulmalı.");
            return default;
        }

        TryRegisterMainMenuCallbacks();

        StartGameResult result = await _networkRunner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = roomName,
            SceneManager = _sceneManager,
            PlayerCount = 4
        });

        if (result.Ok)
        {
            _hasBeenStarted = true;
            Debug.Log($"StartGame başarılı. Mode: {mode}, Room: {roomName}");
        }
        else
        {
            Debug.LogError($"StartGame başarısız: {result.ShutdownReason}");
        }

        return result;
    }

    public async Task ShutdownRunner()
    {
        if (_networkRunner == null)
            return;

        if (_isShuttingDown)
            return;

        _isShuttingDown = true;

        try
        {
            if (!_networkRunner.IsShutdown)
            {
                await _networkRunner.Shutdown();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"ShutdownRunner hata verdi: {ex.Message}");
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