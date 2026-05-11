using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.25f;

    public bool IsGrounded { get; private set; }

    public void CheckGround()
    {
        Vector3 checkPosition = groundCheckPoint != null
            ? groundCheckPoint.position
            : transform.position + Vector3.down * 0.95f;

        IsGrounded = Physics.CheckSphere(checkPosition, groundCheckRadius, groundLayer);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 checkPosition = groundCheckPoint != null
            ? groundCheckPoint.position
            : transform.position + Vector3.down * 0.95f;

        Gizmos.DrawWireSphere(checkPosition, groundCheckRadius);
    }
#endif
}
