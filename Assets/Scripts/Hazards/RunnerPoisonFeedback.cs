using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
public class RunnerPoisonFeedback : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private RoleHandler roleHandler;

    [Header("Poison Audio")]
    [SerializeField] private AudioClip poisonClip;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private float poisonVolume = 1f;

    [Header("Poison Screen Side Effect")]
    [SerializeField] private Color poisonColor = new Color(0f, 1f, 0.15f, 0.55f);
    [SerializeField] private float sideWidthPercent = 0.32f;
    [SerializeField] private float fadeInTime = 0.08f;
    [SerializeField] private float holdTime = 0.25f;
    [SerializeField] private float fadeOutTime = 0.45f;

    private AudioSource poisonAudioSource;
    private Image leftOverlay;
    private Image rightOverlay;
    private Coroutine poisonRoutine;

    private void Awake()
    {
        CacheReferences();
        CreateAudioSourceIfNeeded();
    }

    public override void Spawned()
    {
        CacheReferences();
        CreateAudioSourceIfNeeded();

        // Sadece local player efekt görür ve sesi duyar.
        // Trapper'ın ekranı yeşillenmez, trapper bu sesi duymaz.
        if (!Object.HasInputAuthority)
            return;

        CreateSideOverlaysIfNeeded();

        if (playerHealth != null)
        {
            playerHealth.DamageTaken -= OnDamageTaken;
            playerHealth.DamageTaken += OnDamageTaken;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.DamageTaken -= OnDamageTaken;
    }

    private void OnDamageTaken(PlayerHealth health, int damageAmount, int currentHealth, DamageSourceType source)
    {
        if (source != DamageSourceType.Poison)
            return;

        if (!IsLocalRunner())
            return;

        PlayPoisonSound();
        StartPoisonScreenEffect();

        Debug.Log($"[RunnerPoisonFeedback] Poison yedim. Damage={damageAmount}, CurrentHealth={currentHealth}", this);
    }

    private bool IsLocalRunner()
    {
        if (Object == null || !Object.HasInputAuthority)
            return false;

        if (roleHandler == null)
            roleHandler = GetComponentInChildren<RoleHandler>(true);

        if (roleHandler == null)
            return true;

        return roleHandler.currentRole == RoleHandler.PlayerRole.Runner;
    }

    private void PlayPoisonSound()
    {
        if (poisonAudioSource == null)
            CreateAudioSourceIfNeeded();

        if (poisonAudioSource == null || poisonClip == null)
            return;

        poisonAudioSource.PlayOneShot(poisonClip, poisonVolume);
    }

    private void CreateAudioSourceIfNeeded()
    {
        if (poisonAudioSource != null)
            return;

        GameObject audioObject = new GameObject("LocalPoisonSFXAudioSource");
        audioObject.transform.SetParent(transform);
        audioObject.transform.localPosition = Vector3.zero;
        audioObject.transform.localRotation = Quaternion.identity;
        audioObject.transform.localScale = Vector3.one;

        poisonAudioSource = audioObject.AddComponent<AudioSource>();

        poisonAudioSource.playOnAwake = false;
        poisonAudioSource.loop = false;
        poisonAudioSource.spatialBlend = 0f;
        poisonAudioSource.volume = 1f;

        if (sfxMixerGroup != null)
            poisonAudioSource.outputAudioMixerGroup = sfxMixerGroup;
    }

    private void StartPoisonScreenEffect()
    {
        if (leftOverlay == null || rightOverlay == null)
            return;

        if (poisonRoutine != null)
            StopCoroutine(poisonRoutine);

        poisonRoutine = StartCoroutine(PoisonScreenRoutine());
    }

    private IEnumerator PoisonScreenRoutine()
    {
        yield return FadeSideOverlays(0f, poisonColor.a, fadeInTime);
        yield return new WaitForSeconds(holdTime);
        yield return FadeSideOverlays(poisonColor.a, 0f, fadeOutTime);

        poisonRoutine = null;
    }

    private IEnumerator FadeSideOverlays(float fromAlpha, float toAlpha, float duration)
    {
        duration = Mathf.Max(0.01f, duration);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);

            SetSideOverlayAlpha(alpha);

            yield return null;
        }

        SetSideOverlayAlpha(toAlpha);
    }

    private void SetSideOverlayAlpha(float alpha)
    {
        Color color = poisonColor;
        color.a = alpha;

        if (leftOverlay != null)
            leftOverlay.color = color;

        if (rightOverlay != null)
            rightOverlay.color = color;
    }

    private void CreateSideOverlaysIfNeeded()
    {
        if (leftOverlay != null && rightOverlay != null)
        {
            SetSideOverlayAlpha(0f);
            return;
        }

        GameObject canvasObject = new GameObject("LocalPoisonSideFeedbackCanvas");
        canvasObject.transform.SetParent(null);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        Sprite leftSprite = CreateSideGradientSprite(true);
        Sprite rightSprite = CreateSideGradientSprite(false);

        leftOverlay = CreateSideImage("PoisonLeftOverlay", canvasObject.transform, true, leftSprite);
        rightOverlay = CreateSideImage("PoisonRightOverlay", canvasObject.transform, false, rightSprite);

        SetSideOverlayAlpha(0f);
    }

    private Image CreateSideImage(string objectName, Transform parent, bool leftSide, Sprite sprite)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.raycastTarget = false;

        RectTransform rectTransform = image.rectTransform;

        float width = Mathf.Clamp01(sideWidthPercent);

        if (leftSide)
        {
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(width, 1f);
        }
        else
        {
            rectTransform.anchorMin = new Vector2(1f - width, 0f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
        }

        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        return image;
    }

    private Sprite CreateSideGradientSprite(bool leftSide)
    {
        int width = 256;
        int height = 8;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int x = 0; x < width; x++)
        {
            float t = x / (float)(width - 1);
            float alphaMultiplier = leftSide ? 1f - t : t;

            Color pixelColor = Color.white;
            pixelColor.a = alphaMultiplier;

            for (int y = 0; y < height; y++)
                texture.SetPixel(x, y, pixelColor);
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }

    private void CacheReferences()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (roleHandler == null)
            roleHandler = GetComponentInChildren<RoleHandler>(true);
    }

    private void OnValidate()
    {
        poisonVolume = Mathf.Clamp01(poisonVolume);
        sideWidthPercent = Mathf.Clamp(sideWidthPercent, 0.05f, 0.5f);
        fadeInTime = Mathf.Max(0.01f, fadeInTime);
        holdTime = Mathf.Max(0f, holdTime);
        fadeOutTime = Mathf.Max(0.01f, fadeOutTime);
    }
}
