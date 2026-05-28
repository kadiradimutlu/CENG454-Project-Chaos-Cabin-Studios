using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class RoundManager : NetworkBehaviour
{
    public enum RoundState
    {
        Waiting = 0,
        Countdown = 1,
        Playing = 2,
        Finished = 3
    }

    public enum RoundWinner
    {
        None = 0,
        Runners = 1,
        Trappers = 2
    }

    [Header("Round Settings")]
    [SerializeField] private bool autoStartWhenGameStarted = true;
    [SerializeField] private float countdownSeconds = 3f;
    [SerializeField] private float roundSeconds = 180f;
    [SerializeField] private bool lockMovementDuringCountdown = true;
    [SerializeField] private bool lockMovementAfterFinish = true;

    [Header("Optional References")]
    [SerializeField] private LobbyState lobbyState;

    private bool? lastMovementAllowed;

    private static RoundManager activeManager;
    private static bool hasDisplay;
    private static bool hasSeenActiveRound;
    private static RoundState localDisplayState = RoundState.Waiting;
    private static RoundWinner localDisplayWinner = RoundWinner.None;
    private static float localCountdownRemaining;
    private static float localRoundRemaining;
    private static float localRoundDuration = 180f;

    public float RoundDuration => Mathf.Max(1f, roundSeconds);
    public RoundState DisplayState => GetDisplayState();
    public RoundWinner DisplayWinner => GetDisplayWinner();
    public bool IsPlaying => DisplayState == RoundState.Playing;
    public bool IsFinished => DisplayState == RoundState.Finished;

    public static bool HasSeenActiveRound => hasSeenActiveRound;

    public static bool TryGetDisplayData(out RoundState state, out RoundWinner winner, out float countdownRemaining, out float roundRemaining, out float roundDuration)
    {
        state = localDisplayState;
        winner = localDisplayWinner;
        countdownRemaining = localCountdownRemaining;
        roundRemaining = localRoundRemaining;
        roundDuration = localRoundDuration;

        return hasDisplay && activeManager != null && activeManager.isActiveAndEnabled;
    }

    public static void ClearLocalDisplay()
    {
        activeManager = null;
        hasDisplay = false;
        hasSeenActiveRound = false;
        localDisplayState = RoundState.Waiting;
        localDisplayWinner = RoundWinner.None;
        localCountdownRemaining = 0f;
        localRoundRemaining = 0f;
        localRoundDuration = 180f;
    }

    private void Awake()
    {
        PublishLocalDisplay();
    }

    private void OnEnable()
    {
        activeManager = this;
        hasSeenActiveRound = false;
        PublishLocalDisplay();
    }

    private void OnDisable()
    {
        if (activeManager == this)
            ClearLocalDisplay();
    }

    public override void Spawned()
    {
        CacheLobbyState();
        PublishLocalDisplay();
    }

    public override void FixedUpdateNetwork()
    {
        CacheLobbyState();

        if (!HasRoundAuthority())
            return;

        if (lobbyState.RoundStateValue == (int)RoundState.Waiting)
        {
            if (autoStartWhenGameStarted && IsGameStarted() && HasSpawnedPlayers())
                StartCountdown();

            return;
        }

        if (lobbyState.RoundStateValue == (int)RoundState.Countdown)
        {
            if (lobbyState.IsSharedCountdownExpired())
                BeginPlaying();

            return;
        }

        if (lobbyState.RoundStateValue == (int)RoundState.Playing)
        {
            if (lobbyState.IsSharedRoundTimerExpired())
                EndRound(RoundWinner.Trappers);
        }
    }

    private void Update()
    {
        CacheLobbyState();

        if (HasRoundAuthority() && lobbyState.RoundStateValue == (int)RoundState.Waiting)
        {
            if (autoStartWhenGameStarted && IsGameStarted() && HasSpawnedPlayers())
                StartCountdown();
        }

        PublishLocalDisplay();
        UpdateLocalMovementLock();
    }

    public float GetRemainingCountdownTime()
    {
        if (lobbyState == null)
            return 0f;

        return lobbyState.GetSharedCountdownRemaining();
    }

    public float GetRemainingRoundTime()
    {
        if (lobbyState == null)
            return RoundDuration;

        return lobbyState.GetSharedRoundRemaining(RoundDuration);
    }

    public void StartCountdown()
    {
        if (!HasRoundAuthority())
            return;

        lobbyState.StartRoundCountdownServer(countdownSeconds);

        if (lockMovementDuringCountdown)
            SetPlayersMovementAllowed(false);

        Debug.Log("RoundManager: Countdown started.");
    }

    public void ReportRunnerFinished(NetworkObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayerRef player = playerObject.InputAuthority;

        if (player == default)
            return;

        CacheLobbyState();

        if (lobbyState == null)
            return;

        if (HasRoundAuthority())
        {
            if (!lobbyState.CanAcceptRunnerFinish(player))
                return;

            Debug.Log($"RoundManager: Runner finished. Player={player}");
            EndRound(RoundWinner.Runners);
            return;
        }

        if (lobbyState.CanAcceptRunnerFinish(player))
            lobbyState.RPC_RequestRunnerFinished(player);
    }

    public void ForceEndForTrappers()
    {
        if (!HasRoundAuthority())
            return;

        EndRound(RoundWinner.Trappers);
    }

    public void ApplyRemoteCountdown(float seconds)
    {
        hasSeenActiveRound = true;
        localDisplayState = RoundState.Countdown;
        localDisplayWinner = RoundWinner.None;
        localCountdownRemaining = Mathf.Max(0f, seconds);
        localRoundRemaining = RoundDuration;
        localRoundDuration = RoundDuration;
        hasDisplay = true;
        activeManager = this;
    }

    public void ApplyRemotePlaying(float seconds)
    {
        hasSeenActiveRound = true;
        localDisplayState = RoundState.Playing;
        localDisplayWinner = RoundWinner.None;
        localCountdownRemaining = 0f;
        localRoundRemaining = Mathf.Max(0f, seconds);
        localRoundDuration = RoundDuration;
        hasDisplay = true;
        activeManager = this;
    }

    public void ApplyRemoteFinished(int winnerValue)
    {
        hasSeenActiveRound = true;
        localDisplayState = RoundState.Finished;
        localDisplayWinner = (RoundWinner)Mathf.Clamp(winnerValue, 0, 2);
        localCountdownRemaining = 0f;
        localRoundRemaining = 0f;
        localRoundDuration = RoundDuration;
        hasDisplay = true;
        activeManager = this;
    }

    private void BeginPlaying()
    {
        if (!HasRoundAuthority())
            return;

        lobbyState.BeginRoundPlayingServer(RoundDuration);
        SetPlayersMovementAllowed(true);

        Debug.Log("RoundManager: Round playing.");
    }

    private void EndRound(RoundWinner winner)
    {
        if (!HasRoundAuthority())
            return;

        lobbyState.FinishRoundServer((int)winner);

        if (lockMovementAfterFinish)
            SetPlayersMovementAllowed(false);

        Debug.Log($"RoundManager: Round finished. Winner={winner}");
    }

    private RoundState GetDisplayState()
    {
        if (lobbyState == null || !lobbyState.GameStarted)
            return RoundState.Waiting;

        int value = Mathf.Clamp(lobbyState.RoundStateValue, 0, 3);
        return (RoundState)value;
    }

    private RoundWinner GetDisplayWinner()
    {
        if (lobbyState == null || !lobbyState.GameStarted)
            return RoundWinner.None;

        int value = Mathf.Clamp(lobbyState.RoundWinnerValue, 0, 2);
        return (RoundWinner)value;
    }

    private bool HasRoundAuthority()
    {
        return lobbyState != null && lobbyState.HasStateAuthority;
    }

    private bool IsGameStarted()
    {
        return lobbyState != null && lobbyState.GameStarted;
    }

    private bool HasSpawnedPlayers()
    {
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        return players != null && players.Length > 0;
    }

    private void CacheLobbyState()
    {
        if (lobbyState != null)
            return;

        lobbyState = FindFirstObjectByType<LobbyState>();
    }

    private void PublishLocalDisplay()
    {
        activeManager = this;
        hasDisplay = true;
        localRoundDuration = RoundDuration;

        RoundState state = DisplayState;
        localDisplayState = state;
        localDisplayWinner = DisplayWinner;

        if (state == RoundState.Countdown)
        {
            hasSeenActiveRound = true;
            localCountdownRemaining = GetRemainingCountdownTime();
            localRoundRemaining = RoundDuration;
        }
        else if (state == RoundState.Playing)
        {
            hasSeenActiveRound = true;
            localCountdownRemaining = 0f;
            localRoundRemaining = GetRemainingRoundTime();
        }
        else if (state == RoundState.Finished)
        {
            localCountdownRemaining = 0f;
            localRoundRemaining = 0f;
        }
        else
        {
            localCountdownRemaining = 0f;
            localRoundRemaining = RoundDuration;
        }
    }

    private void UpdateLocalMovementLock()
    {
        RoundState state = DisplayState;

        bool allowMovement = true;

        if (state == RoundState.Countdown && lockMovementDuringCountdown)
            allowMovement = false;

        if (state == RoundState.Finished && lockMovementAfterFinish)
            allowMovement = false;

        if (lastMovementAllowed.HasValue && lastMovementAllowed.Value == allowMovement)
            return;

        lastMovementAllowed = allowMovement;
        SetPlayersMovementAllowed(allowMovement);
    }

    private void SetPlayersMovementAllowed(bool allowed)
    {
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null)
                players[i].SetMovementAllowed(allowed);
        }
    }

    private void OnValidate()
    {
        countdownSeconds = Mathf.Max(0.1f, countdownSeconds);
        roundSeconds = Mathf.Max(1f, roundSeconds);
    }
}
