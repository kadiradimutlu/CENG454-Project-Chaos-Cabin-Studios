using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : NetworkBehaviour
{
    private bool isMovementAllowed = true;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.6f;
    [SerializeField] private float rotationSpeed = 14f;

    [Header("Gravity / Jump")]
    [SerializeField] private float gravity = -16f;
    [SerializeField] private float jumpForce = 11.5f;
    [SerializeField] private float groundedGravity = -2f;
    [SerializeField] private float groundSnapDistance = 0.35f;
    [SerializeField] private float jumpGroundIgnoreTime = 0.22f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Crouch")]
    [SerializeField] private float standingCapsuleHeight = 2f;
    [SerializeField] private float standingCapsuleCenterY = 1f;
    [SerializeField] private float crouchingCapsuleHeight = 1.25f;
    [SerializeField] private float crouchingCapsuleCenterY = 0.625f;
    [SerializeField] private bool allowKeyboardCrouchFallback = true;

    [Header("Player Collision")]
    [SerializeField] private bool ignorePlayerToPlayerCollision = true;

    [Networked] public Vector2 NetMoveInput { get; private set; }
    [Networked] public float NetSpeed01 { get; private set; }
    [Networked] public NetworkBool NetGrounded { get; private set; }
    [Networked] public float NetVerticalVelocity { get; private set; }
    [Networked] public NetworkBool NetCrouching { get; private set; }
    [Networked] public NetworkBool NetSprinting { get; private set; }

    public Vector2 CurrentMoveInput { get; private set; }
    public float CurrentSpeed01 { get; private set; }
    public bool CurrentGrounded { get; private set; }
    public float CurrentVerticalVelocity { get; private set; }
    public bool CurrentCrouching { get; private set; }
    public bool CurrentSprinting { get; private set; }

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private PlayerHealth playerHealth;

    [Networked] private float VerticalVelocity { get; set; }
    [Networked] private float JumpGroundIgnoreTimer { get; set; }
    [Networked] private NetworkButtons PreviousButtons { get; set; }

    // --- Slow / speed modifier (server-authoritative, tick based) ---
    // SpeedMultiplier 1 = normal. < 1 = slowed. Replicated to all peers.
    [Networked] public float SpeedMultiplier { get; private set; }
    // The network tick at which the active slow expires. 0 = no active slow.
    [Networked] private int SlowExpiryTick { get; set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        playerHealth = GetComponent<PlayerHealth>();
        SetupCollider(false);
    }

    public override void Spawned()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (capsule == null)
            capsule = GetComponent<CapsuleCollider>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        if (HasStateAuthority)
        {
            VerticalVelocity = groundedGravity;
            JumpGroundIgnoreTimer = 0f;
            PreviousButtons = default;
            SpeedMultiplier = 1f;
            SlowExpiryTick = 0;
        }

        SetupCollider(false);

        if (ignorePlayerToPlayerCollision)
            StartCoroutine(RefreshPlayerCollisionIgnores());
    }

    public override void FixedUpdateNetwork()
    {
        if (!CanSimulateMovement())
        {
            PullAnimationStateFromNetwork();
            ApplyCrouchCollider(CurrentCrouching);
            return;
        }

        PlayerNetworkInputData inputData = default;
        bool hasInput = GetInput(out inputData);

        if (!hasInput)
            inputData.MoveInput = Vector2.zero;

        if (IsEliminated())
        {
            inputData.MoveInput = Vector2.zero;
            inputData.Buttons = default;
        }

        float deltaTime = Runner.DeltaTime;

        // Expire an active slow once its tick deadline has passed (StateAuthority only).
        // Tick-based instead of a timer/coroutine so the effect self-clears even if the
        // trap that applied it is destroyed -> no "permanent slow" bug.
        if (HasStateAuthority && SlowExpiryTick != 0 && Runner.Tick >= SlowExpiryTick)
        {
            SpeedMultiplier = 1f;
            SlowExpiryTick = 0;
        }

        float verticalVelocity = VerticalVelocity;
        float jumpGroundIgnoreTimer = JumpGroundIgnoreTimer;

        if (Mathf.Approximately(verticalVelocity, 0f) && CheckGround(transform.position, out _))
            verticalVelocity = groundedGravity;

        if (jumpGroundIgnoreTimer > 0f)
            jumpGroundIgnoreTimer -= deltaTime;

        Vector2 moveInput = inputData.MoveInput;

        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        bool crouching = inputData.Buttons.IsSet((int)PlayerInputButton.Crouch);
        bool sprinting = inputData.Buttons.IsSet((int)PlayerInputButton.Sprint);

        if (allowKeyboardCrouchFallback && HasInputAuthority && Keyboard.current != null)
        {
            crouching =
                crouching ||
                Keyboard.current.leftCtrlKey.isPressed ||
                Keyboard.current.rightCtrlKey.isPressed;
        }

        if (crouching)
            sprinting = false;

        ApplyCrouchCollider(crouching);

        bool jumpPressed =
            inputData.Buttons.IsSet((int)PlayerInputButton.Jump) &&
            !PreviousButtons.IsSet((int)PlayerInputButton.Jump);

        bool grounded = jumpGroundIgnoreTimer <= 0f && CheckGround(transform.position, out _);

        if (grounded && verticalVelocity < 0f)
            verticalVelocity = groundedGravity;

        if (jumpPressed && grounded && isMovementAllowed && !crouching)
        {
            verticalVelocity = jumpForce;
            grounded = false;
            jumpGroundIgnoreTimer = jumpGroundIgnoreTime;
        }

        float speed = moveSpeed;

        if (sprinting)
            speed = sprintSpeed;
        else if (crouching)
            speed = crouchSpeed;

        // Apply slow / speed modifier. Clamp guards against an uninitialized 0 multiplier
        // (which would freeze the player) and against absurd values.
        speed *= Mathf.Clamp(SpeedMultiplier <= 0f ? 1f : SpeedMultiplier, 0.05f, 1f);

        Vector3 moveDirection = isMovementAllowed
            ? GetCameraRelativeMoveDirection(moveInput, inputData.CameraYaw)
            : Vector3.zero;

        verticalVelocity += gravity * deltaTime;

        Vector3 horizontalVelocity = moveDirection * speed;
        Vector3 velocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
        Vector3 nextPosition = transform.position + velocity * deltaTime;

        if (jumpGroundIgnoreTimer <= 0f && verticalVelocity <= 0f && CheckGround(nextPosition, out RaycastHit snapHit))
        {
            float desiredY = GetRootYFromGround(snapHit.point.y);

            if (nextPosition.y <= desiredY + 0.20f)
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
        CurrentSprinting = sprinting;
        CurrentCrouching = crouching;
        CurrentGrounded = grounded;
        CurrentVerticalVelocity = verticalVelocity;

        float speed01 = moveInput.magnitude;

        if (sprinting)
            speed01 = Mathf.Clamp01(speed01 * 1.15f);
        else if (crouching)
            speed01 = Mathf.Clamp01(speed01 * 0.55f);

        CurrentSpeed01 = speed01;

        VerticalVelocity = verticalVelocity;
        JumpGroundIgnoreTimer = jumpGroundIgnoreTimer;
        PreviousButtons = inputData.Buttons;

        if (HasStateAuthority)
        {
            NetMoveInput = CurrentMoveInput;
            NetSpeed01 = CurrentSpeed01;
            NetGrounded = CurrentGrounded;
            NetVerticalVelocity = CurrentVerticalVelocity;
            NetCrouching = CurrentCrouching;
            NetSprinting = CurrentSprinting;
        }
    }

    public override void Render()
    {
        if (!CanSimulateMovement())
        {
            PullAnimationStateFromNetwork();
            ApplyCrouchCollider(CurrentCrouching);
        }
    }


    private bool CanSimulateMovement()
    {
        return HasStateAuthority || HasInputAuthority;
    }

    private bool IsEliminated()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        return playerHealth != null && playerHealth.IsEliminated;
    }

    private void PullAnimationStateFromNetwork()
    {
        CurrentMoveInput = NetMoveInput;
        CurrentSpeed01 = NetSpeed01;
        CurrentGrounded = NetGrounded;
        CurrentVerticalVelocity = NetVerticalVelocity;
        CurrentCrouching = NetCrouching;
        CurrentSprinting = NetSprinting;
    }

    private void SetupCollider(bool crouching)
    {
        if (capsule == null)
            return;

        capsule.isTrigger = false;
        capsule.radius = 0.5f;
        capsule.direction = 1;

        ApplyCrouchCollider(crouching);
    }

    private void ApplyCrouchCollider(bool crouching)
    {
        if (capsule == null)
            return;

        capsule.height = crouching ? crouchingCapsuleHeight : standingCapsuleHeight;
        capsule.center = new Vector3(
            0f,
            crouching ? crouchingCapsuleCenterY : standingCapsuleCenterY,
            0f
        );
    }


    private Vector3 GetCameraRelativeMoveDirection(Vector2 input, float cameraYaw)
    {
        if (input.sqrMagnitude <= 0.001f)
            return Vector3.zero;

        Quaternion yawRotation = Quaternion.Euler(0f, cameraYaw, 0f);
        Vector3 forward = yawRotation * Vector3.forward;
        Vector3 right = yawRotation * Vector3.right;

        forward.y = 0f;
        right.y = 0f;

        if (forward.sqrMagnitude > 0.001f)
            forward.Normalize();

        if (right.sqrMagnitude > 0.001f)
            right.Normalize();

        Vector3 direction = right * input.x + forward * input.y;

        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        return direction;
    }

    private bool CheckGround(Vector3 rootPosition, out RaycastHit hit)
    {
        if (capsule == null)
        {
            hit = default;
            return false;
        }

        int effectiveGroundMask = groundMask.value == 0 ? ~0 : groundMask.value;

        Vector3 capsuleCenter = rootPosition + capsule.center;
        float halfHeight = capsule.height * 0.5f;
        float rayDistance = halfHeight + groundSnapDistance;
        Vector3 origin = capsuleCenter + Vector3.up * 0.10f;

        bool didHit = Physics.SphereCast(
            origin,
            Mathf.Max(0.06f, capsule.radius * 0.82f),
            Vector3.down,
            out hit,
            rayDistance,
            effectiveGroundMask,
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
        if (capsule == null)
            return groundY;

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
        VerticalVelocity = velocity.y;

        Vector3 planar = new Vector3(velocity.x, 0f, velocity.z);
        transform.position += planar * Runner.DeltaTime;
    }

    public void AddExternalImpulse(Vector3 impulse)
    {
        ApplyExternalVelocity(impulse);
    }

    public void ResetMovementForRespawn(Vector3 position, Quaternion rotation)
    {
        if (!HasStateAuthority)
            return;

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (capsule == null)
            capsule = GetComponent<CapsuleCollider>();

        transform.SetPositionAndRotation(position, rotation);

        if (rb != null)
        {
            rb.position = position;
            rb.rotation = rotation;
            rb.angularVelocity = Vector3.zero;

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
        }

        isMovementAllowed = true;
        VerticalVelocity = groundedGravity;
        JumpGroundIgnoreTimer = 0f;
        PreviousButtons = default;
        SpeedMultiplier = 1f;
        SlowExpiryTick = 0;

        CurrentMoveInput = Vector2.zero;
        CurrentSpeed01 = 0f;
        CurrentGrounded = true;
        CurrentVerticalVelocity = groundedGravity;
        CurrentCrouching = false;
        CurrentSprinting = false;

        NetMoveInput = Vector2.zero;
        NetSpeed01 = 0f;
        NetGrounded = true;
        NetVerticalVelocity = groundedGravity;
        NetCrouching = false;
        NetSprinting = false;

        SetupCollider(false);
    }

    public void SetMovementAllowed(bool value)
    {
        isMovementAllowed = value;
    }

    
    public void ApplySlow(float multiplier, float duration)
    {
        if (!HasStateAuthority)
            return;

        multiplier = Mathf.Clamp(multiplier, 0.05f, 1f);
        duration = Mathf.Max(0f, duration);

        if (duration <= 0f)
            return;

        
        float effective = SpeedMultiplier <= 0f ? 1f : SpeedMultiplier;
        SpeedMultiplier = (SlowExpiryTick != 0) ? Mathf.Min(effective, multiplier) : multiplier;

        int durationTicks = Mathf.CeilToInt(duration / Runner.DeltaTime);
        SlowExpiryTick = Runner.Tick + durationTicks;
    }

    
    public void ClearSlow()
    {
        if (!HasStateAuthority)
            return;

        SpeedMultiplier = 1f;
        SlowExpiryTick = 0;
    }
}






