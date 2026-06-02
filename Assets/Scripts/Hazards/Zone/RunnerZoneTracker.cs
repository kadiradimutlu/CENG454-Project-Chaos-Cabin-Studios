using Fusion;
using UnityEngine;

/// <summary>
/// Lives on the Runner player prefab. Tracks which zone the runner is currently in.
/// </summary>
[DisallowMultipleComponent]
public class RunnerZoneTracker : NetworkBehaviour
{
    
    [Networked] public ZoneType CurrentZone { get; private set; }

    public bool IsInZone(ZoneType zone) => zone != ZoneType.None && CurrentZone == zone;

    
    public void EnterZone(ZoneType zone)
    {
        if (!Object.HasStateAuthority)
            return;

        CurrentZone = zone;
    }

    
    public void ExitZone(ZoneType zone)
    {
        if (!Object.HasStateAuthority)
            return;

        if (CurrentZone == zone)
            CurrentZone = ZoneType.None;
    }
}
