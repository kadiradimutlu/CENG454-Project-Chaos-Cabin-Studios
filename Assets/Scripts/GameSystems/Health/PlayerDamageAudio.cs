using UnityEngine;

public class PlayerDamageAudio : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hurtClip;

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponentInParent<PlayerHealth>();
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.Damaged += OnDamaged;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Damaged -= OnDamaged;
        }
    }

    private void OnDamaged(int damageAmount)
    {
        if (audioSource != null && hurtClip != null)
        {
            audioSource.PlayOneShot(hurtClip);
        }
    }
}
