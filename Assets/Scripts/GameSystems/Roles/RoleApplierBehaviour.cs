using UnityEngine;

public abstract class RoleApplierBehaviour : MonoBehaviour, IRoleApplier
{
    public abstract void ApplyRole(PlayerRole role, bool isLocalPlayer);
}
