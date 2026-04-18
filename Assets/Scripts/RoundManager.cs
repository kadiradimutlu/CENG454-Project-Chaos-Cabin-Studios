using FishNet;
using FishNet.Object;
using FishNet.Connection;
using UnityEngine;

public class RoundManager : NetworkBehaviour
{
    [Header("World Settings")]
    [SerializeField] private GameObject gameWorldObject;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Prefab")]
    [SerializeField] private NetworkObject playerPrefab;

    private bool _roundStarted;

    /// <summary>
    /// Only call this from the server/host.
    /// </summary>
    public void StartRoundServer()
    {
        if (!IsServer)
        {
            Debug.LogWarning("StartRoundServer can only be called on the server.");
            return;
        }

        if (_roundStarted)
        {
            Debug.LogWarning("Round already started.");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab is not assigned.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned.");
            return;
        }

        _roundStarted = true;

        Debug.Log("Server: Round is starting...");

        EnableWorldServer();
        EnableWorldClient();

        SpawnPlayersForConnectedClients();
    }

    private void SpawnPlayersForConnectedClients()
    {
        int spawnIndex = 0;

        foreach (NetworkConnection conn in InstanceFinder.ServerManager.Clients.Values)
        {
            if (conn == null || !conn.IsActive)
                continue;

            Transform spawnPoint = spawnPoints[spawnIndex % spawnPoints.Length];

            NetworkObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            InstanceFinder.ServerManager.Spawn(player, conn);

            spawnIndex++;
        }
    }

    private void EnableWorldServer()
    {
        if (gameWorldObject != null)
            gameWorldObject.SetActive(true);
    }

    [ObserversRpc(BufferLast = true)]
    private void EnableWorldClient()
    {
        if (gameWorldObject != null)
            gameWorldObject.SetActive(true);

        Debug.Log("Client: GameWorld activated!");
    }

    public void ResetRoundState()
    {
        _roundStarted = false;
    }
}