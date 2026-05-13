using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour, IKnockbackable, IMovementBlocker
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float deceleration = 22f;
    [SerializeField] private float airControlMultiplier = 0.45f;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;

    private Rigidbody rb;
    private GroundChecker groundChecker;
    private PlayerCrouch playerCrouch;

    public bool IsMovementAllowed { get; set; } = true;
    public Rigidbody Rigidbody => rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        groundChecker = GetComponent<GroundChecker>();
        playerCrouch = GetComponent<PlayerCrouch>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public void Move(Vector3 moveDirection, bool isRunPressed)
    {
        if (!IsMovementAllowed)
        {
            return;
        }

        bool isCrouching = playerCrouch != null && playerCrouch.IsCrouching;
        bool isGrounded = groundChecker == null || groundChecker.IsGrounded;

        float targetSpeed = isRunPressed && !isCrouching ? runSpeed : walkSpeed;

        if (isCrouching)
        {
            targetSpeed *= crouchSpeedMultiplier;
        }

        Vector3 targetVelocity = moveDirection * targetSpeed;
        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        float controlMultiplier = isGrounded ? 1f : airControlMultiplier;
        float speedChangeRate = moveDirection.magnitude > 0.1f
            ? acceleration * controlMultiplier
            : deceleration * controlMultiplier;

        Vector3 smoothVelocity = Vector3.MoveTowards(
            currentHorizontalVelocity,
            targetVelocity,
            speedChangeRate * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector3(smoothVelocity.x, rb.linearVelocity.y, smoothVelocity.z);
    }

    public void ApplyKnockback(Vector3 force)
    {
        rb.AddForce(force, ForceMode.Impulse);
    }
}
