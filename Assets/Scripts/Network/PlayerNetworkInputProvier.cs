using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkRunner))]
public class PlayerNetworkInputProvider : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;
    private bool _registered;

    private void Awake()
    {
        _runner = GetComponent<NetworkRunner>();
    }

    private void OnEnable()
    {
        Register();
    }

    private void OnDisable()
    {
        if (_runner != null && _registered)
        {
            _runner.RemoveCallbacks(this);
            _registered = false;
        }
    }

    private void Register()
    {
        if (_registered)
            return;

        if (_runner == null)
            _runner = GetComponent<NetworkRunner>();

        if (_runner == null)
            return;

        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);
        _registered = true;
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        PlayerNetworkInputData inputData = new PlayerNetworkInputData();

        Vector2 moveInput = Vector2.zero;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) moveInput.y += 1f;
            if (keyboard.sKey.isPressed) moveInput.y -= 1f;
            if (keyboard.dKey.isPressed) moveInput.x += 1f;
            if (keyboard.aKey.isPressed) moveInput.x -= 1f;

            inputData.Buttons.Set((int)PlayerInputButton.Jump, keyboard.spaceKey.isPressed);
            inputData.Buttons.Set((int)PlayerInputButton.Sprint, keyboard.leftShiftKey.isPressed);
            inputData.Buttons.Set((int)PlayerInputButton.Crouch, keyboard.leftCtrlKey.isPressed);
        }
        else
        {
            if (Input.GetKey(KeyCode.W)) moveInput.y += 1f;
            if (Input.GetKey(KeyCode.S)) moveInput.y -= 1f;
            if (Input.GetKey(KeyCode.D)) moveInput.x += 1f;
            if (Input.GetKey(KeyCode.A)) moveInput.x -= 1f;

            inputData.Buttons.Set((int)PlayerInputButton.Jump, Input.GetKey(KeyCode.Space));
            inputData.Buttons.Set((int)PlayerInputButton.Sprint, Input.GetKey(KeyCode.LeftShift));
            inputData.Buttons.Set((int)PlayerInputButton.Crouch, Input.GetKey(KeyCode.LeftControl));
        }

        inputData.MoveInput = Vector2.ClampMagnitude(moveInput, 1f);
        input.Set(inputData);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        input.Set(new PlayerNetworkInputData());
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
