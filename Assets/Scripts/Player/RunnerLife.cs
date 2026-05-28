using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerMovement))]
public class RunnerLife : NetworkBehaviour
{
    [SerializeField] private int startingDeathRights = 2;
    [SerializeField] private float respawnYOffset = 1.2f;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private RoleHandler roleHandler;

    [Networked] public int RemainingDeathRights { get; private set; }

    private Vector3 fallbackPosition;
    private Quaternion fallbackRotation;
    private bool hasFallback;
    private Vector3 checkpointPosition;
    private Quaternion checkpointRotation;
    private bool hasCheckpoint;
    public bool HasCheckpoint => hasCheckpoint;
    public bool IsRunnerPlayer => IsRunnerRole();

    private void Awake()
    {
        CacheReferences();
    }

    public override void Spawned()
    {
        CacheReferences();

        if (Object.HasStateAuthority)
        {
            RemainingDeathRights = Mathf.Max(0, startingDeathRights);
            SetFallbackRespawn(transform.position, transform.rotation);
        }
    }

    public void ResetForNewRound(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        hasCheckpoint = false;
        SetFallbackRespawn(spawnPosition, spawnRotation);

        if (Object.HasStateAuthority)
            RemainingDeathRights = Mathf.Max(0, startingDeathRights);
    }

    public void SaveCheckpoint(Vector3 position, Quaternion rotation)
    {
        if (!Object.HasStateAuthority)
            return;

        if (!IsRunnerRole())
            return;

        checkpointPosition = position;
        checkpointRotation = rotation;
        hasCheckpoint = true;
    }

    public bool TryRespawnAfterLethalDamage()
    {
        if (!Object.HasStateAuthority)
            return false;

        if (!IsRunnerRole())
            return false;

        if (RemainingDeathRights <= 0)
            return false;

        RemainingDeathRights--;

        GetRespawnPose(out Vector3 position, out Quaternion rotation);

        position.y += respawnYOffset;

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (playerMovement != null)
            playerMovement.ResetMovementForRespawn(position, rotation);
        else
            transform.SetPositionAndRotation(position, rotation);

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth != null)
            playerHealth.ResetHealth();

        return true;
    }

    private void GetRespawnPose(out Vector3 position, out Quaternion rotation)
    {
        if (hasCheckpoint)
        {
            position = checkpointPosition;
            rotation = checkpointRotation;
            return;
        }

        PlayerSpawner spawner = FindObjectOfType<PlayerSpawner>();

        if (spawner != null && spawner.TryGetSpawnPoseForPlayer(Object.InputAuthority, out position, out rotation))
            return;

        if (hasFallback)
        {
            position = fallbackPosition;
            rotation = fallbackRotation;
            return;
        }

        position = transform.position;
        rotation = transform.rotation;
    }

    private bool IsRunnerRole()
    {
        if (roleHandler == null)
            roleHandler = GetComponentInChildren<RoleHandler>(true);

        if (roleHandler == null)
            return false;

        if (!roleHandler.TryGetRole(out RoleHandler.PlayerRole role))
            return false;

        return role == RoleHandler.PlayerRole.Runner;
    }

    private void SetFallbackRespawn(Vector3 position, Quaternion rotation)
    {
        fallbackPosition = position;
        fallbackRotation = rotation;
        hasFallback = true;

        if (!hasCheckpoint)
        {
            checkpointPosition = position;
            checkpointRotation = rotation;
        }
    }

    private void CacheReferences()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (roleHandler == null)
            roleHandler = GetComponentInChildren<RoleHandler>(true);
    }

    private void OnValidate()
    {
        startingDeathRights = Mathf.Max(0, startingDeathRights);
        respawnYOffset = Mathf.Max(0f, respawnYOffset);
    }
}
