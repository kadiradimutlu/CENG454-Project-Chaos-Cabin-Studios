using UnityEngine;
using Fusion;
using System.Threading.Tasks;

[RequireComponent(typeof(NetworkRunnerHandler))]
public class NetworkConnectionTester : MonoBehaviour
{
    private NetworkRunnerHandler _runnerHandler;

    private void Start()
    {
        _runnerHandler = GetComponent<NetworkRunnerHandler>();
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 100));
        
        if (GUILayout.Button("Start Host"))
        {
            Debug.Log("Starting as Host...");
            _ = StartGameAsync(GameMode.Host, "TestRoom"); 
        }
        
        if (GUILayout.Button("Start Client"))
        {
            Debug.Log("Starting as Client...");
            _ = StartGameAsync(GameMode.Client, "TestRoom");
        }
        
        GUILayout.EndArea();
    }

    // CS4014 uyarısını çözmek için async/await kullanan yardımcı bir metot
    private async Task StartGameAsync(GameMode mode, string roomName)
    {
        await _runnerHandler.StartGame(mode, roomName);
    }
}