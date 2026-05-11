using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(GroundChecker))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerJump))]
[RequireComponent(typeof(PlayerCrouch))]
public class PlayerController : NetworkBehaviour
{
    private PlayerInputReader inputReader;
    private GroundChecker groundChecker;
    private PlayerMovement playerMovement;
    private PlayerJump playerJump;
    private PlayerCrouch playerCrouch;
    private PlayerAnimationController playerAnimationController;

    public bool IsMovementAllowed
    {
        get => playerMovement != null && playerMovement.IsMovementAllowed;
        set
        {
            if (playerMovement != null)
            {
                playerMovement.IsMovementAllowed = value;
            }
        }
    }

    public override void Spawned()
    {
        inputReader = GetComponent<PlayerInputReader>();
        groundChecker = GetComponent<GroundChecker>();
        playerMovement = GetComponent<PlayerMovement>();
        playerJump = GetComponent<PlayerJump>();
        playerCrouch = GetComponent<PlayerCrouch>();
        playerAnimationController = GetComponent<PlayerAnimationController>();
    }

    private void Update()
    {
        if (!CanControlPlayer())
        {
            return;
        }

        inputReader.ReadInput();
        groundChecker.CheckGround();
        playerCrouch.SetCrouch(inputReader.IsCrouchPressed);
        playerJump.TryJump(inputReader.IsJumpPressed);
        playerJump.ApplyBetterGravity(inputReader.IsJumpHeld);

        if (playerAnimationController != null)
        {
            playerAnimationController.UpdateMovementAnimation();
        }
    }

    private void FixedUpdate()
    {
        if (!CanControlPlayer())
        {
            return;
        }

        playerMovement.Move(inputReader.MoveDirection, inputReader.IsRunPressed);
    }

    private bool CanControlPlayer()
    {
        return Object != null && (HasStateAuthority || HasInputAuthority) && IsMovementAllowed;
    }
}
