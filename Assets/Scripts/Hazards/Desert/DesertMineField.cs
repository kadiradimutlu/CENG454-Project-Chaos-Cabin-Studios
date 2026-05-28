sing Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class DesertMinefield : NetworkBehaviour
{
   
    [SerializeField] private Transform targetRoot;
    [SerializeField] private bool includeInactive = true;

   
    [SerializeField] private float invisibleDuration = 10f;
    [SerializeField] private float cooldown = 0f;

   
    [Networked] private int HiddenUntilTick { get; set; }
    [Networked] private int CooldownUntilTick { get; set; }

  
    private Renderer[] _renderers;
    private bool _lastAppliedVisible = true;

    public bool IsHidden => Runner != null && HiddenUntilTick != 0 && Runner.Tick < HiddenUntilTick;
    public bool IsOnCooldown => Runner != null && CooldownUntilTick != 0 && Runner.Tick < CooldownUntilTick;
    public override void Spawned()
    {
        CacheRenderers();
        ApplyVisibility(visible: true, force: true);
    }

    public void Activate()
    {
        if (Object == null)
            return;

        if (Object.HasStateAuthority)
            ServerActivate();
        else
            RPC_RequestActivate();
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

        if (IsHidden || IsOnCooldown)
            return;

        HiddenUntilTick = Runner.Tick + SecondsToTicks(invisibleDuration);
        if (cooldown > 0f)
            CooldownUntilTick = Runner.Tick + SecondsToTicks(cooldown);
    }
}