using UnityEngine;

public class PlayerInputReader : MonoBehaviour
{
    public Vector3 MoveDirection { get; private set; }
    public bool IsRunPressed { get; private set; }
    public bool IsJumpPressed { get; private set; }
    public bool IsJumpHeld { get; private set; }
    public bool IsCrouchPressed { get; private set; }

    public void ReadInput()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        MoveDirection = transform.right * horizontalInput + transform.forward * verticalInput;
        MoveDirection = Vector3.ClampMagnitude(MoveDirection, 1f);

        IsRunPressed = Input.GetKey(KeyCode.LeftShift);
        IsJumpPressed = Input.GetButtonDown("Jump");
        IsJumpHeld = Input.GetButton("Jump");
        IsCrouchPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
    }
}
