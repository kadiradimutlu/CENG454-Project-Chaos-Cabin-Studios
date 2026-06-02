using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(AudioSource))]
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

    [Header("Obstacle Collision")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float obstacleSkin = 0.04f;
    [SerializeField] private bool slideAlongObstacles = true;

    [Header("SFX Routing / Nereden Alıyor")]
    [SerializeField] private bool enableMovementSfx = true;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [TextArea(2, 3)]
    [SerializeField] private string sfxRouteInfo =
        "Sesler PlayerMovement üzerindeki SFX Source'tan çalar. Output: SFX Mixer Group.";

    [Header("Movement SFX")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landingClip;
    [SerializeField] private bool useAnimationFootstepEvents = false;
    [Range(0f, 1f)] [SerializeField] private float footstepVolume = 0.75f;
    [Range(0f, 1f)] [SerializeField] private float jumpVolume = 0.9f;
    [Range(0f, 1f)] [SerializeField] private float landingVolume = 0.9f;
    [SerializeField] private float footstepInterval = 0.42f;
    [SerializeField] private float sprintFootstepIntervalMultiplier = 0.72f;
    [SerializeField] private float crouchFootstepIntervalMultiplier = 1.35f;
    [SerializeField] private float footstepMoveThreshold = 0.08f;
    [SerializeField] private float jumpSfxVelocityThreshold = 0.2f;
    [SerializeField] private float landingMinFallSpeed = 2.5f;
    [Range(0f, 1f)] [SerializeField] private float sfxSpatialBlend = 1f;

    [Header("Snow Zone Multiplier")]
    [SerializeField] private float iceAcceleration = 4.5f;
    [SerializeField] private float iceDeceleration = 1.25f;
    [SerializeField] private float iceTopSpeedMultiplier = 1.05f;

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

    private bool sfxStateInitialized;
    private bool sfxWasGrounded;
    private float sfxPreviousVerticalVelocity;
    private float footstepTimer;

    [Networked] private float VerticalVelocity { get; set; }
    [Networked] private float JumpGroundIgnoreTimer { get; set; }
    [Networked] private NetworkButtons PreviousButtons { get; set; }

    // --- Slow / speed modifier (server-authoritative, tick based) ---
    // SpeedMultiplier 1 = normal. < 1 = slowed. Replicated to all peers.
    [Networked] public float SpeedMultiplier { get; private set; }
    // The network tick at which the active slow expires. 0 = no active slow.
    [Networked] private int SlowExpiryTick { get; set; }
    [Networked] private Vector3 IceHorizontalVelocity { get; set; }

    private bool temporaryIceActive;
    private float temporaryIceAcceleration;
    private float temporaryIceDeceleration;
    private float temporaryIceTopSpeedMultiplier;
    private float temporaryIceExpireTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        playerHealth = GetComponent<PlayerHealth>();
        SetupCollider(false);
        SetupSfxSource();
        ResetSfxState();
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
            IceHorizontalVelocity = Vector3.zero;
        }

        SetupCollider(false);
        SetupSfxSource();
        ResetSfxState();

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

        Vector3 desiredHorizontalVelocity = moveDirection * speed;
        Vector3 horizontalVelocity = GetHorizontalVelocityForSurface(desiredHorizontalVelocity, deltaTime);
        Vector3 horizontalStep = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z) * deltaTime;
        Vector3 nextPosition = ResolveHorizontalMovement(transform.position, horizontalStep);
        nextPosition.y += verticalVelocity * deltaTime;

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

        UpdateMovementSfx(Time.deltaTime);
    }

    private void SetupSfxSource()
    {
        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = sfxSpatialBlend;
        sfxSource.outputAudioMixerGroup = sfxMixerGroup;
    }

    private void ResetSfxState()
    {
        sfxStateInitialized = false;
        sfxWasGrounded = CurrentGrounded;
        sfxPreviousVerticalVelocity = CurrentVerticalVelocity;
        footstepTimer = 0f;
    }

    private void UpdateMovementSfx(float deltaTime)
    {
        if (!enableMovementSfx)
            return;

        if (sfxSource == null)
            SetupSfxSource();

        if (sfxSource == null)
            return;

        if (!sfxStateInitialized)
        {
            sfxWasGrounded = CurrentGrounded;
            sfxPreviousVerticalVelocity = CurrentVerticalVelocity;
            sfxStateInitialized = true;
            return;
        }

        bool jumped =
            sfxWasGrounded &&
            !CurrentGrounded &&
            CurrentVerticalVelocity > jumpSfxVelocityThreshold;

        if (jumped)
        {
            PlaySfx(jumpClip, jumpVolume);
            footstepTimer = GetFootstepInterval();
        }

        bool landed =
            !sfxWasGrounded &&
            CurrentGrounded &&
            sfxPreviousVerticalVelocity <= -Mathf.Abs(landingMinFallSpeed);

        if (landed)
        {
            PlaySfx(landingClip, landingVolume);
            footstepTimer = Mathf.Max(0.05f, GetFootstepInterval() * 0.5f);
        }

        UpdateFootstepSfx(deltaTime);

        sfxWasGrounded = CurrentGrounded;
        sfxPreviousVerticalVelocity = CurrentVerticalVelocity;
    }

    private void UpdateFootstepSfx(float deltaTime)
    {
        if (useAnimationFootstepEvents)
            return;

        bool shouldStep =
            CurrentGrounded &&
            CurrentSpeed01 > footstepMoveThreshold &&
            isMovementAllowed;

        if (!shouldStep)
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer -= deltaTime;

        if (footstepTimer > 0f)
            return;

        PlayRandomFootstepSfx();
        footstepTimer = GetFootstepInterval();
    }

    public void PlayFootstepSfxFromAnimation()
    {
        if (!enableMovementSfx)
            return;

        if (!CurrentGrounded || CurrentSpeed01 <= footstepMoveThreshold)
            return;

        PlayRandomFootstepSfx();
        footstepTimer = GetFootstepInterval();
    }

    private float GetFootstepInterval()
    {
        float interval = Mathf.Max(0.05f, footstepInterval);

        if (CurrentSprinting)
            interval *= sprintFootstepIntervalMultiplier;
        else if (CurrentCrouching)
            interval *= crouchFootstepIntervalMultiplier;

        return Mathf.Max(0.05f, interval);
    }

    private void PlayRandomFootstepSfx()
    {
        if (footstepClips == null || footstepClips.Length == 0)
            return;

        for (int i = 0; i < footstepClips.Length; i++)
        {
            AudioClip clip = footstepClips[UnityEngine.Random.Range(0, footstepClips.Length)];

            if (clip == null)
                continue;

            PlaySfx(clip, footstepVolume);
            return;
        }
    }

    private void PlaySfx(AudioClip clip, float volume)
    {
        if (!enableMovementSfx || clip == null)
            return;

        if (sfxSource == null)
            SetupSfxSource();

        if (sfxSource == null)
            return;

        sfxSource.spatialBlend = sfxSpatialBlend;
        sfxSource.outputAudioMixerGroup = sfxMixerGroup;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
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

    private bool IsRunnerOnSnow()
    {
        RoleHandler role = GetComponent<RoleHandler>();

        if (role == null || role.currentRole != RoleHandler.PlayerRole.Runner)
            return false;

        RunnerZoneTracker tracker = GetComponent<RunnerZoneTracker>();

        if (tracker == null)
            return false;

        return tracker.IsInZone(ZoneType.Snow);
    }

    private Vector3 GetHorizontalVelocityForSurface(Vector3 desiredHorizontalVelocity, float deltaTime)
    {
        if (!isMovementAllowed)
        {
            IceHorizontalVelocity = Vector3.zero;
            return Vector3.zero;
        }

        bool useIceMovement = IsRunnerOnSnow();

        float activeIceAcceleration = iceAcceleration;
        float activeIceDeceleration = iceDeceleration;
        float activeIceTopSpeedMultiplier = iceTopSpeedMultiplier;

        if (IsTemporaryIceActive())
        {
            useIceMovement = true;
            activeIceAcceleration = temporaryIceAcceleration;
            activeIceDeceleration = temporaryIceDeceleration;
            activeIceTopSpeedMultiplier = temporaryIceTopSpeedMultiplier;
        }

        if (!useIceMovement)
        {
            IceHorizontalVelocity = Vector3.zero;
            return desiredHorizontalVelocity;
        }

        Vector3 target = desiredHorizontalVelocity * Mathf.Max(0.2f, activeIceTopSpeedMultiplier);
        float accelerationStep = Mathf.Max(0f, activeIceAcceleration) * deltaTime;
        float decelerationStep = Mathf.Max(0f, activeIceDeceleration) * deltaTime;

        if (desiredHorizontalVelocity.sqrMagnitude > 0.0001f)
            IceHorizontalVelocity = Vector3.MoveTowards(IceHorizontalVelocity, target, accelerationStep);
        else
            IceHorizontalVelocity = Vector3.MoveTowards(IceHorizontalVelocity, Vector3.zero, decelerationStep);

        IceHorizontalVelocity = new Vector3(IceHorizontalVelocity.x, 0f, IceHorizontalVelocity.z);
        return IceHorizontalVelocity;
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


    private Vector3 ResolveHorizontalMovement(Vector3 startPosition, Vector3 horizontalStep)
    {
        if (horizontalStep.sqrMagnitude <= 0.000001f || capsule == null)
            return startPosition + horizontalStep;

        Vector3 direction = horizontalStep.normalized;
        float distance = horizontalStep.magnitude;

        if (!CastObstacle(startPosition, direction, distance + obstacleSkin, out RaycastHit hit))
            return startPosition + horizontalStep;

        float allowedDistance = Mathf.Max(0f, hit.distance - obstacleSkin);
        Vector3 result = startPosition + direction * Mathf.Min(allowedDistance, distance);

        if (!slideAlongObstacles)
            return result;

        Vector3 remainingStep = horizontalStep - direction * allowedDistance;
        Vector3 slideStep = Vector3.ProjectOnPlane(remainingStep, hit.normal);
        slideStep.y = 0f;

        if (slideStep.sqrMagnitude <= 0.000001f)
            return result;

        Vector3 slideDirection = slideStep.normalized;
        float slideDistance = slideStep.magnitude;

        if (CastObstacle(result, slideDirection, slideDistance + obstacleSkin, out RaycastHit slideHit))
        {
            float slideAllowed = Mathf.Max(0f, slideHit.distance - obstacleSkin);
            result += slideDirection * Mathf.Min(slideAllowed, slideDistance);
        }
        else
        {
            result += slideStep;
        }

        return result;
    }

    private bool CastObstacle(Vector3 rootPosition, Vector3 direction, float distance, out RaycastHit closestHit)
    {
        closestHit = default;

        if (capsule == null || distance <= 0f)
            return false;

        int effectiveObstacleMask = obstacleMask.value == 0 ? ~0 : obstacleMask.value;
        GetCapsuleCastPoints(rootPosition, out Vector3 bottom, out Vector3 top, out float radius);

        RaycastHit[] hits = Physics.CapsuleCastAll(
            bottom,
            top,
            radius,
            direction,
            distance,
            effectiveObstacleMask,
            QueryTriggerInteraction.Ignore
        );

        bool found = false;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            if (ShouldIgnoreObstacleHit(hit))
                continue;

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                closestHit = hit;
                found = true;
            }
        }

        return found;
    }

    private void GetCapsuleCastPoints(Vector3 rootPosition, out Vector3 bottom, out Vector3 top, out float radius)
    {
        radius = Mathf.Max(0.05f, capsule.radius * 0.92f);
        Vector3 center = rootPosition + capsule.center;
        float halfHeight = Mathf.Max(capsule.height * 0.5f, radius);
        float pointOffset = Mathf.Max(0f, halfHeight - radius);

        bottom = center + Vector3.down * pointOffset + Vector3.up * 0.06f;
        top = center + Vector3.up * pointOffset;
    }

    private bool ShouldIgnoreObstacleHit(RaycastHit hit)
    {
        if (hit.collider == null)
            return true;

        if (hit.collider == capsule || hit.collider.transform.IsChildOf(transform))
            return true;

        if (hit.collider.GetComponentInParent<PlayerMovement>() != null)
            return true;

        if (hit.normal.y > 0.65f)
            return true;

        return false;
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
        IceHorizontalVelocity = Vector3.zero;

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
        ResetSfxState();
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

    public void ApplyTemporaryIceSurface(
        float acceleration,
        float deceleration,
        float topSpeedMultiplier,
        float refreshDuration)
    {
        if (!HasStateAuthority && !HasInputAuthority)
            return;

        temporaryIceActive = true;
        temporaryIceAcceleration = Mathf.Max(0f, acceleration);
        temporaryIceDeceleration = Mathf.Max(0f, deceleration);
        temporaryIceTopSpeedMultiplier = Mathf.Clamp(topSpeedMultiplier, 0.2f, 3f);
        temporaryIceExpireTime = Time.time + Mathf.Max(0.1f, refreshDuration);
    }

    private bool IsTemporaryIceActive()
    {
        if (!temporaryIceActive)
            return false;

        if (Time.time <= temporaryIceExpireTime)
            return true;

        temporaryIceActive = false;
        return false;
    }

    public void ClearSlow()
    {
        if (!HasStateAuthority)
            return;

        SpeedMultiplier = 1f;
        SlowExpiryTick = 0;
    }

        public void RidePlatform(Vector3 platformDelta)
    {
        if (!HasStateAuthority)
            return;
 
        transform.position += platformDelta;
    }

    private void OnValidate()
    {
        iceAcceleration = Mathf.Max(0f, iceAcceleration);
        iceDeceleration = Mathf.Max(0f, iceDeceleration);
        iceTopSpeedMultiplier = Mathf.Clamp(iceTopSpeedMultiplier, 0.2f, 2f);

        footstepInterval = Mathf.Max(0.05f, footstepInterval);
        sprintFootstepIntervalMultiplier = Mathf.Clamp(sprintFootstepIntervalMultiplier, 0.25f, 2f);
        crouchFootstepIntervalMultiplier = Mathf.Clamp(crouchFootstepIntervalMultiplier, 0.25f, 3f);
        footstepMoveThreshold = Mathf.Max(0f, footstepMoveThreshold);
        jumpSfxVelocityThreshold = Mathf.Max(0f, jumpSfxVelocityThreshold);
        landingMinFallSpeed = Mathf.Max(0f, landingMinFallSpeed);

        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = sfxSpatialBlend;
            sfxSource.outputAudioMixerGroup = sfxMixerGroup;
        }
    }
}






