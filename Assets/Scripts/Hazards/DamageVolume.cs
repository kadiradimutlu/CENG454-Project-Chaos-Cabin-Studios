using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class DamageVolume : MonoBehaviour
{
    [SerializeField] private int damage = 25;
    [SerializeField] private bool instantElimination;

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
        if (!CanApplyDamage())
            return;

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (instantElimination)
            playerHealth.Eliminate();
        else
            playerHealth.TakeDamage(damage);
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
    }
}
