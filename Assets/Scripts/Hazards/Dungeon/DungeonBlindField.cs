using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class DungeonBlindField : NetworkBehaviour
{
    [Header("Zone")]
    [SerializeField] private ZoneType zoneType = ZoneType.Dungeon;

    [Header("Blind")]
    [Range(0.05f, 1f)]
    [SerializeField] private float blindStrength = 0.85f;

    [SerializeField] private float duration = 4f;

    [Header("Cooldown")]
    [SerializeField] private float cooldown = 8f;

    [Header("Feedback (optional, no animation)")]
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

    public override void Spawned() { }

    public void Activate()
    {
        if (Object == null) return;

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
        ServerActivate();
    }

    private void ServerActivate()
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsOnCooldown)
            return;

        ApplyBlindToZoneRunners();

        int durationTicks = Mathf.CeilToInt(Mathf.Max(0f, duration) / Runner.DeltaTime);
        int cooldownTicks = Mathf.CeilToInt(Mathf.Max(0f, cooldown) / Runner.DeltaTime);

        ActiveUntilTick = Runner.Tick + durationTicks;
        CooldownUntilTick = Runner.Tick + cooldownTicks;
    }

}