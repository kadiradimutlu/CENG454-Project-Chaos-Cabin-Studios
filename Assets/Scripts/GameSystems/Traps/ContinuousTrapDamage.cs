using System.Collections.Generic;
using UnityEngine;

public class ContinuousTrapDamage : MonoBehaviour
{
    [SerializeField] private int damage = 5;
    [SerializeField] private float damageInterval = 1f;

    private readonly Dictionary<IDamageable, float> damageTimers = new Dictionary<IDamageable, float>();

    private void OnTriggerStay(Collider other)
    {
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            return;
        }

        if (!damageTimers.ContainsKey(damageable))
        {
            damageTimers.Add(damageable, 0f);
        }

        damageTimers[damageable] += Time.deltaTime;

        if (damageTimers[damageable] >= damageInterval)
        {
            damageable.TakeDamage(damage);
            damageTimers[damageable] = 0f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageTimers.Remove(damageable);
        }
    }
}
