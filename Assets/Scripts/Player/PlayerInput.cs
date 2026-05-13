using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;
using Fusion.Sockets;
using System;
using System.Collections.Generic;


public struct GameplayInput : INetworkInput
{
    public Vector2 MoveDirection;
    public NetworkBool JumpButton;
}


public class PlayerInput : NetworkBehaviour, INetworkRunnerCallbacks
{
    public override void Spawned() => Runner?.AddCallbacks(this);
    public override void Despawned(NetworkRunner runner, bool hasState) => runner?.RemoveCallbacks(this);

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var myInput = new GameplayInput();
        var keyboard = Keyboard.current;

        var moveDirection = Vector2.zero;
        if (keyboard.wKey.isPressed) moveDirection += Vector2.up;
        if (keyboard.sKey.isPressed) moveDirection += Vector2.down;
        if (keyboard.aKey.isPressed) moveDirection += Vector2.left;
        if (keyboard.dKey.isPressed) moveDirection += Vector2.right;

        myInput.MoveDirection = moveDirection.normalized;

        input.Set(myInput);
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        throw new NotImplementedException();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        throw new NotImplementedException();
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        throw new NotImplementedException();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        throw new NotImplementedException();
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new NotImplementedException();
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        throw new NotImplementedException();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        throw new NotImplementedException();
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        throw new NotImplementedException();
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        throw new NotImplementedException();
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }
}