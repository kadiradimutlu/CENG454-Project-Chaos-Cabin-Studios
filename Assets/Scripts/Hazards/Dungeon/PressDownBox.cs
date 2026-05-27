using System.Collections.Generic;
using Fusion;
using UnityEngine;


[RequireComponent(typeof(Collider))]
public class PressDownBox : NetworkBehaviour
{
    [Header("Motion")]
    [SerializeField] private float dropDistance = 1.5f;

    
    [SerializeField] private float moveDuration = 0.4f;

   
    [SerializeField] private float stayDownDuration = 1f;

    [Header("Trigger Filter")]
    [SerializeField] private string requiredTag = "Player";

    [Networked] private NetworkBool HasTriggered { get; set; }
    [Networked] private int StartTick { get; set; }

    private Vector3 upPosition;
    private bool capturedUp;
    private Vector3 lastBoxPosition;

    private readonly HashSet<PlayerMovement> riders = new HashSet<PlayerMovement>();


    public override void Spawned()
    {
        upPosition = transform.position;
        lastBoxPosition = upPosition;
        capturedUp = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player != null)
            riders.Add(player);

        if (HasTriggered)
            return;

        HasTriggered = true;
        StartTick = Runner.Tick;
    }

    private void OnTriggerExit(Collider other)
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player != null)
            riders.Remove(player);
    }



    public override void FixedUpdateNetwork()
    {
        if (!capturedUp)
            return;

        Vector3 target = ComputeBoxPosition();
        Vector3 delta = target - lastBoxPosition;

        if (Object.HasStateAuthority && delta.sqrMagnitude > 0f)
        {
            foreach (PlayerMovement rider in riders)
            {
                if (rider != null)
                    rider.RidePlatform(delta);
            }
        }

        transform.position = target;
        lastBoxPosition = target;
    }

    public override void Render()
    {
        if (!capturedUp)
            return;

        transform.position = ComputeBoxPosition();
    }

    private Vector3 ComputeBoxPosition()
    {
        if (!HasTriggered || StartTick == 0)
            return upPosition;

        float elapsed = (Runner.Tick - StartTick) * Runner.DeltaTime;
        float down = Mathf.Max(0.0001f, moveDuration);
        float stay = Mathf.Max(0f, stayDownDuration);

        Vector3 downPosition = upPosition + Vector3.down * dropDistance;

        if (elapsed < down)
        {
            float t = elapsed / down;
            return Vector3.Lerp(upPosition, downPosition, Smooth(t));
        }

        if (elapsed < down + stay)
        {
            return downPosition;
        }

        if (elapsed < down + stay + down)
        {
            float t = (elapsed - down - stay) / down;
            return Vector3.Lerp(downPosition, upPosition, Smooth(t));
        }

        return upPosition;
    }

    private static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private void OnValidate()
    {
        dropDistance = Mathf.Max(0f, dropDistance);
        moveDuration = Mathf.Max(0.01f, moveDuration);
        stayDownDuration = Mathf.Max(0f, stayDownDuration);
    }
}
