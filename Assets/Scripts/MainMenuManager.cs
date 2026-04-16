using UnityEngine;
using TMPro;
using FishNet;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel; // Yeni eklenen Settings Paneli
    public GameObject hostJoinPanel;
    public GameObject joinCodePanel;
    public GameObject lobbyRoomPanel;

    [Header("Input Fields")]
    public TMP_InputField codeInputField;

    private void Start()
    {
        // Show only the main menu when the game starts
        ShowMainMenu(); 
    }

    // --- PANEL NAVIGATION ---
    
    public void ShowMainMenu()
    {
        CloseAllPanels();
        mainMenuPanel.SetActive(true);
    }

    public void ShowSettingsPanel()
    {
        CloseAllPanels();
        settingsPanel.SetActive(true);
    }

    public void ShowHostJoinPanel()
    {
        CloseAllPanels();
        hostJoinPanel.SetActive(true);
    }

    public void ShowJoinCodePanel()
    {
        CloseAllPanels();
        joinCodePanel.SetActive(true);
    }

    public void ShowLobbyRoom()
    {
        CloseAllPanels();
        lobbyRoomPanel.SetActive(true);
    }

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

    public void ClickHost()
    {
        // Starts the Fish-Net server and client, then jumps to the Lobby Panel
        InstanceFinder.ServerManager.StartConnection();
        InstanceFinder.ClientManager.StartConnection();
        
        ShowLobbyRoom(); 
    }

    public void ClickJoinWithCode()
    {
        // Gets the input (IP or Code), connects via Fish-Net, and jumps to the Lobby Panel
        string joinCodeOrIP = codeInputField.text;
        InstanceFinder.ClientManager.StartConnection(joinCodeOrIP);
        
        ShowLobbyRoom(); 
    }

    public void StartMatch()
    {
        // Only the Host (Server) can start the actual game
        if (InstanceFinder.IsServer)
        {
            CloseAllPanels();
            Debug.Log("Game Starting! Spawning characters...");
            
            // Future: Tell GameManager/RoundManager to start the timer and spawn players
        }
        else
        {
            Debug.Log("Only the Host can start the game!");
        }
    }
}