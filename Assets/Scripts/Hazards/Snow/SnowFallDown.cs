using Fusion;
using UnityEngine;

public class SnowFallDown : NetworkBehaviour
{
[SerializeField] private Transform targetRoot;
[SerializeField] private string demirTag = "Demir";
[SerializeField] private string massTag = "Mass";
[SerializeField] private float massDestroyDelay = 6f;

[Networked] private NetworkBool Triggered { get; set; }
[Networked] private NetworkBool DemirRemoved { get; set; }
[Networked] private NetworkBool MassRemoved { get; set; }
[Networked] private int MassRemoveAtTick { get; set; }

private GameObject demirObject;
private GameObject massObject;
private bool demirApplied;
private bool massApplied;


private void ResolveTargets()
{
    Transform root = targetRoot != null ? targetRoot : transform;
    if (demirObject == null) demirObject = FindByTagInChildren(root, demirTag);
    if (massObject == null) massObject = FindByTagInChildren(root, massTag);
}

public void Activate()
{
    if (Object.HasStateAuthority) ServerActivate();
    else RPC_RequestActivate();
}

public override void FixedUpdateNetwork()
{
    if (HasStateAuthority && Triggered && !MassRemoved && MassRemoveAtTick != 0)
    {
        if (Runner.Tick >= MassRemoveAtTick) MassRemoved = true;
    }
}

public override void Render()
{
    ResolveTargets();
    if (DemirRemoved && !demirApplied) { RemoveObject(ref demirObject); demirApplied = true; }
    if (MassRemoved && !massApplied) { RemoveObject(ref massObject); massApplied = true; }
}



private void RemoveObject(ref GameObject obj)
{
    if (obj == null) return;
    NetworkObject no = obj.GetComponent<NetworkObject>();
    if (no != null && Runner.IsServer) Runner.Despawn(no);
    else Destroy(obj);
    obj = null;
}

[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
private void RPC_RequestActivate() => ServerActivate();

private void ServerActivate()
{
    if (Triggered) return;
    Triggered = true;
    DemirRemoved = true;
    int delayTicks = Mathf.Max(1, Mathf.CeilToInt(massDestroyDelay / Runner.DeltaTime));
    MassRemoveAtTick = Runner.Tick + delayTicks;
}

private static GameObject FindByTagInChildren(Transform root, string tagName)
{
    if (root == null || string.IsNullOrWhiteSpace(tagName)) return null;
    foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
    {
        if (t.CompareTag(tagName)) return t.gameObject;
    }
    return null;
}
}