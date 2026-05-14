using Fusion;
using UnityEngine;
 
 
 
public class PlayerMovement : NetworkBehaviour
{
    private bool isMovementAllowed = true;
 
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpForce = 5f;
   
 
    private Rigidbody _rb;
    private void Awake()
    {
    _rb = GetComponent<Rigidbody>();
    }
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out GameplayInput input))
        {
           
            var direction = input.MoveDirection.normalized;
 
            DoMove(direction, input);
        }
    }
 
    private void DoMove(Vector2 direction, GameplayInput input)
    {
        if (isMovementAllowed)
        {
            Vector3 moveVector = new Vector3(direction.x, 0, direction.y);
 
            float currentSpeed = moveSpeed;
            if (input.SprintButton) currentSpeed = moveSpeed * 2f;
            else if (input.CrouchButton) currentSpeed = moveSpeed * 0.5f;
   
            _rb.MovePosition(_rb.position + moveVector * currentSpeed * Runner.DeltaTime);
 
            if (input.JumpButton && IsGrounded())
            {
                _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
 
   
            if (moveVector != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveVector);
                _rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Runner.DeltaTime));
            }
           
        }
    }
 
    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }
}
 