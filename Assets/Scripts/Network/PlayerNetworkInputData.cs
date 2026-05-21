using Fusion;
using UnityEngine;

public enum PlayerInputButton
{
    Jump = 0,
    Sprint = 1,
    Crouch = 2
}

public struct PlayerNetworkInputData : INetworkInput
{
    public Vector2 MoveInput;
    public NetworkButtons Buttons;
    public float CameraYaw;
}