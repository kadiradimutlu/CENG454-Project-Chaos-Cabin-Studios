using UnityEngine;

[DisallowMultipleComponent]
public class WarmupBridgeGateController : MonoBehaviour
{
    [Header("Round State Source")]
    [SerializeField] private LobbyState lobbyState;

    [Header("WarmUp Transition Objects")]
    [SerializeField] private GameObject warmupGate;
    [SerializeField] private GameObject warmupToWesternBridge;
    [SerializeField] private GameObject bridgeBlockers;

    private int lastAppliedState = -999;

    private void Awake()
    {
        ApplyClosedState();
    }

    private void OnEnable()
    {
        lastAppliedState = -999;
        ApplyCurrentState();
    }

    private void Update()
    {
        ApplyCurrentState();
    }

    private void ApplyCurrentState()
    {
        if (lobbyState == null)
            lobbyState = FindFirstObjectByType<LobbyState>();

        int state = 0;

        if (lobbyState != null && lobbyState.GameStarted)
            state = lobbyState.RoundStateValue;

        if (state == lastAppliedState)
            return;

        lastAppliedState = state;

        bool roundIsOpen =
            state == (int)RoundManager.RoundState.Playing ||
            state == (int)RoundManager.RoundState.Finished;

        if (roundIsOpen)
            ApplyOpenState();
        else
            ApplyClosedState();
    }

    private void ApplyClosedState()
    {
        SetActiveIfNeeded(warmupGate, true);
        SetActiveIfNeeded(warmupToWesternBridge, false);
        SetActiveIfNeeded(bridgeBlockers, false);
    }

    private void ApplyOpenState()
    {
        SetActiveIfNeeded(warmupGate, false);
        SetActiveIfNeeded(warmupToWesternBridge, true);
        SetActiveIfNeeded(bridgeBlockers, true);
    }

    private void SetActiveIfNeeded(GameObject target, bool active)
    {
        if (target == null)
            return;

        if (target.activeSelf == active)
            return;

        target.SetActive(active);
    }
}