using Fusion;
using UnityEngine;

public class PlayerSkinApplier : NetworkBehaviour
{
    [Networked] public int SkinIndex { get; set; }

    [Header("Auto Apply Settings")]
    [SerializeField] private bool includeInactiveRenderers = true;

    [Tooltip("If empty, the script automatically finds child renderers.")]
    [SerializeField] private Renderer[] targetRenderers;

    private int _lastAppliedSkinIndex = -1;

    private void Awake()
    {
        CacheRenderersIfNeeded();
    }

    public override void Spawned()
    {
        CacheRenderersIfNeeded();
        ApplySkin();
    }

    public override void Render()
    {
        if (_lastAppliedSkinIndex != SkinIndex)
            ApplySkin();
    }

    private void CacheRenderersIfNeeded()
    {
        if (targetRenderers != null && targetRenderers.Length > 0)
            return;

        targetRenderers = GetComponentsInChildren<Renderer>(includeInactiveRenderers);
    }

    private void ApplySkin()
    {
        _lastAppliedSkinIndex = SkinIndex;

        CacheRenderersIfNeeded();

        CharacterSkinDatabase database = CharacterSkinDatabase.Instance;

        if (database == null)
            return;

        Material selectedMaterial = database.GetMaterial(SkinIndex);

        if (selectedMaterial == null)
            return;

        if (targetRenderers == null || targetRenderers.Length == 0)
            return;

        foreach (Renderer renderer in targetRenderers)
        {
            if (renderer == null)
                continue;

            renderer.material = selectedMaterial;
        }
    }
}