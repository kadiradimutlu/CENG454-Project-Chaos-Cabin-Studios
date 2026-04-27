using UnityEngine;

public class TrapDamage : MonoBehaviour
{
    public int damage = 20;
    public float knockbackForce = 8f;

    private void OnCollisionEnter(Collision collision)
    {
        PlayerController player = collision.collider.GetComponent<PlayerController>();

        if (player != null)
        {
            player.TakeDamage(damage);

            Rigidbody playerRb = collision.collider.GetComponent<Rigidbody>();

            if (playerRb != null)
            {
                Vector3 direction = collision.transform.position - transform.position;
                direction.y = 0.5f;

                playerRb.AddForce(direction.normalized * knockbackForce, ForceMode.Impulse);
            }
        }
    }
}