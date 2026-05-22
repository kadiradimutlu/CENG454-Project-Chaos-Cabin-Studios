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
    [SerializeField] private GameObject menuCameraObject;
    [SerializeField] private GameObject playerSpawnManagerObject;

    [Header("Join Code UI")]
    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private TextMeshProUGUI joinCodeInfoText;
    [SerializeField] private TextMeshProUGUI roomCodeText;

    [Header("Lobby Code Copy UI")]
    [SerializeField] private Button copyRoomCodeButton;
    [SerializeField] private TextMeshProUGUI copyRoomCodeButtonText;

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

    [Header("Lobby - Role Selection Per Slot")]
    [Tooltip("Size 4. Element 0 = Player 1 Runner button, Element 1 = Player 2 Runner button, etc.")]
    [SerializeField] private Button[] runnerRoleButtons;

    [Tooltip("Size 4. Element 0 = Player 1 Trapper button, Element 1 = Player 2 Trapper button, etc.")]
    [SerializeField] private Button[] trapperRoleButtons;

    [Tooltip("Optional. Size 4. Shows selected role text for each lobby slot.")]
    [SerializeField] private TextMeshProUGUI[] roleInfoTexts;

    [Header("Lobby - Skin Selection")]
    [SerializeField] private Button[] previousSkinButtons;
    [SerializeField] private Button[] nextSkinButtons;
    [SerializeField] private TextMeshProUGUI[] skinNameTexts;
    [SerializeField] private Image[] skinPreviewImages;

    [Header("Lobby - 3D Character Preview")]
    [SerializeField] private LobbyCharacterPreview[] characterPreviewDisplays;

    [Header("Lobby - Other Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button leaveButton;

    [Header("Fusion")]
    [SerializeField] private NetworkRunnerHandler networkManagersPrefab;
    [SerializeField] private LobbyState lobbyStatePrefab;

    private NetworkRunnerHandler runnerHandler;
    private NetworkRunner _runner;
    private LobbyState _lobbyState;

    private GameObject _currentPanel;
    private GameObject _previousPanelBeforeSettings;

    private string _currentRoomCode = "";
    private bool _localReady = false;
    private bool _gameWorldOpened = false;

    private float _uiRefreshTimer = 0f;
    private const float UiRefreshInterval = 0.10f;

    private bool _isLeavingLobby = false;
    private bool _isStartingFusion = false;

    private void Awake()
    {
        EnsureRunnerHandler();
        SetupSkinSelectionButtons();
        SetupRoleSelectionButtons();

        SetGameplayView(false);
        ResetCopyRoomCodeButtonText();
        OpenPanel(mainMenuPanel);
        RefreshLobbyUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBack();
        }

        if (!IsLobbyStateAlive())
        {
            _lobbyState = null;
        }
        else
        {
            if (_lobbyState.GameStarted && !_gameWorldOpened)
            {
                Debug.Log("GameStarted detected in MainMenuManager. Entering GameWorld.");
                EnterGameWorld();
            }
        }

        if (_currentPanel == lobbyPanel && IsLobbyStateAlive())
        {
            _uiRefreshTimer += Time.deltaTime;

            if (_uiRefreshTimer >= UiRefreshInterval)
            {
                _uiRefreshTimer = 0f;
                RefreshLobbyUI();
            }
        }
    }

    // ==================================================
    // HELPERS
    // ==================================================

    private bool EnsureRunnerHandler()
    {
        if (runnerHandler != null && runnerHandler.IsReusable())
        {
            _runner = runnerHandler.GetRunner();
            return true;
        }

        if (NetworkRunnerHandler.Instance != null &&
            IsSceneObject(NetworkRunnerHandler.Instance) &&
            NetworkRunnerHandler.Instance.IsReusable())
        {
            runnerHandler = NetworkRunnerHandler.Instance;
            _runner = runnerHandler.GetRunner();
            return true;
        }

        NetworkRunnerHandler found = FindObjectOfType<NetworkRunnerHandler>();

        if (found != null && IsSceneObject(found) && found.IsReusable())
        {
            runnerHandler = found;
            _runner = runnerHandler.GetRunner();
            return true;
        }

        DestroyLiveRunnerHandlerObject();

        if (networkManagersPrefab != null)
        {
            runnerHandler = Instantiate(networkManagersPrefab);
            _runner = runnerHandler != null ? runnerHandler.GetRunner() : null;
            return runnerHandler != null;
        }

        Debug.LogError("NetworkRunnerHandler could not be found and networkManagersPrefab is not assigned.");
        return false;
    }

    private bool IsSceneObject(UnityEngine.Object obj)
    {
        if (obj == null)
            return false;

        if (obj is Component component)
            return component.gameObject.scene.IsValid();

        if (obj is GameObject gameObject)
            return gameObject.scene.IsValid();

        return false;
    }

    private void DestroyLiveRunnerHandlerObject()
    {
        GameObject sceneObjectToDestroy = null;

        if (runnerHandler != null && IsSceneObject(runnerHandler))
        {
            sceneObjectToDestroy = runnerHandler.gameObject;
        }
        else if (NetworkRunnerHandler.Instance != null && IsSceneObject(NetworkRunnerHandler.Instance))
        {
            sceneObjectToDestroy = NetworkRunnerHandler.Instance.gameObject;
        }
        else
        {
            NetworkRunnerHandler found = FindObjectOfType<NetworkRunnerHandler>();

            if (found != null && IsSceneObject(found))
                sceneObjectToDestroy = found.gameObject;
        }

        runnerHandler = null;
        _runner = null;

        if (sceneObjectToDestroy != null)
        {
            Destroy(sceneObjectToDestroy);
        }
    }

    private bool IsLobbyStateAlive()
    {
        return _lobbyState != null;
    }

    private void SafeClearLobbyState()
    {
        _lobbyState = null;
    }

    private void ResetCopyRoomCodeButtonText()
    {
        if (copyRoomCodeButtonText != null)
            copyRoomCodeButtonText.text = "Copy";
    }

    private void SetGameplayView(bool gameplayActive)
    {
        if (gameWorld != null)
            gameWorld.SetActive(gameplayActive);

        if (!gameplayActive)
        {
            EnableMenuCameraForMenu();
        }
    }

    private void CloseAllPanels()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (hostJoinPanel) hostJoinPanel.SetActive(false);
        if (joinCodePanel) joinCodePanel.SetActive(false);
        if (lobbyPanel) lobbyPanel.SetActive(false);

        _currentPanel = null;
    }

    private void SetStatusText(TextMeshProUGUI textObject, string value)
    {
        if (textObject == null)
            return;

        textObject.text = value;

        if (textObject.gameObject != null)
            textObject.gameObject.SetActive(!string.IsNullOrWhiteSpace(value));
    }

    // ==================================================
    // MAIN MENU
    // ==================================================

    public void ClickPlay()
    {
        if (_isLeavingLobby || _isStartingFusion)
            return;

        OpenPanel(hostJoinPanel);
    }

    public void ClickSettings()
    {
        if (_isLeavingLobby || _isStartingFusion)
            return;

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
        if (_isLeavingLobby || _isStartingFusion)
            return;

        _isStartingFusion = true;

        try
        {
            _currentRoomCode = GenerateRandomCode(6);
            ResetCopyRoomCodeButtonText();

            if (roomCodeText != null)
                roomCodeText.text = $"Room Code: {_currentRoomCode}";

            await StartFusion(GameMode.Host, _currentRoomCode);
        }
        finally
        {
            _isStartingFusion = false;
        }
    }

    public void ClickJoin()
    {
        if (_isLeavingLobby || _isStartingFusion)
            return;

        if (codeInputField != null)
            codeInputField.text = string.Empty;

        if (joinCodeInfoText != null)
            joinCodeInfoText.text = "Enter Room Code";

        OpenPanel(joinCodePanel);
    }

    public async void ClickJoinYes()
    {
        if (_isLeavingLobby || _isStartingFusion)
            return;

        string joinCode = codeInputField != null
            ? codeInputField.text.Trim().ToUpper()
            : string.Empty;

        if (joinCode.Contains(":"))
        {
            string[] parts = joinCode.Split(':');
            joinCode = parts[parts.Length - 1].Trim().ToUpper();
        }

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogWarning("Room code cannot be empty.");

            if (joinCodeInfoText != null)
                joinCodeInfoText.text = "Room code cannot be empty.";

            return;
        }

        _isStartingFusion = true;

        try
        {
            _currentRoomCode = joinCode;
            ResetCopyRoomCodeButtonText();

            if (roomCodeText != null)
                roomCodeText.text = $"Room Code: {_currentRoomCode}";

            await StartFusion(GameMode.Client, _currentRoomCode);
        }
        finally
        {
            _isStartingFusion = false;
        }
    }

    public void ClickJoinNo()
    {
        if (_isLeavingLobby || _isStartingFusion)
            return;

        OpenPanel(hostJoinPanel);
    }

    // ==================================================
    // LOBBY
    // ==================================================

    public void ClickReady()
    {
        if (_isLeavingLobby || _isStartingFusion)
            return;

        if (_runner == null || !IsLobbyStateAlive())
            return;

        if (_lobbyState.IsHostPlayer(_runner.LocalPlayer))
            return;

        _lobbyState.RPC_ToggleReady();
    }


    private void SetupRoleSelectionButtons()
    {
        SetupRoleButtonArray(runnerRoleButtons, RoleHandler.PlayerRole.Runner);
        SetupRoleButtonArray(trapperRoleButtons, RoleHandler.PlayerRole.Trapper);
    }

    private void SetupRoleButtonArray(Button[] buttons, RoleHandler.PlayerRole role)
    {
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            int slotIndex = i;

            if (buttons[i] == null)
                continue;

            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => ClickSelectRoleForSlot(slotIndex, role));
        }
    }

    private void ClickSelectRoleForSlot(int slotIndex, RoleHandler.PlayerRole role)
    {
        if (_isLeavingLobby || _isStartingFusion)
            return;

        if (_runner == null || !IsLobbyStateAlive())
            return;

        if (_lobbyState.GameStarted)
            return;

        int localSlotIndex = _lobbyState.GetPlayerSlotIndex(_runner.LocalPlayer);

        // Her oyuncu sadece kendi slotundaki Runner/Trapper butonlarını kullanabilir.
        if (localSlotIndex != slotIndex)
            return;

        _lobbyState.RPC_SelectRole((int)role);
        RefreshLobbyUI();
    }

    private void SetupSkinSelectionButtons()
    {
        SetupSkinButtonArray(previousSkinButtons, -1);
        SetupSkinButtonArray(nextSkinButtons, 1);
    }

    private void SetupSkinButtonArray(Button[] buttons, int direction)
    {
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            int slotIndex = i;

            if (buttons[i] == null)
                continue;

            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => ClickChangeSkin(slotIndex, direction));
        }
    }

    private void ClickChangeSkin(int slotIndex, int direction)
    {
        if (_isLeavingLobby || _isStartingFusion)
            return;

        if (_runner == null || !IsLobbyStateAlive())
            return;

        int localSlotIndex = _lobbyState.GetPlayerSlotIndex(_runner.LocalPlayer);

        if (localSlotIndex != slotIndex)
            return;

        _lobbyState.RPC_ChangeSkin(direction);
    }

    public void ClickStartGame()
    {
        Debug.Log("Start button clicked.");

        if (_isLeavingLobby || _isStartingFusion)
        {
            Debug.LogWarning("Start blocked: leave/start process is still running.");
            return;
        }

        if (_runner == null)
        {
            Debug.LogWarning("Start blocked: runner is null.");
            return;
        }

        if (!IsLobbyStateAlive())
        {
            Debug.LogWarning("Start blocked: LobbyState is missing.");
            return;
        }

        bool isHost = _lobbyState.IsHostPlayer(_runner.LocalPlayer);
        bool canStart = _lobbyState.CanHostStartGame();
        int playerCount = _lobbyState.GetPlayerCount();

        Debug.Log($"Start validation -> IsHost: {isHost}, CanStart: {canStart}, PlayerCount: {playerCount}");

        if (!isHost)
        {
            Debug.LogWarning("Start blocked: local player is not the host.");
            return;
        }

        if (!canStart)
        {
            Debug.LogWarning("Start blocked: CanHostStartGame returned false.");
            return;
        }

        _lobbyState.RPC_RequestStartGame();
        Debug.Log("Start game RPC sent.");
    }

    public void ClickCopyRoomCode()
    {
        string codeToCopy = _currentRoomCode;

        if (string.IsNullOrWhiteSpace(codeToCopy) && roomCodeText != null)
        {
            codeToCopy = roomCodeText.text;
        }

        if (string.IsNullOrWhiteSpace(codeToCopy))
        {
            Debug.LogWarning("No room code found to copy.");

            if (copyRoomCodeButtonText != null)
                copyRoomCodeButtonText.text = "No Code";

            return;
        }

        codeToCopy = codeToCopy.Replace("Room Code:", "").Trim();

        if (string.IsNullOrWhiteSpace(codeToCopy) || codeToCopy == "------")
        {
            Debug.LogWarning("There is no valid room code to copy.");

            if (copyRoomCodeButtonText != null)
                copyRoomCodeButtonText.text = "No Code";

            return;
        }

        bool copied = false;

        try
        {
            GUIUtility.systemCopyBuffer = codeToCopy;
            copied = !string.IsNullOrEmpty(GUIUtility.systemCopyBuffer);
        }
        catch (Exception e)
        {
            Debug.LogWarning("GUIUtility copy failed: " + e.Message);
        }

        if (!copied)
        {
            try
            {
                TextEditor te = new TextEditor();
                te.text = codeToCopy;
                te.SelectAll();
                te.Copy();
                copied = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("TextEditor copy failed: " + e.Message);
            }
        }

        if (copied)
        {
            Debug.Log("Room code copied: " + codeToCopy);

            if (copyRoomCodeButtonText != null)
                copyRoomCodeButtonText.text = "Copied!";
        }
        else
        {
            Debug.LogWarning("Room code could not be copied.");

            if (copyRoomCodeButtonText != null)
                copyRoomCodeButtonText.text = "Failed";
        }

        CancelInvoke(nameof(ResetCopyRoomCodeButtonText));
        Invoke(nameof(ResetCopyRoomCodeButtonText), 1.5f);
    }

    public async void LeaveLobby()
    {
        if (_isLeavingLobby)
            return;

        _isLeavingLobby = true;

        try
        {
            _localReady = false;
            _gameWorldOpened = false;
            _uiRefreshTimer = 0f;
            _currentRoomCode = "";
            ResetCopyRoomCodeButtonText();

            SetGameplayView(false);
            SafeClearLobbyState();

            if (runnerHandler != null)
            {
                await runnerHandler.ShutdownRunner();
            }

            DestroyLiveRunnerHandlerObject();

            _lobbyState = null;

            OpenPanel(mainMenuPanel);
            RefreshLobbyUI();
        }
        catch (Exception ex)
        {
            Debug.LogError($"LeaveLobby failed: {ex.Message}");

            SafeClearLobbyState();
            DestroyLiveRunnerHandlerObject();

            _localReady = false;
            _gameWorldOpened = false;
            _currentRoomCode = "";
            _uiRefreshTimer = 0f;
            ResetCopyRoomCodeButtonText();

            SetGameplayView(false);

            OpenPanel(mainMenuPanel);
            RefreshLobbyUI();
        }
        finally
        {
            _isLeavingLobby = false;
            _isStartingFusion = false;
        }
    }

    // ==================================================
    // BACK / ESC
    // ==================================================

    public void HandleBack()
    {
        if (_isLeavingLobby || _isStartingFusion)
            return;

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
    }

    // ==================================================
    // FUSION START
    // ==================================================

    private async Task StartFusion(GameMode mode, string sessionName)
    {
        if (_isLeavingLobby)
        {
            Debug.LogWarning("A new connection cannot be started while leaving the lobby.");
            return;
        }

        if (!EnsureRunnerHandler())
            return;

        StartGameResult result = await runnerHandler.StartGame(mode, sessionName);

        if (!result.Ok)
        {
            Debug.LogError($"Fusion StartGame failed: {result.ShutdownReason}");
            DestroyLiveRunnerHandlerObject();
            return;
        }

        _runner = runnerHandler.GetRunner();

        if (_runner == null)
        {
            Debug.LogError("NetworkRunner could not be retrieved.");
            DestroyLiveRunnerHandlerObject();
            return;
        }

        SetGameplayView(false);
        OpenPanel(lobbyPanel);

        if (mode == GameMode.Host)
        {
            SpawnLobbyStateIfNeeded();

            if (_lobbyState != null &&
                _runner.LocalPlayer != default &&
                !_lobbyState.ContainsPlayer(_runner.LocalPlayer))
            {
                _lobbyState.AssignPlayer(_runner.LocalPlayer);
            }
        }

        TryFindLobbyState();
        RefreshLobbyUI();

        Debug.Log($"Lobby opened successfully. Mode: {mode}, Room: {sessionName}");
    }

    private void SpawnLobbyStateIfNeeded()
    {
        if (_runner == null || !_runner.IsServer || lobbyStatePrefab == null)
            return;

        if (IsLobbyStateAlive())
            return;

        _lobbyState = _runner.Spawn(lobbyStatePrefab, Vector3.zero, Quaternion.identity, null);

        if (_lobbyState != null && _runner.LocalPlayer != default)
        {
            _lobbyState.AssignPlayer(_runner.LocalPlayer);
        }
    }

    private void TryFindLobbyState()
    {
        if (IsLobbyStateAlive())
            return;

        _lobbyState = FindObjectOfType<LobbyState>();
    }

    public void RegisterLobbyState(LobbyState state)
    {
        if (state == null)
            return;

        _lobbyState = state;
        RefreshLobbyUI();
    }

    // ==================================================
    // GAME START
    // ==================================================

    private void EnterGameWorld()
    {
        if (_gameWorldOpened)
            return;

        _gameWorldOpened = true;

        CloseAllPanels();
        SetGameplayView(true);

        if (playerSpawnManagerObject != null)
        {
            playerSpawnManagerObject.SendMessage("TryStartGameplaySpawn", SendMessageOptions.DontRequireReceiver);
        }

        Debug.Log("GameWorld opened. Gameplay has started.");
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
        if (player1NameText) player1NameText.text = "Player 1";
        if (player2NameText) player2NameText.text = "Player 2";
        if (player3NameText) player3NameText.text = "Player 3";
        if (player4NameText) player4NameText.text = "Player 4";

        SetStatusText(player1StatusText, "");
        SetStatusText(player2StatusText, "");
        SetStatusText(player3StatusText, "");
        SetStatusText(player4StatusText, "");

        if (player2ReadyButton) player2ReadyButton.gameObject.SetActive(false);
        if (player3ReadyButton) player3ReadyButton.gameObject.SetActive(false);
        if (player4ReadyButton) player4ReadyButton.gameObject.SetActive(false);

        if (player2ReadyButtonText) player2ReadyButtonText.text = "Ready";
        if (player3ReadyButtonText) player3ReadyButtonText.text = "Ready";
        if (player4ReadyButtonText) player4ReadyButtonText.text = "Ready";

        // Do not clear/re-enable slot UI every refresh.
        // Clearing here causes role/skin buttons to flicker because RefreshLobbyUI runs repeatedly.

        if (roomCodeText != null)
        {
            roomCodeText.text = string.IsNullOrEmpty(_currentRoomCode)
                ? "Room Code: ------"
                : $"Room Code: {_currentRoomCode}";
        }

        if (copyRoomCodeButton != null)
        {
            bool hasRoomCode = !string.IsNullOrWhiteSpace(_currentRoomCode);
            copyRoomCodeButton.gameObject.SetActive(hasRoomCode);
            copyRoomCodeButton.interactable = hasRoomCode;
        }

        if (_runner == null || !IsLobbyStateAlive())
        {
            ClearSkinSelectionUI();
            ClearRoleSelectionUI();

            if (startButton != null)
            {
                startButton.gameObject.SetActive(false);
                startButton.interactable = false;
            }

            return;
        }

        LobbyState.LobbySlotData[] slots = _lobbyState.GetSlots();

        if (_runner.IsServer && _runner.LocalPlayer != default)
        {
            if (!slots[0].HasPlayer)
            {
                _lobbyState.AssignPlayer(_runner.LocalPlayer);
                slots = _lobbyState.GetSlots();
            }
        }

        if (slots.Length > 0)
        {
            if (player1NameText) player1NameText.text = slots[0].DisplayName;
            SetStatusText(player1StatusText, slots[0].StatusText);
        }

        if (slots.Length > 1)
        {
            if (player2NameText) player2NameText.text = slots[1].DisplayName;
            SetStatusText(player2StatusText, slots[1].StatusText);
        }

        if (slots.Length > 2)
        {
            if (player3NameText) player3NameText.text = slots[2].DisplayName;
            SetStatusText(player3StatusText, slots[2].StatusText);
        }

        if (slots.Length > 3)
        {
            if (player4NameText) player4NameText.text = slots[3].DisplayName;
            SetStatusText(player4StatusText, slots[3].StatusText);
        }

        bool isHost = _lobbyState.IsHostPlayer(_runner.LocalPlayer);
        int localSlotIndex = _lobbyState.GetPlayerSlotIndex(_runner.LocalPlayer);

        RefreshSkinSelectionUI(slots, localSlotIndex);
        RefreshRoleSelectionUI(localSlotIndex);

        _localReady = _lobbyState.GetPlayerReady(_runner.LocalPlayer);

        if (!isHost)
        {
            if (localSlotIndex == 1 && player2ReadyButton != null)
            {
                player2ReadyButton.gameObject.SetActive(true);
                player2ReadyButton.interactable = true;

                if (player2ReadyButtonText)
                    player2ReadyButtonText.text = _localReady ? "Cancel Ready" : "Ready";
            }
            else if (localSlotIndex == 2 && player3ReadyButton != null)
            {
                player3ReadyButton.gameObject.SetActive(true);
                player3ReadyButton.interactable = true;

                if (player3ReadyButtonText)
                    player3ReadyButtonText.text = _localReady ? "Cancel Ready" : "Ready";
            }
            else if (localSlotIndex == 3 && player4ReadyButton != null)
            {
                player4ReadyButton.gameObject.SetActive(true);
                player4ReadyButton.interactable = true;

                if (player4ReadyButtonText)
                    player4ReadyButtonText.text = _localReady ? "Cancel Ready" : "Ready";
            }
        }

        if (startButton != null)
        {
            bool isHostForStart = _lobbyState.IsHostPlayer(_runner.LocalPlayer);
            bool canStartGame = _lobbyState.CanHostStartGame();

            startButton.gameObject.SetActive(isHostForStart);
            startButton.interactable = isHostForStart && canStartGame;
        }
    }


    private void RefreshRoleSelectionUI(int localSlotIndex)
    {
        LobbyState.LobbySlotData[] slots = IsLobbyStateAlive()
            ? _lobbyState.GetSlots()
            : null;

        for (int i = 0; i < 4; i++)
        {
            bool slotExists = slots != null && i < slots.Length;
            bool slotHasPlayer = slotExists && slots[i].HasPlayer;
            bool isLocalSlot = i == localSlotIndex;
            bool canChooseThisSlot = _runner != null &&
                                     IsLobbyStateAlive() &&
                                     !_lobbyState.GameStarted &&
                                     slotHasPlayer &&
                                     isLocalSlot;

            RoleHandler.PlayerRole slotRole = slotExists
                ? slots[i].Role
                : RoleHandler.PlayerRole.None;

            Button runnerButton = GetArrayItem(runnerRoleButtons, i);
            if (runnerButton != null)
            {
                runnerButton.gameObject.SetActive(slotHasPlayer);
                runnerButton.interactable = canChooseThisSlot && slotRole != RoleHandler.PlayerRole.Runner;
            }

            Button trapperButton = GetArrayItem(trapperRoleButtons, i);
            if (trapperButton != null)
            {
                trapperButton.gameObject.SetActive(slotHasPlayer);
                trapperButton.interactable = canChooseThisSlot && slotRole != RoleHandler.PlayerRole.Trapper;
            }

            TextMeshProUGUI roleInfoText = GetArrayItem(roleInfoTexts, i);
            if (roleInfoText != null)
            {
                roleInfoText.gameObject.SetActive(slotHasPlayer);

                if (slotHasPlayer)
                {
                    string roleName = slotRole == RoleHandler.PlayerRole.None
                        ? "Choose"
                        : slots[i].RoleName;

                    roleInfoText.text = $"Role: {roleName}";
                }
                else
                {
                    roleInfoText.text = string.Empty;
                }
            }
        }
    }

    private T GetArrayItem<T>(T[] array, int index) where T : class
    {
        if (array == null)
            return null;

        if (index < 0 || index >= array.Length)
            return null;

        return array[index];
    }

    private void ClearRoleSelectionUI()
    {
        for (int i = 0; i < 4; i++)
        {
            Button runnerButton = GetArrayItem(runnerRoleButtons, i);
            if (runnerButton != null)
            {
                runnerButton.gameObject.SetActive(false);
                runnerButton.interactable = false;
            }

            Button trapperButton = GetArrayItem(trapperRoleButtons, i);
            if (trapperButton != null)
            {
                trapperButton.gameObject.SetActive(false);
                trapperButton.interactable = false;
            }

            TextMeshProUGUI roleInfoText = GetArrayItem(roleInfoTexts, i);
            if (roleInfoText != null)
            {
                roleInfoText.text = string.Empty;
                roleInfoText.gameObject.SetActive(false);
            }
        }
    }

    private void RefreshSkinSelectionUI(LobbyState.LobbySlotData[] slots, int localSlotIndex)
    {
        for (int i = 0; i < 4; i++)
        {
            bool hasSlot = slots != null && i < slots.Length;
            bool hasPlayer = hasSlot && slots[i].HasPlayer;
            bool isLocalSlot = i == localSlotIndex;

            string skinName = hasPlayer ? slots[i].SkinName : string.Empty;
            int skinIndex = hasPlayer ? slots[i].SkinIndex : 0;

            if (skinNameTexts != null && i < skinNameTexts.Length && skinNameTexts[i] != null)
            {
                skinNameTexts[i].text = hasPlayer ? skinName : string.Empty;
                skinNameTexts[i].gameObject.SetActive(hasPlayer);
            }

            bool canChangeSkin = hasPlayer && isLocalSlot && !_lobbyState.GameStarted;

            if (previousSkinButtons != null && i < previousSkinButtons.Length && previousSkinButtons[i] != null)
            {
                previousSkinButtons[i].gameObject.SetActive(canChangeSkin);
                previousSkinButtons[i].interactable = canChangeSkin;
            }

            if (nextSkinButtons != null && i < nextSkinButtons.Length && nextSkinButtons[i] != null)
            {
                nextSkinButtons[i].gameObject.SetActive(canChangeSkin);
                nextSkinButtons[i].interactable = canChangeSkin;
            }

            LobbyCharacterPreview previewDisplay = GetCharacterPreviewDisplay(i);

            if (previewDisplay != null)
            {
                // Keep the preview GameObject active. Only hide/show its RawImage.
                // Deactivating the object every UI refresh can stop the RenderTexture
                // from displaying even though the runtime model and camera exist.
                if (!previewDisplay.gameObject.activeSelf)
                    previewDisplay.gameObject.SetActive(true);

                if (hasPlayer)
                {
                    previewDisplay.ShowCharacter(skinIndex);
                    previewDisplay.SetPreviewVisible(true);
                }
                else
                {
                    previewDisplay.ClearPreview();
                    previewDisplay.SetPreviewVisible(false);
                }

                if (skinPreviewImages != null && i < skinPreviewImages.Length && skinPreviewImages[i] != null)
                    skinPreviewImages[i].gameObject.SetActive(false);

                continue;
            }

            if (skinPreviewImages != null && i < skinPreviewImages.Length && skinPreviewImages[i] != null)
            {
                Image previewImage = skinPreviewImages[i];
                previewImage.gameObject.SetActive(hasPlayer);

                if (hasPlayer)
                {
                    CharacterSkinDatabase database = CharacterSkinDatabase.Instance;

                    if (database != null)
                    {
                        Sprite previewSprite = database.GetPreviewSprite(skinIndex);
                        Color previewColor = database.GetPreviewColor(skinIndex);
                        previewColor.a = 1f;

                        previewImage.enabled = true;
                        previewImage.sprite = previewSprite;
                        previewImage.color = previewSprite != null ? Color.white : previewColor;
                        previewImage.canvasRenderer.SetAlpha(1f);
                        previewImage.SetAllDirty();
                    }
                }
            }
        }
    }

    private LobbyCharacterPreview GetCharacterPreviewDisplay(int index)
    {
        if (characterPreviewDisplays == null)
            return null;

        if (index < 0 || index >= characterPreviewDisplays.Length)
            return null;

        return characterPreviewDisplays[index];
    }

    private void ClearSkinSelectionUI()
    {
        for (int i = 0; i < 4; i++)
        {
            if (skinNameTexts != null && i < skinNameTexts.Length && skinNameTexts[i] != null)
            {
                skinNameTexts[i].text = string.Empty;
                skinNameTexts[i].gameObject.SetActive(false);
            }

            if (previousSkinButtons != null && i < previousSkinButtons.Length && previousSkinButtons[i] != null)
                previousSkinButtons[i].gameObject.SetActive(false);

            if (nextSkinButtons != null && i < nextSkinButtons.Length && nextSkinButtons[i] != null)
                nextSkinButtons[i].gameObject.SetActive(false);

            LobbyCharacterPreview previewDisplay = GetCharacterPreviewDisplay(i);
            if (previewDisplay != null)
            {
                // Keep the preview component alive; only hide the RawImage.
                // RefreshSkinSelectionUI will decide whether to show/clear each slot.
                if (!previewDisplay.gameObject.activeSelf)
                    previewDisplay.gameObject.SetActive(true);

                previewDisplay.SetPreviewVisible(false);
            }

            if (skinPreviewImages != null && i < skinPreviewImages.Length && skinPreviewImages[i] != null)
                skinPreviewImages[i].gameObject.SetActive(false);
        }
    }

    public void DisableMenuCameraForGameplay()
    {
        if (menuCameraObject != null && menuCameraObject.activeSelf)
        {
            menuCameraObject.SetActive(false);
            Debug.Log("MainMenuManager: Menu camera disabled for gameplay.");
        }
    }

    public void EnableMenuCameraForMenu()
    {
        if (menuCameraObject != null && !menuCameraObject.activeSelf)
        {
            menuCameraObject.SetActive(true);
            Debug.Log("MainMenuManager: Menu camera enabled for menu.");
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
            {
                bool assigned = _lobbyState.AssignPlayer(player);

                if (!assigned)
                {
                    Debug.LogWarning($"Player could not be assigned: {player.PlayerId}");
                }
            }
        }

        RefreshLobbyUI();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        _runner = runner;
        Debug.Log($"Player left: {player.PlayerId}");

        if (runner.IsServer && _lobbyState != null)
        {
            _lobbyState.RemovePlayer(player);
        }

        RefreshLobbyUI();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        _runner = runner;
        Debug.Log("Connected to server.");
        RefreshLobbyUI();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        _runner = null;
        SafeClearLobbyState();
        _localReady = false;
        _gameWorldOpened = false;
        _currentRoomCode = "";
        _uiRefreshTimer = 0f;
        _isLeavingLobby = false;
        _isStartingFusion = false;
        ResetCopyRoomCodeButtonText();

        SetGameplayView(false);

        DestroyLiveRunnerHandlerObject();
        OpenPanel(mainMenuPanel);
        RefreshLobbyUI();

        Debug.LogWarning($"Disconnected from server. Reason: {reason}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        _runner = null;
        SafeClearLobbyState();
        _localReady = false;
        _gameWorldOpened = false;
        _currentRoomCode = "";
        _uiRefreshTimer = 0f;
        _isLeavingLobby = false;
        _isStartingFusion = false;
        ResetCopyRoomCodeButtonText();

        SetGameplayView(false);

        DestroyLiveRunnerHandlerObject();
        OpenPanel(mainMenuPanel);
        RefreshLobbyUI();

        Debug.LogWarning($"Runner shutdown. Reason: {shutdownReason}");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        _runner = runner;
        TryFindLobbyState();
        RefreshLobbyUI();

        Debug.Log("Scene load completed.");
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("Scene load started.");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogWarning($"Connection failed. Address: {remoteAddress}, Reason: {reason}");
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"Session list updated. Session count: {sessionList.Count}");
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        Debug.Log("Custom authentication response received.");
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.LogWarning("Host migration started.");
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        Debug.Log($"Reliable data received from player {player.PlayerId}.");
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        Debug.Log($"Reliable data progress from player {player.PlayerId}: {progress}");
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        Debug.Log($"Object entered AOI for player {player.PlayerId}.");
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        Debug.Log($"Object exited AOI for player {player.PlayerId}.");
    }
}