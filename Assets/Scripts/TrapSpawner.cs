using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
 
/// <summary>
/// Tuzakları (buton + trap field paketlerini) host tarafında Runner.Spawn ile spawn eder.
/// Sahneye gömülü (scene-baked) trap objeleri host/client'ta farklı NetworkId aldığı için
/// RPC'ler yanlış butona gidiyordu. Runner.Spawn'da ID'yi SUNUCU atayıp client'a bildirir,
/// böylece tüm peer'lerde aynı ID kullanılır ve RPC'ler doğru tuzağı tetikler.
///
/// Kullanım:
///   1) Her tuzağı (buton + ilgili field) TEK bir prefab haline getir. Buton'un Inspector'daki
///      trap referansı (slowField/minefield/blindField/snowFallDown veya IslandPoisonZone)
///      aynı prefab içinde kurulu olsun ki runtime'da da geçerli kalsın.
///   2) Trap objelerini sahneden (gameWorld altından) sil; yerlerine sadece konum işaretleyen
///      boş Transform'lar bırak.
///   3) Bu script'i PlayerSpawner ile aynı GameObject'e (ya da kalıcı bir objeye) ekle.
///   4) trapSpawns listesini doldur: her satıra prefab + konum işaretçisi.
///   5) MainMenuManager.EnterGameWorld içinde, player spawn çağrısının yanına ekle:
///        playerSpawnManagerObject.SendMessage("TryStartTrapSpawn", SendMessageOptions.DontRequireReceiver);
///      (TrapSpawner PlayerSpawner ile aynı objedeyse bu satır onu da tetikler.)
/// </summary>
[DisallowMultipleComponent]
public class TrapSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Serializable]
    public class TrapSpawnEntry
    {
        [Tooltip("Buton + trap field'ı tek NetworkObject paketinde barındıran prefab.")]
        public NetworkObject trapPrefab;
 
        [Tooltip("Tuzağın konacağı konum/rotasyon işaretçisi (sahnedeki boş Transform). " +
                 "Boşsa Vector3.zero kullanılır.")]
        public Transform placement;
    }
 
    [Header("Trap Spawn")]
    [Tooltip("Spawn edilecek tüm tuzak paketleri. Her satır = bir prefab + bir konum.")]
    [SerializeField] private TrapSpawnEntry[] trapSpawns;
 
    [Header("Hierarchy Parent (opsiyonel)")]
    [Tooltip("Spawn edilen tuzakların altına gireceği parent. Boşsa parent atanmaz.")]
    [SerializeField] private Transform spawnedTrapsParent;
 
    private NetworkRunner _runner;
    private bool _callbacksRegistered;
    private bool _trapsSpawned;
 
    private readonly List<NetworkObject> _spawnedTraps = new List<NetworkObject>();
 
    private void Awake()
    {
        TryCacheRunner();
    }
 
    private void OnEnable()
    {
        TryCacheRunner();
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
 
    private void TryRegisterCallbacks()
    {
        if (!TryCacheRunner())
            return;
 
        if (_callbacksRegistered)
            return;
 
        _runner.AddCallbacks(this);
        _callbacksRegistered = true;
    }
 
    /// <summary>
    /// MainMenuManager.EnterGameWorld'den SendMessage ile çağrılır.
    /// Sadece host (sunucu) spawn eder; client ID'leri sunucudan alır.
    /// </summary>
    public void TryStartTrapSpawn()
    {
        if (!TryCacheRunner())
        {
            Debug.LogError("TrapSpawner: NetworkRunner bulunamadı.");
            return;
        }
 
        TryRegisterCallbacks();
 
        if (!_runner.IsServer)
            return;
 
        if (_trapsSpawned)
        {
            Debug.Log("TrapSpawner: Tuzaklar zaten spawn edildi.");
            return;
        }
 
        if (trapSpawns == null || trapSpawns.Length == 0)
        {
            Debug.LogWarning("TrapSpawner: trapSpawns listesi boş.");
            return;
        }
 
        for (int i = 0; i < trapSpawns.Length; i++)
        {
            TrapSpawnEntry entry = trapSpawns[i];
 
            if (entry == null || entry.trapPrefab == null)
            {
                Debug.LogWarning($"TrapSpawner: trapSpawns[{i}] eksik (prefab atanmadı).");
                continue;
            }
 
            Vector3 pos = entry.placement != null ? entry.placement.position : Vector3.zero;
            Quaternion rot = entry.placement != null ? entry.placement.rotation : Quaternion.identity;
 
            // inputAuthority = null  ->  StateAuthority host'ta kalır (sahne tuzakları gibi davranır).
            NetworkObject spawned = _runner.Spawn(
                entry.trapPrefab,
                pos,
                rot,
                null,
                (runner, obj) =>
                {
                    if (spawnedTrapsParent != null && obj != null)
                        obj.transform.SetParent(spawnedTrapsParent, true);
                }
            );
 
            if (spawned != null)
            {
                _spawnedTraps.Add(spawned);
            }
            else
            {
                Debug.LogWarning($"TrapSpawner: {entry.trapPrefab.name} spawn edilemedi.");
            }
        }
 
        _trapsSpawned = true;
        Debug.Log($"TrapSpawner: {_spawnedTraps.Count} tuzak spawn edildi.");
    }
 
    private void ResetState()
    {
        _trapsSpawned = false;
        _spawnedTraps.Clear();
        _callbacksRegistered = false;
        _runner = null;
    }
 
    // ===========================================================
    // INetworkRunnerCallbacks — sadece shutdown/disconnect'te reset
    // ===========================================================
 
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        ResetState();
    }
 
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        ResetState();
    }
 
    public void OnConnectedToServer(NetworkRunner runner)
    {
        _runner = runner;
        TryRegisterCallbacks();
    }
 
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
 
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
 
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
 
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
 
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