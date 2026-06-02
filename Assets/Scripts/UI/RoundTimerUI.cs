using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class RoundTimerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float roundSeconds = 600f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color countdownColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    [SerializeField] private float criticalThreshold = 10f;

    private LobbyState lobbyState;

    private void Awake()
    {
        if (timerText == null)
            timerText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        lobbyState = null;
        HideStaleText();
    }

    private void Update()
    {
        if (timerText == null)
            return;

        if (TryShowRoundManagerData())
            return;

        CacheLobbyState();

        if (lobbyState == null || !lobbyState.GameStarted)
        {
            HideStaleText();
            return;
        }

        UpdateFromLobbyState();
    }

    private bool TryShowRoundManagerData()
    {
        if (!RoundManager.TryGetDisplayData(
                out RoundManager.RoundState state,
                out RoundManager.RoundWinner winner,
                out float countdownRemaining,
                out float roundRemaining,
                out float roundDuration))
        {
            return false;
        }

        roundSeconds = Mathf.Max(1f, roundDuration);

        if (state == RoundManager.RoundState.Countdown)
        {
            timerText.text = Mathf.CeilToInt(countdownRemaining).ToString();
            timerText.color = countdownColor;
            return true;
        }

        if (state == RoundManager.RoundState.Playing)
        {
            timerText.text = FormatTime(roundRemaining);
            timerText.color = roundRemaining <= criticalThreshold ? criticalColor : normalColor;
            return true;
        }

        if (state == RoundManager.RoundState.Finished)
        {
            timerText.text = "00:00";
            timerText.color = criticalColor;
            return true;
        }

        if (!RoundManager.HasSeenActiveRound)
            return false;

        HideStaleText();
        return true;
    }

    private void UpdateFromLobbyState()
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

        HideStaleText();
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
        if (IsLobbyStateUsable(lobbyState))
            return;

        LobbyState[] states = FindObjectsByType<LobbyState>(FindObjectsSortMode.None);
        LobbyState fallbackState = null;

        for (int i = 0; i < states.Length; i++)
        {
            LobbyState state = states[i];

            if (!IsLobbyStateUsable(state))
                continue;

            if (state.GameStarted)
            {
                lobbyState = state;
                return;
            }

            if (fallbackState == null)
                fallbackState = state;
        }

        lobbyState = fallbackState;
    }

    private bool IsLobbyStateUsable(LobbyState state)
    {
        if (state == null)
            return false;

        if (!state.isActiveAndEnabled)
            return false;

        if (state.Runner == null || state.Runner.IsShutdown)
            return false;

        return true;
    }

    private string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }

    private void OnValidate()
    {
        roundSeconds = Mathf.Max(1f, roundSeconds);
        criticalThreshold = Mathf.Max(0f, criticalThreshold);
    }
}
