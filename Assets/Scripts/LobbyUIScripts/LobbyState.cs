using Fusion;
using UnityEngine;

public class LobbyState : NetworkBehaviour
{
    private const int MaxPlayers = 4;

    [Header("Lobby State")]
    [Networked] public PlayerRef Slot1Player { get; set; }
    [Networked] public NetworkBool Slot1Ready { get; set; }

    [Networked] public PlayerRef Slot2Player { get; set; }
    [Networked] public NetworkBool Slot2Ready { get; set; }

    [Networked] public PlayerRef Slot3Player { get; set; }
    [Networked] public NetworkBool Slot3Ready { get; set; }

    [Networked] public PlayerRef Slot4Player { get; set; }
    [Networked] public NetworkBool Slot4Ready { get; set; }

    [Networked] public NetworkBool GameStarted { get; set; }

    public struct LobbySlotData
    {
        public bool HasPlayer;
        public bool IsHost;
        public bool IsReady;
        public bool CanUseReady;
        public PlayerRef Player;
        public string DisplayName;
        public string StatusText;
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

        if (Slot1Player == default) Slot1Ready = false;
        if (Slot2Player == default) Slot2Ready = false;
        if (Slot3Player == default) Slot3Ready = false;
        if (Slot4Player == default) Slot4Ready = false;

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
            changed = true;
        }

        if (Slot2Player != default && !Runner.IsPlayerValid(Slot2Player))
        {
            Slot2Player = default;
            Slot2Ready = false;
            changed = true;
        }

        if (Slot3Player != default && !Runner.IsPlayerValid(Slot3Player))
        {
            Slot3Player = default;
            Slot3Ready = false;
            changed = true;
        }

        if (Slot4Player != default && !Runner.IsPlayerValid(Slot4Player))
        {
            Slot4Player = default;
            Slot4Ready = false;
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
            return true;
        }

        if (Slot2Player == default)
        {
            Slot2Player = player;
            Slot2Ready = false;
            return true;
        }

        if (Slot3Player == default)
        {
            Slot3Player = player;
            Slot3Ready = false;
            return true;
        }

        if (Slot4Player == default)
        {
            Slot4Player = player;
            Slot4Ready = false;
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
        }

        if (Slot2Player == player)
        {
            Slot2Player = default;
            Slot2Ready = false;
        }

        if (Slot3Player == player)
        {
            Slot3Player = default;
            Slot3Ready = false;
        }

        if (Slot4Player == player)
        {
            Slot4Player = default;
            Slot4Ready = false;
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

        PlayerRef[] compactPlayers = new PlayerRef[MaxPlayers];
        bool[] compactReady = new bool[MaxPlayers];

        int writeIndex = 0;

        for (int i = 0; i < MaxPlayers; i++)
        {
            if (players[i] != default)
            {
                compactPlayers[writeIndex] = players[i];
                compactReady[writeIndex] = writeIndex == 0 ? false : readyStates[i];
                writeIndex++;
            }
        }

        Slot1Player = compactPlayers[0];
        Slot1Ready = false;

        Slot2Player = compactPlayers[1];
        Slot2Ready = compactReady[1];

        Slot3Player = compactPlayers[2];
        Slot3Ready = compactReady[2];

        Slot4Player = compactPlayers[3];
        Slot4Ready = compactReady[3];
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

        LobbySlotData data = new LobbySlotData
        {
            HasPlayer = hasPlayer,
            IsHost = hasPlayer && isHost,
            IsReady = hasPlayer && !isHost && readyValue,
            CanUseReady = hasPlayer && !isHost,
            Player = player,
            DisplayName = hasPlayer
                ? (isHost ? $"Player {slotIndex + 1} (Host)" : $"Player {slotIndex + 1}")
                : $"Player {slotIndex + 1}: Bekleniyor...",
            StatusText = GetStatusText(hasPlayer, isHost, readyValue)
        };

        return data;
    }

    private string GetStatusText(bool hasPlayer, bool isHost, bool readyValue)
    {
        if (!hasPlayer)
            return "Bekleniyor...";

        if (isHost)
            return "Host";

        return readyValue ? "Hazır!" : "Lobide";
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

        return true;
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
    public void RPC_RequestStartGame(RpcInfo info = default)
    {
        PlayerRef sender = info.Source;

        if (!IsHostPlayer(sender))
            return;

        if (!CanHostStartGame())
            return;

        GameStarted = true;
    }
}