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