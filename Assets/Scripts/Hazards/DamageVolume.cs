using System.Collections.Generic;
using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class DamageVolume : MonoBehaviour
{
    [SerializeField] private int damage = 25;
    [SerializeField] private bool instantElimination;
    [SerializeField] private bool damageOnEnter = true;
    [SerializeField] private bool damageRepeatedly;
    [SerializeField] private float repeatInterval = 1f;
    [SerializeField] private bool damageOnCollision = true;
    [SerializeField] private float nonTriggerContactPadding = 0.12f;

    private readonly Dictionary<PlayerHealth, float> nextDamageTimes = new Dictionary<PlayerHealth, float>();
    private readonly HashSet<PlayerHealth> overlappingPlayers = new HashSet<PlayerHealth>();
    private readonly HashSet<PlayerHealth> currentOverlapBuffer = new HashSet<PlayerHealth>();
    private readonly List<PlayerHealth> removeBuffer = new List<PlayerHealth>();
    private NetworkRunner runner;
    private Collider damageCollider;
    private Collider[] overlapResults = new Collider[64];

    private void Awake()
    {
        damageCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (instantElimination || damageOnEnter)
            TryApplyDamage(other, true);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!instantElimination && damageRepeatedly)
            TryApplyDamage(other, false);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
            nextDamageTimes.Remove(playerHealth);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!damageOnCollision)
            return;

        if (instantElimination || damageOnEnter)
            TryApplyDamage(collision.collider, true);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!damageOnCollision)
            return;

        if (!instantElimination && damageRepeatedly)
            TryApplyDamage(collision.collider, false);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!damageOnCollision)
            return;

        PlayerHealth playerHealth = collision.collider.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            nextDamageTimes.Remove(playerHealth);
            overlappingPlayers.Remove(playerHealth);
        }
    }

    private void FixedUpdate()
    {
        if (!CanApplyDamage())
            return;

       
        if (damageCollider == null || damageCollider.isTrigger || !damageOnCollision)
            return;

        SampleOverlapsForNonTrigger();
    }

    private void SampleOverlapsForNonTrigger()
    {
        Bounds bounds = damageCollider.bounds;
        Vector3 extents = bounds.extents + Vector3.one * Mathf.Max(0f, nonTriggerContactPadding);

        int hitCount = Physics.OverlapBoxNonAlloc(
            bounds.center,
            extents,
            overlapResults,
            transform.rotation,
            ~0,
            QueryTriggerInteraction.Collide
        );

        currentOverlapBuffer.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapResults[i];

            if (hit == null)
                continue;

            PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();

            if (playerHealth == null)
                continue;

            if (!currentOverlapBuffer.Add(playerHealth))
                continue;

            bool isNewContact = !overlappingPlayers.Contains(playerHealth);

            if (instantElimination || damageOnEnter)
            {
                if (isNewContact)
                    TryApplyDamage(hit, true);
            }

            if (!instantElimination && damageRepeatedly && !isNewContact)
                TryApplyDamage(hit, false);
        }

        removeBuffer.Clear();
        foreach (PlayerHealth player in overlappingPlayers)
        {
            if (!currentOverlapBuffer.Contains(player))
                removeBuffer.Add(player);
        }

        for (int i = 0; i < removeBuffer.Count; i++)
        {
            PlayerHealth player = removeBuffer[i];
            overlappingPlayers.Remove(player);
            nextDamageTimes.Remove(player);
        }

        overlappingPlayers.Clear();
        foreach (PlayerHealth player in currentOverlapBuffer)
            overlappingPlayers.Add(player);
    }

    private void TryApplyDamage(Collider other, bool ignoreCooldown)
    {
        if (!CanApplyDamage())
            return;

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        RoleHandler roleHandler = other.GetComponentInParent<RoleHandler>();

        //bunu da mı eklemediniz  https://www.youtube.com/shorts/c6aypfcOz2E
        if (roleHandler == null || roleHandler.currentRole != RoleHandler.PlayerRole.Runner)
            return;

        if (!ignoreCooldown && !CanDamageNow(playerHealth))
            return;

        if (instantElimination)
            playerHealth.Eliminate();
        else
            playerHealth.TakeDamage(damage);

        nextDamageTimes[playerHealth] = Time.time + repeatInterval;
    }

    private bool CanDamageNow(PlayerHealth playerHealth)
    {
        if (!nextDamageTimes.TryGetValue(playerHealth, out float nextDamageTime))
            return true;

        return Time.time >= nextDamageTime;
    }

    private bool CanApplyDamage()
    {
        if (runner == null)
            runner = FindObjectOfType<NetworkRunner>();

        return runner == null || runner.IsServer;
    }

    private void OnValidate()
    {
        damage = Mathf.Max(0, damage);
        repeatInterval = Mathf.Max(0.05f, repeatInterval);
        nonTriggerContactPadding = Mathf.Max(0f, nonTriggerContactPadding);
    }
}
