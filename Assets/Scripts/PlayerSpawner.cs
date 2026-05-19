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

    [Header("Hierarchy Parent")]
    [SerializeField] private Transform spawnedPlayersParent;

    [Header("Spawn Safety")]
    [SerializeField] private float spawnYOffset = 1.2f;

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

        TryCacheLobbyState();
        TryRegisterCallbacks();

        if (!_runner.IsServer)
            return;

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
            RoleHandler.PlayerRole role = i < 2
                ? RoleHandler.PlayerRole.Runner
                : RoleHandler.PlayerRole.Trapper;

            SpawnPlayerIfNeeded(activePlayers[i], role, i);
        }

        _gameplaySpawnStarted = true;
        Debug.Log("PlayerSpawner: Gameplay spawn tamamlandı.");
    }

    private void SpawnPlayerIfNeeded(PlayerRef player, RoleHandler.PlayerRole role, int indexHint = -1)
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

        Transform spawnPoint = GetSpawnPoint(indexHint);

        Vector3 spawnPosition = spawnPoint != null
            ? spawnPoint.position
            : Vector3.zero;

        spawnPosition.y += spawnYOffset;

        Quaternion spawnRotation = spawnPoint != null
            ? spawnPoint.rotation
            : Quaternion.identity;

        int skinIndex = GetSkinIndexForPlayer(player);

        NetworkObject spawnedObject = _runner.Spawn(
            playerPrefab,
            spawnPosition,
            spawnRotation,
            player,
            (runner, obj) =>
            {
                ForceSpawnTransform(obj, spawnPosition, spawnRotation);
                ParentSpawnedObject(obj);
                ApplyPlayerData(obj, role, skinIndex);
            }
        );

        if (spawnedObject != null)
        {
            ForceSpawnTransform(spawnedObject, spawnPosition, spawnRotation);
            ParentSpawnedObject(spawnedObject);
            ApplyPlayerData(spawnedObject, role, skinIndex);

            _runner.SetPlayerObject(player, spawnedObject);
            _spawnedPlayers[player] = spawnedObject;

            Debug.Log(
                $"PlayerSpawner: Player {player.PlayerId} spawn edildi. " +
                $"Role={role} | SkinIndex={skinIndex} | " +
                $"SpawnPoint={(spawnPoint != null ? spawnPoint.name : "NULL")} | " +
                $"Position={spawnPosition} | " +
                $"Parent={(spawnedObject.transform.parent != null ? spawnedObject.transform.parent.name : "ROOT")}"
            );
        }
        else
        {
            Debug.LogWarning($"PlayerSpawner: Player {player.PlayerId} spawn edilemedi.");
        }
    }

    private void ApplyPlayerData(NetworkObject obj, RoleHandler.PlayerRole role, int skinIndex)
    {
        if (obj == null)
            return;

        RoleHandler roleHandler = obj.GetComponent<RoleHandler>();
        if (roleHandler != null)
            roleHandler.currentRole = role;

        PlayerSkinApplier skinApplier = obj.GetComponent<PlayerSkinApplier>();
        if (skinApplier != null)
            skinApplier.SkinIndex = skinIndex;
    }

    private int GetSkinIndexForPlayer(PlayerRef player)
    {
        TryCacheLobbyState();

        if (_lobbyState == null)
            return 0;

        return _lobbyState.GetPlayerSkinIndex(player);
    }

    private void ForceSpawnTransform(NetworkObject obj, Vector3 position, Quaternion rotation)
    {
        if (obj == null)
            return;

        obj.transform.SetPositionAndRotation(position, rotation);

        Rigidbody rb = obj.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.position = position;
            rb.rotation = rotation;
            rb.angularVelocity = Vector3.zero;

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
        }
    }

    private void ParentSpawnedObject(NetworkObject obj)
    {
        if (obj == null)
            return;

        if (spawnedPlayersParent == null)
            return;

        obj.transform.SetParent(spawnedPlayersParent, true);
    }

    private Transform GetSpawnPoint(int indexHint)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        if (indexHint < 0)
            indexHint = 0;

        indexHint = Mathf.Clamp(indexHint, 0, spawnPoints.Length - 1);
        return spawnPoints[indexHint];
    }

    private void DespawnPlayer(PlayerRef player)
    {
        if (_runner == null || !_runner.IsServer)
            return;

        if (!_spawnedPlayers.TryGetValue(player, out NetworkObject spawnedObject))
            return;

        if (spawnedObject != null)
            _runner.Despawn(spawnedObject);

        _spawnedPlayers.Remove(player);
        Debug.Log($"PlayerSpawner: Player {player.PlayerId} despawn edildi.");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        _runner = runner;

        if (_runner.IsServer && _gameplaySpawnStarted)
        {
            int index = _spawnedPlayers.Count;

            RoleHandler.PlayerRole role = index < 2
                ? RoleHandler.PlayerRole.Runner
                : RoleHandler.PlayerRole.Trapper;

            SpawnPlayerIfNeeded(player, role, index);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        _runner = runner;

        if (_runner.IsServer)
            DespawnPlayer(player);
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
