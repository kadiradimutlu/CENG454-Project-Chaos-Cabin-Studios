using System.Text;
using Fusion;
using UnityEngine;

public class LobbyState : NetworkBehaviour
{
    private const int MaxPlayers = 4;

    private const int ChatMaxMessageLength = 120;
    private const int ChatLineCapacity = 180;

    [Header("Lobby State")]
    [Networked] public PlayerRef Slot1Player { get; set; }
    [Networked] public NetworkBool Slot1Ready { get; set; }
    [Networked] public int Slot1SkinIndex { get; set; }
    [Networked] public RoleHandler.PlayerRole Slot1Role { get; set; }

    [Networked] public PlayerRef Slot2Player { get; set; }
    [Networked] public NetworkBool Slot2Ready { get; set; }
    [Networked] public int Slot2SkinIndex { get; set; }
    [Networked] public RoleHandler.PlayerRole Slot2Role { get; set; }

    [Networked] public PlayerRef Slot3Player { get; set; }
    [Networked] public NetworkBool Slot3Ready { get; set; }
    [Networked] public int Slot3SkinIndex { get; set; }
    [Networked] public RoleHandler.PlayerRole Slot3Role { get; set; }

    [Networked] public PlayerRef Slot4Player { get; set; }
    [Networked] public NetworkBool Slot4Ready { get; set; }
    [Networked] public int Slot4SkinIndex { get; set; }
    [Networked] public RoleHandler.PlayerRole Slot4Role { get; set; }

    [Networked] public NetworkBool GameStarted { get; set; }

    [Header("Lobby Chat")]
    [Networked] public int ChatMessageVersion { get; set; }

    [Networked, Capacity(ChatLineCapacity)] public string ChatLine1 { get; set; }
    [Networked, Capacity(ChatLineCapacity)] public string ChatLine2 { get; set; }
    [Networked, Capacity(ChatLineCapacity)] public string ChatLine3 { get; set; }
    [Networked, Capacity(ChatLineCapacity)] public string ChatLine4 { get; set; }
    [Networked, Capacity(ChatLineCapacity)] public string ChatLine5 { get; set; }
    [Networked, Capacity(ChatLineCapacity)] public string ChatLine6 { get; set; }
    [Networked, Capacity(ChatLineCapacity)] public string ChatLine7 { get; set; }
    [Networked, Capacity(ChatLineCapacity)] public string ChatLine8 { get; set; }
    [Networked, Capacity(ChatLineCapacity)] public string ChatLine9 { get; set; }
    [Networked, Capacity(ChatLineCapacity)] public string ChatLine10 { get; set; }

    public struct LobbySlotData
    {
        public bool HasPlayer;
        public bool IsHost;
        public bool IsReady;
        public bool CanUseReady;
        public PlayerRef Player;
        public string DisplayName;
        public string StatusText;
        public int SkinIndex;
        public string SkinName;
        public RoleHandler.PlayerRole Role;
        public string RoleName;
    }

