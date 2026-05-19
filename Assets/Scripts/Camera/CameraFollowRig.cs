using UnityEngine;

public class CameraFollowRig : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform target;

    [Header("Camera Offset")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 4f, -6f);

    [Header("Smoothing")]
    [SerializeField] private float followSpeed = 14f;
    [SerializeField] private float lookSpeed = 16f;

    [Header("Look")]
    [SerializeField] private bool lookAtTarget = true;
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.2f, 0f);

    private bool hasTarget;

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

        if (snapInstantly)
        {
            transform.position = GetDesiredPosition();
            ApplyLookRotation(true);
        }

        Debug.Log($"CameraFollowRig: target assigned -> {target.name}");
    }

    private void LateUpdate()
    {
        if (!hasTarget || target == null)
            return;

        Vector3 desiredPosition = GetDesiredPosition();

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        ApplyLookRotation(false);
    }

    private Vector3 GetDesiredPosition()
    {
        return target.position + offset;
    }

    private void ApplyLookRotation(bool instant)
    {
        if (!lookAtTarget || target == null)
            return;

        Vector3 lookPoint = target.position + lookOffset;
        Vector3 direction = lookPoint - transform.position;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        if (instant)
        {
            transform.rotation = targetRotation;
        }
        else
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                lookSpeed * Time.deltaTime
            );
        }
    }
}
