using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAnimation : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;

    [Header("Animation State Names")]
    [SerializeField] private string movementStateName = "Movement";
    [SerializeField] private string crouchStateName = "Crouch";
    [SerializeField] private string jumpStateName = "Jump";
    [SerializeField] private string fallingStateName = "Fall";
    [SerializeField] private string landingStateName = "Land";

    [Header("Blending")]
    [SerializeField] private float crossFadeDuration = 0.10f;
    [SerializeField] private float speedDampTime = 0.08f;
    [SerializeField] private float jumpVelocityThreshold = 0.35f;
    [SerializeField] private float fallingVelocityThreshold = -0.60f;
    [SerializeField] private float minimumJumpStateTime = 0.24f;
    [SerializeField] private float landingStateTime = 0.18f;

    [Header("Ground Visual Clamp")]
    [SerializeField] private bool keepVisualAboveGround = true;
    [SerializeField] private float visualGroundClearance = 0.02f;
    [SerializeField] private float visualClampLerpSpeed = 18f;

    private PlayerMovement movement;
    private RuntimeAnimatorController fallbackController;

    private int currentStateHash;
    private float jumpStateTimer;
    private float landingTimer;
    private bool wasGrounded = true;

    private Transform visualRoot;
    private Vector3 visualRootBaseLocalPosition;
    private Renderer[] visualRenderers;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int VerticalHash = Animator.StringToHash("Vertical");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
    private static readonly int IsGroundedLowerHash = Animator.StringToHash("isGrounded");
    private static readonly int IsGroundedUpperHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsCrouchingHash = Animator.StringToHash("isCrouching");
    private static readonly int IsSprintingHash = Animator.StringToHash("isSprinting");

    private int movementStateHash;
    private int crouchStateHash;
    private int jumpStateHash;
    private int fallingStateHash;
    private int landingStateHash;

    private void Awake()
    {
        CacheHashes();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (animator != null)
        {
            fallbackController = animator.runtimeAnimatorController;
            CacheVisualRoot();
        }

        movement = GetComponent<PlayerMovement>();
    }

    private void CacheHashes()
    {
        movementStateHash = Animator.StringToHash(movementStateName);
        crouchStateHash = Animator.StringToHash(crouchStateName);
        jumpStateHash = Animator.StringToHash(jumpStateName);
        fallingStateHash = Animator.StringToHash(fallingStateName);
        landingStateHash = Animator.StringToHash(landingStateName);
    }

    public void SetAnimator(Animator newAnimator)
    {
        if (newAnimator == null)
            return;

        RuntimeAnimatorController controllerToUse = fallbackController;

        if (controllerToUse == null && animator != null)
            controllerToUse = animator.runtimeAnimatorController;

        animator = newAnimator;

        if (animator.runtimeAnimatorController == null && controllerToUse != null)
            animator.runtimeAnimatorController = controllerToUse;

        fallbackController = animator.runtimeAnimatorController;

        animator.enabled = true;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        CacheHashes();

        animator.Rebind();
        animator.Update(0f);

        currentStateHash = 0;
        jumpStateTimer = 0f;
        landingTimer = 0f;
        wasGrounded = true;

        CacheVisualRoot();
    }

    public override void Render()
    {
        UpdateAnimation(Time.deltaTime);
    }

    private void UpdateAnimation(float deltaTime)
    {
        if (animator == null)
            return;

        if (!animator.enabled)
            animator.enabled = true;

        if (movement == null)
            movement = GetComponent<PlayerMovement>();

        if (movement == null)
            return;

        Vector2 moveInput = movement.CurrentMoveInput;
        float speed01 = Mathf.Clamp01(movement.CurrentSpeed01);
        bool grounded = movement.CurrentGrounded;
        bool crouching = movement.CurrentCrouching;
        bool sprinting = movement.CurrentSprinting;
        float verticalVelocity = movement.CurrentVerticalVelocity;

        animator.SetFloat(HorizontalHash, moveInput.x, speedDampTime, deltaTime);
        animator.SetFloat(VerticalHash, moveInput.y, speedDampTime, deltaTime);
        animator.SetFloat(SpeedHash, speed01, speedDampTime, deltaTime);
        animator.SetFloat(VerticalVelocityHash, verticalVelocity);

        animator.SetBool(IsGroundedLowerHash, grounded);
        animator.SetBool(IsGroundedUpperHash, grounded);
        animator.SetBool(IsCrouchingHash, crouching);
        animator.SetBool(IsSprintingHash, sprinting);

        if (verticalVelocity > jumpVelocityThreshold)
            jumpStateTimer = minimumJumpStateTime;
        else if (jumpStateTimer > 0f)
            jumpStateTimer -= deltaTime;

        if (!wasGrounded && grounded)
            landingTimer = landingStateTime;
        else if (landingTimer > 0f)
            landingTimer -= deltaTime;

        int targetStateHash = GetTargetStateHash(grounded, crouching, verticalVelocity);
        CrossFadeIfNeeded(targetStateHash);

        wasGrounded = grounded;
    }

    private void LateUpdate()
    {
        ClampVisualAboveGround();
    }

    private int GetTargetStateHash(bool grounded, bool crouching, float verticalVelocity)
    {
        if (!grounded)
        {
            if (jumpStateTimer > 0f || verticalVelocity > jumpVelocityThreshold)
                return jumpStateHash;

            if (verticalVelocity < fallingVelocityThreshold)
                return fallingStateHash;

            return jumpStateHash;
        }

        if (landingTimer > 0f)
            return landingStateHash;

        if (crouching)
            return crouchStateHash;

        return movementStateHash;
    }

    private void CrossFadeIfNeeded(int targetStateHash)
    {
        if (targetStateHash == 0)
            return;

        if (currentStateHash == targetStateHash)
            return;

        if (!animator.HasState(0, targetStateHash))
        {
            currentStateHash = 0;
            return;
        }

        animator.CrossFade(targetStateHash, crossFadeDuration, 0);
        currentStateHash = targetStateHash;
    }

    private void CacheVisualRoot()
    {
        visualRoot = animator != null ? animator.transform : null;

        if (visualRoot == null)
            return;

        visualRootBaseLocalPosition = visualRoot.localPosition;
        visualRenderers = visualRoot.GetComponentsInChildren<Renderer>(true);
    }

    private void ClampVisualAboveGround()
    {
        if (!keepVisualAboveGround || animator == null || movement == null)
            return;

        if (visualRoot == null)
            CacheVisualRoot();

        if (visualRoot == null || visualRenderers == null || visualRenderers.Length == 0)
            return;

        Vector3 targetLocalPosition = visualRootBaseLocalPosition;

        if (movement.CurrentGrounded)
        {
            if (TryGetVisualMinY(out float minY))
            {
                float groundY = transform.position.y + visualGroundClearance;

                if (minY < groundY)
                    targetLocalPosition.y += groundY - minY;
            }
        }

        visualRoot.localPosition = Vector3.Lerp(
            visualRoot.localPosition,
            targetLocalPosition,
            Mathf.Clamp01(visualClampLerpSpeed * Time.deltaTime)
        );
    }

    private bool TryGetVisualMinY(out float minY)
    {
        minY = float.PositiveInfinity;
        bool found = false;

        for (int i = 0; i < visualRenderers.Length; i++)
        {
            Renderer renderer = visualRenderers[i];

            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            Bounds bounds = renderer.bounds;

            if (bounds.size.sqrMagnitude <= 0.0001f)
                continue;

            minY = Mathf.Min(minY, bounds.min.y);
            found = true;
        }

        return found;
    }
}
