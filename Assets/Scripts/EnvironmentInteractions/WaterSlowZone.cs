using System.Collections.Generic;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterSlowZone : MonoBehaviour
{
    [Header("Slow Settings")]
    [SerializeField, Range(0.1f, 1f)]
    private float speedMultiplier = 0.55f;

    [Header("Audio")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip enterSplashClip;

    private readonly Dictionary<IMovementSpeedModifierReceiver, int> receiverEnterCounts = new();

    private string sourceId;

    private void Awake()
    {
        sourceId = $"{nameof(WaterSlowZone)}_{GetInstanceID()}";

        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        IMovementSpeedModifierReceiver receiver = other.GetComponentInParent<IMovementSpeedModifierReceiver>();

        if (receiver == null)
            return;

        if (!ShouldApplyModifier(receiver))
            return;

        if (receiverEnterCounts.TryGetValue(receiver, out int enterCount))
        {
            receiverEnterCounts[receiver] = enterCount + 1;
            return;
        }

        receiverEnterCounts.Add(receiver, 1);
        receiver.AddSpeedModifier(sourceId, speedMultiplier);

        if (ShouldPlayLocalAudio(receiver))
            PlayEnterSplashSound();
    }

    private void OnTriggerExit(Collider other)
    {
        IMovementSpeedModifierReceiver receiver = other.GetComponentInParent<IMovementSpeedModifierReceiver>();

        if (receiver == null)
            return;

        if (!ShouldApplyModifier(receiver))
            return;

        if (!receiverEnterCounts.TryGetValue(receiver, out int enterCount))
            return;

        enterCount--;

        if (enterCount <= 0)
        {
            receiverEnterCounts.Remove(receiver);
            receiver.RemoveSpeedModifier(sourceId);
            return;
        }

        receiverEnterCounts[receiver] = enterCount;
    }

    private bool ShouldApplyModifier(IMovementSpeedModifierReceiver receiver)
    {
        NetworkBehaviour networkBehaviour = receiver as NetworkBehaviour;

        if (networkBehaviour == null || networkBehaviour.Object == null)
            return true;

        return networkBehaviour.Object.HasStateAuthority || networkBehaviour.Object.HasInputAuthority;
    }

    private bool ShouldPlayLocalAudio(IMovementSpeedModifierReceiver receiver)
    {
        NetworkBehaviour networkBehaviour = receiver as NetworkBehaviour;

        if (networkBehaviour == null || networkBehaviour.Object == null)
            return true;

        return networkBehaviour.Object.HasInputAuthority;
    }

    private void PlayEnterSplashSound()
    {
        if (audioSource == null || enterSplashClip == null)
            return;

        audioSource.PlayOneShot(enterSplashClip);
    }
}