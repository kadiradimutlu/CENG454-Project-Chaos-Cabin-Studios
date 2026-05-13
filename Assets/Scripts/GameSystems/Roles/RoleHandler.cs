using Fusion;
using UnityEngine;

public class RoleHandler : NetworkBehaviour
{
    [Networked] public PlayerRole CurrentRole { get; set; }

    [SerializeField] private RoleApplierBehaviour[] roleAppliers;

    private PlayerRole lastRenderedRole = PlayerRole.None;

    public override void Spawned()
    {
        if (roleAppliers == null || roleAppliers.Length == 0)
        {
            roleAppliers = GetComponents<RoleApplierBehaviour>();
        }

        ApplyRoleSettings(CurrentRole);
        lastRenderedRole = CurrentRole;
    }

    public override void Render()
    {
        if (lastRenderedRole == CurrentRole)
        {
            return;
        }

        ApplyRoleSettings(CurrentRole);
        lastRenderedRole = CurrentRole;
    }

    private void ApplyRoleSettings(PlayerRole role)
    {
        bool isLocalPlayer = Object != null && Object.HasInputAuthority;

        foreach (RoleApplierBehaviour roleApplier in roleAppliers)
        {
            if (roleApplier != null)
            {
                roleApplier.ApplyRole(role, isLocalPlayer);
            }
        }
    }
}
