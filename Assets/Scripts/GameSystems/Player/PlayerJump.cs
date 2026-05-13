using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerJump : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    private Rigidbody rb;
    private GroundChecker groundChecker;
    private PlayerCrouch playerCrouch;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        groundChecker = GetComponent<GroundChecker>();
        playerCrouch = GetComponent<PlayerCrouch>();
    }

    public void TryJump(bool jumpPressed)
    {
        bool isGrounded = groundChecker != null && groundChecker.IsGrounded;
        bool isCrouching = playerCrouch != null && playerCrouch.IsCrouching;

        if (!jumpPressed || !isGrounded || isCrouching)
        {
            return;
        }

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    public void ApplyBetterGravity(bool isJumpHeld)
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.AddForce(Vector3.up * Physics.gravity.y * (fallMultiplier - 1f), ForceMode.Acceleration);
        }
        else if (rb.linearVelocity.y > 0 && !isJumpHeld)
        {
            rb.AddForce(Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1f), ForceMode.Acceleration);
        }
    }
}
