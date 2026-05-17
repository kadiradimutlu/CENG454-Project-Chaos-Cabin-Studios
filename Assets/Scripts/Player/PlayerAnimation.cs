using Fusion;
using UnityEngine;

public class PlayerAnimation : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;

    private Rigidbody _rb;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int isCrouchingHash = Animator.StringToHash("isCrouching");
    private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int VerticalHash = Animator.StringToHash("Vertical");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
    private static readonly int JumpTriggerHash = Animator.StringToHash("JumpTrigger");

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        _rb = GetComponent<Rigidbody>();
    }

    public void SetAnimator(Animator newAnimator)
    {
        if (newAnimator == null)
            return;

        animator = newAnimator;
    }

    public override void FixedUpdateNetwork()
    {
        if (animator == null)
            return;

        if (GetInput(out GameplayInput input))
        {
            float h = input.MoveDirection.x;
            float v = input.MoveDirection.y;
            float speed = input.MoveDirection.magnitude;

            if (input.SprintButton && speed > 0)
            {
                h *= 2f;
                v *= 2f;
                speed *= 2f;
            }
            else if (input.CrouchButton)
            {
                h *= 0.5f;
                v *= 0.5f;
                speed *= 0.5f;
            }

            animator.SetFloat(HorizontalHash, h);
            animator.SetFloat(VerticalHash, v);
            animator.SetFloat(SpeedHash, speed);
            animator.SetBool(isCrouchingHash, input.CrouchButton);

            if (input.JumpButton)
                animator.SetTrigger(JumpTriggerHash);
        }

        if (_rb != null)
        {
            bool isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
            animator.SetBool(IsGroundedHash, isGrounded);
            animator.SetFloat(VerticalVelocityHash, _rb.linearVelocity.y);
        }
    }
}
