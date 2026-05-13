using Fusion;
using UnityEngine;



public class PlayerMovement : NetworkBehaviour
{
    private bool isMovementAllowed;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    private CharacterController _controller;
    private void Awake() {
    _controller = GetComponent<CharacterController>();
}
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out GameplayInput input))
        {
            
            var direction = input.MoveDirection.normalized;

            DoMove(direction);
        }
    }

    private void DoMove(Vector2 direction)
{
    if (isMovementAllowed)
    {
    Vector3 moveVector = new Vector3(direction.x, 0, direction.y);

    
    transform.position += moveVector * moveSpeed * Runner.DeltaTime;

    
    if (moveVector != Vector3.zero)
    {
        Quaternion targetRotation = Quaternion.LookRotation(moveVector);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Runner.DeltaTime);
    }
}
}
}