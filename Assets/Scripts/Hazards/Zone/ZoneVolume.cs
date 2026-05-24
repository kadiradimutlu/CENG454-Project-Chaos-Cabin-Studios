using Fusion;
using UnityEngine;


[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class ZoneVolume : MonoBehaviour
{
    [SerializeField] private ZoneType zoneType = ZoneType.None;

    private Collider zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();

        if (zoneCollider != null)
            zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        RunnerZoneTracker tracker = other.GetComponentInParent<RunnerZoneTracker>();

        if (tracker == null)
            return;

        
        if (tracker.Object == null || !tracker.Object.HasStateAuthority)
            return;

        tracker.EnterZone(zoneType);
    }

    private void OnTriggerExit(Collider other)
    {
        RunnerZoneTracker tracker = other.GetComponentInParent<RunnerZoneTracker>();

        if (tracker == null)
            return;

        if (tracker.Object == null || !tracker.Object.HasStateAuthority)
            return;

        tracker.ExitZone(zoneType);
    }

    public ZoneType ZoneType => zoneType;
}
