using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class RunnerCheckpoint : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private bool acceptOnlyRunners = true;

    private NetworkRunner runner;
    private Collider checkpointCollider;

    private void Awake()
    {
        checkpointCollider = GetComponent<Collider>();

        if (checkpointCollider != null)
            checkpointCollider.isTrigger = true;

        if (respawnPoint == null)
            respawnPoint = transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!CanSaveCheckpoint())
            return;

        RunnerLife runnerLife = other.GetComponentInParent<RunnerLife>();

        if (runnerLife == null)
            return;

        if (acceptOnlyRunners && !runnerLife.IsRunnerPlayer)
            return;

        Vector3 position = respawnPoint != null ? respawnPoint.position : transform.position;
        Quaternion rotation = respawnPoint != null ? respawnPoint.rotation : transform.rotation;

        runnerLife.SaveCheckpoint(position, rotation);
    }

    private bool CanSaveCheckpoint()
    {
        if (runner == null)
            runner = FindObjectOfType<NetworkRunner>();

        return runner == null || runner.IsServer;
    }
}
