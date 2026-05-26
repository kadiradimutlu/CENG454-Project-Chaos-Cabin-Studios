using Fusion;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerBlindEffect : NetworkBehaviour
{
    [Header("Overlay")]
    [SerializeField] private Image blindOverlay;

    [Tooltip("Seconds to fade the overlay in/out so it isn't a hard cut. Visual only.")]
    [SerializeField] private float fadeSpeed = 3f;

    [Networked] public float BlindStrength { get; private set; }
    [Networked] private int BlindExpiryTick { get; set; }

    private float currentAlpha;

    public bool IsBlinded =>
        BlindExpiryTick != 0 && Runner != null && Runner.Tick < BlindExpiryTick;


    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            BlindStrength = 0f;
            BlindExpiryTick = 0;
        }

        if (blindOverlay != null)
        {
            currentAlpha = 0f;
            SetOverlayAlpha(0f);

            if (!Object.HasInputAuthority)
                blindOverlay.gameObject.SetActive(false);
        }
    }

    public void ApplyBlind(float strength, float duration)
    {
        if (!HasStateAuthority)
            return;

        strength = Mathf.Clamp01(strength);
        duration = Mathf.Max(0f, duration);

        if (duration <= 0f)
            return;

        BlindStrength = (BlindExpiryTick != 0)
            ? Mathf.Max(BlindStrength, strength)
            : strength;

        int durationTicks = Mathf.CeilToInt(duration / Runner.DeltaTime);
        BlindExpiryTick = Runner.Tick + durationTicks;
    }

    public void ClearBlind()
    {
        if (!HasStateAuthority)
            return;

        BlindStrength = 0f;
        BlindExpiryTick = 0;
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority && BlindExpiryTick != 0 && Runner.Tick >= BlindExpiryTick)
        {
            BlindStrength = 0f;
            BlindExpiryTick = 0;
        }
    }

    public override void Render()
    {
        if (!Object.HasInputAuthority || blindOverlay == null)
            return;

        float targetAlpha = IsBlinded ? BlindStrength : 0f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
        SetOverlayAlpha(currentAlpha);
    }

    private void SetOverlayAlpha(float a)
    {
        if (blindOverlay == null)
            return;

        Color c = blindOverlay.color;
        c.a = a;
        blindOverlay.color = c;
    }
}