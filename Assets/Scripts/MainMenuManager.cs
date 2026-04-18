using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using FishNet.Transporting;
using FishNet.Transporting.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using System.Linq;

public class MainMenuManager : NetworkBehaviour
{
    [Header("Managers")]
    public RoundManager roundManager;

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject hostJoinPanel;
    public GameObject joinCodePanel;
    public GameObject lobbyRoomPanel;
    public GameObject confirmationPanel;

    [Header("Input & Output Fields")]
    public TMP_InputField codeInputField;
    public TextMeshProUGUI roomCodeText;

    [Header("Lobby Player UI")]
    public Button startGameButton;
    public Button readyButton;
    public TextMeshProUGUI[] playerSlotTexts;

    public readonly SyncDictionary<int, bool> playerReadyStates = new SyncDictionary<int, bool>();

    private bool _isServicesInitialized = false;
    private Task<bool> _servicesInitializationTask;

    private void Awake()
    {
        CloseAllPanels();
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        _servicesInitializationTask = InitializeUnityServicesAsync();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            HandleEscapeKey();
    }

    private Task<bool> EnsureUnityServicesInitializedAsync()
    {
        if (_isServicesInitialized)
            return Task.FromResult(true);

        if (_servicesInitializationTask == null)
            _servicesInitializationTask = InitializeUnityServicesAsync();

        return _servicesInitializationTask;
    }

