using System.Collections.Generic;
using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class TrapperTeleporter : MonoBehaviour
{
    [Header("Teleport Target")]
    [SerializeField] private Transform targetPoint;

    [Header("Rules")]
    [SerializeField] private bool requireTrapperRole = true;
    [SerializeField] private float teleportCooldownSeconds = 1.0f;

    [Header("Debug")]
    [SerializeField] private bool logTeleportEvents = false;

    private static readonly Dictionary<NetworkObject, float> NextAllowedTeleportTimeByPlayer = new();

    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTeleport(other);
    }

    private void TryTeleport(Collider other)
    {
        if (targetPoint == null)
            return;

        NetworkObject playerObject = other.GetComponentInParent<NetworkObject>();

        if (playerObject == null)
            return;

        if (!playerObject.HasStateAuthority)
            return;

        RoleHandler roleHandler = playerObject.GetComponentInChildren<RoleHandler>(true);

        if (requireTrapperRole)
        {
            if (roleHandler == null)
                return;

            if (roleHandler.currentRole != RoleHandler.PlayerRole.Trapper)
                return;
        }

        if (IsOnCooldown(playerObject))
            return;

        PlayerMovement playerMovement = playerObject.GetComponentInChildren<PlayerMovement>(true);

        if (playerMovement != null)
        {
            playerMovement.ResetMovementForRespawn(targetPoint.position, targetPoint.rotation);
        }
        else
        {
            playerObject.transform.SetPositionAndRotation(targetPoint.position, targetPoint.rotation);
        }

        NextAllowedTeleportTimeByPlayer[playerObject] = Time.time + teleportCooldownSeconds;

        if (logTeleportEvents)
        {
            Debug.Log(
                $"TrapperTeleporter: {playerObject.name} teleported to {targetPoint.name}",
                this
            );
        }
    }

    private bool IsOnCooldown(NetworkObject playerObject)
    {
        if (NextAllowedTeleportTimeByPlayer.TryGetValue(playerObject, out float nextAllowedTime))
        {
            return Time.time < nextAllowedTime;
        }

        return false;
    }

    private void OnValidate()
    {
        Collider col = GetComponent<Collider>();

        if (col != null)
            col.isTrigger = true;
    }
}