    public override void Spawned()
    {
        MainMenuManager menu = FindObjectOfType<MainMenuManager>();

        if (menu != null)
        {
            menu.RegisterLobbyState(this);
        }

        if (HasStateAuthority)
        {
            EnsureValidLobbyState();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        CleanupInvalidPlayers();
    }

    // ==================================================
    // INITIAL / SAFETY
    // ==================================================

    private void EnsureValidLobbyState()
    {
        if (!HasStateAuthority)
            return;

        if (Slot1Player == default)
        {
            Slot1Ready = false;
            Slot1SkinIndex = 0;
            Slot1Role = RoleHandler.PlayerRole.None;
        }

        if (Slot2Player == default)
        {
            Slot2Ready = false;
            Slot2SkinIndex = 0;
            Slot2Role = RoleHandler.PlayerRole.None;
        }

        if (Slot3Player == default)
        {
            Slot3Ready = false;
            Slot3SkinIndex = 0;
            Slot3Role = RoleHandler.PlayerRole.None;
        }

        if (Slot4Player == default)
        {
            Slot4Ready = false;
            Slot4SkinIndex = 0;
            Slot4Role = RoleHandler.PlayerRole.None;
        }

        if (Slot1Player == default && GameStarted)
            GameStarted = false;
    }

    private void CleanupInvalidPlayers()
    {
        bool changed = false;

        if (Slot1Player != default && !Runner.IsPlayerValid(Slot1Player))
        {
            Slot1Player = default;
            Slot1Ready = false;
            Slot1SkinIndex = 0;
            Slot1Role = RoleHandler.PlayerRole.None;
            changed = true;
        }

        if (Slot2Player != default && !Runner.IsPlayerValid(Slot2Player))
        {
            Slot2Player = default;
            Slot2Ready = false;
            Slot2SkinIndex = 0;
            Slot2Role = RoleHandler.PlayerRole.None;
            changed = true;
        }

        if (Slot3Player != default && !Runner.IsPlayerValid(Slot3Player))
        {
            Slot3Player = default;
            Slot3Ready = false;
            Slot3SkinIndex = 0;
            Slot3Role = RoleHandler.PlayerRole.None;
            changed = true;
        }

        if (Slot4Player != default && !Runner.IsPlayerValid(Slot4Player))
        {
            Slot4Player = default;
            Slot4Ready = false;
            Slot4SkinIndex = 0;
            Slot4Role = RoleHandler.PlayerRole.None;
            changed = true;
        }

        if (changed)
        {
            CompactSlots();
            EnsureValidLobbyState();
        }
    }

    // ==================================================
    // PLAYER SLOT MANAGEMENT
    // ==================================================

    public bool AssignPlayer(PlayerRef player)
    {
        if (!HasStateAuthority)
            return false;

        if (player == default)
            return false;

        if (!Runner.IsPlayerValid(player))
            return false;

        if (ContainsPlayer(player))
            return true;

        if (GameStarted)
            return false;

        if (Slot1Player == default)
        {
            Slot1Player = player;
            Slot1Ready = false;
            Slot1SkinIndex = 0;
            Slot1Role = RoleHandler.PlayerRole.None;
            return true;
        }

        if (Slot2Player == default)
        {
            Slot2Player = player;
            Slot2Ready = false;
            Slot2SkinIndex = 0;
            Slot2Role = RoleHandler.PlayerRole.None;
            return true;
        }

        if (Slot3Player == default)
        {
            Slot3Player = player;
            Slot3Ready = false;
            Slot3SkinIndex = 0;
            Slot3Role = RoleHandler.PlayerRole.None;
            return true;
        }

        if (Slot4Player == default)
        {
            Slot4Player = player;
            Slot4Ready = false;
            Slot4SkinIndex = 0;
            Slot4Role = RoleHandler.PlayerRole.None;
            return true;
        }

        return false;
    }

    public void RemovePlayer(PlayerRef player)
    {
        if (!HasStateAuthority)
            return;

        if (player == default)
            return;

        if (Slot1Player == player)
        {
            Slot1Player = default;
            Slot1Ready = false;
            Slot1SkinIndex = 0;
            Slot1Role = RoleHandler.PlayerRole.None;
        }

        if (Slot2Player == player)
        {
            Slot2Player = default;
            Slot2Ready = false;
            Slot2SkinIndex = 0;
            Slot2Role = RoleHandler.PlayerRole.None;
        }

        if (Slot3Player == player)
        {
            Slot3Player = default;
            Slot3Ready = false;
            Slot3SkinIndex = 0;
            Slot3Role = RoleHandler.PlayerRole.None;
        }

        if (Slot4Player == player)
        {
            Slot4Player = default;
            Slot4Ready = false;
            Slot4SkinIndex = 0;
            Slot4Role = RoleHandler.PlayerRole.None;
        }

        CompactSlots();
        EnsureValidLobbyState();

        if (GetPlayerCount() < 2)
        {
            GameStarted = false;
        }
    }

    private void CompactSlots()
    {
        if (!HasStateAuthority)
            return;

        PlayerRef[] players = new PlayerRef[MaxPlayers]
        {
            Slot1Player,
            Slot2Player,
            Slot3Player,
            Slot4Player
        };

        bool[] readyStates = new bool[MaxPlayers]
        {
            Slot1Ready,
            Slot2Ready,
            Slot3Ready,
            Slot4Ready
        };

        int[] skinStates = new int[MaxPlayers]
        {
            Slot1SkinIndex,
            Slot2SkinIndex,
            Slot3SkinIndex,
            Slot4SkinIndex
        };

        RoleHandler.PlayerRole[] roleStates = new RoleHandler.PlayerRole[MaxPlayers]
        {
            Slot1Role,
            Slot2Role,
            Slot3Role,
            Slot4Role
        };

        PlayerRef[] compactPlayers = new PlayerRef[MaxPlayers];
        bool[] compactReady = new bool[MaxPlayers];
        int[] compactSkins = new int[MaxPlayers];
        RoleHandler.PlayerRole[] compactRoles = new RoleHandler.PlayerRole[MaxPlayers];

        int writeIndex = 0;

        for (int i = 0; i < MaxPlayers; i++)
        {
            if (players[i] != default)
            {
                compactPlayers[writeIndex] = players[i];
                compactReady[writeIndex] = writeIndex == 0 ? false : readyStates[i];
                compactSkins[writeIndex] = NormalizeSkinIndex(skinStates[i]);
                compactRoles[writeIndex] = NormalizeRole(roleStates[i]);
                writeIndex++;
            }
        }

        Slot1Player = compactPlayers[0];
        Slot1Ready = false;
        Slot1SkinIndex = compactSkins[0];
        Slot1Role = Slot1Player != default ? compactRoles[0] : RoleHandler.PlayerRole.None;

        Slot2Player = compactPlayers[1];
        Slot2Ready = compactReady[1];
        Slot2SkinIndex = compactSkins[1];
        Slot2Role = Slot2Player != default ? compactRoles[1] : RoleHandler.PlayerRole.None;

        Slot3Player = compactPlayers[2];
        Slot3Ready = compactReady[2];
        Slot3SkinIndex = compactSkins[2];
        Slot3Role = Slot3Player != default ? compactRoles[2] : RoleHandler.PlayerRole.None;

        Slot4Player = compactPlayers[3];
        Slot4Ready = compactReady[3];
        Slot4SkinIndex = compactSkins[3];
        Slot4Role = Slot4Player != default ? compactRoles[3] : RoleHandler.PlayerRole.None;
    }

    // ==================================================
    // LOOKUP HELPERS
    // ==================================================

    public bool ContainsPlayer(PlayerRef player)
    {
        return Slot1Player == player ||
               Slot2Player == player ||
               Slot3Player == player ||
               Slot4Player == player;
    }

    public bool IsHostPlayer(PlayerRef player)
    {
        return Slot1Player != default && Slot1Player == player;
    }

    public int GetPlayerSlotIndex(PlayerRef player)
    {
        if (Slot1Player == player) return 0;
        if (Slot2Player == player) return 1;
        if (Slot3Player == player) return 2;
        if (Slot4Player == player) return 3;

        return -1;
    }

    public bool GetPlayerReady(PlayerRef player)
    {
        if (Slot1Player == player) return false;
        if (Slot2Player == player) return Slot2Ready;
        if (Slot3Player == player) return Slot3Ready;
        if (Slot4Player == player) return Slot4Ready;

        return false;
    }

    public int GetPlayerCount()
    {
        int count = 0;

        if (Slot1Player != default) count++;
        if (Slot2Player != default) count++;
        if (Slot3Player != default) count++;
        if (Slot4Player != default) count++;

        return count;
    }

    public int GetConnectedClientCount()
    {
        int count = 0;

        if (Slot2Player != default) count++;
        if (Slot3Player != default) count++;
        if (Slot4Player != default) count++;

        return count;
    }

    public bool HasAnyClients()
    {
        return GetConnectedClientCount() > 0;
    }

    public bool AreAllClientsReady()
    {
        if (!HasAnyClients())
            return false;

        if (Slot2Player != default && !Slot2Ready) return false;
        if (Slot3Player != default && !Slot3Ready) return false;
        if (Slot4Player != default && !Slot4Ready) return false;

        return true;
    }

    public bool CanPlayerUseReady(PlayerRef player)
    {
        if (player == default)
            return false;

        if (!ContainsPlayer(player))
            return false;

        if (IsHostPlayer(player))
            return false;

        return true;
    }

    public LobbySlotData[] GetSlots()
    {
        return new LobbySlotData[]
        {
            BuildSlotData(0, Slot1Player, false, true),
            BuildSlotData(1, Slot2Player, Slot2Ready, false),
            BuildSlotData(2, Slot3Player, Slot3Ready, false),
            BuildSlotData(3, Slot4Player, Slot4Ready, false)
        };
    }

    private LobbySlotData BuildSlotData(int slotIndex, PlayerRef player, bool readyValue, bool isHost)
    {
        bool hasPlayer = player != default;
        int skinIndex = hasPlayer ? GetSlotSkinIndex(slotIndex) : 0;
        RoleHandler.PlayerRole role = hasPlayer ? GetSlotRole(slotIndex) : RoleHandler.PlayerRole.None;

        LobbySlotData data = new LobbySlotData
        {
            HasPlayer = hasPlayer,
            IsHost = hasPlayer && isHost,
            IsReady = hasPlayer && !isHost && readyValue,
            CanUseReady = hasPlayer && !isHost,
            Player = player,

            // Empty slots now only show Player 2 / Player 3 / Player 4.
            // Host slot shows Player 1 (Host).
            DisplayName = hasPlayer
                ? (isHost ? $"Player {slotIndex + 1} (Host)" : $"Player {slotIndex + 1}")
                : $"Player {slotIndex + 1}",

            // Empty slots show no waiting/status text.
            StatusText = GetStatusText(hasPlayer, isHost, readyValue, role),

            SkinIndex = skinIndex,
            SkinName = hasPlayer ? GetSkinDisplayName(skinIndex) : string.Empty,
            Role = role,
            RoleName = hasPlayer ? GetRoleDisplayName(role) : string.Empty
        };

        return data;
    }

    private string GetStatusText(bool hasPlayer, bool isHost, bool readyValue, RoleHandler.PlayerRole role)
    {
        if (!hasPlayer)
            return string.Empty;

        string roleText = role == RoleHandler.PlayerRole.None
            ? "No Role"
            : GetRoleDisplayName(role);

        if (isHost)
            return $"Host | {roleText}";

        string readyText = readyValue ? "Ready!" : "In Lobby";
        return $"{readyText} | {roleText}";
    }


    // ==================================================
    // ROLE SELECTION
    // ==================================================

    private RoleHandler.PlayerRole NormalizeRole(RoleHandler.PlayerRole role)
    {
        if (role == RoleHandler.PlayerRole.Runner || role == RoleHandler.PlayerRole.Trapper)
            return role;

        return RoleHandler.PlayerRole.None;
    }

    private RoleHandler.PlayerRole GetSlotRole(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return NormalizeRole(Slot1Role);
            case 1: return NormalizeRole(Slot2Role);
            case 2: return NormalizeRole(Slot3Role);
            case 3: return NormalizeRole(Slot4Role);
            default: return RoleHandler.PlayerRole.None;
        }
    }

