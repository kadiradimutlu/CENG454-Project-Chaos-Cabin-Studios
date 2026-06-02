using System.Collections;
using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class IceSurfaceTrap : NetworkBehaviour
{
    [Header("Ice Trap Objects")]
    [SerializeField] private GameObject iceVisual;
    [SerializeField] private Collider iceTrigger;

    [Header("Timing")]
    [SerializeField] private float activeDuration = 25f;

    [Header("Sliding")]
    [SerializeField] private bool affectOnlyRunners = true;
    [SerializeField] private float iceAcceleration = 2.5f;
    [SerializeField] private float iceDeceleration = 0.35f;
    [SerializeField] private float iceTopSpeedMultiplier = 1.25f;
    [SerializeField] private float effectRefreshDuration = 0.25f;

    [Header("Debug")]
    [SerializeField] private bool logEvents = false;

    private Coroutine activeRoutine;
    private bool isActive;

    private void Awake()
    {
        SetIceActive(false);
    }

    public void ActivateIce()
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(ActivateForDuration());
    }

    private IEnumerator ActivateForDuration()
    {
        SetIceActive(true);

        yield return new WaitForSeconds(activeDuration);

        SetIceActive(false);
        activeRoutine = null;
    }

    private void SetIceActive(bool active)
    {
        isActive = active;

        if (iceVisual != null)
            iceVisual.SetActive(active);

        if (iceTrigger != null)
            iceTrigger.enabled = active;

        if (logEvents)
            Debug.Log($"IceSurfaceTrap active: {active}", this);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isActive)
            return;

        NetworkObject playerObject = other.GetComponentInParent<NetworkObject>();

        if (playerObject == null)
            return;

        if (!playerObject.HasInputAuthority && !playerObject.HasStateAuthority)
            return;

        RoleHandler roleHandler = playerObject.GetComponentInChildren<RoleHandler>(true);

        if (affectOnlyRunners)
        {
            if (roleHandler == null)
                return;

            if (!roleHandler.TryGetRole(out RoleHandler.PlayerRole role))
                return;

            if (role != RoleHandler.PlayerRole.Runner)
                return;
        }

        PlayerMovement playerMovement = playerObject.GetComponentInChildren<PlayerMovement>(true);

        if (playerMovement == null)
            return;

        playerMovement.ApplyTemporaryIceSurface(
            iceAcceleration,
            iceDeceleration,
            iceTopSpeedMultiplier,
            effectRefreshDuration
        );
    }

    private void OnValidate()
    {
        if (activeDuration < 1f)
            activeDuration = 1f;

        if (iceAcceleration < 0f)
            iceAcceleration = 0f;

        if (iceDeceleration < 0f)
            iceDeceleration = 0f;

        iceTopSpeedMultiplier = Mathf.Clamp(iceTopSpeedMultiplier, 0.2f, 3f);

        if (effectRefreshDuration < 0.1f)
            effectRefreshDuration = 0.1f;

        if (iceTrigger != null)
            iceTrigger.isTrigger = true;
    }
}