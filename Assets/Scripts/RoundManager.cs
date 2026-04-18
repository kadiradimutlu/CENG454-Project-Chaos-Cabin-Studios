using FishNet.Object;
using FishNet.Connection;
using UnityEngine;
using System.Collections.Generic;

public class RoundManager : NetworkBehaviour
{
    [Header("World Settings")]
    public GameObject gameWorldObject; // The main object holding the entire arena
    public Transform[] spawnPoints;    // Our 4 spawn points

    [Header("Prefab")]
    public GameObject playerPrefab;    // The player prefab to spawn

    // Only the Server (Host) runs this: Called when "Start Game" is pressed in the Lobby UI
    [ServerRpc(RequireOwnership = false)]
    public void StartRoundServer()
    {
        Debug.Log("Server: Round is starting...");

        // 1. Send a message to all clients to enable the game world
        EnableWorldClient();

        // 2. Find all connected players (clients)
        int spawnIndex = 0;
        foreach (NetworkConnection conn in ServerManager.Clients.Values)
        {
            // Determine the spawn point (0, 1, 2, 3...)
            Transform spawnPoint = spawnPoints[spawnIndex % spawnPoints.Length];

            // Instantiate the player at the specified point
            GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            
            // Tell Fish-Net to spawn this object on the network and give ownership to this connection
            ServerManager.Spawn(player, conn);

            spawnIndex++;
        }
    }

    // The Server sends this to all clients (Observers): "Make the map visible!"
    [ObserversRpc]
    public void EnableWorldClient()
    {
        gameWorldObject.SetActive(true);
        Debug.Log("Client: GameWorld activated!");
    }
}