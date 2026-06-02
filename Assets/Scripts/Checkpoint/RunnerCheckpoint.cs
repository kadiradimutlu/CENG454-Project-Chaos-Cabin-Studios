using System.Collections.Generic;
using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class RunnerCheckpoint : MonoBehaviour
{
    private struct CheckpointSaveData
    {
        public int Order;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    private static readonly Dictionary<RunnerLife, CheckpointSaveData> SavedCheckpoints =
        new Dictionary<RunnerLife, CheckpointSaveData>();

    [Header("Checkpoint")]
    [Tooltip("Checkpoint sırası. Base spawn 0 gibi düşün. Checkpointler: 1, 2, 3.")]
    [SerializeField, Min(1)] private int checkpointOrder = 1;

    [Tooltip("Oyuncu ölünce buraya döner. Boşsa checkpoint objesinin transform'u kullanılır.")]
    [SerializeField] private Transform respawnPoint;

    [SerializeField] private bool acceptOnlyRunners = true;

    [Header("Order Rules")]
    [Tooltip("Açıksa Checkpoint 2 için önce 1, Checkpoint 3 için önce 2 alınmış olmalı.")]
    [SerializeField] private bool requirePreviousCheckpoint = false;

    [Tooltip("Aynı checkpoint tekrar tetiklenirse pozisyonu güncellesin mi?")]
    [SerializeField] private bool saveSameCheckpointAgain = true;

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

        int currentOrder = GetSavedCheckpointOrder(runnerLife);

        if (requirePreviousCheckpoint && checkpointOrder > 1 && currentOrder < checkpointOrder - 1)
        {
            Debug.Log(
                $"RunnerCheckpoint: Checkpoint {checkpointOrder} kaydedilmedi. " +
                $"Önce Checkpoint {checkpointOrder - 1} alınmalı."
            );
            return;
        }

        // En yüksek checkpoint korunur.
        // Oyuncu Checkpoint 3 aldıysa sonra Checkpoint 2 trigger'ına girse bile 2'ye düşmez.
        if (checkpointOrder < currentOrder)
        {
            Debug.Log(
                $"RunnerCheckpoint: Daha düşük checkpoint görmezden gelindi. " +
                $"Current={currentOrder}, Incoming={checkpointOrder}"
            );
            return;
        }

        if (checkpointOrder == currentOrder && !saveSameCheckpointAgain)
            return;

        Vector3 position = respawnPoint != null ? respawnPoint.position : transform.position;
        Quaternion rotation = respawnPoint != null ? respawnPoint.rotation : transform.rotation;

        SaveCheckpointForRunner(runnerLife, checkpointOrder, position, rotation);
    }

    private bool CanSaveCheckpoint()
    {
        if (runner == null)
            runner = FindObjectOfType<NetworkRunner>();

        // Singleplayer testte runner yoksa çalışsın, multiplayerda sadece server kaydetsin.
        return runner == null || runner.IsServer;
    }

    private static void SaveCheckpointForRunner(
        RunnerLife runnerLife,
        int order,
        Vector3 position,
        Quaternion rotation)
    {
        if (runnerLife == null)
            return;

        SavedCheckpoints[runnerLife] = new CheckpointSaveData
        {
            Order = order,
            Position = position,
            Rotation = rotation
        };

        // Mevcut RunnerLife sistemin de aynı respawn noktasını bilsin.
        runnerLife.SaveCheckpoint(position, rotation);

        Debug.Log(
            $"RunnerCheckpoint: Checkpoint {order} kaydedildi. " +
            $"Position={position}"
        );
    }

    public static bool TryGetSavedCheckpoint(
        RunnerLife runnerLife,
        out Vector3 position,
        out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (runnerLife == null)
            return false;

        if (!SavedCheckpoints.TryGetValue(runnerLife, out CheckpointSaveData data))
            return false;

        position = data.Position;
        rotation = data.Rotation;
        return true;
    }

    public static int GetSavedCheckpointOrder(RunnerLife runnerLife)
    {
        if (runnerLife == null)
            return 0;

        if (!SavedCheckpoints.TryGetValue(runnerLife, out CheckpointSaveData data))
            return 0;

        return data.Order;
    }

    public static void ResetProgress(RunnerLife runnerLife)
    {
        if (runnerLife == null)
            return;

        SavedCheckpoints.Remove(runnerLife);
    }

    public static void ClearAllProgress()
    {
        SavedCheckpoints.Clear();
    }
}
