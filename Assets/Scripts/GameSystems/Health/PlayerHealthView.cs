using UnityEngine;

public class PlayerHealthView : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private HealthBar healthBar;

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
            playerHealth.HealthChanged += OnHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= OnHealthChanged;
        }
    }

    private void Start()
    {
        if (playerHealth != null && healthBar != null)
        {
            healthBar.SetHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth, maxHealth);
        }
    }
}
