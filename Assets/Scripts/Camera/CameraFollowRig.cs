using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollowRig : MonoBehaviour
{
    public static CameraFollowRig LocalRig { get; private set; }

    private static bool gameplayCursorActive;

    public static void SetGameplayCursorActive(bool isActive)
    {
        gameplayCursorActive = isActive;

        if (!gameplayCursorActive)
            ForceUnlockCursor();
    }

    public static void ForceUnlockCursor()
    {
        if (Cursor.lockState != CursorLockMode.None)
            Cursor.lockState = CursorLockMode.None;

        if (!Cursor.visible)
            Cursor.visible = true;
    }

    [Header("Follow Target")]
    [SerializeField] private Transform target;

    [Header("Orbit")]
    [SerializeField] private float distance = 6f;
    [SerializeField] private float shoulderOffset = 0.35f;
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.4f, 0f);
    [SerializeField] private float mouseSensitivityX = 0.08f;
    [SerializeField] private float mouseSensitivityY = 0.07f;
    [SerializeField] private float minPitch = -25f;
    [SerializeField] private float maxPitch = 65f;
    [SerializeField] private bool invertY = false;

    [Header("Smoothing")]
    [SerializeField] private float followSpeed = 18f;
    [SerializeField] private float lookSpeed = 20f;
    [SerializeField] private float snapDistance = 12f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnTarget = true;
    [SerializeField] private bool unlockCursorWithEscape = true;
    [SerializeField] private bool relockCursorWithLeftClick = true;

    [Header("Collision")]
    [SerializeField] private bool avoidWallClipping = false;
    [SerializeField] private float collisionRadius = 0.25f;
    [SerializeField] private LayerMask collisionMask = ~0;

    private bool hasTarget;
    private float yaw;
    private float pitch = 18f;
    private float mouseSensitivityMultiplier = SettingsManager.DefaultMouseSensitivity;

    public float CurrentYaw => yaw;

    public static bool TryGetLocalYaw(out float localYaw)
    {
        if (LocalRig == null)
        {
            localYaw = 0f;
            return false;
        }

        localYaw = LocalRig.CurrentYaw;
        return true;
    }

    public void SetTarget(Transform newTarget, bool snapInstantly = true)
    {
        target = newTarget;
        hasTarget = target != null;

        if (!hasTarget)
        {
            Debug.LogWarning("CameraFollowRig: Target null geldi.");
            return;
        }

        if (target == transform)
        {
            Debug.LogError("CameraFollowRig HATA: Camera rig kendisini target olarak takip edemez.");
            target = null;
            hasTarget = false;
            return;
        }

        LocalRig = this;
        SetYawFromTarget();

        if (lockCursorOnTarget && gameplayCursorActive)
            LockCursor();
        else
            ForceUnlockCursor();

        if (snapInstantly)
        {
            transform.position = GetDesiredPosition();
            ApplyLookRotation(true, Time.deltaTime);
        }

        Debug.Log($"CameraFollowRig: target assigned -> {target.name}");
    }

    private void OnEnable()
    {
        mouseSensitivityMultiplier = SettingsManager.MouseSensitivity;
        SettingsManager.MouseSensitivityChanged += HandleMouseSensitivityChanged;
    }

    private void OnDisable()
    {
        if (LocalRig == this)
            LocalRig = null;

        SettingsManager.MouseSensitivityChanged -= HandleMouseSensitivityChanged;
    }

    private void HandleMouseSensitivityChanged(float value)
    {
        mouseSensitivityMultiplier = value;
    }

    private void OnDestroy()
    {
        if (LocalRig == this)
            LocalRig = null;
    }

    private void LateUpdate()
    {
        if (!gameplayCursorActive)
        {
            ForceUnlockCursor();
            return;
        }

        if (!hasTarget || target == null)
            return;

        HandleCursorState();
        HandleMouseLook();

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 desiredPosition = GetDesiredPosition();
        float distanceToTarget = Vector3.Distance(transform.position, desiredPosition);

        if (distanceToTarget > snapDistance)
        {
            transform.position = desiredPosition;
        }
        else
        {
            float followT = 1f - Mathf.Exp(-followSpeed * deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followT);
        }

        ApplyLookRotation(false, deltaTime);
    }

    private void SetYawFromTarget()
    {
        Vector3 forward = target.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude > 0.001f)
            yaw = Quaternion.LookRotation(forward.normalized, Vector3.up).eulerAngles.y;
        else
            yaw = transform.eulerAngles.y;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void HandleCursorState()
    {
        if (!gameplayCursorActive)
        {
            ForceUnlockCursor();
            return;
        }

        if (PauseMenuManager.IsPaused)
        {
            ForceUnlockCursor();
            return;
        }

        if (unlockCursorWithEscape && IsEscapePressed())
        {
            ForceUnlockCursor();
        }

        if (relockCursorWithLeftClick && IsLeftMousePressed())
            LockCursor();
    }

    private void HandleMouseLook()
    {
        if (!gameplayCursorActive || PauseMenuManager.IsPaused)
            return;

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        Vector2 mouseDelta = GetMouseDelta();

        if (mouseDelta.sqrMagnitude <= 0.001f)
            return;

        float sensitivity = Mathf.Max(0.01f, mouseSensitivityMultiplier);

        yaw += mouseDelta.x * mouseSensitivityX * sensitivity;

        float yDirection = invertY ? 1f : -1f;
        pitch += mouseDelta.y * mouseSensitivityY * yDirection * sensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private Vector2 GetMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.delta.ReadValue();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxisRaw("Mouse X") * 12f, Input.GetAxisRaw("Mouse Y") * 12f);
#else
        return Vector2.zero;
#endif
    }

    private bool IsEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    private bool IsLeftMousePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private Vector3 GetDesiredPosition()
    {
        Vector3 lookPoint = GetLookPoint();
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = lookPoint + orbitRotation * new Vector3(shoulderOffset, 0f, -distance);

        if (!avoidWallClipping)
            return desiredPosition;

        Vector3 toCamera = desiredPosition - lookPoint;
        float castDistance = toCamera.magnitude;

        if (castDistance <= 0.01f)
            return desiredPosition;

        Vector3 direction = toCamera / castDistance;

        if (Physics.SphereCast(
            lookPoint,
            collisionRadius,
            direction,
            out RaycastHit hit,
            castDistance,
            collisionMask,
            QueryTriggerInteraction.Ignore))
        {
            return hit.point - direction * collisionRadius;
        }

        return desiredPosition;
    }

    private Vector3 GetLookPoint()
    {
        return target.position + lookOffset;
    }

    private void ApplyLookRotation(bool instant, float deltaTime)
    {
        Vector3 direction = GetLookPoint() - transform.position;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        if (instant)
        {
            transform.rotation = targetRotation;
        }
        else
        {
            float lookT = 1f - Mathf.Exp(-lookSpeed * deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookT);
        }
    }
}
