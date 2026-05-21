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

    private readonly Dictionary<PlayerHealth, float> nextDamageTimes = new Dictionary<PlayerHealth, float>();
    private NetworkRunner runner;
    private Collider damageCollider;

    private void Awake()
    {
        damageCollider = GetComponent<Collider>();

        if (damageCollider != null)
            damageCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (damageOnEnter)
            TryApplyDamage(other, true);
    }

    private void OnTriggerStay(Collider other)
    {
        if (damageRepeatedly)
            TryApplyDamage(other, false);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
            nextDamageTimes.Remove(playerHealth);
    }

    private void TryApplyDamage(Collider other, bool ignoreCooldown)
    {
        if (!CanApplyDamage())
            return;

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
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
    }
}
