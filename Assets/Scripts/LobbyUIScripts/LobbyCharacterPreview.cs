using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(RawImage))]
public class LobbyCharacterPreview : MonoBehaviour
{
    private static int _nextPreviewId;

    [Header("UI")]
    [SerializeField] private RawImage targetImage;

    [Header("Render Texture")]
    [SerializeField] private int textureSize = 768;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0f);

    [Header("Preview Camera")]
    [SerializeField] private bool useOrthographicCamera = true;
    [SerializeField] private float orthographicPadding = 0.78f;
    [SerializeField] private float horizontalFitInfluence = 0.28f;
    [SerializeField] private float perspectiveFieldOfView = 24f;
    [SerializeField] private float perspectiveDistanceMultiplier = 0.95f;
    [SerializeField] private Vector3 cameraDirection = Vector3.forward;
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 0.15f, 0f);

    [Header("Preview Model")]
    [Tooltip("Use this only if the character appears backwards/sideways in the lobby preview.")]
    [SerializeField] private Vector3 previewModelEulerAngles = new Vector3(0f, 180f, 0f);

    [Tooltip("Preview-only scale multiplier. Does not affect gameplay player scale.")]
    [SerializeField] private float previewScaleMultiplier = 1.35f;

    [Header("Preview Animation")]
    [Tooltip("Optional. Assign Assets/Player_System/PlayerAnimator.controller here. In the editor this script also tries to auto-fill it.")]
    [SerializeField] private RuntimeAnimatorController previewAnimatorController;

    [Tooltip("Optional. Leave empty to use the controller default state. Example: Movement")]
    [SerializeField] private string previewStateName = "Movement";

    [SerializeField] private bool keepAnimatorEnabled = true;
    [SerializeField] private bool forceIdleParameters = true;

    [Header("Preview Pose Fallback")]
    [Tooltip("Used when no compatible preview animation is available. It bends arm bones downward so the character does not stay in a strong T-pose.")]
    [SerializeField] private bool applyFallbackPose = true;

    [SerializeField] private Vector3 leftUpperArmLocalEuler = new Vector3(0f, 0f, -58f);
    [SerializeField] private Vector3 rightUpperArmLocalEuler = new Vector3(0f, 0f, 58f);
    [SerializeField] private Vector3 leftForearmLocalEuler = new Vector3(0f, 0f, -18f);
    [SerializeField] private Vector3 rightForearmLocalEuler = new Vector3(0f, 0f, 18f);

    [Header("Preview Lighting")]
    [SerializeField] private bool createPreviewLight = true;
    [SerializeField] private Vector3 lightEulerAngles = new Vector3(45f, -25f, 0f);
    [SerializeField] private float lightIntensity = 2.4f;
    [SerializeField] private bool createFillLight = true;
    [SerializeField] private float fillLightIntensity = 0.85f;

    [Header("Preview Isolation")]
    [SerializeField] private bool isolateOnPreviewLayer = true;
    [Range(0, 31)]
    [SerializeField] private int previewLayer = 31;

    private int _previewId = -1;
    private int _currentSkinIndex = -999;
    private GameObject _currentVisualPrefab;

    private RenderTexture _renderTexture;
    private GameObject _runtimeRoot;
    private Transform _modelRoot;
    private Camera _previewCamera;
    private Light _previewLight;
    private Light _fillLight;
    private GameObject _spawnedVisual;

    private bool _hasVisibleCharacter;

#if UNITY_EDITOR
    private const string DefaultPreviewAnimatorPath = "Assets/Player_System/PlayerAnimator.controller";
