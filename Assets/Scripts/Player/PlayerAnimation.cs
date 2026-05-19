using Fusion;
using UnityEngine;

public class PlayerAnimation : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;

    private PlayerMovement movement;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsCrouchingHash = Animator.StringToHash("isCrouching");
    private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int VerticalHash = Animator.StringToHash("Vertical");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        movement = GetComponent<PlayerMovement>();
    }

    public void SetAnimator(Animator newAnimator)
    {
        if (newAnimator == null)
            return;

        animator = newAnimator;
    }

    public override void Render()
    {
        if (animator == null)
            return;

        if (movement == null)
            movement = GetComponent<PlayerMovement>();

        if (movement == null)
            return;

        Vector2 moveInput = movement.CurrentMoveInput;

        animator.SetFloat(HorizontalHash, moveInput.x);
        animator.SetFloat(VerticalHash, moveInput.y);
        animator.SetFloat(SpeedHash, movement.CurrentSpeed01);
        animator.SetBool(IsGroundedHash, movement.CurrentGrounded);
        animator.SetBool(IsCrouchingHash, movement.CurrentCrouching);
        animator.SetFloat(VerticalVelocityHash, movement.CurrentVerticalVelocity);
    }
}
