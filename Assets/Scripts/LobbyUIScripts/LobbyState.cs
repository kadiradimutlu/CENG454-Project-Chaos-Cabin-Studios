using Fusion;
using UnityEngine;

public class LobbyState : NetworkBehaviour
{
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
        public PlayerRef Player;
    }

    public override void Spawned()
    {
        MainMenuManager menu = FindObjectOfType<MainMenuManager>();
        if (menu != null)
        {
            menu.RegisterLobbyState(this);
        }
    }

    // ==================================================
    // PLAYER SLOT MANAGEMENT
    // ==================================================

    public void AssignPlayer(PlayerRef player)
    {
        if (!HasStateAuthority)
            return;

        if (ContainsPlayer(player))
            return;

        if (Slot1Player == default)
        {
            Slot1Player = player;
            Slot1Ready = false;
            return;
        }

        if (Slot2Player == default)
        {
            Slot2Player = player;
            Slot2Ready = false;
            return;
        }

        if (Slot3Player == default)
        {
            Slot3Player = player;
            Slot3Ready = false;
            return;
        }

        if (Slot4Player == default)
        {
            Slot4Player = player;
            Slot4Ready = false;
            return;
        }
    }

    public void RemovePlayer(PlayerRef player)
    {
        if (!HasStateAuthority)
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
    }

    private void CompactSlots()
    {
        if (!HasStateAuthority)
            return;

        PlayerRef[] players = new PlayerRef[4]
        {
            Slot1Player,
            Slot2Player,
            Slot3Player,
            Slot4Player
        };

        bool[] readyStates = new bool[4]
        {
            Slot1Ready,
            Slot2Ready,
            Slot3Ready,
            Slot4Ready
        };

        PlayerRef[] compactPlayers = new PlayerRef[4];
        bool[] compactReady = new bool[4];

        int writeIndex = 0;

        for (int i = 0; i < 4; i++)
        {
            if (players[i] != default)
            {
                compactPlayers[writeIndex] = players[i];
                compactReady[writeIndex] = readyStates[i];
                writeIndex++;
            }
        }

        Slot1Player = compactPlayers[0];
        Slot1Ready = compactReady[0];

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
        return Slot1Player == player && Slot1Player != default;
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
        if (Slot1Player == player) return Slot1Ready;
        if (Slot2Player == player) return Slot2Ready;
        if (Slot3Player == player) return Slot3Ready;
        if (Slot4Player == player) return Slot4Ready;
        return false;
    }

    public LobbySlotData[] GetSlots()
    {
        return new LobbySlotData[]
        {
            new LobbySlotData
            {
                HasPlayer = Slot1Player != default,
                IsHost = Slot1Player != default,
                IsReady = Slot1Ready,
                Player = Slot1Player
            },
            new LobbySlotData
            {
                HasPlayer = Slot2Player != default,
                IsHost = false,
                IsReady = Slot2Ready,
                Player = Slot2Player
            },
            new LobbySlotData
            {
                HasPlayer = Slot3Player != default,
                IsHost = false,
                IsReady = Slot3Ready,
                Player = Slot3Player
            },
            new LobbySlotData
            {
                HasPlayer = Slot4Player != default,
                IsHost = false,
                IsReady = Slot4Ready,
                Player = Slot4Player
            }
        };
    }

    // ==================================================
    // GAME START RULES
    // ==================================================

    public bool CanHostStartGame()
    {
        if (Slot1Player == default)
            return false;

        int playerCount = 0;

        if (Slot1Player != default) playerCount++;
        if (Slot2Player != default) playerCount++;
        if (Slot3Player != default) playerCount++;
        if (Slot4Player != default) playerCount++;

        if (playerCount < 2)
            return false;

        if (Slot2Player != default && !Slot2Ready) return false;
        if (Slot3Player != default && !Slot3Ready) return false;
        if (Slot4Player != default && !Slot4Ready) return false;

        return true;
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

        if (IsHostPlayer(sender))
            return;

        if (Slot2Player == sender)
        {
            Slot2Ready = value;
            return;
        }

        if (Slot3Player == sender)
        {
            Slot3Ready = value;
            return;
        }

        if (Slot4Player == sender)
        {
            Slot4Ready = value;
            return;
        }
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