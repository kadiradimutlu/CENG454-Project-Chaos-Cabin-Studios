using Fusion;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class RoundTimerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float countdownSeconds = 3f;
    [SerializeField] private float roundSeconds = 180f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color countdownColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    [SerializeField] private float criticalThreshold = 10f;

    private LobbyState lobbyState;
    private bool? lastMovementAllowed;

    private void Awake()
    {
        if (timerText == null)
            timerText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        lobbyState = null;
        lastMovementAllowed = null;
        HideStaleText();
    }

    private void OnDisable()
    {
        lastMovementAllowed = null;
    }

    private void Update()
    {
        CacheLobbyState();

        if (timerText == null)
            return;

        if (lobbyState == null || !lobbyState.GameStarted)
        {
            HideStaleText();
            return;
        }

        DriveRoundIfAuthority();
        UpdateTimerText();
        UpdateMovementLock();
    }

    private void DriveRoundIfAuthority()
    {
        if (lobbyState == null || !lobbyState.HasStateAuthority)
            return;

        if (!lobbyState.GameStarted)
        {
            if (lobbyState.RoundStateValue != 0)
                lobbyState.ResetRoundStateServer();

            return;
        }

        if (lobbyState.RoundStateValue == 0)
        {
            if (HasSpawnedPlayers())
                lobbyState.StartRoundCountdownServer(countdownSeconds);

            return;
        }

        if (lobbyState.RoundStateValue == 1)
        {
            if (lobbyState.IsSharedCountdownExpired())
                lobbyState.BeginRoundPlayingServer(roundSeconds);

            return;
        }

        if (lobbyState.RoundStateValue == 2)
        {
            if (lobbyState.IsSharedRoundTimerExpired())
                lobbyState.FinishRoundServer(2);
        }
    }

    private void UpdateTimerText()
    {
        int state = lobbyState.RoundStateValue;

        if (state == 1)
        {
            timerText.text = Mathf.CeilToInt(lobbyState.GetSharedCountdownRemaining()).ToString();
            timerText.color = countdownColor;
            return;
        }

        if (state == 2)
        {
            float remaining = lobbyState.GetSharedRoundRemaining(roundSeconds);
            timerText.text = FormatTime(remaining);
            timerText.color = remaining <= criticalThreshold ? criticalColor : normalColor;
            return;
        }

        if (state == 3)
        {
            timerText.text = "00:00";
            timerText.color = criticalColor;
            return;
        }

        timerText.text = FormatTime(roundSeconds);
        timerText.color = normalColor;
    }

    private void UpdateMovementLock()
    {
        int state = lobbyState != null ? lobbyState.RoundStateValue : 0;
        bool allowMovement = state == 2;

        if (lastMovementAllowed.HasValue && lastMovementAllowed.Value == allowMovement)
            return;

        lastMovementAllowed = allowMovement;

        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null)
                players[i].SetMovementAllowed(allowMovement);
        }
    }

    private bool HasSpawnedPlayers()
    {
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        return players != null && players.Length > 0;
    }

    private void HideStaleText()
    {
        if (timerText == null)
            return;

        timerText.text = "--:--";
        timerText.color = normalColor;
    }

    private void CacheLobbyState()
    {
        if (lobbyState != null)
            return;

        lobbyState = FindFirstObjectByType<LobbyState>();
    }

    private string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }
}
