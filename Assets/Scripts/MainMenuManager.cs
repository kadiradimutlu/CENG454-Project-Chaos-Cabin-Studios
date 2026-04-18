using UnityEngine;
using TMPro;
using FishNet;
using FishNet.Transporting.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;

public class MainMenuManager : MonoBehaviour
{
    [Header("Managers")]
    public RoundManager roundManager;

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject hostJoinPanel;
    public GameObject joinCodePanel;
    public GameObject lobbyRoomPanel;

    [Header("Input & Output Fields")]
    public TMP_InputField codeInputField;
    public TextMeshProUGUI roomCodeText;

    private async void Start()
    {
        // Initialize cloud services and show main menu when the game starts
        await InitializeUnityServices();
        ShowMainMenu(); 
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Connected to Unity Services. Player ID: " + AuthenticationService.Instance.PlayerId);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to initialize Unity Services: " + e.Message);
        }
    }

    // --- PANEL NAVIGATION ---
    
    public void ShowMainMenu() { CloseAllPanels(); mainMenuPanel.SetActive(true); }
    public void ShowSettingsPanel() { CloseAllPanels(); settingsPanel.SetActive(true); }
    public void ShowHostJoinPanel() { CloseAllPanels(); hostJoinPanel.SetActive(true); }
    public void ShowJoinCodePanel() { CloseAllPanels(); joinCodePanel.SetActive(true); }
    public void ShowLobbyRoom() { CloseAllPanels(); lobbyRoomPanel.SetActive(true); }

    private void CloseAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (hostJoinPanel != null) hostJoinPanel.SetActive(false);
        if (joinCodePanel != null) joinCodePanel.SetActive(false);
        if (lobbyRoomPanel != null) lobbyRoomPanel.SetActive(false);
    }

    // --- BUTTON ACTIONS ---

    public void QuitGame()
    {
        Debug.Log("Quitting the game...");
        Application.Quit();
    }

    // --- RELAY (ROOM CODE) ACTIONS ---

    public async void ClickHost()
    {
        try
        {
            Debug.Log("Creating Relay Room...");
            
            // Create a room for 4 players (1 Host + 3 Clients)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            if (roomCodeText != null) roomCodeText.text = "Room Code: " + joinCode;
            Debug.Log("Generated Room Code: " + joinCode);

            UnityTransport utp = InstanceFinder.TransportManager.GetTransport<UnityTransport>();
            
            // FIXED: Using FishNet's built-in raw data method instead of RelayServerData
            utp.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                true // isSecure (DTLS)
            );

            // Start Server and Client for the Host
            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection();
            
            ShowLobbyRoom(); 
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to create Relay room: " + e.Message);
        }
    }

    public async void ClickJoinWithCode()
    {
        try
        {
            string joinCode = codeInputField.text;
            Debug.Log("Joining room with code: " + joinCode);

            // Request to join the allocation using the code
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            UnityTransport utp = InstanceFinder.TransportManager.GetTransport<UnityTransport>();
            
            // FIXED: Using FishNet's built-in raw data method instead of RelayServerData
            utp.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData,
                true // isSecure (DTLS)
            );

            // Start Client connection
            InstanceFinder.ClientManager.StartConnection();
            ShowLobbyRoom(); 
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to join room. Please check the code. Error: " + e.Message);
        }
    }

    // --- START MATCH ---
    
    public void StartMatch()
    {
        // Only the Host (Server) can start the actual game
        if (InstanceFinder.IsServer)
        {
            CloseAllPanels();
            Debug.Log("Game Starting! Spawning characters...");
            
            if (roundManager != null)
            {
                roundManager.StartRoundServer();
            }
            else
            {
                Debug.LogWarning("RoundManager is not assigned in the Inspector!");
            }
        }
        else
        {
            Debug.Log("Only the Host can start the game!");
        }
    }
}