using System.Collections;
using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : NetworkBehaviour
{
    private bool isMovementAllowed = true;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float rotationSpeed = 18f;

    [Header("Gravity / Jump")]
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float groundedGravity = -2f;
    [SerializeField] private float groundSnapDistance = 0.9f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Player Collision")]
    [SerializeField] private bool ignorePlayerToPlayerCollision = true;

    [Networked] public Vector2 NetMoveInput { get; private set; }
    [Networked] public float NetSpeed01 { get; private set; }
    [Networked] public NetworkBool NetGrounded { get; private set; }
    [Networked] public float NetVerticalVelocity { get; private set; }
    [Networked] public NetworkBool NetCrouching { get; private set; }

    public Vector2 CurrentMoveInput { get; private set; }
    public float CurrentSpeed01 { get; private set; }
    public bool CurrentGrounded { get; private set; }
    public float CurrentVerticalVelocity { get; private set; }
    public bool CurrentCrouching { get; private set; }

    private Rigidbody rb;
    private CapsuleCollider capsule;

    private float verticalVelocity;
    private NetworkButtons previousButtons;
    private bool canSimulate;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        SetupCollider();
    }

    public override void Spawned()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (capsule == null)
            capsule = GetComponent<CapsuleCollider>();

        canSimulate = HasStateAuthority || HasInputAuthority;

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        verticalVelocity = groundedGravity;

        if (ignorePlayerToPlayerCollision)
            StartCoroutine(RefreshPlayerCollisionIgnores());
    }

    public override void FixedUpdateNetwork()
    {
        if (!canSimulate)
        {
            PullAnimationStateFromNetwork();
            return;
        }

        PlayerNetworkInputData inputData = default;

        bool hasInput = GetInput(out inputData);

        if (!hasInput)
            inputData.MoveInput = Vector2.zero;

        float deltaTime = Runner.DeltaTime;

        Vector2 moveInput = inputData.MoveInput;

        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        bool sprinting = inputData.Buttons.IsSet((int)PlayerInputButton.Sprint);
        bool crouching = inputData.Buttons.IsSet((int)PlayerInputButton.Crouch);

        bool jumpPressed =
            inputData.Buttons.IsSet((int)PlayerInputButton.Jump) &&
            !previousButtons.IsSet((int)PlayerInputButton.Jump);

        float speed = moveSpeed;

        if (sprinting)
            speed = sprintSpeed;
        else if (crouching)
            speed = crouchSpeed;

        Vector3 moveDirection = isMovementAllowed
            ? new Vector3(moveInput.x, 0f, moveInput.y)
            : Vector3.zero;

        bool grounded = CheckGround(transform.position, out RaycastHit groundHit);

        if (grounded && verticalVelocity < 0f)
            verticalVelocity = groundedGravity;

        if (jumpPressed && grounded && isMovementAllowed)
            verticalVelocity = jumpForce;

        verticalVelocity += gravity * deltaTime;

        Vector3 horizontalVelocity = moveDirection * speed;

        Vector3 velocity = new Vector3(
            horizontalVelocity.x,
            verticalVelocity,
            horizontalVelocity.z
        );

        Vector3 nextPosition = transform.position + velocity * deltaTime;

        if (verticalVelocity <= 0f && CheckGround(nextPosition, out RaycastHit snapHit))
        {
            float desiredY = GetRootYFromGround(snapHit.point.y);

            if (nextPosition.y <= desiredY + 0.15f)
            {
                nextPosition.y = desiredY;
                verticalVelocity = groundedGravity;
                grounded = true;
            }
        }

        transform.position = nextPosition;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * deltaTime
            );
        }

        CurrentMoveInput = moveInput;
        CurrentSpeed01 = moveInput.magnitude * (sprinting ? 2f : (crouching ? 0.5f : 1f));
        CurrentGrounded = grounded;
        CurrentVerticalVelocity = verticalVelocity;
        CurrentCrouching = crouching;

        if (HasStateAuthority)
        {
            NetMoveInput = CurrentMoveInput;
            NetSpeed01 = CurrentSpeed01;
            NetGrounded = CurrentGrounded;
            NetVerticalVelocity = CurrentVerticalVelocity;
            NetCrouching = CurrentCrouching;
        }

        previousButtons = inputData.Buttons;
    }

    public override void Render()
    {
        if (!canSimulate)
            PullAnimationStateFromNetwork();
    }

    private void PullAnimationStateFromNetwork()
    {
        CurrentMoveInput = NetMoveInput;
        CurrentSpeed01 = NetSpeed01;
        CurrentGrounded = NetGrounded;
        CurrentVerticalVelocity = NetVerticalVelocity;
        CurrentCrouching = NetCrouching;
    }

    private void SetupCollider()
    {
        if (capsule == null)
            return;

        capsule.isTrigger = false;
        capsule.center = new Vector3(0f, 1f, 0f);
        capsule.radius = 0.35f;
        capsule.height = 2f;
        capsule.direction = 1;
    }

    private bool CheckGround(Vector3 rootPosition, out RaycastHit hit)
    {
        if (capsule == null)
        {
            hit = default;
            return false;
        }

        Vector3 capsuleCenter = rootPosition + capsule.center;

        float halfHeight = capsule.height * 0.5f;
        float rayDistance = halfHeight + groundSnapDistance;

        Vector3 origin = capsuleCenter + Vector3.up * 0.15f;

        bool didHit = Physics.SphereCast(
            origin,
            Mathf.Max(0.05f, capsule.radius * 0.85f),
            Vector3.down,
            out hit,
            rayDistance,
            groundMask.value == 0 ? ~0 : groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (!didHit)
            return false;

        if (hit.collider == capsule)
            return false;

        if (hit.collider.GetComponentInParent<PlayerMovement>() != null)
            return false;

        return true;
    }

    private float GetRootYFromGround(float groundY)
    {
        return groundY - capsule.center.y + capsule.height * 0.5f;
    }

    private IEnumerator RefreshPlayerCollisionIgnores()
    {
        for (int i = 0; i < 90; i++)
        {
            IgnoreCollisionsWithOtherPlayers();
            yield return null;
        }
    }

    private void IgnoreCollisionsWithOtherPlayers()
    {
        Collider[] myColliders = GetComponentsInChildren<Collider>(true);

#if UNITY_2023_1_OR_NEWER
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
#else
        PlayerMovement[] players = FindObjectsOfType<PlayerMovement>();
#endif

        foreach (PlayerMovement other in players)
        {
            if (other == null || other == this)
                continue;

            Collider[] otherColliders = other.GetComponentsInChildren<Collider>(true);

            foreach (Collider myCollider in myColliders)
            {
                if (myCollider == null)
                    continue;

                foreach (Collider otherCollider in otherColliders)
                {
                    if (otherCollider == null || myCollider == otherCollider)
                        continue;

                    Physics.IgnoreCollision(myCollider, otherCollider, true);
                }
            }
        }
    }

    public void ApplyExternalVelocity(Vector3 velocity)
    {
        verticalVelocity = velocity.y;

        Vector3 planar = new Vector3(velocity.x, 0f, velocity.z);
        transform.position += planar * Runner.DeltaTime;
    }

    public void AddExternalImpulse(Vector3 impulse)
    {
        ApplyExternalVelocity(impulse);
    }

    public void SetMovementAllowed(bool value)
    {
        isMovementAllowed = value;
    }
}