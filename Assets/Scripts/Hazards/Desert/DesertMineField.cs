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

    public override void Render()
    {
        bool shouldBeVisible = !IsHidden;
        if (shouldBeVisible != _lastAppliedVisible)
            ApplyVisibility(shouldBeVisible, force: false);
    }
     private void CacheRenderers()
    {
        Transform root = targetRoot != null ? targetRoot : transform;
        _renderers = root.GetComponentsInChildren<Renderer>(includeInactive);
    }

      private void ApplyVisibility(bool visible, bool force)
    {
        if (_renderers == null || _renderers.Length == 0)
            CacheRenderers();

        if (!force && visible == _lastAppliedVisible)
            return;

        if (_renderers != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer r = _renderers[i];
                if (r == null) continue;
                r.forceRenderingOff = !visible;
            }
        }

        _lastAppliedVisible = visible;
    }

    private int SecondsToTicks(float seconds)
    {
        if (Runner == null || Runner.DeltaTime <= 0f)
            return 1;
        return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0f, seconds) / Runner.DeltaTime));
    }
}