    private void SetSlotRole(int slotIndex, RoleHandler.PlayerRole role)
    {
        role = NormalizeRole(role);

        switch (slotIndex)
        {
            case 0:
                Slot1Role = role;
                break;
            case 1:
                Slot2Role = role;
                break;
            case 2:
                Slot3Role = role;
                break;
            case 3:
                Slot4Role = role;
                break;
        }
    }

    private string GetRoleDisplayName(RoleHandler.PlayerRole role)
    {
        switch (role)
        {
            case RoleHandler.PlayerRole.Runner:
                return "Runner";
            case RoleHandler.PlayerRole.Trapper:
                return "Trapper";
            default:
                return "No Role";
        }
    }

    public RoleHandler.PlayerRole GetPlayerRole(PlayerRef player)
    {
        int slotIndex = GetPlayerSlotIndex(player);

        if (slotIndex < 0)
            return RoleHandler.PlayerRole.None;

        return GetSlotRole(slotIndex);
    }

    public string GetPlayerRoleName(PlayerRef player)
    {
        return GetRoleDisplayName(GetPlayerRole(player));
    }

    public void SetPlayerRoleServer(PlayerRef player, RoleHandler.PlayerRole role)
    {
        if (!HasStateAuthority)
            return;

        if (player == default)
            return;

        if (!ContainsPlayer(player))
            return;

        role = NormalizeRole(role);

        if (role == RoleHandler.PlayerRole.None)
            return;

        int slotIndex = GetPlayerSlotIndex(player);

        if (slotIndex < 0)
            return;

        SetSlotRole(slotIndex, role);
    }

