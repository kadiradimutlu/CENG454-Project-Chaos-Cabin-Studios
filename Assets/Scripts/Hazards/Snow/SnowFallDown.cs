using Fusion;
using UnityEngine;

public class SnowFallDown : NetworkBehaviour
{
   
    [SerializeField] private Transform targetRoot;
    [SerializeField] private string demirTag = "Demir";
    [SerializeField] private string massTag = "Mass";

    protected GameObject demirObject;
    protected GameObject massObject;

    [Networked] protected NetworkBool Triggered { get; set; }
    [Networked] protected NetworkBool DemirRemoved { get; set; }
    [Networked] protected NetworkBool MassRemoved { get; set; }

    public override void Spawned() => ResolveTargets();

    public void Activate()
    {
        if (Object == null) return; // Offline için sonraki commit
        if (Object.HasStateAuthority) ServerActivate();
        else RPC_RequestActivate();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestActivate() => ServerActivate();

    private void ServerActivate()
    {
        if (!Object.HasStateAuthority || Triggered) return;
        Triggered = true;
        DemirRemoved = true;
    }

    protected void ResolveTargets()
    {
        Transform root = targetRoot != null ? targetRoot : transform;
        if (demirObject == null) demirObject = FindByTagInChildren(root, demirTag);
        if (massObject == null) massObject = FindByTagInChildren(root, massTag);
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