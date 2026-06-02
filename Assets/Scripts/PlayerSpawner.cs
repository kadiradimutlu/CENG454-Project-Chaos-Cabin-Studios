using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Serializable]
    public class PlayerRoleSpawnPoints
    {
        public Transform runnerSpawnPoint;
        public Transform trapperSpawnPoint;
    }

    [Header("Player Spawn")]
    [SerializeField] private NetworkObject playerPrefab;

    [Header("Role Based Spawn Points")]
    [Tooltip("Index 0 = Player 1, Index 1 = Player 2, Index 2 = Player 3, Index 3 = Player 4")]
    [SerializeField] private PlayerRoleSpawnPoints[] roleSpawnPoints = new PlayerRoleSpawnPoints[4];

    [Header("Legacy / Fallback Spawn Points")]
    [Tooltip("Role spawn point boşsa slot index'e göre burası kullanılır.")]
    [SerializeField] private Transform[] fallbackSpawnPoints;

    [Header("Hierarchy Parent")]
    [SerializeField] private Transform spawnedPlayersParent;

    [Header("Spawn Safety")]
    [SerializeField] private float spawnYOffset = 1.2f;

    private NetworkRunner _runner;
    private LobbyState _lobbyState;

    private bool _callbacksRegistered;
    private bool _gameplaySpawnStarted;

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
        if (_runner != null && !_runner.IsShutdown)
            return true;

        _runner = FindFirstObjectByType<NetworkRunner>();
        return _runner != null && !_runner.IsShutdown;
    }

    private bool TryCacheLobbyState()
    {
        if (_lobbyState != null && IsLobbyStateUsable(_lobbyState))
            return true;

        LobbyState[] states = FindObjectsByType<LobbyState>(FindObjectsSortMode.None);

        for (int i = 0; i < states.Length; i++)
        {
            if (IsLobbyStateUsable(states[i]))
            {
                _lobbyState = states[i];
                return true;
            }
        }

        _lobbyState = null;
        return false;
    }

    private bool IsLobbyStateUsable(LobbyState state)
    {
        if (state == null)
            return false;

        if (_runner == null || _runner.IsShutdown)
            return false;

        return state.Runner == _runner;
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

    public void ResetLocalGameplayState()
    {
        if (_runner != null && _callbacksRegistered)
            _runner.RemoveCallbacks(this);

        _runner = null;
        _lobbyState = null;
        _callbacksRegistered = false;
        _gameplaySpawnStarted = false;
        _spawnedPlayers.Clear();
        RunnerCheckpoint.ClearAllProgress();
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

        activePlayers.Sort(ComparePlayersByLobbySlot);

        for (int i = 0; i < activePlayers.Count; i++)
        {
            PlayerRef player = activePlayers[i];
            RoleHandler.PlayerRole role = GetRoleForPlayer(player);

            if (role == RoleHandler.PlayerRole.None)
            {
                Debug.LogError($"PlayerSpawner: Player {player.PlayerId} role seçmemiş. Spawn iptal edildi.");
                return;
            }
        }

        for (int i = 0; i < activePlayers.Count; i++)
        {
            PlayerRef player = activePlayers[i];
            int slotIndex = GetSlotIndexForPlayer(player, i);
            RoleHandler.PlayerRole role = GetRoleForPlayer(player);

            SpawnPlayerIfNeeded(player, role, slotIndex);
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

        if (_spawnedPlayers.TryGetValue(player, out NetworkObject existingObject) && existingObject != null)
            return;

        if (playerPrefab == null)
        {
            Debug.LogError("PlayerSpawner: playerPrefab atanmadı.");
            return;
        }

        if (role == RoleHandler.PlayerRole.None)
        {
            Debug.LogError($"PlayerSpawner: Player {player.PlayerId} role None olduğu için spawn edilmedi.");
            return;
        }

        int slotIndex = GetSlotIndexForPlayer(player, indexHint);
        Transform spawnPoint = GetSpawnPoint(slotIndex, role);

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        spawnPosition.y += spawnYOffset;

        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

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

        if (spawnedObject == null)
        {
            Debug.LogWarning($"PlayerSpawner: Player {player.PlayerId} spawn edilemedi.");
            return;
        }

        ForceSpawnTransform(spawnedObject, spawnPosition, spawnRotation);
        ParentSpawnedObject(spawnedObject);
        ResetRunnerLifeForNewRound(spawnedObject, spawnPosition, spawnRotation);

        _runner.SetPlayerObject(player, spawnedObject);
        _spawnedPlayers[player] = spawnedObject;

        Debug.Log(
            $"PlayerSpawner: Player {player.PlayerId} spawn edildi. " +
            $"Slot={slotIndex + 1} | Role={role} | SkinIndex={skinIndex} | " +
            $"SpawnPoint={(spawnPoint != null ? spawnPoint.name : "NULL")} | " +
            $"Position={spawnPosition}"
        );
    }

    private void ApplyPlayerData(NetworkObject obj, RoleHandler.PlayerRole role, int skinIndex)
    {
        if (obj == null)
            return;

        RoleHandler roleHandler = obj.GetComponentInChildren<RoleHandler>(true);

        if (roleHandler != null)
        {
            roleHandler.SetRoleFromServer(role);
            Debug.Log($"PlayerSpawner: Applied role -> {role} | Player={obj.InputAuthority}");
        }
        else
        {
            Debug.LogWarning("PlayerSpawner: RoleHandler bulunamadı.");
        }

        PlayerSkinApplier skinApplier = obj.GetComponentInChildren<PlayerSkinApplier>(true);

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

    private RoleHandler.PlayerRole GetRoleForPlayer(PlayerRef player)
    {
        TryCacheLobbyState();

        if (_lobbyState == null)
        {
            Debug.LogWarning("PlayerSpawner: LobbyState bulunamadı, role None döndü.");
            return RoleHandler.PlayerRole.None;
        }

        return _lobbyState.GetPlayerRole(player);
    }

    private int GetSlotIndexForPlayer(PlayerRef player, int fallbackIndex = 0)
    {
        TryCacheLobbyState();

        if (_lobbyState != null)
        {
            int slotIndex = _lobbyState.GetPlayerSlotIndex(player);

            if (slotIndex >= 0)
                return slotIndex;
        }

        return Mathf.Max(0, fallbackIndex);
    }

    private int ComparePlayersByLobbySlot(PlayerRef a, PlayerRef b)
    {
        int aSlot = GetSlotIndexForPlayer(a, 999 + a.PlayerId);
        int bSlot = GetSlotIndexForPlayer(b, 999 + b.PlayerId);

        int slotCompare = aSlot.CompareTo(bSlot);

        if (slotCompare != 0)
            return slotCompare;

        return a.PlayerId.CompareTo(b.PlayerId);
    }

    public bool TryGetSpawnPoseForPlayer(PlayerRef player, out Vector3 position, out Quaternion rotation)
    {
        RoleHandler.PlayerRole role = GetRoleForPlayer(player);
        int slotIndex = GetSlotIndexForPlayer(player, 0);
        Transform spawnPoint = GetSpawnPoint(slotIndex, role);

        if (spawnPoint == null)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        position = spawnPoint.position;
        rotation = spawnPoint.rotation;
        return true;
    }

    private Transform GetSpawnPoint(int slotIndex, RoleHandler.PlayerRole role)
    {
        if (slotIndex < 0)
            slotIndex = 0;

        Transform roleSpawnPoint = GetRoleSpawnPoint(slotIndex, role);

        if (roleSpawnPoint != null)
            return roleSpawnPoint;

        Transform fallback = GetFallbackSpawnPoint(slotIndex);

        if (fallback != null)
        {
            Debug.LogWarning(
                $"PlayerSpawner: Slot {slotIndex + 1} için {role} spawn point boş. " +
                $"Fallback spawn point kullanıldı: {fallback.name}"
            );
        }
        else
        {
            Debug.LogWarning(
                $"PlayerSpawner: Slot {slotIndex + 1} için {role} spawn point ve fallback spawn point yok. Vector3.zero kullanılacak."
            );
        }

        return fallback;
    }

    private Transform GetRoleSpawnPoint(int slotIndex, RoleHandler.PlayerRole role)
    {
        if (roleSpawnPoints == null || roleSpawnPoints.Length == 0)
            return null;

        slotIndex = Mathf.Clamp(slotIndex, 0, roleSpawnPoints.Length - 1);

        PlayerRoleSpawnPoints points = roleSpawnPoints[slotIndex];

        if (points == null)
            return null;

        switch (role)
        {
            case RoleHandler.PlayerRole.Runner:
                return points.runnerSpawnPoint;

            case RoleHandler.PlayerRole.Trapper:
                return points.trapperSpawnPoint;

            case RoleHandler.PlayerRole.None:
            default:
                return null;
        }
    }

    private Transform GetFallbackSpawnPoint(int slotIndex)
    {
        if (fallbackSpawnPoints == null || fallbackSpawnPoints.Length == 0)
            return null;

        slotIndex = Mathf.Clamp(slotIndex, 0, fallbackSpawnPoints.Length - 1);
        return fallbackSpawnPoints[slotIndex];
    }

    private void ForceSpawnTransform(NetworkObject obj, Vector3 position, Quaternion rotation)
    {
        if (obj == null)
            return;

        obj.transform.SetPositionAndRotation(position, rotation);

        Rigidbody rb = obj.GetComponent<Rigidbody>();

        if (rb == null)
            return;

        rb.position = position;
        rb.rotation = rotation;
        rb.angularVelocity = Vector3.zero;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
    }

    private void ParentSpawnedObject(NetworkObject obj)
    {
        if (obj == null)
            return;

        if (spawnedPlayersParent == null)
            return;

        obj.transform.SetParent(spawnedPlayersParent, true);
    }

    public void TryRespawnAllPlayers()
    {
        if (!TryCacheRunner())
        {
            Debug.LogError("PlayerSpawner: NetworkRunner bulunamadı.");
            return;
        }

        if (!_runner.IsServer)
            return;

        List<PlayerRef> activePlayers = new List<PlayerRef>(_runner.ActivePlayers);

        if (activePlayers.Count == 0)
        {
            Debug.LogWarning("PlayerSpawner: Respawn edilecek aktif oyuncu yok.");
            return;
        }

        activePlayers.Sort(ComparePlayersByLobbySlot);

        for (int i = 0; i < activePlayers.Count; i++)
            TryRespawnPlayer(activePlayers[i], GetSlotIndexForPlayer(activePlayers[i], i));
    }

    public void TryRespawnPlayer(PlayerRef player)
    {
        TryRespawnPlayer(player, GetSortedPlayerIndex(player));
    }

    private void TryRespawnPlayer(PlayerRef player, int indexHint)
    {
        if (_runner == null || !_runner.IsServer)
            return;

        if (player == default)
            return;

        NetworkObject spawnedObject = GetSpawnedPlayerObject(player);

        if (spawnedObject == null)
        {
            Debug.LogWarning($"PlayerSpawner: Player {player.PlayerId} respawn edilemedi çünkü player object bulunamadı.");
            return;
        }

        RoleHandler.PlayerRole role = GetRoleForPlayer(player);

        if (role == RoleHandler.PlayerRole.None)
        {
            Debug.LogWarning($"PlayerSpawner: Player {player.PlayerId} role None. Respawn role spawn seçilemedi.");
        }

        int slotIndex = GetSlotIndexForPlayer(player, indexHint);
        Transform spawnPoint = GetSpawnPoint(slotIndex, role);

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        spawnPosition.y += spawnYOffset;

        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        RunnerLife runnerLife = spawnedObject.GetComponent<RunnerLife>();
        bool usedCheckpoint = false;

        if (runnerLife != null &&
            role == RoleHandler.PlayerRole.Runner &&
            RunnerCheckpoint.TryGetSavedCheckpoint(runnerLife, out Vector3 checkpointPosition, out Quaternion checkpointRotation))
        {
            spawnPosition = checkpointPosition;
            spawnRotation = checkpointRotation;
            usedCheckpoint = true;
        }

        RoleHandler roleHandler = spawnedObject.GetComponentInChildren<RoleHandler>(true);

        if (roleHandler != null && role != RoleHandler.PlayerRole.None)
            roleHandler.SetRoleFromServer(role);

        PlayerMovement movement = spawnedObject.GetComponent<PlayerMovement>();

        if (movement != null)
            movement.ResetMovementForRespawn(spawnPosition, spawnRotation);
        else
            ForceSpawnTransform(spawnedObject, spawnPosition, spawnRotation);

        // Burada checkpoint progress'i temizlemiyoruz.
        // Ölünce/respawn olunca en yüksek checkpoint kalmaya devam etmeli.
        if (runnerLife != null)
            runnerLife.ResetForNewRound(spawnPosition, spawnRotation);

        PlayerHealth health = spawnedObject.GetComponent<PlayerHealth>();

        if (health != null)
            health.ResetHealth();

        Debug.Log(
            $"PlayerSpawner: Player {player.PlayerId} respawn edildi. " +
            $"Slot={slotIndex + 1} | Role={role} | " +
            $"Respawn={(usedCheckpoint ? "Checkpoint" : "BaseSpawn")}"
        );
    }


    private void ResetRunnerLifeForNewRound(NetworkObject obj, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (obj == null)
            return;

        RunnerLife runnerLife = obj.GetComponent<RunnerLife>();

        if (runnerLife != null)
        {
            RunnerCheckpoint.ResetProgress(runnerLife);
            runnerLife.ResetForNewRound(spawnPosition, spawnRotation);
        }
    }

    private NetworkObject GetSpawnedPlayerObject(PlayerRef player)
    {
        if (_spawnedPlayers.TryGetValue(player, out NetworkObject spawnedObject) && spawnedObject != null)
            return spawnedObject;

        if (_runner == null)
            return null;

        spawnedObject = _runner.GetPlayerObject(player);

        if (spawnedObject != null)
            _spawnedPlayers[player] = spawnedObject;

        return spawnedObject;
    }

    private int GetSortedPlayerIndex(PlayerRef player)
    {
        if (_runner == null)
            return 0;

        List<PlayerRef> activePlayers = new List<PlayerRef>(_runner.ActivePlayers);
        activePlayers.Sort(ComparePlayersByLobbySlot);

        for (int i = 0; i < activePlayers.Count; i++)
        {
            if (activePlayers[i] == player)
                return i;
        }

        return 0;
    }

    [ContextMenu("Debug Respawn All Players")]
    private void DebugRespawnAllPlayers()
    {
        TryRespawnAllPlayers();
    }

    private void DespawnPlayer(PlayerRef player)
    {
        if (_runner == null || !_runner.IsServer)
            return;

        NetworkObject spawnedObject = GetSpawnedPlayerObject(player);

        if (spawnedObject != null)
        {
            RunnerLife runnerLife = spawnedObject.GetComponent<RunnerLife>();

            if (runnerLife != null)
                RunnerCheckpoint.ResetProgress(runnerLife);

            _runner.Despawn(spawnedObject);
        }

        _spawnedPlayers.Remove(player);
        Debug.Log($"PlayerSpawner: Player {player.PlayerId} despawn edildi.");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        _runner = runner;

        if (!_runner.IsServer)
            return;

        if (!_gameplaySpawnStarted)
            return;

        TryCacheLobbyState();

        int slotIndex = GetSlotIndexForPlayer(player, _spawnedPlayers.Count);
        RoleHandler.PlayerRole role = GetRoleForPlayer(player);

        if (role == RoleHandler.PlayerRole.None)
        {
            Debug.LogWarning($"PlayerSpawner: Player {player.PlayerId} role bilgisi bulunamadı, gameplay sırasında spawn edilmedi.");
            return;
        }

        SpawnPlayerIfNeeded(player, role, slotIndex);
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
        ResetLocalGameplayState();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        _runner = runner;
        TryCacheLobbyState();
        TryRegisterCallbacks();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        ResetLocalGameplayState();
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