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
}