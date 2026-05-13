using UnityEngine;

public class MovementRoleApplier : RoleApplierBehaviour
{
    [SerializeField] private MonoBehaviour movementBlockerBehaviour;

    private IMovementBlocker movementBlocker;

    private void Awake()
    {
        movementBlocker = movementBlockerBehaviour as IMovementBlocker;

        if (movementBlocker == null)
        {
            movementBlocker = GetComponent<IMovementBlocker>();
        }
    }

    public override void ApplyRole(PlayerRole role, bool isLocalPlayer)
    {
        if (movementBlocker == null || role == PlayerRole.None)
        {
            return;
        }

        movementBlocker.IsMovementAllowed = role == PlayerRole.Runner;
    }
}
