using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Player Spawn")]
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform[] runnerSpawnPoints;
    [SerializeField] private Transform[] trapperSpawnPoints;

    private NetworkRunner _runner;
    private LobbyState _lobbyState;
    private bool _callbacksRegistered = false;
    private bool _gameplaySpawnStarted = false;

    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    private void Awake()
    {
        TryCacheRunner();
        TryCacheLobbyState();
    }

    private void OnEnable()
    {
        TryCacheRunner();
        TryCacheLobbyState();
        TryRegisterCallbacks();
    }

    private void OnDisable()
    {
        if (_runner != null && _callbacksRegistered)
        {
            _runner.RemoveCallbacks(this);
            _callbacksRegistered = false;
        }
    }

    private bool TryCacheRunner()
    {
        if (_runner != null)
            return true;

        _runner = FindObjectOfType<NetworkRunner>();
        return _runner != null;
    }

    private bool TryCacheLobbyState()
    {
        if (_lobbyState != null)
            return true;

        _lobbyState = FindObjectOfType<LobbyState>();
        return _lobbyState != null;
    }

    private int GetSkinIndexForPlayer(PlayerRef player)
    {
        TryCacheLobbyState();

        if (_lobbyState == null)
            return 0;

        return _lobbyState.GetPlayerSkinIndex(player);
    }

    private void TryRegisterCallbacks()
    {
        if (!TryCacheRunner())
            return;

        if (_callbacksRegistered)
            return;

        _runner.AddCallbacks(this);
        _callbacksRegistered = true;
    }

    public void TryStartGameplaySpawn()
    {
        if (!TryCacheRunner())
        {
            Debug.LogError("PlayerSpawner: NetworkRunner bulunamadı.");
            return;
        }

        TryRegisterCallbacks();
        TryCacheLobbyState();

        if (!_runner.IsServer)
        {
            // Sadece host/server spawn yapar
            return;
        }

        if (_gameplaySpawnStarted)
        {
            Debug.Log("PlayerSpawner: Oyuncular zaten spawn edildi.");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("PlayerSpawner: playerPrefab atanmadı.");
            return;
        }

        List<PlayerRef> activePlayers = new List<PlayerRef>(_runner.ActivePlayers);

        if (activePlayers.Count == 0)
        {
            Debug.LogWarning("PlayerSpawner: Spawn edilecek aktif oyuncu yok.");
            return;
        }

        activePlayers.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));

        for (int i = 0; i < activePlayers.Count; i++)
        {
            RoleHandler.PlayerRole assignedRole = (i < 2) ? RoleHandler.PlayerRole.Runner : RoleHandler.PlayerRole.Trapper;
            SpawnPlayerIfNeeded(activePlayers[i], assignedRole, i);
        }

        _gameplaySpawnStarted = true;
        Debug.Log("PlayerSpawner: Gameplay spawn tamamlandı.");
    }

    private void SpawnPlayerIfNeeded(PlayerRef player, RoleHandler.PlayerRole assignedRole, int indexHint = -1)
    {
        if (_runner == null || !_runner.IsServer)
            return;

        if (player == default)
            return;

        if (_spawnedPlayers.ContainsKey(player) && _spawnedPlayers[player] != null)
            return;

        if (playerPrefab == null)
        {
            Debug.LogError("PlayerSpawner: playerPrefab atanmadı.");
            return;
        }

        Transform spawnPoint = GetSpawnPoint(assignedRole, indexHint);
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        NetworkObject spawnedObject = _runner.Spawn(
            playerPrefab,
            spawnPosition,
            spawnRotation,
            player
        );

        if (spawnedObject != null)
        {
            _spawnedPlayers[player] = spawnedObject;

            var roleHandler = spawnedObject.GetComponent<RoleHandler>();
            if (roleHandler != null)
            {
                roleHandler.currentRole = assignedRole;
            }

            var skinApplier = spawnedObject.GetComponent<PlayerSkinApplier>();
            if (skinApplier != null)
            {
                skinApplier.SkinIndex = GetSkinIndexForPlayer(player);
            }

            Debug.Log($"PlayerSpawner: Player {player.PlayerId} spawn edildi. Rol: {assignedRole}, SkinIndex: {GetSkinIndexForPlayer(player)}");
        }
        else
        {
            Debug.LogWarning($"PlayerSpawner: Player {player.PlayerId} spawn edilemedi.");
        }
    }

    private Transform GetSpawnPoint(RoleHandler.PlayerRole role, int indexHint)
    {
        Transform[] points = (role == RoleHandler.PlayerRole.Runner) ? runnerSpawnPoints : trapperSpawnPoints;
        
        if (points == null || points.Length == 0)
            points = spawnPoints;

        if (indexHint < 0)
            indexHint = 0;

        indexHint = Mathf.Clamp(indexHint, 0, points.Length - 1);
        return points[indexHint];
    }

    private void DespawnPlayer(PlayerRef player)
    {
        if (_runner == null || !_runner.IsServer)
            return;

        if (!_spawnedPlayers.TryGetValue(player, out NetworkObject spawnedObject))
            return;

        if (spawnedObject != null)
        {
            _runner.Despawn(spawnedObject);
        }

        _spawnedPlayers.Remove(player);
        Debug.Log($"PlayerSpawner: Player {player.PlayerId} despawn edildi.");
    }

    // ==================================================
    // FUSION CALLBACKS
    // ==================================================

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        _runner = runner;

        if (_runner.IsServer && _gameplaySpawnStarted)
        {
            int index = _spawnedPlayers.Count;
            RoleHandler.PlayerRole role = (index < 2) ? RoleHandler.PlayerRole.Runner : RoleHandler.PlayerRole.Trapper;
            SpawnPlayerIfNeeded(player, role, index);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        _runner = runner;

        if (_runner.IsServer)
        {
            DespawnPlayer(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        _runner = null;
        _lobbyState = null;
        _callbacksRegistered = false;
        _gameplaySpawnStarted = false;
        _spawnedPlayers.Clear();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        _runner = runner;
        TryCacheLobbyState();
        TryRegisterCallbacks();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        _runner = null;
        _lobbyState = null;
        _callbacksRegistered = false;
        _gameplaySpawnStarted = false;
        _spawnedPlayers.Clear();
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

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
