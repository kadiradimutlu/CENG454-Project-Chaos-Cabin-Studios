using UnityEngine;

public class TrapDamage : MonoBehaviour
{
    [SerializeField] private int damage = 20;
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private float upwardKnockback = 0.5f;

    private void OnCollisionEnter(Collision collision)
    {
        IDamageable damageable = collision.collider.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            return;
        }

        damageable.TakeDamage(damage);
        ApplyKnockback(collision);
    }

    private void ApplyKnockback(Collision collision)
    {
        IKnockbackable knockbackable = collision.collider.GetComponentInParent<IKnockbackable>();
        if (knockbackable == null)
        {
            return;
        }

        Vector3 direction = collision.transform.position - transform.position;
        direction.y = upwardKnockback;

        knockbackable.ApplyKnockback(direction.normalized * knockbackForce);
    }
}