#endif

    private void Reset()
    {
        targetImage = GetComponent<RawImage>();
        ApplyEditModeRawImageDefaults();
        TryAutoAssignPreviewAnimatorController();
    }

    private void OnValidate()
    {
        if (targetImage == null)
            targetImage = GetComponent<RawImage>();

        textureSize = Mathf.Clamp(textureSize, 128, 2048);
        orthographicPadding = Mathf.Max(0.1f, orthographicPadding);
        horizontalFitInfluence = Mathf.Clamp01(horizontalFitInfluence);
        perspectiveFieldOfView = Mathf.Clamp(perspectiveFieldOfView, 5f, 90f);
        perspectiveDistanceMultiplier = Mathf.Max(0.1f, perspectiveDistanceMultiplier);
        previewScaleMultiplier = Mathf.Max(0.01f, previewScaleMultiplier);
        lightIntensity = Mathf.Max(0f, lightIntensity);
        fillLightIntensity = Mathf.Max(0f, fillLightIntensity);

        TryAutoAssignPreviewAnimatorController();

        if (!Application.isPlaying)
            ApplyEditModeRawImageDefaults();
    }

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<RawImage>();

        if (_previewId < 0)
            _previewId = _nextPreviewId++;

        TryAutoAssignPreviewAnimatorController();
        ApplyEditModeRawImageDefaults();
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            EnsureRuntimeObjects();

            if (_currentSkinIndex >= 0)
                ShowCharacter(_currentSkinIndex);
        }
    }

    private void LateUpdate()
    {
        if (!_hasVisibleCharacter)
            return;

        PrepareTargetImage(true);
        UpdatePreviewAnimator();
        RenderPreviewOnce();
    }

    private void OnDisable()
    {
        // Keep runtime objects alive while the lobby is refreshing.
        // MainMenuManager may hide/show the RawImage many times per second.
    }

    private void OnDestroy()
    {
        CleanupRuntimeObjects();
    }

    public void ShowCharacter(int skinIndex)
    {
        EnsureRuntimeObjects();

        CharacterSkinDatabase database = CharacterSkinDatabase.Instance;

        if (database == null)
        {
            ClearPreview();
            return;
        }

        GameObject visualPrefab = database.GetVisualPrefab(skinIndex);

        if (visualPrefab == null)
        {
            ClearPreview();
            return;
        }

        if (_spawnedVisual != null &&
            _currentSkinIndex == skinIndex &&
            _currentVisualPrefab == visualPrefab)
        {
            _hasVisibleCharacter = true;
            PrepareTargetImage(true);
            UpdatePreviewAnimator();
            FitCameraToVisual(_spawnedVisual);
            RenderPreviewOnce();
            return;
        }

        DestroySpawnedVisual();

        _currentSkinIndex = skinIndex;
        _currentVisualPrefab = visualPrefab;

        GameObject previewPivot = new GameObject($"{visualPrefab.name}_PreviewPivot");
        previewPivot.hideFlags = HideFlags.DontSave;
        previewPivot.transform.SetParent(_modelRoot, false);
        previewPivot.transform.localPosition = Vector3.zero;
        previewPivot.transform.localRotation = Quaternion.Euler(previewModelEulerAngles);
        previewPivot.transform.localScale = Vector3.one * Mathf.Max(0.01f, previewScaleMultiplier);

        _spawnedVisual = Instantiate(visualPrefab, previewPivot.transform);
        _spawnedVisual.name = visualPrefab.name;
        _spawnedVisual.hideFlags = HideFlags.DontSave;

        Transform visualTransform = _spawnedVisual.transform;
        visualTransform.localPosition = database.GetVisualLocalPosition(skinIndex);
        visualTransform.localRotation = database.GetVisualLocalRotation(skinIndex);
        visualTransform.localScale = database.GetVisualLocalScale(skinIndex);

        PreparePreviewInstance(_spawnedVisual);
        UpdatePreviewAnimator();

        if (applyFallbackPose)
            ApplyPreviewPoseFallback(_spawnedVisual);

        FitCameraToVisual(_spawnedVisual);

        _hasVisibleCharacter = true;
        PrepareTargetImage(true);
        RenderPreviewOnce();
    }

    public void ClearPreview()
    {
        DestroySpawnedVisual();

        _currentSkinIndex = -999;
        _currentVisualPrefab = null;
        _hasVisibleCharacter = false;

        PrepareTargetImage(false);
    }

    public void SetPreviewVisible(bool visible)
    {
        if (visible)
        {
            EnsureRuntimeObjects();
            PrepareTargetImage(_hasVisibleCharacter);
        }
        else
        {
            PrepareTargetImage(false);
        }
    }

    private void ApplyEditModeRawImageDefaults()
    {
        if (targetImage == null)
            return;

        if (!Application.isPlaying)
        {
            targetImage.texture = null;
            targetImage.color = new Color(1f, 1f, 1f, 0f);
            targetImage.enabled = false;
            targetImage.raycastTarget = false;
        }
    }

    private void PrepareTargetImage(bool visible)
    {
        if (targetImage == null)
            targetImage = GetComponent<RawImage>();

        if (targetImage == null)
            return;

        if (visible && _renderTexture != null)
            targetImage.texture = _renderTexture;

        targetImage.enabled = visible;
        targetImage.raycastTarget = false;
        targetImage.color = visible ? Color.white : new Color(1f, 1f, 1f, 0f);
        targetImage.canvasRenderer.SetAlpha(visible ? 1f : 0f);
        targetImage.SetAllDirty();
    }

    private void EnsureRuntimeObjects()
    {
        if (_previewId < 0)
            _previewId = _nextPreviewId++;

        if (_runtimeRoot != null &&
            _modelRoot != null &&
            _previewCamera != null &&
            _renderTexture != null)
        {
            PrepareTargetImage(_hasVisibleCharacter);
            return;
        }

        CleanupRuntimeObjects();

        _renderTexture = new RenderTexture(textureSize, textureSize, 24, RenderTextureFormat.ARGB32)
        {
            name = $"{name}_CharacterPreview_RT",
            antiAliasing = 4,
            useMipMap = false,
            autoGenerateMips = false
        };
        _renderTexture.Create();

        Vector3 basePosition = new Vector3(10000f, 10000f, 10000f);
        Vector3 previewPosition = basePosition + new Vector3(_previewId * 25f, 0f, 0f);

        _runtimeRoot = new GameObject($"{name}_CharacterPreviewRuntime");
        _runtimeRoot.hideFlags = HideFlags.DontSave;
        _runtimeRoot.transform.position = previewPosition;

        GameObject modelRootObject = new GameObject("ModelRoot");
        modelRootObject.hideFlags = HideFlags.DontSave;
        modelRootObject.transform.SetParent(_runtimeRoot.transform, false);
        _modelRoot = modelRootObject.transform;

        GameObject cameraObject = new GameObject("PreviewCamera");
        cameraObject.hideFlags = HideFlags.DontSave;
        cameraObject.transform.SetParent(_runtimeRoot.transform, false);

        _previewCamera = cameraObject.AddComponent<Camera>();
        _previewCamera.enabled = true;
        _previewCamera.targetTexture = _renderTexture;
        _previewCamera.clearFlags = CameraClearFlags.SolidColor;
        _previewCamera.backgroundColor = backgroundColor;
        _previewCamera.nearClipPlane = 0.01f;
        _previewCamera.farClipPlane = 200f;
        _previewCamera.orthographic = useOrthographicCamera;
        _previewCamera.fieldOfView = perspectiveFieldOfView;
        _previewCamera.allowHDR = true;
        _previewCamera.allowMSAA = true;
        _previewCamera.cullingMask = isolateOnPreviewLayer ? (1 << previewLayer) : ~0;

        if (createPreviewLight)
        {
            GameObject lightObject = new GameObject("PreviewKeyLight");
            lightObject.hideFlags = HideFlags.DontSave;
            lightObject.transform.SetParent(_runtimeRoot.transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(lightEulerAngles);

            _previewLight = lightObject.AddComponent<Light>();
            _previewLight.type = LightType.Directional;
            _previewLight.intensity = lightIntensity;
        }

        if (createFillLight)
        {
            GameObject fillObject = new GameObject("PreviewFillLight");
            fillObject.hideFlags = HideFlags.DontSave;
            fillObject.transform.SetParent(_runtimeRoot.transform, false);
            fillObject.transform.localRotation = Quaternion.Euler(20f, 145f, 0f);

            _fillLight = fillObject.AddComponent<Light>();
            _fillLight.type = LightType.Directional;
            _fillLight.intensity = fillLightIntensity;
        }

        PrepareTargetImage(_hasVisibleCharacter);
    }

    private void PreparePreviewInstance(GameObject visual)
    {
        if (visual == null)
            return;

        if (isolateOnPreviewLayer)
            SetLayerRecursively(visual, previewLayer);

        Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider != null)
                collider.enabled = false;
        }

        SkinnedMeshRenderer[] skinnedMeshRenderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
        {
            if (skinnedMeshRenderer != null)
                skinnedMeshRenderer.updateWhenOffscreen = true;
        }

        Rigidbody[] rigidbodies = visual.GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rigidbody in rigidbodies)
        {
            if (rigidbody == null)
                continue;

            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }

        AudioSource[] audioSources = visual.GetComponentsInChildren<AudioSource>(true);
        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource == null)
                continue;

            audioSource.Stop();
            audioSource.enabled = false;
        }

        MonoBehaviour[] behaviours = visual.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour != null)
                behaviour.enabled = false;
        }

        PrepareAnimators(visual);
    }

    private void PrepareAnimators(GameObject visual)
    {
        Animator[] animators = visual.GetComponentsInChildren<Animator>(true);

        foreach (Animator animator in animators)
        {
            if (animator == null)
                continue;

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (previewAnimatorController != null)
                animator.runtimeAnimatorController = previewAnimatorController;

            animator.enabled = keepAnimatorEnabled && animator.runtimeAnimatorController != null;

            if (!animator.enabled)
                continue;

            if (forceIdleParameters)
            {
                SetAnimatorFloatIfExists(animator, "Speed", 0f);
                SetAnimatorFloatIfExists(animator, "Horizontal", 0f);
                SetAnimatorFloatIfExists(animator, "Vertical", 0f);
                SetAnimatorFloatIfExists(animator, "VerticalVelocity", 0f);
                SetAnimatorBoolIfExists(animator, "IsGrounded", true);
                SetAnimatorBoolIfExists(animator, "isGrounded", true);
                SetAnimatorBoolIfExists(animator, "isCrouching", false);
            }

            animator.Rebind();
            animator.Update(0f);

            if (!string.IsNullOrWhiteSpace(previewStateName) && HasAnimatorState(animator, previewStateName))
            {
                animator.Play(previewStateName, 0, 0f);
                animator.Update(0.08f);
            }
        }
    }

    private void UpdatePreviewAnimator()
    {
        if (_spawnedVisual == null)
            return;

        Animator[] animators = _spawnedVisual.GetComponentsInChildren<Animator>(true);

        foreach (Animator animator in animators)
        {
            if (animator == null || !animator.enabled)
                continue;

            if (forceIdleParameters)
            {
                SetAnimatorFloatIfExists(animator, "Speed", 0f);
                SetAnimatorFloatIfExists(animator, "Horizontal", 0f);
                SetAnimatorFloatIfExists(animator, "Vertical", 0f);
                SetAnimatorFloatIfExists(animator, "VerticalVelocity", 0f);
                SetAnimatorBoolIfExists(animator, "IsGrounded", true);
                SetAnimatorBoolIfExists(animator, "isGrounded", true);
                SetAnimatorBoolIfExists(animator, "isCrouching", false);
            }

            animator.Update(Time.unscaledDeltaTime);
        }
    }

    private void ApplyPreviewPoseFallback(GameObject visual)
    {
        if (visual == null)
            return;

        Animator[] animators = visual.GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            if (animator != null && animator.enabled && animator.runtimeAnimatorController != null)
            {
                // Animation is available, so do not fight it with manual bone rotations.
                return;
            }
        }

        Transform leftUpperArm = FindBone(visual.transform, HumanBodyBones.LeftUpperArm, "upperarm_l", "upper_arm_l", "leftupperarm", "arm_l");
        Transform rightUpperArm = FindBone(visual.transform, HumanBodyBones.RightUpperArm, "upperarm_r", "upper_arm_r", "rightupperarm", "arm_r");
        Transform leftForearm = FindBone(visual.transform, HumanBodyBones.LeftLowerArm, "forearm_l", "lowerarm_l", "leftforearm");
        Transform rightForearm = FindBone(visual.transform, HumanBodyBones.RightLowerArm, "forearm_r", "lowerarm_r", "rightforearm");

        ApplyLocalEulerIfFound(leftUpperArm, leftUpperArmLocalEuler);
        ApplyLocalEulerIfFound(rightUpperArm, rightUpperArmLocalEuler);
        ApplyLocalEulerIfFound(leftForearm, leftForearmLocalEuler);
        ApplyLocalEulerIfFound(rightForearm, rightForearmLocalEuler);
    }

    private Transform FindBone(Transform root, HumanBodyBones humanoidBone, params string[] nameTokens)
    {
        if (root == null)
            return null;

        Animator animator = root.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.avatar != null && animator.avatar.isHuman)
        {
            Transform humanoidTransform = animator.GetBoneTransform(humanoidBone);
            if (humanoidTransform != null)
                return humanoidTransform;
        }

        Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);

        foreach (string token in nameTokens)
        {
            foreach (Transform candidate in allTransforms)
            {
                if (candidate == null)
                    continue;

                string normalizedName = NormalizeName(candidate.name);
                string normalizedToken = NormalizeName(token);

                if (normalizedName.Contains(normalizedToken))
                    return candidate;
            }
        }

        return null;
    }

    private static string NormalizeName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("_", "")
            .Replace("-", "")
            .Replace(" ", "")
            .ToLowerInvariant();
    }

    private static void ApplyLocalEulerIfFound(Transform bone, Vector3 localEuler)
    {
        if (bone == null)
            return;

        bone.localRotation = bone.localRotation * Quaternion.Euler(localEuler);
    }

    private static bool HasAnimatorState(Animator animator, string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return false;

        int fullPathHash = Animator.StringToHash($"Base Layer.{stateName}");
        int shortNameHash = Animator.StringToHash(stateName);
        return animator.HasState(0, fullPathHash) || animator.HasState(0, shortNameHash);
    }

    private static void SetAnimatorFloatIfExists(Animator animator, string parameterName, float value)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == parameterName)
            {
                animator.SetFloat(parameterName, value);
                return;
            }
        }
    }

    private static void SetAnimatorBoolIfExists(Animator animator, string parameterName, bool value)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName)
            {
                animator.SetBool(parameterName, value);
                return;
            }
        }
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null)
            return;

        root.layer = layer;

        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void RenderPreviewOnce()
    {
        if (_previewCamera == null || _renderTexture == null)
            return;

        _previewCamera.backgroundColor = backgroundColor;
        _previewCamera.targetTexture = _renderTexture;
        _previewCamera.Render();
    }

    private void FitCameraToVisual(GameObject visual)
    {
        if (_previewCamera == null || visual == null)
            return;

        if (!TryCalculateRendererBounds(visual, out Bounds bounds))
            return;

        Vector3 target = bounds.center + lookAtOffset;
        Vector3 direction = cameraDirection.sqrMagnitude > 0.0001f
            ? cameraDirection.normalized
            : Vector3.forward;

        if (useOrthographicCamera)
        {
            _previewCamera.orthographic = true;

            float verticalSize = Mathf.Max(0.1f, bounds.extents.y * Mathf.Max(0.1f, orthographicPadding));
            float horizontalSafetySize = Mathf.Max(0.1f, bounds.extents.x * horizontalFitInfluence);
            _previewCamera.orthographicSize = Mathf.Max(verticalSize, horizontalSafetySize);

            float distance = Mathf.Max(4f, bounds.size.z * 3.5f, bounds.size.y * 2f);
            _previewCamera.transform.position = target + direction * distance;
            _previewCamera.transform.LookAt(target);
        }
        else
        {
            _previewCamera.orthographic = false;
            _previewCamera.fieldOfView = perspectiveFieldOfView;

            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float fovRadians = _previewCamera.fieldOfView * Mathf.Deg2Rad;
            float distance = (maxSize * perspectiveDistanceMultiplier) / (2f * Mathf.Tan(fovRadians * 0.5f));
            distance = Mathf.Max(2f, distance);

            _previewCamera.transform.position = target + direction * distance;
            _previewCamera.transform.LookAt(target);
        }
    }

    private bool TryCalculateRendererBounds(GameObject visual, out Bounds bounds)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);

        bounds = new Bounds(visual.transform.position, Vector3.one);
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void DestroySpawnedVisual()
    {
        if (_spawnedVisual != null)
        {
            Transform pivot = _spawnedVisual.transform.parent;
            Destroy(_spawnedVisual);

            if (pivot != null)
                Destroy(pivot.gameObject);
        }

        _spawnedVisual = null;
        _hasVisibleCharacter = false;
    }

    private void CleanupRuntimeObjects()
    {
        DestroySpawnedVisual();

        if (_runtimeRoot != null)
            Destroy(_runtimeRoot);

        _runtimeRoot = null;
        _modelRoot = null;
        _previewCamera = null;
        _previewLight = null;
        _fillLight = null;

        if (_renderTexture != null)
        {
            if (targetImage != null && targetImage.texture == _renderTexture)
                targetImage.texture = null;

            _renderTexture.Release();
            Destroy(_renderTexture);
        }

        _renderTexture = null;
    }

    private void TryAutoAssignPreviewAnimatorController()
    {
#if UNITY_EDITOR
        if (previewAnimatorController != null)
            return;

        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DefaultPreviewAnimatorPath);

        if (controller != null)
            previewAnimatorController = controller;
#endif
    }
}
