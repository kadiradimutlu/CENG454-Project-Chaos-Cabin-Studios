using Fusion;
using UnityEngine;

public class SnowFallDown : NetworkBehaviour
{
   
    [SerializeField] private Transform targetRoot;
    [SerializeField] private string demirTag = "Demir";
    [SerializeField] private string massTag = "Mass";

    protected GameObject demirObject;
    protected GameObject massObject;

    public override void Spawned() => ResolveTargets();

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