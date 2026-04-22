using UnityEngine;

public class CameraFollowRig : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 4f, -6f);
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private float lookSpeed = 8f;
    [SerializeField] private bool lookAtTarget = true;

    public void SetTarget(Transform newTarget, bool snapInstantly = true)
    {
        target = newTarget;

        if (target == null)
        {
            Debug.LogWarning("CameraFollowRig: target is null.");
            return;
        }

        if (snapInstantly)
        {
            transform.position = target.position + offset;

            if (lookAtTarget)
            {
                Vector3 dir = target.position - transform.position;
                if (dir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(dir.normalized);
                }
            }
        }

        Debug.Log($"CameraFollowRig: target assigned -> {target.name}");
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        if (lookAtTarget)
        {
            Vector3 dir = target.position - transform.position;

            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    lookSpeed * Time.deltaTime
                );
            }
        }
    }
}