    private async Task<bool> InitializeUnityServicesAsync()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            _isServicesInitialized = true;
            Debug.Log("✅ Unity Services Initialized Successfully.");
            return true;
        }
        catch (System.Exception e)
        {
            _isServicesInitialized = false;
            Debug.LogError("❌ Initialization Failed: " + e.Message);
            return false;
        }
    }

    // --- PANEL NAVIGATION ---
    public void ShowMainMenu()
    {
        CloseAllPanels();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void ShowSettingsPanel()
    {
        CloseAllPanels();
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void ShowHostJoinPanel()
    {
        CloseAllPanels();
        if (hostJoinPanel != null) hostJoinPanel.SetActive(true);
    }

    public void ShowJoinCodePanel()
    {
        CloseAllPanels();
        if (joinCodePanel != null) joinCodePanel.SetActive(true);
    }

    public void ShowLobbyRoom()
    {
        CloseAllPanels();
        if (lobbyRoomPanel != null) lobbyRoomPanel.SetActive(true);
    }

    private void CloseAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (hostJoinPanel != null) hostJoinPanel.SetActive(false);
        if (joinCodePanel != null) joinCodePanel.SetActive(false);
        if (lobbyRoomPanel != null) lobbyRoomPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
    }

    // --- ESC VE GERİ DÖNÜŞ KONTROLLERİ ---
    public void HandleEscapeKey()
    {
        if (confirmationPanel != null && confirmationPanel.activeSelf)
            CloseConfirmation();
        else if (settingsPanel != null && settingsPanel.activeSelf)
            ShowMainMenu();
        else if (hostJoinPanel != null && hostJoinPanel.activeSelf)
            ShowMainMenu();
        else if (joinCodePanel != null && joinCodePanel.activeSelf)
            ShowHostJoinPanel();
        else if (lobbyRoomPanel != null && lobbyRoomPanel.activeSelf)
            OpenConfirmation();
    }

    // --- ONAY PANELİ FONKSİYONLARI ---
    public void OpenConfirmation()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(true);
    }

    public void CloseConfirmation()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }

    public void ConfirmLeaveLobby()
    {
        CloseConfirmation();
        LeaveLobby();
    }

    public void LeaveLobby()
    {
        if (InstanceFinder.NetworkManager == null)
        {
            ShowMainMenu();
            return;
        }

        if (InstanceFinder.IsServer)
            InstanceFinder.ServerManager.StopConnection(true);
        else
            InstanceFinder.ClientManager.StopConnection();

        ShowMainMenu();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // --- NETWORK LOGIC ---
    public override void OnStartServer()
    {
        base.OnStartServer();

        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnRemoteConnectionState += OnClientConnectionState;

        playerReadyStates.Clear();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnRemoteConnectionState -= OnClientConnectionState;

        playerReadyStates.Clear();
    }

    private void OnClientConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            if (!playerReadyStates.ContainsKey(conn.ClientId))
                playerReadyStates.Add(conn.ClientId, false);
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            if (playerReadyStates.ContainsKey(conn.ClientId))
                playerReadyStates.Remove(conn.ClientId);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        playerReadyStates.OnChange += OnPlayerListChanged;

        if (InstanceFinder.IsServer && InstanceFinder.IsClient)
            MarkHostReady();

        UpdateLobbyUI();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        playerReadyStates.OnChange -= OnPlayerListChanged;
    }

    private void OnPlayerListChanged(SyncDictionaryOperation op, int key, bool value, bool asServer)
    {
        UpdateLobbyUI();
    }

    private void MarkHostReady()
    {
        const int hostClientId = 0;

        if (playerReadyStates.ContainsKey(hostClientId))
            playerReadyStates[hostClientId] = true;
        else
            playerReadyStates.Add(hostClientId, true);
    }

    private void UpdateLobbyUI()
    {
        var sortedPlayers = playerReadyStates.OrderBy(p => p.Key).ToList();
        bool allClientsReady = true;

        for (int i = 0; i < playerSlotTexts.Length; i++)
        {
            if (i < sortedPlayers.Count)
            {
                bool isReady = sortedPlayers[i].Value;

                if (i == 0)
                {
                    playerSlotTexts[i].text = "Player 1 (Host) - <color=yellow>Ready</color>";
                }
                else
                {
                    string status = isReady ? "<color=green>Ready</color>" : "<color=red>Not Ready</color>";
                    playerSlotTexts[i].text = $"Player {i + 1} - {status}";
                    if (!isReady) allClientsReady = false;
                }
            }
            else
            {
                playerSlotTexts[i].text = "Waiting...";
            }
        }

        if (InstanceFinder.IsServer)
        {
            if (startGameButton) startGameButton.gameObject.SetActive(true);
            if (readyButton) readyButton.gameObject.SetActive(false);
            if (startGameButton) startGameButton.interactable = allClientsReady && sortedPlayers.Count > 1;
        }
        else
        {
            if (startGameButton) startGameButton.gameObject.SetActive(false);
            if (readyButton) readyButton.gameObject.SetActive(true);
        }
    }

    public void ClickReady()
    {
        CmdToggleReady();
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdToggleReady(NetworkConnection caller = null)
    {
        if (caller == null)
            return;

        if (playerReadyStates.ContainsKey(caller.ClientId))
            playerReadyStates[caller.ClientId] = !playerReadyStates[caller.ClientId];
    }

    // --- UI BUTTONS ---
    public void ClickJoin()
    {
        Debug.Log("🟡 Join butonuna tıklandı -> JoinCodePanel açılıyor.");
        ShowJoinCodePanel();
    }

    public async void ClickHost()
    {
        Debug.Log("🟡 Host butonuna tıklandı!");

        if (InstanceFinder.NetworkManager == null)
        {
            Debug.LogError("❌ FATAL: NetworkManager sahnede bulunamadı!");
            return;
        }

        if (!await EnsureUnityServicesInitializedAsync())
        {
            Debug.LogError("❌ Unity Services hazır değil. Host açılamadı.");
            return;
        }

        try
        {
            Debug.Log("🟡 Oda kuruluyor...");

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            if (roomCodeText != null)
                roomCodeText.text = "Room Code: " + joinCode;

            UnityTransport utp = InstanceFinder.NetworkManager.TransportManager.GetTransport<UnityTransport>();
            if (utp == null)
            {
                Debug.LogError("❌ UnityTransport bulunamadı. Package Manager'dan Unity Transport kurulu olmalı.");
                return;
            }

            utp.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                true
            );

            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection();

            MarkHostReady();
            ShowLobbyRoom();

            Debug.Log("✅ Oda başarıyla kuruldu!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Host Error: " + e.Message);
        }
    }

    public async void ClickJoinWithCode()
    {
        Debug.Log("🟡 Connect butonuna tıklandı!");

        if (InstanceFinder.NetworkManager == null)
        {
            Debug.LogError("❌ FATAL: NetworkManager sahnede bulunamadı!");
            return;
        }

        if (!await EnsureUnityServicesInitializedAsync())
        {
            Debug.LogWarning("⚠️ Unity Servisleri hazır değil!");
            return;
        }

        string joinCode = codeInputField != null ? codeInputField.text.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogWarning("⚠️ Join kodu boş olamaz.");
            return;
        }

        try
        {
            Debug.Log("🟡 Odaya bağlanılıyor...");

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            UnityTransport utp = InstanceFinder.NetworkManager.TransportManager.GetTransport<UnityTransport>();
            if (utp == null)
            {
                Debug.LogError("❌ UnityTransport bulunamadı. Package Manager'dan Unity Transport kurulu olmalı.");
                return;
            }

            utp.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData,
                true
            );

            InstanceFinder.ClientManager.StartConnection();
            ShowLobbyRoom();

            Debug.Log("✅ Odaya başarıyla katılındı!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Join Error: " + e.Message);
        }
    }

    public void StartMatch()
    {
        if (InstanceFinder.IsServer)
        {
            CloseAllPanels();
            if (roundManager != null)
                roundManager.StartRoundServer();
        }
    }
}