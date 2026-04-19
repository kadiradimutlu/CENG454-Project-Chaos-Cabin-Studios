using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject hostJoinPanel;
    [SerializeField] private GameObject joinCodePanel;
    [SerializeField] private GameObject lobbyPanel;

    [Header("Game World")]
    [SerializeField] private GameObject gameWorld;

    [Header("Join Code UI")]
    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private TextMeshProUGUI joinCodeInfoText;
    [SerializeField] private TextMeshProUGUI roomCodeText;

    [Header("Lobby - Player Names")]
    [SerializeField] private TextMeshProUGUI player1NameText;
    [SerializeField] private TextMeshProUGUI player2NameText;
    [SerializeField] private TextMeshProUGUI player3NameText;
    [SerializeField] private TextMeshProUGUI player4NameText;

    [Header("Lobby - Player Status")]
    [SerializeField] private TextMeshProUGUI player1StatusText;
    [SerializeField] private TextMeshProUGUI player2StatusText;
    [SerializeField] private TextMeshProUGUI player3StatusText;
    [SerializeField] private TextMeshProUGUI player4StatusText;

    [Header("Lobby - Ready Buttons")]
    [SerializeField] private Button player2ReadyButton;
    [SerializeField] private Button player3ReadyButton;
    [SerializeField] private Button player4ReadyButton;

    [Header("Lobby - Ready Button Texts")]
    [SerializeField] private TextMeshProUGUI player2ReadyButtonText;
    [SerializeField] private TextMeshProUGUI player3ReadyButtonText;
    [SerializeField] private TextMeshProUGUI player4ReadyButtonText;

    [Header("Lobby - Other Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button leaveButton;

    [Header("Fusion")]
    [SerializeField] private NetworkRunnerHandler runnerHandler;
    [SerializeField] private LobbyState lobbyStatePrefab;

    private NetworkRunner _runner;
    private LobbyState _lobbyState;

    private GameObject _currentPanel;
    private GameObject _previousPanelBeforeSettings;

    private string _currentRoomCode = "";
    private bool _localReady = false;
    private bool _gameWorldOpened = false;

    private void Awake()
    {
        if (runnerHandler == null)
            runnerHandler = GetComponent<NetworkRunnerHandler>();

        if (runnerHandler != null)
            _runner = runnerHandler.GetRunner();

        if (gameWorld != null)
            gameWorld.SetActive(false);

        OpenPanel(mainMenuPanel);
        RefreshLobbyUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBack();
        }

        if (_lobbyState != null && _lobbyState.Object != null)
        {
            if (_lobbyState.GameStarted && !_gameWorldOpened)
            {
                EnterGameWorld();
            }
        }
    }

    // ==================================================
    // MAIN MENU
    // ==================================================

    public void ClickPlay()
    {
        OpenPanel(hostJoinPanel);
    }

    public void ClickSettings()
    {
        _previousPanelBeforeSettings = _currentPanel;
        OpenPanel(settingsPanel);
    }

    public void ClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ==================================================
    // HOST / JOIN
    // ==================================================

    public async void ClickHost()
    {
        _currentRoomCode = GenerateRandomCode(6);

        if (roomCodeText != null)
            roomCodeText.text = $"Room Code: {_currentRoomCode}";

        await StartFusion(GameMode.Host, _currentRoomCode);
    }

    public void ClickJoin()
    {
        if (codeInputField != null)
            codeInputField.text = "";

        if (joinCodeInfoText != null)
            joinCodeInfoText.text = "Join code gir";

        OpenPanel(joinCodePanel);
    }

    public async void ClickJoinYes()
    {
        string joinCode = codeInputField != null ? codeInputField.text.Trim().ToUpper() : string.Empty;

        if (joinCode.Contains(":"))
        {
            string[] parts = joinCode.Split(':');
            joinCode = parts[parts.Length - 1].Trim().ToUpper();
        }

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogWarning("Join code boş olamaz.");
            if (joinCodeInfoText != null)
                joinCodeInfoText.text = "Kod boş olamaz";
            return;
        }

        _currentRoomCode = joinCode;

        if (roomCodeText != null)
            roomCodeText.text = $"Room Code: {_currentRoomCode}";

        await StartFusion(GameMode.Client, _currentRoomCode);
    }

    public void ClickJoinNo()
    {
        OpenPanel(hostJoinPanel);
    }

    // ==================================================
    // LOBBY
    // ==================================================

    public void ClickReady()
    {
        if (_runner == null || _lobbyState == null)
            return;

        if (_lobbyState.IsHostPlayer(_runner.LocalPlayer))
            return;

        _localReady = !_localReady;
        _lobbyState.RPC_SetReady(_localReady);
        RefreshLobbyUI();
    }

    public void ClickStartGame()
    {
        if (_runner == null || _lobbyState == null)
            return;

        if (!_lobbyState.IsHostPlayer(_runner.LocalPlayer))
            return;

        _lobbyState.RPC_RequestStartGame();
    }

    public async void LeaveLobby()
    {
        _localReady = false;
        _gameWorldOpened = false;
        _lobbyState = null;

        if (gameWorld != null)
            gameWorld.SetActive(false);

        if (runnerHandler != null)
            await runnerHandler.ShutdownRunner();

        _runner = runnerHandler != null ? runnerHandler.GetRunner() : null;

        OpenPanel(hostJoinPanel);
        RefreshLobbyUI();
    }

    // ==================================================
    // BACK / ESC
    // ==================================================

    public void HandleBack()
    {
        if (_currentPanel == settingsPanel)
        {
            OpenPanel(_previousPanelBeforeSettings != null ? _previousPanelBeforeSettings : mainMenuPanel);
            return;
        }

        if (_currentPanel == joinCodePanel)
        {
            OpenPanel(hostJoinPanel);
            return;
        }

        if (_currentPanel == hostJoinPanel)
        {
            OpenPanel(mainMenuPanel);
            return;
        }

        if (_currentPanel == lobbyPanel)
        {
            LeaveLobby();
            return;
        }

        if (_currentPanel == mainMenuPanel)
        {
            return;
        }
    }

    // ==================================================
    // FUSION START
    // ==================================================

    private async Task StartFusion(GameMode mode, string sessionName)
    {
        if (runnerHandler == null)
        {
            Debug.LogError("NetworkRunnerHandler atanmadı.");
            return;
        }

        StartGameResult result = await runnerHandler.StartGame(mode, sessionName);

        if (!result.Ok)
        {
            Debug.LogError($"Fusion StartGame başarısız: {result.ShutdownReason}");
            return;
        }

        _runner = runnerHandler.GetRunner();

        OpenPanel(lobbyPanel);

        if (mode == GameMode.Host)
            SpawnLobbyStateIfNeeded();

        TryFindLobbyState();
        RefreshLobbyUI();
    }

    private void SpawnLobbyStateIfNeeded()
    {
        if (_runner == null || !_runner.IsServer || lobbyStatePrefab == null)
            return;

        if (_lobbyState != null)
            return;

        _lobbyState = _runner.Spawn(lobbyStatePrefab, Vector3.zero, Quaternion.identity, null);
    }

    private void TryFindLobbyState()
    {
        if (_lobbyState == null)
            _lobbyState = FindObjectOfType<LobbyState>();
    }

    public void RegisterLobbyState(LobbyState state)
    {
        _lobbyState = state;
        RefreshLobbyUI();
    }

    // ==================================================
    // GAME START
    // ==================================================

    private void EnterGameWorld()
    {
        _gameWorldOpened = true;

        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);

        if (gameWorld != null)
            gameWorld.SetActive(true);

        Debug.Log("GameWorld açıldı.");
    }

    // ==================================================
    // UI
    // ==================================================

    private void OpenPanel(GameObject panel)
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (hostJoinPanel) hostJoinPanel.SetActive(false);
        if (joinCodePanel) joinCodePanel.SetActive(false);
        if (lobbyPanel) lobbyPanel.SetActive(false);

        if (panel != null)
            panel.SetActive(true);

        _currentPanel = panel;
    }

    private void RefreshLobbyUI()
    {
        if (player1NameText) player1NameText.text = "Player 1: Empty";
        if (player2NameText) player2NameText.text = "Player 2: Empty";
        if (player3NameText) player3NameText.text = "Player 3: Empty";
        if (player4NameText) player4NameText.text = "Player 4: Empty";

        if (player1StatusText) player1StatusText.text = "";
        if (player2StatusText) player2StatusText.text = "";
        if (player3StatusText) player3StatusText.text = "";
        if (player4StatusText) player4StatusText.text = "";

        if (player2ReadyButton) player2ReadyButton.gameObject.SetActive(false);
        if (player3ReadyButton) player3ReadyButton.gameObject.SetActive(false);
        if (player4ReadyButton) player4ReadyButton.gameObject.SetActive(false);

        if (startButton) startButton.gameObject.SetActive(false);

        if (roomCodeText != null && !string.IsNullOrEmpty(_currentRoomCode))
            roomCodeText.text = $"Room Code: {_currentRoomCode}";

        if (_runner == null || _lobbyState == null)
            return;

        LobbyState.LobbySlotData[] slots = _lobbyState.GetSlots();

        if (slots[0].HasPlayer)
        {
            if (player1NameText) player1NameText.text = "Player 1 (Host)";
            if (player1StatusText) player1StatusText.text = "HOST";
        }

        if (slots[1].HasPlayer)
        {
            if (player2NameText) player2NameText.text = "Player 2";
            if (player2StatusText) player2StatusText.text = slots[1].IsReady ? "READY" : "NOT READY";
        }

        if (slots[2].HasPlayer)
        {
            if (player3NameText) player3NameText.text = "Player 3";
            if (player3StatusText) player3StatusText.text = slots[2].IsReady ? "READY" : "NOT READY";
        }

        if (slots[3].HasPlayer)
        {
            if (player4NameText) player4NameText.text = "Player 4";
            if (player4StatusText) player4StatusText.text = slots[3].IsReady ? "READY" : "NOT READY";
        }

        bool isHost = _lobbyState.IsHostPlayer(_runner.LocalPlayer);
        int localSlotIndex = _lobbyState.GetPlayerSlotIndex(_runner.LocalPlayer);

        if (!isHost)
        {
            if (localSlotIndex == 1 && player2ReadyButton != null)
            {
                player2ReadyButton.gameObject.SetActive(true);
                if (player2ReadyButtonText) player2ReadyButtonText.text = _localReady ? "Unready" : "Ready";
            }
            else if (localSlotIndex == 2 && player3ReadyButton != null)
            {
                player3ReadyButton.gameObject.SetActive(true);
                if (player3ReadyButtonText) player3ReadyButtonText.text = _localReady ? "Unready" : "Ready";
            }
            else if (localSlotIndex == 3 && player4ReadyButton != null)
            {
                player4ReadyButton.gameObject.SetActive(true);
                if (player4ReadyButtonText) player4ReadyButtonText.text = _localReady ? "Unready" : "Ready";
            }
        }

        if (startButton != null)
        {
            startButton.gameObject.SetActive(isHost);
            startButton.interactable = isHost && _lobbyState.CanHostStartGame();
        }
    }

    private string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();
        char[] buffer = new char[length];

        for (int i = 0; i < length; i++)
            buffer[i] = chars[random.Next(chars.Length)];

        return new string(buffer);
    }

    // ==================================================
    // FUSION CALLBACKS
    // ==================================================

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        _runner = runner;
        Debug.Log($"Player joined: {player.PlayerId}");

        if (runner.IsServer)
        {
            SpawnLobbyStateIfNeeded();

            if (_lobbyState != null)
                _lobbyState.AssignPlayer(player);
        }

        Invoke(nameof(DelayedRefresh), 0.2f);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        _runner = runner;
        Debug.Log($"Player left: {player.PlayerId}");

        if (runner.IsServer && _lobbyState != null)
            _lobbyState.RemovePlayer(player);

        Invoke(nameof(DelayedRefresh), 0.2f);
    }

    private void DelayedRefresh()
    {
        TryFindLobbyState();

        if (_lobbyState != null && _runner != null)
            _localReady = _lobbyState.GetPlayerReady(_runner.LocalPlayer);

        RefreshLobbyUI();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        _runner = runner;
        Debug.Log("Server'a bağlandı.");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        _runner = runner;
        Debug.LogWarning($"Server bağlantısı kesildi: {reason}");

        _lobbyState = null;
        _localReady = false;
        _gameWorldOpened = false;

        if (gameWorld != null)
            gameWorld.SetActive(false);

        OpenPanel(mainMenuPanel);
        RefreshLobbyUI();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        _runner = runner;
        Debug.Log($"Shutdown: {shutdownReason}");

        _lobbyState = null;
        _localReady = false;
        _gameWorldOpened = false;

        if (gameWorld != null)
            gameWorld.SetActive(false);

        RefreshLobbyUI();
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        _runner = runner;
        TryFindLobbyState();
        RefreshLobbyUI();
    }

    public void OnSceneLoadStart(NetworkRunner runner) { }
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
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}