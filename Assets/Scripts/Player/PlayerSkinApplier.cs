using System.Reflection;
using Fusion;
using UnityEngine;

public class PlayerSkinApplier : NetworkBehaviour
{
    [Networked] public int SkinIndex { get; set; }

    [Header("Prefab Visual Settings")]
    [Tooltip("Optional. If empty, the selected visual prefab is spawned under this player root.")]
    [SerializeField] private Transform visualRoot;

    [Tooltip("Old/default character renderers are hidden when a prefab-based character visual is used.")]
    [SerializeField] private bool hideDefaultRenderersWhenUsingPrefab = true;

    [Tooltip("The selected modular character receives the animator controller from this animator if needed.")]
    [SerializeField] private Animator templateAnimator;

    [Header("Fallback Material Settings")]
    [SerializeField] private bool includeInactiveRenderers = true;

    [Tooltip("Used only if the selected character has no visual prefab. If empty, renderers are found automatically.")]
    [SerializeField] private Renderer[] targetRenderers;

    private GameObject _spawnedVisual;
    private Renderer[] _defaultRenderers;
    private int _lastAppliedSkinIndex = -999;

    private void Awake()
    {
        CacheDefaultsIfNeeded();
    }

    public override void Spawned()
    {
        CacheDefaultsIfNeeded();
        ApplySkin();
    }

    public override void Render()
    {
        if (_lastAppliedSkinIndex != SkinIndex)
            ApplySkin();
    }

    private void CacheDefaultsIfNeeded()
    {
        if (visualRoot == null)
            visualRoot = transform;

        if (templateAnimator == null)
            templateAnimator = GetComponentInChildren<Animator>(includeInactiveRenderers);

        if (_defaultRenderers == null || _defaultRenderers.Length == 0)
            _defaultRenderers = GetComponentsInChildren<Renderer>(includeInactiveRenderers);

        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = _defaultRenderers;
    }

    private void ApplySkin()
    {
        _lastAppliedSkinIndex = SkinIndex;
        CacheDefaultsIfNeeded();

        CharacterSkinDatabase database = CharacterSkinDatabase.Instance;

        if (database == null)
            return;

        GameObject visualPrefab = database.GetVisualPrefab(SkinIndex);

        if (visualPrefab != null)
        {
            ApplyVisualPrefab(database, visualPrefab);
            return;
        }

        ApplyFallbackMaterial(database);
    }

    private void ApplyVisualPrefab(CharacterSkinDatabase database, GameObject visualPrefab)
    {
        if (_spawnedVisual != null)
            Destroy(_spawnedVisual);

        if (hideDefaultRenderersWhenUsingPrefab)
            SetDefaultRenderersVisible(false);

        _spawnedVisual = Instantiate(visualPrefab, visualRoot);
        _spawnedVisual.name = visualPrefab.name;

        Transform visualTransform = _spawnedVisual.transform;
        visualTransform.localPosition = database.GetVisualLocalPosition(SkinIndex);
        visualTransform.localRotation = database.GetVisualLocalRotation(SkinIndex);
        visualTransform.localScale = database.GetVisualLocalScale(SkinIndex);

        Animator newAnimator = _spawnedVisual.GetComponentInChildren<Animator>(true);
        ConfigureAnimator(newAnimator);
        AssignAnimatorToPlayerSystems(newAnimator);
    }

    private void ConfigureAnimator(Animator newAnimator)
    {
        if (newAnimator == null || templateAnimator == null)
            return;

        if (newAnimator.runtimeAnimatorController == null && templateAnimator.runtimeAnimatorController != null)
            newAnimator.runtimeAnimatorController = templateAnimator.runtimeAnimatorController;

        if (newAnimator.avatar == null && templateAnimator.avatar != null)
            newAnimator.avatar = templateAnimator.avatar;

        newAnimator.applyRootMotion = templateAnimator.applyRootMotion;
        newAnimator.updateMode = templateAnimator.updateMode;
        newAnimator.cullingMode = templateAnimator.cullingMode;
    }

    private void AssignAnimatorToPlayerSystems(Animator newAnimator)
    {
        if (newAnimator == null)
            return;

        PlayerAnimation playerAnimation = GetComponent<PlayerAnimation>();
        if (playerAnimation != null)
            playerAnimation.SetAnimator(newAnimator);

        TryAssignAnimatorByReflection(GetComponent<NetworkMecanimAnimator>(), newAnimator);
    }

    private void TryAssignAnimatorByReflection(Component component, Animator newAnimator)
    {
        if (component == null || newAnimator == null)
            return;

        System.Type type = component.GetType();

        PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (PropertyInfo property in properties)
        {
            if (property.PropertyType != typeof(Animator) || !property.CanWrite)
                continue;

            try
            {
                property.SetValue(component, newAnimator);
                return;
            }
            catch
            {
                // Some Fusion internals may reject runtime assignment. Safe to ignore.
            }
        }

        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (FieldInfo field in fields)
        {
            if (field.FieldType != typeof(Animator))
                continue;

            try
            {
                field.SetValue(component, newAnimator);
                return;
            }
            catch
            {
                // Some Fusion internals may reject runtime assignment. Safe to ignore.
            }
        }
    }

    private void ApplyFallbackMaterial(CharacterSkinDatabase database)
    {
        SetDefaultRenderersVisible(true);

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

    private void SetDefaultRenderersVisible(bool visible)
    {
        if (_defaultRenderers == null)
            return;

        foreach (Renderer renderer in _defaultRenderers)
        {
            if (renderer == null)
                continue;

            renderer.enabled = visible;
        }
    }
}