    // ==================================================
    // SKIN SELECTION
    // ==================================================

    private int GetSkinCount()
    {
        CharacterSkinDatabase database = CharacterSkinDatabase.Instance;

        if (database == null)
            return 4;

        return Mathf.Max(1, database.SkinCount);
    }

    private int NormalizeSkinIndex(int skinIndex)
    {
        int skinCount = GetSkinCount();

        if (skinIndex < 0)
            return skinCount - 1;

        if (skinIndex >= skinCount)
            return 0;

        return skinIndex;
    }

    private int GetSlotSkinIndex(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return NormalizeSkinIndex(Slot1SkinIndex);
            case 1: return NormalizeSkinIndex(Slot2SkinIndex);
            case 2: return NormalizeSkinIndex(Slot3SkinIndex);
            case 3: return NormalizeSkinIndex(Slot4SkinIndex);
            default: return 0;
        }
    }

    private void SetSlotSkinIndex(int slotIndex, int skinIndex)
    {
        skinIndex = NormalizeSkinIndex(skinIndex);

        switch (slotIndex)
        {
            case 0:
                Slot1SkinIndex = skinIndex;
                break;
            case 1:
                Slot2SkinIndex = skinIndex;
                break;
            case 2:
                Slot3SkinIndex = skinIndex;
                break;
            case 3:
                Slot4SkinIndex = skinIndex;
                break;
        }
    }

    private string GetSkinDisplayName(int skinIndex)
    {
        CharacterSkinDatabase database = CharacterSkinDatabase.Instance;

        if (database == null)
            return $"Skin {skinIndex + 1}";

        return database.GetSkinName(skinIndex);
    }

    public int GetPlayerSkinIndex(PlayerRef player)
    {
        int slotIndex = GetPlayerSlotIndex(player);

        if (slotIndex < 0)
            return 0;

        return GetSlotSkinIndex(slotIndex);
    }

    public void ChangePlayerSkinServer(PlayerRef player, int direction)
    {
        if (!HasStateAuthority)
            return;

        if (player == default)
            return;

        int slotIndex = GetPlayerSlotIndex(player);

        if (slotIndex < 0)
            return;

        int currentSkinIndex = GetSlotSkinIndex(slotIndex);
        int nextSkinIndex = currentSkinIndex + direction;

        SetSlotSkinIndex(slotIndex, nextSkinIndex);
    }

    // ==================================================
    // GAME START RULES
    // ==================================================

    public bool CanHostStartGame()
    {
        if (GameStarted)
            return false;

        if (Slot1Player == default)
            return false;

        if (GetPlayerCount() < 2)
            return false;

        if (!AreAllClientsReady())
            return false;

        if (!HaveAllPlayersSelectedRole())
            return false;

        if (!HasAtLeastOneRunnerAndTrapper())
            return false;

        return true;
    }

    public bool HaveAllPlayersSelectedRole()
    {
        if (Slot1Player != default && Slot1Role == RoleHandler.PlayerRole.None) return false;
        if (Slot2Player != default && Slot2Role == RoleHandler.PlayerRole.None) return false;
        if (Slot3Player != default && Slot3Role == RoleHandler.PlayerRole.None) return false;
        if (Slot4Player != default && Slot4Role == RoleHandler.PlayerRole.None) return false;

        return true;
    }

    public bool HasAtLeastOneRunnerAndTrapper()
    {
        int runnerCount = 0;
        int trapperCount = 0;

        CountRole(Slot1Player, Slot1Role, ref runnerCount, ref trapperCount);
        CountRole(Slot2Player, Slot2Role, ref runnerCount, ref trapperCount);
        CountRole(Slot3Player, Slot3Role, ref runnerCount, ref trapperCount);
        CountRole(Slot4Player, Slot4Role, ref runnerCount, ref trapperCount);

        return runnerCount >= 1 && trapperCount >= 1;
    }

    private void CountRole(PlayerRef player, RoleHandler.PlayerRole role, ref int runnerCount, ref int trapperCount)
    {
        if (player == default)
            return;

        role = NormalizeRole(role);

        if (role == RoleHandler.PlayerRole.Runner)
            runnerCount++;
        else if (role == RoleHandler.PlayerRole.Trapper)
            trapperCount++;
    }

    // ==================================================
    // READY CONTROL
    // ==================================================

    public void SetPlayerReadyServer(PlayerRef player, bool value)
    {
        if (!HasStateAuthority)
            return;

        if (!CanPlayerUseReady(player))
            return;

        if (Slot2Player == player)
        {
            Slot2Ready = value;
            return;
        }

        if (Slot3Player == player)
        {
            Slot3Ready = value;
            return;
        }

        if (Slot4Player == player)
        {
            Slot4Ready = value;
            return;
        }
    }

    public void TogglePlayerReadyServer(PlayerRef player)
    {
        if (!HasStateAuthority)
            return;

        if (!CanPlayerUseReady(player))
            return;

        SetPlayerReadyServer(player, !GetPlayerReady(player));
    }

    // ==================================================
    // LOBBY CHAT
    // ==================================================

    public string GetChatLog()
    {
        StringBuilder builder = new StringBuilder();

        AppendChatLine(builder, ChatLine1);
        AppendChatLine(builder, ChatLine2);
        AppendChatLine(builder, ChatLine3);
        AppendChatLine(builder, ChatLine4);
        AppendChatLine(builder, ChatLine5);
        AppendChatLine(builder, ChatLine6);
        AppendChatLine(builder, ChatLine7);
        AppendChatLine(builder, ChatLine8);
        AppendChatLine(builder, ChatLine9);
        AppendChatLine(builder, ChatLine10);

        return builder.ToString();
    }

    private void AppendChatLine(StringBuilder builder, string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        if (builder.Length > 0)
            builder.AppendLine();

        builder.Append(line);
    }

    private void PushChatLine(string newLine)
    {
        if (!HasStateAuthority)
            return;

        ChatLine1 = ChatLine2;
        ChatLine2 = ChatLine3;
        ChatLine3 = ChatLine4;
        ChatLine4 = ChatLine5;
        ChatLine5 = ChatLine6;
        ChatLine6 = ChatLine7;
        ChatLine7 = ChatLine8;
        ChatLine8 = ChatLine9;
        ChatLine9 = ChatLine10;
        ChatLine10 = newLine;

        ChatMessageVersion++;
    }

    private string SanitizeChatMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        message = message.Replace("\r", " ");
        message = message.Replace("\n", " ");
        message = message.Trim();

        while (message.Contains("  "))
            message = message.Replace("  ", " ");

        if (message.Length > ChatMaxMessageLength)
            message = message.Substring(0, ChatMaxMessageLength);

        return message;
    }

    // ==================================================
    // RPC CALLS
    // ==================================================

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetReady(bool value, RpcInfo info = default)
    {
        PlayerRef sender = info.Source;

        if (sender == default)
            return;

        SetPlayerReadyServer(sender, value);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ToggleReady(RpcInfo info = default)
    {
        PlayerRef sender = info.Source;

        if (sender == default)
            return;

        TogglePlayerReadyServer(sender);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ChangeSkin(int direction, RpcInfo info = default)
    {
        if (!HasStateAuthority)
            return;

        PlayerRef sender = info.Source;

        if (sender == default && Runner != null)
            sender = Runner.LocalPlayer;

        if (sender == default)
            return;

        if (!ContainsPlayer(sender))
            return;

        direction = direction < 0 ? -1 : 1;

        ChangePlayerSkinServer(sender, direction);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SelectRole(int roleValue, RpcInfo info = default)
    {
        if (!HasStateAuthority)
            return;

        PlayerRef sender = info.Source;

        if (sender == default && Runner != null)
            sender = Runner.LocalPlayer;

        if (sender == default)
            return;

        if (!ContainsPlayer(sender))
            return;

        RoleHandler.PlayerRole role = NormalizeRole((RoleHandler.PlayerRole)roleValue);

        if (role == RoleHandler.PlayerRole.None)
            return;

        SetPlayerRoleServer(sender, role);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SendChatMessage(string rawMessage, RpcInfo info = default)
    {
        if (!HasStateAuthority)
            return;

        PlayerRef sender = info.Source;

        if (sender == default && Runner != null)
            sender = Runner.LocalPlayer;

        if (sender == default)
            return;

        if (!ContainsPlayer(sender))
            return;

        string cleanMessage = SanitizeChatMessage(rawMessage);

        if (string.IsNullOrWhiteSpace(cleanMessage))
            return;

        int slotIndex = GetPlayerSlotIndex(sender);

        if (slotIndex < 0)
            return;

        string line = $"Player {slotIndex + 1}: {cleanMessage}";

        PushChatLine(line);

        Debug.Log($"Lobby chat message: {line}");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestStartGame(RpcInfo info = default)
    {
        if (!HasStateAuthority)
        {
            Debug.LogWarning("RPC_RequestStartGame ignored: no state authority.");
            return;
        }

        PlayerRef sender = info.Source;

        if (sender == default && Runner != null)
        {
            sender = Runner.LocalPlayer;
            Debug.Log($"RPC_RequestStartGame fallback sender applied. Sender={sender}");
        }

        Debug.Log(
            $"RPC_RequestStartGame received | Sender={sender} | " +
            $"IsHost={IsHostPlayer(sender)} | CanStart={CanHostStartGame()} | " +
            $"HasStateAuthority={HasStateAuthority}"
        );

        if (sender == default)
        {
            Debug.LogWarning("RPC_RequestStartGame ignored: sender is default.");
            return;
        }

        if (!IsHostPlayer(sender))
        {
            Debug.LogWarning("RPC_RequestStartGame ignored: sender is not the host.");
            return;
        }

        if (!CanHostStartGame())
        {
            Debug.LogWarning("RPC_RequestStartGame ignored: CanHostStartGame returned false.");
            return;
        }

        GameStarted = true;
        Debug.Log("GameStarted has been set to TRUE.");
    }
}
