using Fusion;
using UnityEngine;
 

[DisallowMultipleComponent]
public class DesertSlowField : NetworkBehaviour
{
    [Header("Zone")]
    [Tooltip("Which zone's runners this trap affects. Must match the map's ZoneVolume.")]
    [SerializeField] private ZoneType zoneType = ZoneType.Desert;
 
    [Header("Slow")]
    [Tooltip("Movement speed multiplier while slowed. 1 = no slow, 0.5 = half speed.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float slowMultiplier = 0.5f;
 
    [Tooltip("How long (seconds) each affected runner stays slowed.")]
    [SerializeField] private float duration = 4f;
 
    [Header("Cooldown")]
    [Tooltip("Seconds before the trap can be triggered again.")]
    [SerializeField] private float cooldown = 8f;
 
    [Header("Feedback (optional, no animation)")]
    [Tooltip("Objects enabled while the field is active (e.g. dust/sand particle, SFX source). Optional.")]
    [SerializeField] private GameObject[] activationVisuals;
 
    
    [Networked] private int ActiveUntilTick { get; set; }
    
    [Networked] private int CooldownUntilTick { get; set; }
 
    private bool lastActiveVisualState;
 
    
    public bool IsActive => ActiveUntilTick != 0 && Runner != null && Runner.Tick < ActiveUntilTick;
 
    
    public bool IsOnCooldown => CooldownUntilTick != 0 && Runner != null && Runner.Tick < CooldownUntilTick;
 
    
    public float CooldownRemaining
    {
        get
        {
            if (Runner == null || !IsOnCooldown)
                return 0f;
 
            return (CooldownUntilTick - Runner.Tick) * Runner.DeltaTime;
        }
    }
 
    
    public override void Spawned()
    {
        Debug.Log($"[SlowField] {name}: SPAWNED. Object null mu={Object==null}, HasStateAuthority={(Object!=null && Object.HasStateAuthority)}", this);
    }
 
    
    public void Activate()
    {
        Debug.Log($"[SlowField] {name}: Activate() çağrıldı. Object null mu={Object==null}, HasStateAuthority={(Object!=null && Object.HasStateAuthority)}", this);
 
        if (Object == null)
        {
            Debug.LogWarning($"[SlowField] {name}: Object NULL -> bu trap NetworkObject olarak spawn edilmemiş! Trap pack'i Fusion sahne objesi olmalı.", this);
            return;
        }
 
        if (Object.HasStateAuthority)
        {
            ServerActivate();
        }
        else
        {
            RPC_RequestActivate();
        }
    }
 
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestActivate(RpcInfo info = default)
    {
        Debug.Log($"[SlowField] {name}: RPC StateAuthority'de alındı.", this);
        ServerActivate();
    }
 
    private void ServerActivate()
    {
        if (!Object.HasStateAuthority)
            return;
 
        if (IsOnCooldown)
        {
            Debug.Log($"[SlowField] {name}: COOLDOWN'da, çıkıyorum.", this);
            return; // silently ignored
        }
 
        Debug.Log($"[SlowField] {name}: ServerActivate -> runner aranıyor.", this);
        ApplySlowToZoneRunners();
 
        int durationTicks = Mathf.CeilToInt(Mathf.Max(0f, duration) / Runner.DeltaTime);
        int cooldownTicks = Mathf.CeilToInt(Mathf.Max(0f, cooldown) / Runner.DeltaTime);
 
        ActiveUntilTick = Runner.Tick + durationTicks;
        CooldownUntilTick = Runner.Tick + cooldownTicks;
    }
 
    private void ApplySlowToZoneRunners()
    {
        // Find all runners and filter by zone. RunnerZoneTracker lives on the player root.
#if UNITY_2023_1_OR_NEWER
        RunnerZoneTracker[] trackers = FindObjectsByType<RunnerZoneTracker>(FindObjectsSortMode.None);
#else
        RunnerZoneTracker[] trackers = FindObjectsOfType<RunnerZoneTracker>();
#endif
 
        Debug.Log($"[SlowField] {name}: sahnede {trackers.Length} RunnerZoneTracker var. Aranan zone={zoneType}", this);
        int affected = 0;
        foreach (RunnerZoneTracker tracker in trackers)
        {
            if (tracker == null)
                continue;
 
            Debug.Log($"[SlowField]   - {tracker.name}: CurrentZone={tracker.CurrentZone} eşleşme={tracker.IsInZone(zoneType)}", tracker);
 
            // Zone filter: this is what keeps the effect from going global.
            if (!tracker.IsInZone(zoneType))
                continue;
 
            // Skip eliminated players.
            PlayerHealth health = tracker.GetComponent<PlayerHealth>();
            if (health == null)
                health = tracker.GetComponentInParent<PlayerHealth>();
            if (health != null && health.IsEliminated)
                continue;
 
            // Skip non-runners (e.g. the Trapper) if a RoleHandler is present.
            RoleHandler role = tracker.GetComponent<RoleHandler>();
            if (role == null)
                role = tracker.GetComponentInParent<RoleHandler>();
            if (role != null && role.currentRole != RoleHandler.PlayerRole.Runner)
                continue;
 
            PlayerMovement movement = tracker.GetComponent<PlayerMovement>();
            if (movement == null)
                movement = tracker.GetComponentInParent<PlayerMovement>();
            if (movement == null)
                continue;
 
            movement.ApplySlow(slowMultiplier, duration);
            affected++;
            Debug.Log($"[SlowField]   -> {tracker.name} YAVAŞLATILDI", tracker);
        }
        Debug.Log($"[SlowField] {name}: {affected} runner etkilendi.", this);
    }
 
    public override void Render()
    {
        // Toggle optional feedback objects based on active state (visual only, all peers).
        bool active = IsActive;
 
        if (active == lastActiveVisualState)
            return;
 
        lastActiveVisualState = active;
 
        if (activationVisuals == null)
            return;
 
        foreach (GameObject go in activationVisuals)
        {
            if (go != null)
                go.SetActive(active);
        }
    }
 
    private void OnValidate()
    {
        slowMultiplier = Mathf.Clamp(slowMultiplier, 0.05f, 1f);
        duration = Mathf.Max(0f, duration);
        cooldown = Mathf.Max(0f, cooldown);
    }
}
