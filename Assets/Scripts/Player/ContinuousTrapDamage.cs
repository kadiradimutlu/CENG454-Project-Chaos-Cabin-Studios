using UnityEngine;

public class ContinuousTrapDamage : MonoBehaviour
{
    public int damage = 5;
    public float damageInterval = 1f;
    private float timer = 0f;

    private void OnTriggerStay(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null)
        {
            return;
        }
        timer += Time.deltaTime;

        if (timer >= damageInterval)
        {
            player.TakeDamage(damage);
            timer = 0f;
        }

    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player != null)
        {
            timer = 0f;
        }

    }
}